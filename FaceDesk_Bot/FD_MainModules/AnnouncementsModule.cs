using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using System.Linq;

namespace FaceDesk_Bot.FD_MainModules
{
  public class AnnouncementsModule : ModuleBase<SocketCommandContext>
  {
    [Command("setannouncements")]
    [Alias("setann")]
    [Summary("**Admin only**. Marks a channel for receiving FDB announcements.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task SetAnnouncementChannel([Summary("The channel to mark")] SocketChannel chan)
    {
      bool result = await AnnouncementsStorage.SetAnnouncementsFor(this.Context.Guild.Id, chan);
      if (result) await this.Context.Message.AddReactionAsync(new Emoji("👌"));
      else await this.Context.Message.AddReactionAsync(new Emoji("👎"));
    }

    [Command("unsetannouncements")]
    [Alias("unsetann")]
    [Summary("**Admin only**. Turns off FDB announcements.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task UnsetAnnouncementChannel()
    {
      bool result = await AnnouncementsStorage.SetAnnouncementsFor(this.Context.Guild.Id, null);
      if (result) await this.Context.Message.AddReactionAsync(new Emoji("👌"));
      else await this.Context.Message.AddReactionAsync(new Emoji("👎"));
    }

    [Command("announce")]
    [Alias("ann")]
    [Summary("**Owner only**. Fires an FDB announcement.")]
    public async Task Announce([Remainder] [Summary("The announcement to make")] string msg)
    {
      await AnnouncementsStorage.AnnounceGlobal(msg);
    }
  }

  public class AnnouncementsStorage
  {
    private static FirestoreDb _fs;

    public static CollectionReference Db => _fs.Collection("discord_bot");

    public static async Task<bool> AnnounceGlobal(string msg)
    {
      try
      {
        IReadOnlyCollection<SocketGuild> clientGuilds = EntryPoint.Client.Guilds;
        List<FieldPath> paths = new List<FieldPath>();
        foreach(var socketGuild in clientGuilds)
        {
          DocumentSnapshot dataSnapshot =
            await Db.Document(socketGuild.Id.ToString()).Collection("lite").Document("data").GetSnapshotAsync();
          bool success = dataSnapshot.TryGetValue<ulong>("announcements", out ulong annChan);
          if(success)
          {

            IEnumerable<SocketGuildChannel> matchedChan = socketGuild.Channels.Where(x => x.Id == annChan);
            if (matchedChan.Count() > 0)
            {
              IMessageChannel mchan = matchedChan.First() as IMessageChannel;
              if (mchan != null) await mchan.SendMessageAsync(msg);
            }
          }
        }

        return true;
      }
      catch(Exception e)
      {
        Console.WriteLine(e.Message);
        return false;
      }
    }

    public static async Task<bool> SetAnnouncementsFor(ulong guildID, SocketChannel channel)
    {
      try
      {
        Dictionary<string, object> update = new Dictionary<string, object>() {["announcements"] = channel?.Id};
        await Db.Document(Convert.ToString(guildID)).Collection("lite").Document("data").SetAsync(update, SetOptions.MergeAll);

        return true;
      }
      catch
      {
        return false;
      }
    }

    public static void Setup(FirestoreDb firestore)
    {
      _fs = firestore;
    }
  }
}
