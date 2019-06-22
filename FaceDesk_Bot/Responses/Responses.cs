using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

namespace FaceDesk_Bot.Responses
{
    class Responses
    {
      public static async Task HandleCommandAsync(SocketMessage messageParam)
      {
        var Client = EntryPoint.Client;
        var message = messageParam as SocketUserMessage;
        if (message == null) return;
        var context = new SocketCommandContext(Client, message);

        SocketGuildUser me = context.Guild.Users.Where(x => x.Id == Client.CurrentUser.Id).ToList()[0];

        RestApplication application = await context.Client.GetApplicationInfoAsync();


        if (EntryPoint.Lockdown && context.User.Id != application.Owner.Id)
        {
          await context.Channel.SendMessageAsync("We are on lockdown.");
          return;
        }

        if (messageParam.Content.Count(c => c == '┻') > 1)
        {
          await context.Channel.SendMessageAsync("┬─┬ ノ( ゜-゜ノ)" + "\n" + messageParam.Author.Mention + ", please respect the goddamn tables. 😠");
        }
    }
      
  }
}
