using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;

namespace FaceDesk_Bot.Permissions
{
  class GranularPermissionsModule : ModuleBase<SocketCommandContext>
  {
    [Command("channelmod")]
    [Summary("**Admin only**. Grants... I don't know yet.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task Channelmod(
      [Summary("The user to cmod")] [Remainder]
      SocketGuildUser user)
    {
      CollectionReference collection = GranularPermissions.Db;
      CollectionReference channelCollection = collection.Document(Convert.ToString(this.Context.Guild.Id)).Collection("channels");

      DocumentReference channelDoc = channelCollection.Document(Convert.ToString(this.Context.Channel.Id));
      DocumentSnapshot channelSettingsSnapshot = await channelDoc.GetSnapshotAsync();

      var update = new Dictionary<string, object>();

      if (channelSettingsSnapshot.Exists)
      {
        Dictionary<string, object> dict = channelSettingsSnapshot.ToDictionary();
        if (dict.ContainsKey("mods"))
        {
          List<ulong> mods = dict["mods"] as List<ulong>;
          mods.Add(user.Id);
          update["mods"] = mods;
        }
        else
        {
          List<ulong> mods = new List<ulong>{user.Id};
          update["mods"] = mods;
        }
      }
      else
      {
        List<ulong> mods = new List<ulong> { user.Id };
        update["mods"] = mods;
      }

      await channelDoc.SetAsync(update, SetOptions.MergeAll);

      await this.Context.Message.AddReactionAsync(new Emoji("🔫"));
    }
  }

  class GranularPermissions
  {
    private static FirestoreDb _fs;

    public static CollectionReference Db => _fs.Collection("discord_bot");

    public static void Setup(FirestoreDb firestore)
    {
      _fs = firestore;
    }
  }
}
