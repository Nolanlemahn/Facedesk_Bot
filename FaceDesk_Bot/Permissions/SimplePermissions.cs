using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;

namespace FaceDesk_Bot.Permissions
{
  class SimplePermissionsModule : ModuleBase<SocketCommandContext>
  {
    [Command("sp_reload")]
    [Summary("**Owner only**. Reloads permissions.")]
    public async Task SpReload()
    {
      Task<bool> result = this.Context.IsOwner();
      if (!result.Result) return;

      SimplePermissions.LoadOwners();

      await this.Context.Message.AddReactionAsync(new Emoji("🔫"));
    }
  }

  class SimplePermissions
  {
    public static List<ulong> Owners = new List<ulong>();

    //In owners.txt, newline-separate UIDs that should also act as the owner
    //see LocalExtensions.IsOwner
    public static void LoadOwners()
    {
      string[] owners =
        System.IO.File.ReadAllText(Path.Combine(EntryPoint.RunningFolder, "owners.txt")).Split('\n');
      foreach (string owner in owners)
      {
        ulong uid = Convert.ToUInt64(owner);
        Owners.Add(uid);
      }
    }
  }
}
