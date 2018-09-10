using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace FaceDesk_Bot.FD_MainModules
{
  class FunModule : ModuleBase<SocketCommandContext>
  {
    public static List<string> BallPositiveResponses = new List<string>()
    {
      "It is certain [+]",
      "It is decidedly so [+]",
      "Without a doubt [+]",
      "Yes definitely [+]",
      "You may rely on it [+]",
      "As I see it, yes [+]",
      "Most likely [+]",
      "Outlook good [+]",
      "Signs point to yes [+]",
      "Don't. Wait, you know what? Just go ahead [+]",
      "Yes, but only because I want to see what happens. [+]"
      };

    public static List<string> BallNegativeResponses = new List<string>()
      {
        "Don't count on it [-]",
        "My reply is no [-]",
        "My sources say no [-]",
        "Very doubtful [-]",
        "Don't even dream about it [-]",
        "Not in this lifetime [-]",
        "Impending disaster [-]",
        "Just don't [-]",
        "Don't hold your breath... Actually, you might as well [-]",
        "Pick a number between 1 and 7. You're wrong [-]",
      };

    //includes disasterous
    public static List<string> BallNeutralResponses = new List<string>()
    {
      "Reply hazy try again [?]",
      "Ask again later [?]",
      "Better not tell you now [?]",
      "Cannot predict now [?]",
      "Concentrate and ask again [?]",


      "Swear fucking loudly and ask again [?]",
      "No data. Ask Nolan [?]",
      "I'm busy. Ping an idiot [?]",
      "Depends on your luck [?]",
      "Consider a different path [?]",
      "How about you ask a different question [?]",
      "Ooh! Roll for initiative [?]",
      "Just kill it. With fire [!]",
      "You’re a disappointment for even asking [!]",
      "Shooka demands sacrifice [!]",

      "I really don't give a shit [...]",
      "I'm too busy thinking about that one time someone learned that you could buy multiple things at once [...]",
      "Shooka demands sac- nah, fuck off. [!]",
      "Shooka demands alcohol [!]",
      "Ask anyone else. I don't care who, just not me [!]",
      "Did you ask yourself that question before you asked me? I doubt it [?]",
    };

    public static List<List<string>> BallAllResponses = new List<List<string>>
    {
      BallPositiveResponses,
      BallNegativeResponses,
      BallNeutralResponses,
      BallNeutralResponses//I know, I don't care
    };

    private Random ballRandom = new Random();

    [Command("8ball")]
    [Summary("Shakes the 8ball.")]
    public async Task BallShake()
    {
      List<string> randList =
        // Get a random 8ball message in advance
        BallAllResponses[ballRandom.Next(BallAllResponses.Count)];

      string rand = randList[ballRandom.Next(randList.Count)];

      // Setup the initial message
      var message = await this.Context.Channel.SendMessageAsync("*shooka shooka...*");
      // Pretend to think
      await Task.Delay(1000);
      // Replace the initial message (edit) with the 8ball message
      await message.ModifyAsync(msg => msg.Content = rand);
    }

    [Command("gender")]
    [Summary("Prints the bots gender identity.")]
    public async Task Gender()
    {
      var message = await this.Context.Channel.SendMessageAsync("I am currently identifying as a gender-neutral 8-ball because *apparently* that's all I'm good for! 😢");
    }
  }
}
