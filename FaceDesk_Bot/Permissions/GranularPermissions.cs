using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;

namespace FaceDesk_Bot.Permissions
{
  class GranularPermissionsModule : ModuleBase<SocketCommandContext>
  {
    [Command("addtochannel")]
    [Alias("atc")]
    [Summary("**Channelmod only**. Adds target user (grants message read/send) to channel.")]
    public async Task AddToChannel(
      [Summary("The user to add")] SocketGuildUser user,
      [Summary("The channel to add to")] SocketGuildChannel channel)
    {
      List<ulong> mods = await GranularPermissions.GetChannelmodsFor(this.Context.Guild.Id, channel as ISocketMessageChannel);

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
      List<ulong> mods = await GranularPermissions.GetChannelmodsFor(this.Context.Guild.Id, channel as ISocketMessageChannel);

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
      List<ulong> mods = await GranularPermissions.GetChannelmodsFor(this.Context.Guild.Id, this.Context.Channel as ISocketMessageChannel);

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
      CollectionReference channelCollection = GranularPermissions.Db.Document(Convert.ToString(this.Context.Guild.Id)).Collection("channels");
      DocumentReference channelDoc = channelCollection.Document(Convert.ToString(this.Context.Channel.Id));

      Dictionary<string, object> update = new Dictionary<string, object>();

      List<ulong> mods = await GranularPermissions.GetChannelmodsFor(this.Context.Guild.Id, this.Context.Channel);
      if (!mods.Contains(user.Id))
      {
        mods.Add(user.Id);
        update["mods"] = mods;

        await channelDoc.SetAsync(update, SetOptions.MergeAll);
        await this.Context.Message.AddReactionAsync(new Emoji("👌"));
      }
      else
      {
        await this.Context.Channel.SendMessageAsync("You were already a cmod of that channel.");
      }
    }
  }

  class GranularPermissions
  {
    private static FirestoreDb _fs;

    public static CollectionReference Db => _fs.Collection("discord_bot");

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
            Console.Write(Convert.ToUInt64(id));
          }

          return casted;
        }
      }

      return new List<ulong>();
    }

    public static void Setup(FirestoreDb firestore)
    {
      _fs = firestore;
    }
  }
}
