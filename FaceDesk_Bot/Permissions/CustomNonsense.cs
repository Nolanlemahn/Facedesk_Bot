using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

namespace FaceDesk_Bot.FD_MainModules
{
  class CNModule : ModuleBase<SocketCommandContext>
  {
    [Command("zero_out")]
    [Alias("zero_out", "zo")]
    [Summary("**Admin only**. Zeros out a role.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireBotPermission(GuildPermission.Administrator)]
    public async Task ZeroOut(SocketRole role)
    {
      GuildPermissions noPerms = GuildPermissions.None;
      await role.ModifyAsync(x => x.Permissions = noPerms);

      IReadOnlyCollection<SocketGuildUser> allUsers = this.Context.Guild.Users;

      foreach (SocketGuildUser user in allUsers)
      {
        if (user.Roles.Contains(role))
        {
          await Context.Channel.SendMessageAsync(user.Username + "." + user.Discriminator + " is losing that role.");
          await user.RemoveRoleAsync(role);
        }
      }

      await Context.Message.AddReactionAsync(new Emoji("👌"));
    }

    [Command("orphaned")]
    [Alias("batman", "orph")]
    [Summary("Checks for orphaned roles.")]
    public async Task Orphaned()
    {
      IReadOnlyCollection<SocketRole> allRoles = Context.Guild.Roles;

      string newMsg = "";
      Task<RestUserMessage> startTask = Context.Channel.SendMessageAsync("Starting...");
      await startTask;
      foreach (SocketRole role in allRoles)
      {
        if (!role.Members.Any())
        {
          newMsg += (role.Mention + " has no children.\n");
          await startTask.Result?.ModifyAsync(msg => msg.Content = newMsg);
        }
      }

      await Context.Message.AddReactionAsync(new Emoji("👌"));
    }

    [Command("massassign")]
    [Alias("opera")]
    [Summary("**Admin only**. __Very slow__. Gives a role to absolutely everyone.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireBotPermission(GuildPermission.Administrator)]
    //headpat the nolan
    public async Task Opera(SocketRole role)
    {
      IReadOnlyCollection<SocketGuildUser> allUsers = this.Context.Guild.Users;

      foreach (SocketGuildUser user in allUsers)
      {
        await user.AddRoleAsync(role);
      }

      await Context.Message.AddReactionAsync(new Emoji("👌"));
    }
  }
}
