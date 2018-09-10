using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
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
      await Context.Message.AddReactionAsync(new Emoji("👌"));
    }
  }
}
