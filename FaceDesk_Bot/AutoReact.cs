using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace FaceDesk_Bot
{
  // I know, it couldn't be more hard-coded if I tried.
  class AutoReact
  {
    public static async Task AutoReactAsync(SocketMessage messageParam)
    {
      var message = messageParam as SocketUserMessage;
      if (message == null) return;
      var context = new SocketCommandContext(EntryPoint.Client, message);

      List<ulong> Plank = new List<ulong>()
      {
        89555451364003840
      };

      List<ulong> Chronos = new List<ulong>()
      {
      };

      List<ulong> Smilies = new List<ulong>()
      {
        162120089812860928,
        130591078469337088,
        338751193440452611
      };

      if (Plank.Contains(message.Author.Id)
         && context.Guild.Id == 372226412901564417)
      {
        GuildEmote seg = context.Guild.Emotes.FirstOrDefault(em => em.ToString() == "<:plank:393989115772796929>");
        await message.AddReactionAsync(seg, null);
      }

      if (Chronos.Contains(message.Author.Id)
        && context.Guild.Id == 372226412901564417)
      {
        GuildEmote seg = context.Guild.Emotes.FirstOrDefault(em => em.ToString() == "<:chronojail:392440043974819862>");
        await message.AddReactionAsync(seg, null);
      }

      if (Smilies.Contains(message.Author.Id)
         && context.Guild.Id == 372226412901564417)
      {
        await message.AddReactionAsync(new Emoji("🙂"));
      }
    }
  }
}
