using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace FaceDesk_Bot.Responses
{
  class Responses
  {
    public static async Task HandleUserJoinAsync(SocketGuildUser joiner)
    {
      var guild = joiner.Guild;

      bool authed = await Permissions.GranularPermissionsStorage.GetAuthStatusFor(guild.Id);

      if(authed)
      {
        List<ulong> roleIDs = await Permissions.GranularPermissionsStorage.GetDefaultRoles(guild.Id);
        List<SocketRole> rolesToAssign = new List<SocketRole>();
        if(roleIDs != null) foreach(ulong roleID in roleIDs)
        {
          IEnumerable<SocketRole> foundRoles = joiner.Guild.Roles.Where(x => roleIDs.Contains(x.Id));
          rolesToAssign.AddRange(foundRoles);
        }
        await joiner.AddRolesAsync(rolesToAssign);
      }
    }

    public static async Task HandleMessageAsync(SocketMessage messageParam)
    {
      var Client = EntryPoint.Client;
      var message = messageParam as SocketUserMessage;
      if (message == null) return;
      var context = new SocketCommandContext(Client, message);

      if (messageParam.Content.Count(c => c == '┻') > 1)
      {
        await context.Channel.SendMessageAsync("┬─┬ ノ( ゜-゜ノ)" + "\n" + messageParam.Author.Mention + ", please respect the goddamn tables. 😠");
      }
    }
  }
}
