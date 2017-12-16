using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Drawing;
using Discord.Rest;

namespace FaceDesk_Bot.FD_MainModules
{
  class LookupData
  {
    public static Dictionary<string, string> TimeAbs = new Dictionary<string, string>()
    {
      { "EST", "Eastern Standard Time" },
      { "PST", "Pacific Standard Time" },
      { "CST", "Central Standard Time" },
      { "CET", "Central European Standard Time" },
      { "AKST", "Alaskan Standard Time" }
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
      "Swear fucking loudly and ask again [?]",
      "No data. Ask Nolan [?]",
      "Just kill it. With fire [!]",
      "You’re a disappointment for even asking [!]"
    };
  }

  class UtilityModule : ModuleBase<SocketCommandContext>
  {
    [Command("medit")]
    [Summary("**Owner only**. Edits a message.")]
    public async Task Medit(ulong msgid, [Remainder] string newmsg)
    {
      var message = (RestUserMessage) await this.Context.Channel.GetMessageAsync(msgid);
      await message.ModifyAsync(msg => msg.Content = newmsg);
    }

    private Random ballRandom = new Random();
    [Command("8ball")]
    [Summary("Shakes the 8ball.")]
    public async Task BallShake()
    {
      string rand = LookupData.BallResponses[ballRandom.Next(LookupData.BallResponses.Count)];

      var message = await this.Context.Channel.SendMessageAsync("*shooka shooka...*");
      await Task.Delay(1000);
      await message.ModifyAsync(msg => msg.Content = rand);
    }

    [Command("timespit")]
    [Summary("Converts a time into other timezones.")]
    public async Task Timespit(
      [Summary("The time to convert")] string time,
      [Summary("The timezone of the original timezone")] string origzone,
      [Remainder] [Summary("Other timezones, separated by commas")] string zones)
    {
      try
      {
        DateTime result;
        if (!DateTime.TryParse(time, out result))
        {
          await this.Context.Channel.SendMessageAsync("???");
        }

        string[] aZones = zones.Split(',');
        List<TimeZoneInfo> tzis = new List<TimeZoneInfo>();
        TimeZoneInfo otzi = TimeZoneInfo.FindSystemTimeZoneById(LookupData.TimeAbs[origzone]);
        foreach (var zone in aZones)
        {
          tzis.Add(TimeZoneInfo.FindSystemTimeZoneById(LookupData.TimeAbs[zone]));
        }

        DateTime utcTime = TimeZoneInfo.ConvertTime(result, otzi, TimeZoneInfo.Utc);

        string msg = result.ToString("h:mm tt") + " in " + otzi.StandardName + " converts to...\n```";

        int i = 0;
        foreach (TimeZoneInfo tzi in tzis)
        {
          DateTime newTime = TimeZoneInfo.ConvertTime(utcTime, TimeZoneInfo.Utc, tzi);
          msg += ("- " + aZones[i] + ": " + newTime.ToString("h:mm tt") + " ");
          if ((newTime.Date - result.Date).Days >= 0.99)
          {
            msg += "[next day]";
          }
          if ((newTime.Date - result.Date).Days <= -0.99)
          {
            msg += "[previous day]";
          }
          msg += "\n";
          i++;
        }

        await this.Context.Channel.SendMessageAsync(msg + "```");
      }
      catch (Exception e)
      {
        await this.Context.Channel.SendMessageAsync("???");
      }
    }

    [Command("color")]
    [Summary("Gets a color out.")]
    public async Task Color([Remainder] [Summary("The color desired")] string input)
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
            int offset = y * 5 * 4 + x * 4;
            bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(red, green, blue));
          }
        }
        bmp.Save("Temp.png");
        await this.Context.Channel.SendFileAsync("Temp.png");
      }
      catch (Exception e)
      {
        await this.Context.Channel.SendMessageAsync("???");
      }
    }
  }
}
