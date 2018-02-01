using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Drawing;
using System.Linq;
using Discord.Rest;
using Discord.WebSocket;
using TimeZoneConverter;

namespace FaceDesk_Bot.FD_MainModules
{
  class LookupData
  {
    public static Dictionary<string, string> TimeAbs = new Dictionary<string, string>()
    {
      { "AKST", "Alaskan Standard Time" },
      { "CET", "Central European Standard Time" },
      { "CST", "Central Standard Time" },
      { "EST", "Eastern Standard Time" },
      { "GMT", "Greenwich Standard Time" },
      { "PST", "Pacific Standard Time" },
    };

    public static List<string> BallResponses = new List<string>()
    {
      "It is certain [+]",
      "It is decidedly so [+]",
      "Without a doubt [+]",
      "Yes definitely [+]",
      "You may rely on it [+]",
      "As I see it, yes [+]",
      "Most likely [+]",
      "Outlook good [+]",
      "Yes [+]",
      "Signs point to yes [+]",
      "Reply hazy try again [?]",
      "Ask again later [?]",
      "Better not tell you now [?]",
      "Cannot predict now [?]",
      "Concentrate and ask again [?]",
      "Don't count on it [-]",
      "My reply is no [-]",
      "My sources say no [-]",
      "Outlook not so good [-]",
      "Very doubtful [-]",

      "Don't. Wait, you know what? Just go ahead [+]",
      "Do it if you dare [+]",
      "Swear fucking loudly and ask again [?]",
      "No data. Ask Nolan [?]",
      "I'm busy. Ping a Hall Monitor [?]",
      "Depends on your luck [?]",
      "Consider a different path [?]",
      "How about you ask a different question [?]",
      "Ooh! Roll for initiative [?]",
      "Just kill it. With fire [!]",
      "You’re a disappointment for even asking [!]",
      "Shooka demands sacrifice [!]",
      "Don't even dream about it [-]",
      "Not in this lifetime [-]",
      "Impending disaster [-]",
      "Just don't [-]",
      "Don't hold your breath [-]",
    };
  }

  class UtilityModule : ModuleBase<SocketCommandContext>
  {
    [Command("allhelp")]
    [Summary("Prints everything this bot can do.")]
    public async Task AllHelp()
    {
      var ebh = new EmbedBuilder();
      ebh.WithTitle("FaceDesk_Bot Command Help");
      ebh.WithDescription("Here are all of the commands the bot is capable of doing.\n\n");

      List<CommandInfo> commandCopy = EntryPoint.MainCommandService.Commands.OrderBy(c => c.Name).ToList();
      foreach (var command in commandCopy)
      {
        string realSummary = "";
        int paramCounter = 1;
        foreach (var parameter in command.Parameters)
        {
          realSummary += ("param" + paramCounter + ": ");
          realSummary += (parameter.Summary + "\n");
          paramCounter++;
        }
        realSummary += "`";
        foreach (var alias in command.Aliases)
        {
          realSummary += "[!" + alias + "] ";
        }
        for (int i = 1; i < paramCounter; i++)
        {
          realSummary += "+ param" + i + " ";
        }
        realSummary += ("`\n*" + command.Summary + "*" + "\n\n");

        ebh.AddField(command.Name, realSummary);
      }

      await this.Context.DebugPublicReleasePrivate("", false, ebh);
    }

    [Command("medit")]
    [Summary("**Owner only**. Edits a message for which the bot is the author.")]
    public async Task Medit(
      [Summary("The message id")] ulong msgid,
      [Summary("The new message text")] [Remainder] string newmsg)
    {
      Task<bool> result = this.Context.IsOwner();
      if (!result.Result) return;

      if(this.Context.Guild.GetUser(EntryPoint.Client.CurrentUser.Id).GetPermissions(this.Context.Channel as IGuildChannel).ManageMessages)
        await this.Context.Message.DeleteAsync();

      var message = (RestUserMessage) await this.Context.Channel.GetMessageAsync(msgid);
      await message.ModifyAsync(msg => msg.Content = newmsg);
    }

    [Command("gg")]
    [Summary("**Owner only**. Kills the bot.")]
    public async Task Gg()
    {
      Task<bool> result = this.Context.IsOwner();
      if (!result.Result) return;
      await this.Context.Channel.SendMessageAsync("Bye! 👋");
      Environment.Exit(0);
    }

    [Command("cleanup")]
    [Summary("**Owner only**. Don't ask.")]
    public async Task Cleanup()
    {
      if (this.Context.IsOwner().Result)
      {
        List<SocketGuildUser> kickables = new List<SocketGuildUser>();
        foreach (SocketGuildUser user in this.Context.Guild.Users)
        {
          Console.WriteLine(user.Username + "#" + user.DiscriminatorValue +
            " has " + user.Roles.Count + " roles.");

          if (user.Roles.Count <= 1)
          {
            kickables.Add(user);
          }
        }

        foreach (SocketGuildUser kickable in kickables)
        {
          await kickable.KickAsync();
        }
      }
    }

    //--

    [Command("prune")]
    [Summary("Deletes a specified amount of messages in the channel.")]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public async Task Prune([Summary("Number of messages to delete.")] int delnum)
    {
      var items = await Context.Channel.GetMessagesAsync(delnum + 1).Flatten();
      await this.Context.Channel.DeleteMessagesAsync(items);
    }

    private Random ballRandom = new Random();
    [Command("8ball")]
    [Summary("Shakes the 8ball.")]
    public async Task BallShake()
    {
      string rand =
        // Get a random 8ball message in advance
        LookupData.BallResponses[ballRandom.Next(LookupData.BallResponses.Count)];

        // Setup the initial message
      var message = await this.Context.Channel.SendMessageAsync("*shooka shooka...*");
        // Pretend to think
      await Task.Delay(1000);
        // Replace the initial message (edit) with the 8ball message
      await message.ModifyAsync(msg => msg.Content = rand);
    }

    [Command("timespit")]
    [Summary("Converts a time into other timezones.")]
    public async Task Timespit(
      [Summary("The time to convert")] string time,
      [Summary("The timezone of the original timezone")] string origzone,
      [Remainder] [Summary("Other timezones, separated by commas")] string zones)
    {
      //try
      {
        DateTime result;
        if (!DateTime.TryParse(time, out result))
        {
          await this.Context.Channel.SendMessageAsync("???");
        }

        if (zones.Equals("REZ"))
        {
          zones = "";
          foreach (KeyValuePair<string, string> timePair in LookupData.TimeAbs)
          {
            zones += timePair.Key + ",";
          }
          zones = zones.Replace(origzone + ",", "");
          zones = zones.TrimEnd(',');
        }
        Console.WriteLine(zones);

        string[] aZones = zones.Split(',');
        List<TimeZoneInfo> tzis = new List<TimeZoneInfo>();
        TimeZoneInfo otzi = null;
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
          string stz = LocalExtensions.WindowsToIana(LookupData.TimeAbs[origzone]);
          otzi = TimeZoneInfo.FindSystemTimeZoneById(stz);
        }
        else
        {
          otzi = TimeZoneInfo.FindSystemTimeZoneById(LookupData.TimeAbs[origzone]);
        }

        foreach (var zone in aZones)
        {
          string tz = LookupData.TimeAbs[zone];
          if (Environment.OSVersion.Platform != PlatformID.Win32NT)
          {
            tz = LocalExtensions.WindowsToIana(tz);
            TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(tz);
            tzis.Add(tzi);
          }
          else
          {
            TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(tz);
            tzis.Add(tzi);
          }
        }

        DateTime utcTime = TimeZoneInfo.ConvertTime(result, otzi, TimeZoneInfo.Utc);

        var ebh = new EmbedBuilder();
        ebh.WithTitle(result.ToString("h:mm tt") + " in " + otzi.StandardName + " conversions\n");

        string msg = "";

        int i = 0;
        foreach (TimeZoneInfo tzi in tzis)
        {
          DateTime newTime = TimeZoneInfo.ConvertTime(utcTime, TimeZoneInfo.Utc, tzi);
          string head = "**" + aZones[i] + "** (" + LookupData.TimeAbs[aZones[i]] + ")";
          string body = "__" + newTime.ToString("h: mm tt") + "__";
  
          if ((newTime.Date - result.Date).Days >= 0.99)
          {
            body += " [next day]";
          }
          if ((newTime.Date - result.Date).Days <= -0.99)
          {
            body += " [previous day]";
          }
          body += "\n\n";
          i++;

          ebh.AddField(head, body);
        }

        await this.Context.Channel.SendMessageAsync("", false, ebh);
      }
    }

    [Command("color")]
    [Summary("Displays a color.")]
    public async Task Color([Remainder] [Summary("The color desired, as a hexcode")] string input)
    {
      try
      {
        int red = int.Parse(input.Substring(1, 2), NumberStyles.HexNumber);
        int green = int.Parse(input.Substring(3, 2), NumberStyles.HexNumber);
        int blue = int.Parse(input.Substring(5, 2), NumberStyles.HexNumber);
        Bitmap bmp = new Bitmap(25, 25);
        for (int y = 0; y < 25; ++y)
        {
          for (int x = 0; x < 25; ++x)
          {
            bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(red, green, blue));
          }
        }
        bmp.Save("Temp.png");
        await this.Context.Channel.SendFileAsync("Temp.png");
      }
      catch (Exception)
      {
        await this.Context.Channel.SendMessageAsync("???");
      }
    }
  }
}
