using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace FaceDesk_Bot.FD_MainModules
{
  class CNModule : ModuleBase<SocketCommandContext>
  {
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireBotPermission(GuildPermission.Administrator)]
    [Command("zero_out")]
    [Alias("zero_out", "zo")]
    [Summary("Zeros out a role.")]
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

      foreach (SocketRole role in allRoles)
      {
        if (!role.Members.Any())
        {
          await Context.Channel.SendMessageAsync(role.Mention + " has no children.");
        }
      }

      await Context.Message.AddReactionAsync(new Emoji("👌"));
    }
  }
}
