using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Google.Cloud.Firestore;

namespace FaceDesk_Bot.Permissions
{
  [FirestoreData]
  class ModassignableRole
  {
    [FirestoreProperty]
    public ulong role { get; set; }

    [FirestoreProperty]
    public ulong[] mods { get; set; }
  }

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
    private static OverwritePermissions BasePermissions = new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow);

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

      await channel.AddPermissionOverwriteAsync(user, BasePermissions);
      await this.Context.Message.AddReactionAsync(new Emoji("👌"));
    }

    private static OverwritePermissions RemovePermissions = new OverwritePermissions(viewChannel: PermValue.Deny, sendMessages: PermValue.Deny, manageMessages: PermValue.Allow);

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

      await channel.AddPermissionOverwriteAsync(user, RemovePermissions);
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

      if (this.Context.Channel is SocketGuildChannel sgc) await sgc.AddPermissionOverwriteAsync(user, RemovePermissions);
      await this.Context.Message.AddReactionAsync(new Emoji("👌"));
    }

    private static OverwritePermissions ModPermissions = new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow, manageMessages: PermValue.Allow);

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
        if (this.Context.Channel is SocketGuildChannel sgc) await sgc.AddPermissionOverwriteAsync(user, ModPermissions);
          
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

    #region modassign
    private async Task<bool> RoleToggle(IUser user, ulong role)
    {
      if (user != null)
      {
        RestGuildUser rgu = await Context.Client.Rest.GetGuildUserAsync(this.Context.Guild.Id, user.Id);
        SocketRole roleToAssign = this.Context.Guild.Roles.Where(x => x.Id == role).First();

        if (rgu.RoleIds.Contains(role))
        {
          await rgu.RemoveRoleAsync(roleToAssign);
        }
        else
        {
          await rgu.AddRoleAsync(roleToAssign);
        }

        return true;
      }
      return false;
    }

    private async Task<bool> Modtoggle(string code, IUser user)
    {
      Dictionary<string, ModassignableRole> massables = await GranularPermissionsStorage.GetModAssignable(this.Context.Guild.Id);

      if (massables.ContainsKey(code)) // valid code
      {
        ModassignableRole mrole = massables[code];
        if (mrole.mods != null && mrole.mods.Contains(this.Context.User.Id)) // invoker is mod
        {
          return await RoleToggle(user, mrole.role);
        }
      }

      return false;
    }

    [Command("modassign")]
    [Alias("massign")]
    [Summary("(Un)assign a role by code to a user. Invoker must be a moderator for that code")]
    public async Task Modassign([Summary("The code of the role")] string code, [Summary("The recipient of the role")] IUser user)
    {
      Console.WriteLine("massign entered");
      if(await Modtoggle(code, user))
      {
        await this.Context.Message.AddReactionAsync(new Emoji("👌"));
        return;
      }

      await this.Context.Message.AddReactionAsync(new Emoji("👎"));
    }

    [Command("modassignable")]
    [Alias("massable")]
    [Summary("**Admin only**. (Bot requires Manage Roles.) Mark a role as mod-assignable. If a role isn't specified, the role will be unmarked.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    public async Task MarkModassignable([Summary("The code to add the role as")] string code, [Summary("The role itself")] SocketRole role = null)
    {
      bool success = await GranularPermissionsStorage.SetModassignable(this.Context.Guild.Id, code, role);

      if (success) await this.Context.Message.AddReactionAsync(new Emoji("👌"));
      else await this.Context.Message.AddReactionAsync(new Emoji("👎"));
    }

    //TODO: these strings are terrible
    [Command("modassignablemod")]
    [Alias("massablemod")]
    [Summary("**Admin only**. (Bot requires Manage Roles.) Make a user a mod for a mod-assignable role.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    public async Task MakeModForModassignable([Summary("The role's code")] string code, [Summary("The user to promote")] IGuildUser user)
    {
      bool success = await GranularPermissionsStorage.ToggleModassignableMod(this.Context.Guild.Id, code, user.Id);

      if (success) await this.Context.Message.AddReactionAsync(new Emoji("👌"));
      else await this.Context.Message.AddReactionAsync(new Emoji("👎"));
    }
    #endregion

    #region selfassign

    [Command("selfassignable")]
    [Alias("sassable")]
    [Summary("**Admin only**. (Bot requires Manage Roles.) Mark a role as self-assignable. If a role isn't specified, the role will be unmarked.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    public async Task MarkSelfassignable([Summary("The code to add the role as")] string code, [Summary("The role itself")] SocketRole role = null)
    {
      bool success = await GranularPermissionsStorage.SetSelfassignable(this.Context.Guild.Id, code, role);

      if (success) await this.Context.Message.AddReactionAsync(new Emoji("👌"));
      else await this.Context.Message.AddReactionAsync(new Emoji("👎"));
    }

    [Command("selfassignme")]
    [Alias("sassme")]
    [Summary("**(Bot requires Manage Roles.) Attempt to self-assign a role by nothing.")]
    public async Task SelfassignMe()
    {
      // TODO: configurable, random
      await this.Context.Channel.SendMessageAsync($"{this.Context.Message.Author.Mention}...Your mother eats gym shorts.");
      return;
    }

    [Command("selfassignme")]
    [Alias("sassme")]
    [Summary("**(Bot requires Manage Roles.) Attempt to self-assign a role by code.")]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    public async Task SelfassignMe([Summary("The code")] string code)
    {
      Dictionary<string, ulong> sassables = await GranularPermissionsStorage.GetSelfassignable(this.Context.Guild.Id);

      if (sassables != null && sassables.ContainsKey(code))
      {
        ulong role = this.Context.Guild.Roles.Where(x => x.Id == sassables[code]).First().Id;

        if (await RoleToggle(this.Context.User, role))
        {
          await this.Context.Message.AddReactionAsync(new Emoji("👌"));
          return;
        }
        else
        {
          await this.Context.Message.AddReactionAsync(new Emoji("👎"));
        }
      }
      else
      {
        await this.Context.Message.AddReactionAsync(new Emoji("👎"));
      }
    }

    [Command("unselfassignme")]
    [Alias("unsassme")]
    [Summary("**(Bot requires Manage Roles.) Attempt to self-assign a role by code.")]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    public async Task UnselfassignMe([Summary("The code")] string code)
    {
      Dictionary<string, ulong> sassables = await GranularPermissionsStorage.GetSelfassignable(this.Context.Guild.Id);

      if (sassables != null && sassables.ContainsKey(code))
      {
        SocketGuildUser sgu = this.Context.User as SocketGuildUser;
        if (sgu != null)
        {
          IEnumerable<SocketRole> rolesToRemove = sgu.Guild.Roles.Where(x => x.Id == sassables[code]);
          await sgu.RemoveRolesAsync(rolesToRemove);
          await this.Context.Message.AddReactionAsync(new Emoji("👌"));
          return;
        }
        else
        {
          await this.Context.Message.AddReactionAsync(new Emoji("👎"));
        }
      }
      else
      {
        await this.Context.Message.AddReactionAsync(new Emoji("👎"));
      }
    }
    #endregion

    #region server
    [Command("defaultrole")]
    [Alias("drole")]
    [Summary("**Admin only**. (Bot requires Manage Roles.) Marks/unmarks a role to be assigned to all joining users.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    public async Task DefaultRole([Summary("The role to make default")] SocketRole role)
    {
      bool success = await GranularPermissionsStorage.SetDefaultRole(this.Context.Guild.Id, role);

      if (success) await this.Context.Message.AddReactionAsync(new Emoji("👌"));
      else await this.Context.Message.AddReactionAsync(new Emoji("👎"));
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

    #region defaultroles
    public static async Task<bool> SetDefaultRole(ulong guildID, SocketRole role)
    {
      try
      {
        DocumentReference guildDocument = Db.Document(Convert.ToString(guildID)).Collection("lite").Document("data");
        DocumentSnapshot guildSnapshot = await guildDocument.GetSnapshotAsync();
        guildSnapshot.TryGetValue("defaultRoles", out List<ulong> defaultRoles);

        if (defaultRoles == null) defaultRoles = new List<ulong>();
        if (!defaultRoles.Contains(role.Id)) defaultRoles.Add(role.Id);
        else defaultRoles.Remove(role.Id);

        Dictionary<string, List<ulong>> update = new Dictionary<string, List<ulong>> { ["defaultRoles"] = defaultRoles };
        WriteResult wrire = await guildDocument.SetAsync(update, SetOptions.MergeAll);

        return true;
      }
      catch
      {
        return false;
      }
    }

    public static async Task<List<ulong>> GetDefaultRoles(ulong guildID)
    {
      DocumentReference guildDocument = Db.Document(Convert.ToString(guildID)).Collection("lite").Document("data");
      DocumentSnapshot guildSnapshot = await guildDocument.GetSnapshotAsync();
      guildSnapshot.TryGetValue("defaultRoles", out List<ulong> defaultRoles);

      return defaultRoles;
    }
    #endregion

    #region selfassign
    public static async Task<bool> SetSelfassignable(ulong guildID, string code, SocketRole role)
    {
      try
      {
        DocumentReference guildDocument = Db.Document(Convert.ToString(guildID)).Collection("lite").Document("data");
        DocumentSnapshot guildSnapshot = await guildDocument.GetSnapshotAsync();
        guildSnapshot.TryGetValue("selfAssignable", out Dictionary<string, object> selfAssignables);

        if (selfAssignables == null) selfAssignables = new Dictionary<string, object>();

        if (selfAssignables.ContainsKey(code) && role == null)
        {
          selfAssignables[code] = null;
        }
        else selfAssignables[code] = role.Id;

        Dictionary<string, Dictionary<string, object>> update = new Dictionary<string, Dictionary<string, object>> { ["selfAssignable"] = selfAssignables };
        WriteResult wrire = await guildDocument.SetAsync(update, SetOptions.MergeAll);

        return true;
      }
      catch
      {
        return false;
      }
    }

    public static async Task<Dictionary<string, ulong>> GetSelfassignable(ulong guildID)
    {
      DocumentReference guildDocument = Db.Document(Convert.ToString(guildID)).Collection("lite").Document("data");
      DocumentSnapshot guildSnapshot = await guildDocument.GetSnapshotAsync();
      guildSnapshot.TryGetValue("selfAssignable", out Dictionary<string, object> selfAssignablesUncasted);

      Dictionary<string, ulong> selfAssignablesCasted = new Dictionary<string, ulong>();

      foreach(KeyValuePair<string, object> kvp in selfAssignablesUncasted)
      {
        if (kvp.Value == null) continue;

        ulong roleID = Convert.ToUInt64(kvp.Value);
        selfAssignablesCasted[kvp.Key] = roleID;
      }

      return selfAssignablesCasted;
    }
    #endregion

    #region modassign
    public static async Task<bool> SetModassignable(ulong guildID, string code, SocketRole role)
    {
      try
      {
        DocumentReference guildDocument = Db.Document(Convert.ToString(guildID)).Collection("lite").Document("data");
        DocumentSnapshot guildSnapshot = await guildDocument.GetSnapshotAsync();
        guildSnapshot.TryGetValue("modAssignable", out Dictionary<string, object> modAssignables);

        if (modAssignables == null) modAssignables = new Dictionary<string, object>();

        if (modAssignables.ContainsKey(code) && role == null)
        {
          modAssignables[code] = null;
        }
        else modAssignables[code] = new ModassignableRole { role = role.Id };

        Dictionary<string, Dictionary<string, object>> update = new Dictionary<string, Dictionary<string, object>> { ["modAssignable"] = modAssignables };
        WriteResult wrire = await guildDocument.SetAsync(update, SetOptions.MergeAll);

        return true;
      }
      catch(Exception e)
      {
        Console.WriteLine(e.Message);
        return false;
      }
    }

    public static async Task<bool> ToggleModassignableMod(ulong guildID, string code, ulong user)
    {
      try
      {
        Dictionary<string, ModassignableRole> modAssignables = await GetModAssignable(guildID);

        if(modAssignables.ContainsKey(code))
        {
          ModassignableRole role = modAssignables[code];
          if (role.mods == null)
          {
            role.mods = new ulong[] {user};
          }
          else if(!role.mods.Contains(user))
          {
            role.mods = role.mods.Append(user).ToArray();
          }
          else if(role.mods.Contains(user))
          {
            role.mods = role.mods.Where(x => x != user).ToArray();
          }
          modAssignables[code] = role;

          DocumentReference guildDocument = Db.Document(Convert.ToString(guildID)).Collection("lite").Document("data");
          Dictionary<string, Dictionary<string, ModassignableRole>> update = new Dictionary<string, Dictionary<string, ModassignableRole>> { ["modAssignable"] = modAssignables };
          WriteResult wrire = await guildDocument.SetAsync(update, SetOptions.MergeAll);

          return true;
        }

        return false;
      }
      catch
      {
        return false;
      }
    }

    public static async Task<Dictionary<string, ModassignableRole>> GetModAssignable(ulong guildID)
    {
      DocumentReference guildDocument = Db.Document(Convert.ToString(guildID)).Collection("lite").Document("data");
      DocumentSnapshot guildSnapshot = await guildDocument.GetSnapshotAsync();

      Dictionary<string, ModassignableRole> modAssignablesCasted = new Dictionary<string, ModassignableRole>();

      if (!guildSnapshot.TryGetValue("modAssignable", out modAssignablesCasted))
      {
        Console.WriteLine("GetModAssignable failed.");
      }

      return modAssignablesCasted;
    }
    #endregion

    public static void Setup(FirestoreDb firestore)
    {
      _fs = firestore;

      _guildAuthorized = new Dictionary<ulong, bool>();
      _guildLastChecked = new Dictionary<ulong, DateTime>();
    }
  }
}
