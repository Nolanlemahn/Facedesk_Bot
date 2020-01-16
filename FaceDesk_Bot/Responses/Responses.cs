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
