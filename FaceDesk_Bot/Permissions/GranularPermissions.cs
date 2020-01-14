using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;

namespace FaceDesk_Bot.Permissions
{
  class GranularPermissionsModule : ModuleBase<SocketCommandContext>
  {
    #region auth
    // Mostly for debug. Non-owners can't issue commands on an unauthed server.
    [Command("checkauth")]
    [Summary("**Owner only**. Checks authorization status.")]
    public async Task CheckAuth()
    {
      bool result = await this.Context.IsOwner(); if (!result) return;

      bool authed = await GranularPermissionsStorage.GetAuthStatusFor(this.Context.Guild.Id);

      if(authed) await this.Context.Message.AddReactionAsync(new Emoji("👌"));
      else await this.Context.Message.AddReactionAsync(new Emoji("👎"));
    }

    [Command("addkey")]
    [Summary("**Owner only**. Adds an authorization code.")]
    public async Task AddKey([Summary("The authorization code")] string code)
    {
      bool result = await this.Context.IsOwner(); if (!result) return;

      await GranularPermissionsStorage.AddAuthCode(code);
      await this.Context.Message.AddReactionAsync(new Emoji("👌"));
    }

    [Command("authorize")]
    [Alias("auth")]
    [Summary("Consumes an authorization code to use the bot with this server.")]
    public async Task Authorize([Summary("The authorization code")] string code)
    {
      await GranularPermissionsStorage.TryAuthFromContext(this.Context, code);
    }
    #endregion

    #region channelmod
    [Command("addtochannel")]
    [Alias("atc")]
    [Summary("**Channelmod only**. Adds target user (grants message read/send) to channel.")]
    public async Task AddToChannel(
      [Summary("The user to add")] SocketGuildUser user,
      [Summary("The channel to add to")] SocketGuildChannel channel)
    {
      List<ulong> mods = await GranularPermissionsStorage.GetChannelmodsFor(this.Context.Guild.Id, channel as ISocketMessageChannel);

      if (!mods.Contains(this.Context.User.Id))
      {
        await this.Context.Channel.SendMessageAsync("You're not a channelmod of this channel: **" + channel.Name + "**");
        return;
      }

      await channel.AddPermissionOverwriteAsync(user,
        new OverwritePermissions(readMessages: PermValue.Allow, sendMessages: PermValue.Allow));
      await this.Context.Message.AddReactionAsync(new Emoji("👌"));
    }

    [Command("removefromchannel")]
    [Alias("rfc")]
    [Summary("**Channelmod only**. Removes targets user (removes message read/send) from channel.")]
    public async Task RemoveFromChannel(
      [Summary("The user to remove")] SocketGuildUser user,
      [Summary("The channel to add to")] SocketGuildChannel channel)
    {
      List<ulong> mods = await GranularPermissionsStorage.GetChannelmodsFor(this.Context.Guild.Id, channel as ISocketMessageChannel);

      if (!mods.Contains(this.Context.User.Id))
      {
        await this.Context.Channel.SendMessageAsync("You're not a channelmod of this channel: **" + channel.Name + "**");
        return;
      }

      await channel.AddPermissionOverwriteAsync(user,
        new OverwritePermissions(readMessages: PermValue.Deny, sendMessages: PermValue.Deny));
      await this.Context.Message.AddReactionAsync(new Emoji("👌"));
    }

    [Command("removefromchannel")]
    [Alias("rfc")]
    [Summary("**Channelmod only**. Removes targets user (removes message read/send) from channel.")]
    public async Task RemoveFromChannel(
      [Summary("The user to remove")] SocketGuildUser user)
    {
      List<ulong> mods = await GranularPermissionsStorage.GetChannelmodsFor(this.Context.Guild.Id, this.Context.Channel);

      if (!mods.Contains(this.Context.User.Id))
      {
        await this.Context.Channel.SendMessageAsync("You're not a channelmod of this channel: **" + this.Context.Channel.Name + "**");
        return;
      }

      if(this.Context.Channel is SocketGuildChannel sgc) await sgc.AddPermissionOverwriteAsync(user,
        new OverwritePermissions(readMessages: PermValue.Deny, sendMessages: PermValue.Deny));
      await this.Context.Message.AddReactionAsync(new Emoji("👌"));
    }

    [Command("channelmod")]
    [Alias("cmod")]
    [Summary("**Admin only**. Allows target user to use the bot to manage the channel.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task Channelmod(
      [Summary("The user to cmod")] [Remainder] SocketGuildUser user)
    {
      CollectionReference channelCollection = GranularPermissionsStorage.Db.Document(Convert.ToString(this.Context.Guild.Id)).Collection("channels");
      DocumentReference channelDoc = channelCollection.Document(Convert.ToString(this.Context.Channel.Id));

      Dictionary<string, object> update = new Dictionary<string, object>();

      List<ulong> mods = await GranularPermissionsStorage.GetChannelmodsFor(this.Context.Guild.Id, this.Context.Channel);
      if (!mods.Contains(user.Id))
      {
        mods.Add(user.Id);
        update["mods"] = mods;

        await channelDoc.SetAsync(update, SetOptions.MergeAll);
        if (this.Context.Channel is SocketGuildChannel sgc) await sgc.AddPermissionOverwriteAsync(user,
          new OverwritePermissions(readMessages: PermValue.Allow, sendMessages: PermValue.Allow, manageMessages: PermValue.Allow));
        await this.Context.Message.AddReactionAsync(new Emoji("👌"));
      }
      else
      {
        mods.RemoveAll(x => x == user.Id);
        update["mods"] = mods;

        await channelDoc.SetAsync(update, SetOptions.MergeAll);
        await this.Context.Channel.SendMessageAsync("Removed a channelmod. (Their Discord permissions were not changed.)");
      }
    }
    #endregion
  }

  class GranularPermissionsStorage
  {
    [FirestoreData]
    public struct AuthorizationStatus
    {
      [FirestoreProperty]
      public ulong sid { get; set; }

      [FirestoreProperty]
      public bool used { get; set; }
    }

    private static FirestoreDb _fs;
    private static Dictionary<ulong, bool> _guildAuthorized;
    private static Dictionary<ulong, DateTime> _guildLastChecked;
    private static double _checkintInterval = 60;

    public static CollectionReference Db => _fs.Collection("discord_bot");

    #region auth
    public static async Task TryAuthFromContext(SocketCommandContext context, string code)
    {
      bool authed = await GranularPermissionsStorage.GetAuthStatusFor(context.Guild.Id);

      if (authed)
      {
        await context.Message.AddReactionAsync(new Emoji("❓"));
        await context.Channel.SendMessageAsync("This server is already authorized.");
      }
      else
      {
        bool codeConsumed = await GranularPermissionsStorage.TryConsumeAuthCode(context.Guild.Id, code);

        if (codeConsumed) await context.Message.AddReactionAsync(new Emoji("👌"));
        else
        {
          await context.Message.AddReactionAsync(new Emoji("👎"));
          await context.Channel.SendMessageAsync("That code was already used or didn't exist.");
        }
      }
    }

    public static async Task<bool> GetAuthStatusFor(ulong guildID)
    {
      // if successful auth cached, do not recheck database
      if (_guildAuthorized.GetValueOrDefault(guildID)) return true;

      // if it's been less than X seconds and failed auth cached, do not recheck database
      if (_guildLastChecked.ContainsKey(guildID) &&
        (DateTime.UtcNow - _guildLastChecked[guildID]).Duration() < TimeSpan.FromSeconds(_checkintInterval))
      {
        return false;
      }

      DocumentReference guildDocument = Db.Document(Convert.ToString(guildID)).Collection("lite").Document("data");
      DocumentSnapshot guildDocumentSnapshot = await guildDocument.GetSnapshotAsync();

      if (guildDocumentSnapshot.Exists)
      {
        bool authorizeFetched;
        bool isAuthorized = guildDocumentSnapshot.TryGetValue("authorized", out authorizeFetched);

        _guildAuthorized[guildID] = authorizeFetched && isAuthorized;
        _guildLastChecked[guildID] = DateTime.UtcNow;
        return authorizeFetched && isAuthorized;
      }

      return false;
    }

    public static async Task SetAuthFor(ulong guildID, bool status)
    {
      DocumentReference guildCollection = Db.Document(Convert.ToString(guildID)).Collection("lite").Document("data");

      Dictionary<string, object> update = new Dictionary<string, object>
      {
        ["authorized"] = status
      };

      await guildCollection.SetAsync(update, SetOptions.MergeAll);
    }

    public static async Task AddAuthCode(string code)
    {
      //TODO: should probably not allow key clobbering
      DocumentReference codeDataRef = Db.Document("_authorizeKeys").Collection(code).Document("data");
      await codeDataRef.SetAsync(new AuthorizationStatus{used = false, sid = 0});
    }

    public static async Task<bool> TryConsumeAuthCode(ulong guildID, string code)
    {
      CollectionReference authDocument = Db.Document("_authorizeKeys").Collection(code);
      QuerySnapshot authDocumentSnapshot = await authDocument.GetSnapshotAsync();

      if (authDocumentSnapshot.Documents.Count > 0)
      {
        DocumentSnapshot authDoc = authDocumentSnapshot.Documents[0];
        DocumentReference authRef = authDoc.Reference;

        AuthorizationStatus authStatus = authDoc.ConvertTo<AuthorizationStatus>();
        if (authStatus.used) return false;
        else
        {
          authStatus.used = true;
          authStatus.sid = guildID;
          await authRef.SetAsync(authStatus);
          await SetAuthFor(guildID, true);
          return true;
        }
      }
      return false;
    }
    #endregion

    public static async Task<List<ulong>> GetChannelmodsFor(ulong guildID, ISocketMessageChannel channel)
    {
      CollectionReference channelCollection = Db.Document(Convert.ToString(guildID)).Collection("channels");

      DocumentReference channelDoc = channelCollection.Document(Convert.ToString(channel.Id));
      DocumentSnapshot channelSettingsSnapshot = await channelDoc.GetSnapshotAsync();

      if (channelSettingsSnapshot.Exists)
      {
        Dictionary<string, object> dict = channelSettingsSnapshot.ToDictionary();

        if (dict.ContainsKey("mods"))
        {
          List<object> mods = dict["mods"] as List<object>;

          List<ulong> casted = new List<ulong>();
          foreach (object id in mods)
          {
            casted.Add(Convert.ToUInt64(id));
          }

          return casted;
        }
      }

      return new List<ulong>();
    }

    public static void Setup(FirestoreDb firestore)
    {
      _fs = firestore;

      _guildAuthorized = new Dictionary<ulong, bool>();
      _guildLastChecked = new Dictionary<ulong, DateTime>();
    }
  }
}
