using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace FaceDesk_Bot.FD_MainModules
{
  class FunModule : ModuleBase<SocketCommandContext>
  {
    public static List<string> BallPositiveResponses;
    public static List<string> BallNegativeResponses;
    //includes disasterous
    public static List<string> BallNeutralResponses;

    public static List<List<string>> BallAllResponses;

    private Random ballRandom = new Random();

    [Command("8ball")]
    [Summary("Shakes the 8ball.")]
    public async Task BallShake()
    {
      string eightballPath = Path.Combine(EntryPoint.RunningFolder, "8ball");
      string rawPosResponses = File.ReadAllText(Path.Combine(eightballPath, "positive.txt"));
      string rawNegResponses = File.ReadAllText(Path.Combine(eightballPath, "negative.txt"));
      string rawNeutResponses = File.ReadAllText(Path.Combine(eightballPath, "neutral.txt"));

      BallPositiveResponses = rawPosResponses.Split('\n').ToList();
      BallNegativeResponses = rawNegResponses.Split('\n').ToList();
      BallNeutralResponses = rawNeutResponses.Split('\n').ToList();
      BallAllResponses = new List<List<string>>
      {
        BallPositiveResponses,
        BallNegativeResponses,
        BallNeutralResponses,
        BallNeutralResponses//I know, I don't care
      };

      List<string> randList =
        // Get a random 8ball message in advance
        BallAllResponses[ballRandom.Next(BallAllResponses.Count)];

      string rand = randList[ballRandom.Next(randList.Count)];

      string shakingMessage = "*shooka shooka...*";
      int editDelay = 1000;
      if (ballRandom.Next(10) > 8)
      {
        shakingMessage += " **KA-KRASH?!**";
        editDelay += 1500;
      }

      // Setup the initial message
      var message = await this.Context.Channel.SendMessageAsync(shakingMessage);
      // Pretend to think
      await Task.Delay(editDelay);
      // Replace the initial message (edit) with the 8ball message
      await message.ModifyAsync(msg => msg.Content = rand);
    }

    [Command("gender")]
    [Summary("Prints the bots gender identity.")]
    public async Task Gender()
    {
      var message = await this.Context.Channel.SendMessageAsync("🎶 _I'm a **bitch**, I'm a **lover**_ 🎶");
    }
  }
}
