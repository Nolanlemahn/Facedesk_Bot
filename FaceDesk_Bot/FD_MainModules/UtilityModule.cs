using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;


using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

using FaceDesk_Bot.Permissions;
using Image = Discord.Image;

namespace FaceDesk_Bot.FD_MainModules
{
  class LookupData
  {
    public static void Init()
    {
      byte[] bytes = Encoding.BigEndianUnicode.GetBytes("🇦");
      int modeIndex = bytes.Length - 1; //last
      for (int i = 0; i < 26; i++)
      {
        string ustr = Encoding.BigEndianUnicode.GetString(bytes);
        Emojis.Add(new Emoji(ustr));
        bytes[modeIndex]++;
      }

      bytes = Encoding.BigEndianUnicode.GetBytes("0\u20e3");
      modeIndex = 1; //second
      for (int i = 0; i < 10; i++)
      {
        string ustr = Encoding.BigEndianUnicode.GetString(bytes);
        Emojis.Add(new Emoji(ustr));
        bytes[modeIndex]++;
      }
    }

    public static Dictionary<string, string> TimeAbs = new Dictionary<string, string>()
    {
      { "AEST", "E. Australia Standard Time" },
      { "AKST", "Alaskan Standard Time" },
      { "CET", "Central European Standard Time" },
      { "CST", "Central Standard Time" },
      { "EST", "Eastern Standard Time" },
      { "GMT", "Greenwich Standard Time" },
      { "NZST", "New Zealand Standard Time"},
      { "PST", "Pacific Standard Time" },
    };

    public static List<Emoji> Emojis = new List<Emoji>();

  }

  class UtilityModule : ModuleBase<SocketCommandContext>
  {
    public static IEmote InterfacedEmoteConstructor(string input)
    {
      IEmote res = null;

      Emote cres;
      if (Emote.TryParse(input, out cres))
      {
        res = cres;
      }
      else
      {
        res = new Emoji(input);
      }

      return res;
    }

    [Command("makeup")]
    [Summary("**Owner only**. Changes the profile picture.")]
    public async Task Makeup(
      [Summary("URL to image")] [Remainder] string URLtoimg)
    {
      string bestGuessAtFileName = (URLtoimg.Split('/').Last());
      using (var client = new WebClient())
      {
        client.DownloadFileCompleted += async (sender, e) => await this.postMakeup(bestGuessAtFileName, this.Context);
        client.DownloadFileAsync(new Uri(URLtoimg), bestGuessAtFileName);
      }
    }

    private async Task postMakeup(string bestGuessAtFileName, SocketCommandContext context)
    {
      try
      {
        string imgpath = Path.Combine(Directory.GetCurrentDirectory(), bestGuessAtFileName);
        var fileStream = new FileStream(imgpath, FileMode.Open);
        var image = new Image(fileStream);
        await Context.Client.CurrentUser.ModifyAsync(u => u.Avatar = image);
        image.Stream.Close();
        fileStream.Close();
        File.Delete(imgpath);
      }
      catch (Exception e)
      {
        await context.Channel.SendMessageAsync("Failed to change avatar: " + e.Message);
      }
    }

    [Command("memory")]
    [Summary("Shows RAM usage.")]
    public async Task Memory()
    {
      var proc = Process.GetCurrentProcess();
      var mem = proc.WorkingSet64;
      await this.Context.Channel.SendMessageAsync(String.Format("🖥️ Using {0:n3} MB", mem / 1024.0 / 1024.0));
    }

    [Command("ping")]
    [Summary("Pings discord.gg, and reports latency. If possible, also pings to voice server.")]
    public async Task Ping()
    {
        Ping pinger = null;
        PingReply reply = null;

        try
        {
            pinger = new Ping();
            reply = pinger.Send("discord.gg");
        }
        catch (PingException)
        {
            await this.Context.Channel.SendMessageAsync("⚠ Ping of `discord.gg` totally failed!");
        }
        finally
        {
            pinger?.Dispose();
        }

        await this.Context.Channel.SendMessageAsync("🏓 Pinged `discord.gg` in " + reply.RoundtripTime + " ms.");


        var guildUser = this.Context.User as IGuildUser;
    }

    [Command("vkick")]
    [Summary("**Requires Move Members**. Moves target to the designated AFK channel.")]
    [RequireBotPermission(GuildPermission.MoveMembers)]
    [RequireUserPermission(GuildPermission.MoveMembers)]
    public async Task VoiceKick(SocketUser user = null)
    {
      SocketVoiceChannel afkChannel = this.Context.Guild.AFKChannel;
      if (afkChannel != null)
      {
        var guildUser = user as IGuildUser;
        if (guildUser != null) await guildUser.ModifyAsync(x => x.Channel = afkChannel);
      }
    }

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
          realSummary += ("[" + EntryPoint.Prefix + alias + "] ");
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

    [Command("react")]
    [Summary("Adds a reaction to the previous message")]
    public async Task Mreact(
    [Summary("The reaction")] [Remainder] string reaction)
    {
      IEmote res = UtilityModule.InterfacedEmoteConstructor(reaction);

      var items = await Context.Channel.GetMessagesAsync(2).Flatten();
      int i = 0;
      foreach (IMessage message in items)
      {
        if (i == 1)
        {
          var msg = await Context.Channel.GetMessageAsync(message.Id) as RestUserMessage;
          await msg.AddReactionAsync(res);
          await Context.Message.DeleteAsync();
        }
        i++;
      }
    }

    [Command("react")]
    [Summary("Adds a reaction to the specified message")]
    public async Task Mreact(
      [Summary("The message")] ulong mid,
      [Summary("The reaction (Use the actual emoji.)")] string reaction)
    {
      var msg = await Context.Channel.GetMessageAsync(mid) as RestUserMessage;
      await msg.AddReactionAsync(UtilityModule.InterfacedEmoteConstructor(reaction));
      await Context.Message.DeleteAsync();
    }


    [Command("freact")]
    [Summary("Adds a reaction to the previous message")]
    public async Task Freact(
    [Summary("The reaction (Use the emoji ID.)")] [Remainder] string reaction)
    {
      reaction = "<:" + reaction + ">";
      Emote res;
      Console.WriteLine("Ping");
      if (Emote.TryParse(reaction, out res))
      {
        Console.WriteLine("Pong");
        var items = await Context.Channel.GetMessagesAsync(2).Flatten();
        int i = 0;
        foreach (IMessage message in items)
        {
          if (i == 1)
          {
            var msg = await Context.Channel.GetMessageAsync(message.Id) as RestUserMessage;
            await msg.AddReactionAsync(res);
          }
          i++;
        }
        await Context.Message.DeleteAsync();
      }
    }

    [Command("freact")]
    [Summary("Adds a reaction to the specified message")]
    public async Task Freact(
      [Summary("The message")] ulong mid,
      [Summary("The reaction (Use the emoji ID.)")] string reaction)
    {
      reaction = "<:" + reaction + ">";
      Emote res;
      if (Emote.TryParse(reaction, out res))
      {
        var msg = await Context.Channel.GetMessageAsync(mid) as RestUserMessage;
        await msg.AddReactionAsync(res);
      }
      await Context.Message.DeleteAsync();
    }


    [Command("preact")]
    [Summary("Adds a pollreaction to the specified message")]
    public async Task Preact(
      [Summary("The message")] ulong mid)
    {

      var msg = await Context.Channel.GetMessageAsync(mid) as RestUserMessage;
      var emojiPattern = @"(?:\uD83D(?:[\uDC76\uDC66\uDC67](?:\uD83C[\uDFFB-\uDFFF])?|\uDC68(?:(?:\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:\u2695\uFE0F?|\uD83C[\uDF93\uDFEB\uDF3E\uDF73\uDFED\uDFA4\uDFA8]|\u2696\uFE0F?|\uD83D[\uDD27\uDCBC\uDD2C\uDCBB\uDE80\uDE92]|\u2708\uFE0F?|\uD83E[\uDDB0-\uDDB3]))?)|\u200D(?:\u2695\uFE0F?|\uD83C[\uDF93\uDFEB\uDF3E\uDF73\uDFED\uDFA4\uDFA8]|\u2696\uFE0F?|\uD83D(?:\uDC69\u200D\uD83D(?:\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?)|\uDC68\u200D\uD83D(?:\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?)|\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?|[\uDD27\uDCBC\uDD2C\uDCBB\uDE80\uDE92])|\u2708\uFE0F?|\uD83E[\uDDB0-\uDDB3]|\u2764(?:\uFE0F\u200D\uD83D(?:\uDC8B\u200D\uD83D\uDC68|\uDC68)|\u200D\uD83D(?:\uDC8B\u200D\uD83D\uDC68|\uDC68)))))?|\uDC69(?:(?:\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:\u2695\uFE0F?|\uD83C[\uDF93\uDFEB\uDF3E\uDF73\uDFED\uDFA4\uDFA8]|\u2696\uFE0F?|\uD83D[\uDD27\uDCBC\uDD2C\uDCBB\uDE80\uDE92]|\u2708\uFE0F?|\uD83E[\uDDB0-\uDDB3]))?)|\u200D(?:\u2695\uFE0F?|\uD83C[\uDF93\uDFEB\uDF3E\uDF73\uDFED\uDFA4\uDFA8]|\u2696\uFE0F?|\uD83D(?:\uDC69\u200D\uD83D(?:\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?)|\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?|[\uDD27\uDCBC\uDD2C\uDCBB\uDE80\uDE92])|\u2708\uFE0F?|\uD83E[\uDDB0-\uDDB3]|\u2764(?:\uFE0F\u200D\uD83D(?:\uDC8B\u200D\uD83D[\uDC68\uDC69]|[\uDC68\uDC69])|\u200D\uD83D(?:\uDC8B\u200D\uD83D[\uDC68\uDC69]|[\uDC68\uDC69])))))?|[\uDC74\uDC75](?:\uD83C[\uDFFB-\uDFFF])?|\uDC6E(?:(?:\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:[\u2642\u2640]\uFE0F?))?)|\u200D(?:[\u2642\u2640]\uFE0F?)))?|\uDD75(?:(?:\uFE0F(?:\u200D(?:[\u2642\u2640]\uFE0F?))?|\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:[\u2642\u2640]\uFE0F?))?)|\u200D(?:[\u2642\u2640]\uFE0F?)))?|[\uDC82\uDC77](?:(?:\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:[\u2642\u2640]\uFE0F?))?)|\u200D(?:[\u2642\u2640]\uFE0F?)))?|\uDC78(?:\uD83C[\uDFFB-\uDFFF])?|\uDC73(?:(?:\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:[\u2642\u2640]\uFE0F?))?)|\u200D(?:[\u2642\u2640]\uFE0F?)))?|\uDC72(?:\uD83C[\uDFFB-\uDFFF])?|\uDC71(?:(?:\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:[\u2642\u2640]\uFE0F?))?)|\u200D(?:[\u2642\u2640]\uFE0F?)))?|[\uDC70\uDC7C](?:\uD83C[\uDFFB-\uDFFF])?|[\uDE4D\uDE4E\uDE45\uDE46\uDC81\uDE4B\uDE47\uDC86\uDC87\uDEB6](?:(?:\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:[\u2642\u2640]\uFE0F?))?)|\u200D(?:[\u2642\u2640]\uFE0F?)))?|[\uDC83\uDD7A](?:\uD83C[\uDFFB-\uDFFF])?|\uDC6F(?:\u200D(?:[\u2642\u2640]\uFE0F?))?|[\uDEC0\uDECC](?:\uD83C[\uDFFB-\uDFFF])?|\uDD74(?:(?:\uD83C[\uDFFB-\uDFFF]|\uFE0F))?|\uDDE3\uFE0F?|[\uDEA3\uDEB4\uDEB5](?:(?:\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:[\u2642\u2640]\uFE0F?))?)|\u200D(?:[\u2642\u2640]\uFE0F?)))?|[\uDCAA\uDC48\uDC49\uDC46\uDD95\uDC47\uDD96](?:\uD83C[\uDFFB-\uDFFF])?|\uDD90(?:(?:\uD83C[\uDFFB-\uDFFF]|\uFE0F))?|[\uDC4C-\uDC4E\uDC4A\uDC4B\uDC4F\uDC50\uDE4C\uDE4F\uDC85\uDC42\uDC43](?:\uD83C[\uDFFB-\uDFFF])?|\uDC41(?:(?:\uFE0F(?:\u200D\uD83D\uDDE8\uFE0F?)?|\u200D\uD83D\uDDE8\uFE0F?))?|[\uDDE8\uDDEF\uDD73\uDD76\uDECD\uDC3F\uDD4A\uDD77\uDD78\uDDFA\uDEE3\uDEE4\uDEE2\uDEF3\uDEE5\uDEE9\uDEF0\uDECE\uDD70\uDD79\uDDBC\uDDA5\uDDA8\uDDB1\uDDB2\uDCFD\uDD6F\uDDDE\uDDF3\uDD8B\uDD8A\uDD8C\uDD8D\uDDC2\uDDD2\uDDD3\uDD87\uDDC3\uDDC4\uDDD1\uDDDD\uDEE0\uDDE1\uDEE1\uDDDC\uDECF\uDECB\uDD49]\uFE0F?|[\uDE00-\uDE06\uDE09-\uDE0B\uDE0E\uDE0D\uDE18\uDE17\uDE19\uDE1A\uDE42\uDE10\uDE11\uDE36\uDE44\uDE0F\uDE23\uDE25\uDE2E\uDE2F\uDE2A\uDE2B\uDE34\uDE0C\uDE1B-\uDE1D\uDE12-\uDE15\uDE43\uDE32\uDE41\uDE16\uDE1E\uDE1F\uDE24\uDE22\uDE2D\uDE26-\uDE29\uDE2C\uDE30\uDE31\uDE33\uDE35\uDE21\uDE20\uDE37\uDE07\uDE08\uDC7F\uDC79\uDC7A\uDC80\uDC7B\uDC7D\uDC7E\uDCA9\uDE3A\uDE38\uDE39\uDE3B-\uDE3D\uDE40\uDE3F\uDE3E\uDE48-\uDE4A\uDC64\uDC65\uDC6B-\uDC6D\uDC8F\uDC91\uDC6A\uDC63\uDC40\uDC45\uDC44\uDC8B\uDC98\uDC93-\uDC97\uDC99-\uDC9C\uDDA4\uDC9D-\uDC9F\uDC8C\uDCA4\uDCA2\uDCA3\uDCA5\uDCA6\uDCA8\uDCAB-\uDCAD\uDC53-\uDC62\uDC51\uDC52\uDCFF\uDC84\uDC8D\uDC8E\uDC35\uDC12\uDC36\uDC15\uDC29\uDC3A\uDC31\uDC08\uDC2F\uDC05\uDC06\uDC34\uDC0E\uDC2E\uDC02-\uDC04\uDC37\uDC16\uDC17\uDC3D\uDC0F\uDC11\uDC10\uDC2A\uDC2B\uDC18\uDC2D\uDC01\uDC00\uDC39\uDC30\uDC07\uDC3B\uDC28\uDC3C\uDC3E\uDC14\uDC13\uDC23-\uDC27\uDC38\uDC0A\uDC22\uDC0D\uDC32\uDC09\uDC33\uDC0B\uDC2C\uDC1F-\uDC21\uDC19\uDC1A\uDC0C\uDC1B-\uDC1E\uDC90\uDCAE\uDD2A\uDDFE\uDDFB\uDC92\uDDFC\uDDFD\uDD4C\uDD4D\uDD4B\uDC88\uDE82-\uDE8A\uDE9D\uDE9E\uDE8B-\uDE8E\uDE90-\uDE9C\uDEB2\uDEF4\uDEF9\uDEF5\uDE8F\uDEA8\uDEA5\uDEA6\uDED1\uDEA7\uDEF6\uDEA4\uDEA2\uDEEB\uDEEC\uDCBA\uDE81\uDE9F-\uDEA1\uDE80\uDEF8\uDD5B\uDD67\uDD50\uDD5C\uDD51\uDD5D\uDD52\uDD5E\uDD53\uDD5F\uDD54\uDD60\uDD55\uDD61\uDD56\uDD62\uDD57\uDD63\uDD58\uDD64\uDD59\uDD65\uDD5A\uDD66\uDD25\uDCA7\uDEF7\uDD2E\uDD07-\uDD0A\uDCE2\uDCE3\uDCEF\uDD14\uDD15\uDCFB\uDCF1\uDCF2\uDCDE-\uDCE0\uDD0B\uDD0C\uDCBB\uDCBD-\uDCC0\uDCFA\uDCF7-\uDCF9\uDCFC\uDD0D\uDD0E\uDCA1\uDD26\uDCD4-\uDCDA\uDCD3\uDCD2\uDCC3\uDCDC\uDCC4\uDCF0\uDCD1\uDD16\uDCB0\uDCB4-\uDCB8\uDCB3\uDCB9\uDCB1\uDCB2\uDCE7-\uDCE9\uDCE4-\uDCE6\uDCEB\uDCEA\uDCEC-\uDCEE\uDCDD\uDCBC\uDCC1\uDCC2\uDCC5-\uDCD0\uDD12\uDD13\uDD0F-\uDD11\uDD28\uDD2B\uDD27\uDD29\uDD17\uDD2C\uDD2D\uDCE1\uDC89\uDC8A\uDEAA\uDEBD\uDEBF\uDEC1\uDED2\uDEAC\uDDFF\uDEAE\uDEB0\uDEB9-\uDEBC\uDEBE\uDEC2-\uDEC5\uDEB8\uDEAB\uDEB3\uDEAD\uDEAF\uDEB1\uDEB7\uDCF5\uDD1E\uDD03\uDD04\uDD19-\uDD1D\uDED0\uDD4E\uDD2F\uDD00-\uDD02\uDD3C\uDD3D\uDD05\uDD06\uDCF6\uDCF3\uDCF4\uDD31\uDCDB\uDD30\uDD1F\uDCAF\uDD20-\uDD24\uDD36-\uDD3B\uDCA0\uDD18\uDD32-\uDD35\uDEA9])|\uD83E(?:[\uDDD2\uDDD1\uDDD3](?:\uD83C[\uDFFB-\uDFFF])?|[\uDDB8\uDDB9](?:\u200D(?:[\u2640\u2642]\uFE0F?))?|[\uDD34\uDDD5\uDDD4\uDD35\uDD30\uDD31\uDD36](?:\uD83C[\uDFFB-\uDFFF])?|[\uDDD9-\uDDDD](?:(?:\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:[\u2640\u2642]\uFE0F?))?)|\u200D(?:[\u2640\u2642]\uFE0F?)))?|[\uDDDE\uDDDF](?:\u200D(?:[\u2640\u2642]\uFE0F?))?|[\uDD26\uDD37](?:(?:\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:[\u2642\u2640]\uFE0F?))?)|\u200D(?:[\u2642\u2640]\uFE0F?)))?|[\uDDD6-\uDDD8](?:(?:\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:[\u2640\u2642]\uFE0F?))?)|\u200D(?:[\u2640\u2642]\uFE0F?)))?|\uDD38(?:(?:\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:[\u2642\u2640]\uFE0F?))?)|\u200D(?:[\u2642\u2640]\uFE0F?)))?|\uDD3C(?:\u200D(?:[\u2642\u2640]\uFE0F?))?|[\uDD3D\uDD3E\uDD39](?:(?:\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:[\u2642\u2640]\uFE0F?))?)|\u200D(?:[\u2642\u2640]\uFE0F?)))?|[\uDD33\uDDB5\uDDB6\uDD1E\uDD18\uDD19\uDD1B\uDD1C\uDD1A\uDD1F\uDD32](?:\uD83C[\uDFFB-\uDFFF])?|[\uDD23\uDD70\uDD17\uDD29\uDD14\uDD28\uDD10\uDD24\uDD11\uDD2F\uDD75\uDD76\uDD2A\uDD2C\uDD12\uDD15\uDD22\uDD2E\uDD27\uDD20\uDD21\uDD73\uDD74\uDD7A\uDD25\uDD2B\uDD2D\uDDD0\uDD13\uDD16\uDD3A\uDD1D\uDDB0-\uDDB3\uDDE0\uDDB4\uDDB7\uDDE1\uDD7D\uDD7C\uDDE3-\uDDE6\uDD7E\uDD7F\uDDE2\uDD8D\uDD8A\uDD9D\uDD81\uDD84\uDD93\uDD8C\uDD99\uDD92\uDD8F\uDD9B\uDD94\uDD87\uDD98\uDDA1\uDD83\uDD85\uDD86\uDDA2\uDD89\uDD9A\uDD9C\uDD8E\uDD95\uDD96\uDD88\uDD80\uDD9E\uDD90\uDD91\uDD8B\uDD97\uDD82\uDD9F\uDDA0\uDD40\uDD6D\uDD5D\uDD65\uDD51\uDD54\uDD55\uDD52\uDD6C\uDD66\uDD5C\uDD50\uDD56\uDD68\uDD6F\uDD5E\uDDC0\uDD69\uDD53\uDD6A\uDD59\uDD5A\uDD58\uDD63\uDD57\uDDC2\uDD6B\uDD6E\uDD5F-\uDD61\uDDC1\uDD67\uDD5B\uDD42\uDD43\uDD64\uDD62\uDD44\uDDED\uDDF1\uDDF3\uDDE8\uDDE7\uDD47-\uDD49\uDD4E\uDD4F\uDD4D\uDD4A\uDD4B\uDD45\uDD4C\uDDFF\uDDE9\uDDF8\uDD41\uDDEE\uDDFE\uDDF0\uDDF2\uDDEA-\uDDEC\uDDEF\uDDF4-\uDDF7\uDDF9-\uDDFD])|[\u263A\u2639\u2620]\uFE0F?|\uD83C(?:\uDF85(?:\uD83C[\uDFFB-\uDFFF])?|\uDFC3(?:(?:\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:[\u2642\u2640]\uFE0F?))?)|\u200D(?:[\u2642\u2640]\uFE0F?)))?|[\uDFC7\uDFC2](?:\uD83C[\uDFFB-\uDFFF])?|\uDFCC(?:(?:\uFE0F(?:\u200D(?:[\u2642\u2640]\uFE0F?))?|\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:[\u2642\u2640]\uFE0F?))?)|\u200D(?:[\u2642\u2640]\uFE0F?)))?|[\uDFC4\uDFCA](?:(?:\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:[\u2642\u2640]\uFE0F?))?)|\u200D(?:[\u2642\u2640]\uFE0F?)))?|\uDFCB(?:(?:\uFE0F(?:\u200D(?:[\u2642\u2640]\uFE0F?))?|\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:[\u2642\u2640]\uFE0F?))?)|\u200D(?:[\u2642\u2640]\uFE0F?)))?|[\uDFCE\uDFCD\uDFF5\uDF36\uDF7D\uDFD4-\uDFD6\uDFDC-\uDFDF\uDFDB\uDFD7\uDFD8\uDFDA\uDFD9\uDF21\uDF24-\uDF2C\uDF97\uDF9F\uDF96\uDF99-\uDF9B\uDF9E\uDFF7\uDD70\uDD71\uDD7E\uDD7F\uDE02\uDE37]\uFE0F?|\uDFF4(?:(?:\u200D\u2620\uFE0F?|\uDB40\uDC67\uDB40\uDC62\uDB40(?:\uDC65\uDB40\uDC6E\uDB40\uDC67\uDB40\uDC7F|\uDC73\uDB40\uDC63\uDB40\uDC74\uDB40\uDC7F|\uDC77\uDB40\uDC6C\uDB40\uDC73\uDB40\uDC7F)))?|\uDFF3(?:(?:\uFE0F(?:\u200D\uD83C\uDF08)?|\u200D\uD83C\uDF08))?|\uDDE6\uD83C[\uDDE8-\uDDEC\uDDEE\uDDF1\uDDF2\uDDF4\uDDF6-\uDDFA\uDDFC\uDDFD\uDDFF]|\uDDE7\uD83C[\uDDE6\uDDE7\uDDE9-\uDDEF\uDDF1-\uDDF4\uDDF6-\uDDF9\uDDFB\uDDFC\uDDFE\uDDFF]|\uDDE8\uD83C[\uDDE6\uDDE8\uDDE9\uDDEB-\uDDEE\uDDF0-\uDDF5\uDDF7\uDDFA-\uDDFF]|\uDDE9\uD83C[\uDDEA\uDDEC\uDDEF\uDDF0\uDDF2\uDDF4\uDDFF]|\uDDEA\uD83C[\uDDE6\uDDE8\uDDEA\uDDEC\uDDED\uDDF7-\uDDFA]|\uDDEB\uD83C[\uDDEE-\uDDF0\uDDF2\uDDF4\uDDF7]|\uDDEC\uD83C[\uDDE6\uDDE7\uDDE9-\uDDEE\uDDF1-\uDDF3\uDDF5-\uDDFA\uDDFC\uDDFE]|\uDDED\uD83C[\uDDF0\uDDF2\uDDF3\uDDF7\uDDF9\uDDFA]|\uDDEE\uD83C[\uDDE8-\uDDEA\uDDF1-\uDDF4\uDDF6-\uDDF9]|\uDDEF\uD83C[\uDDEA\uDDF2\uDDF4\uDDF5]|\uDDF0\uD83C[\uDDEA\uDDEC-\uDDEE\uDDF2\uDDF3\uDDF5\uDDF7\uDDFC\uDDFE\uDDFF]|\uDDF1\uD83C[\uDDE6-\uDDE8\uDDEE\uDDF0\uDDF7-\uDDFB\uDDFE]|\uDDF2\uD83C[\uDDE6\uDDE8-\uDDED\uDDF0-\uDDFF]|\uDDF3\uD83C[\uDDE6\uDDE8\uDDEA-\uDDEC\uDDEE\uDDF1\uDDF4\uDDF5\uDDF7\uDDFA\uDDFF]|\uDDF4\uD83C\uDDF2|\uDDF5\uD83C[\uDDE6\uDDEA-\uDDED\uDDF0-\uDDF3\uDDF7-\uDDF9\uDDFC\uDDFE]|\uDDF6\uD83C\uDDE6|\uDDF7\uD83C[\uDDEA\uDDF4\uDDF8\uDDFA\uDDFC]|\uDDF8\uD83C[\uDDE6-\uDDEA\uDDEC-\uDDF4\uDDF7-\uDDF9\uDDFB\uDDFD-\uDDFF]|\uDDF9\uD83C[\uDDE6\uDDE8\uDDE9\uDDEB-\uDDED\uDDEF-\uDDF4\uDDF7\uDDF9\uDDFB\uDDFC\uDDFF]|\uDDFA\uD83C[\uDDE6\uDDEC\uDDF2\uDDF3\uDDF8\uDDFE\uDDFF]|\uDDFB\uD83C[\uDDE6\uDDE8\uDDEA\uDDEC\uDDEE\uDDF3\uDDFA]|\uDDFC\uD83C[\uDDEB\uDDF8]|\uDDFD\uD83C\uDDF0|\uDDFE\uD83C[\uDDEA\uDDF9]|\uDDFF\uD83C[\uDDE6\uDDF2\uDDFC]|[\uDFFB-\uDFFF\uDF92\uDFA9\uDF93\uDF38-\uDF3C\uDF37\uDF31-\uDF35\uDF3E-\uDF43\uDF47-\uDF53\uDF45\uDF46\uDF3D\uDF44\uDF30\uDF5E\uDF56\uDF57\uDF54\uDF5F\uDF55\uDF2D-\uDF2F\uDF73\uDF72\uDF7F\uDF71\uDF58-\uDF5D\uDF60\uDF62-\uDF65\uDF61\uDF66-\uDF6A\uDF82\uDF70\uDF6B-\uDF6F\uDF7C\uDF75\uDF76\uDF7E\uDF77-\uDF7B\uDF74\uDFFA\uDF0D-\uDF10\uDF0B\uDFE0-\uDFE6\uDFE8-\uDFED\uDFEF\uDFF0\uDF01\uDF03-\uDF07\uDF09\uDF0C\uDFA0-\uDFA2\uDFAA\uDF11-\uDF20\uDF00\uDF08\uDF02\uDF0A\uDF83\uDF84\uDF86-\uDF8B\uDF8D-\uDF91\uDF80\uDF81\uDFAB\uDFC6\uDFC5\uDFC0\uDFD0\uDFC8\uDFC9\uDFBE\uDFB3\uDFCF\uDFD1-\uDFD3\uDFF8\uDFA3\uDFBD\uDFBF\uDFAF\uDFB1\uDFAE\uDFB0\uDFB2\uDCCF\uDC04\uDFB4\uDFAD\uDFA8\uDFBC\uDFB5\uDFB6\uDFA4\uDFA7\uDFB7-\uDFBB\uDFA5\uDFAC\uDFEE\uDFF9\uDFE7\uDFA6\uDD8E\uDD91-\uDD9A\uDE01\uDE36\uDE2F\uDE50\uDE39\uDE1A\uDE32\uDE51\uDE38\uDE34\uDE33\uDE3A\uDE35\uDFC1\uDF8C])|\u26F7\uFE0F?|\u26F9(?:(?:\uFE0F(?:\u200D(?:[\u2642\u2640]\uFE0F?))?|\uD83C(?:[\uDFFB-\uDFFF](?:\u200D(?:[\u2642\u2640]\uFE0F?))?)|\u200D(?:[\u2642\u2640]\uFE0F?)))?|[\u261D\u270C](?:(?:\uD83C[\uDFFB-\uDFFF]|\uFE0F))?|[\u270B\u270A](?:\uD83C[\uDFFB-\uDFFF])?|\u270D(?:(?:\uD83C[\uDFFB-\uDFFF]|\uFE0F))?|[\u2764\u2763\u26D1\u2618\u26F0\u26E9\u2668\u26F4\u2708\u23F1\u23F2\u2600\u2601\u26C8\u2602\u26F1\u2744\u2603\u2604\u26F8\u2660\u2665\u2666\u2663\u260E\u2328\u2709\u270F\u2712\u2702\u26CF\u2692\u2694\u2699\u2696\u26D3\u2697\u26B0\u26B1\u26A0\u2622\u2623\u2B06\u2197\u27A1\u2198\u2B07\u2199\u2B05\u2196\u2195\u2194\u21A9\u21AA\u2934\u2935\u269B\u267E\u2721\u2638\u262F\u271D\u2626\u262A\u262E\u25B6\u23ED\u23EF\u25C0\u23EE\u23F8-\u23FA\u23CF\u2640\u2642\u2695\u267B\u269C\u2611\u2714\u2716\u303D\u2733\u2734\u2747\u203C\u2049\u3030\u00A9\u00AE\u2122]\uFE0F?|[\u0023\u002A\u0030-\u0039](?:\uFE0F\u20E3|\u20E3)|[\u2139\u24C2\u3297\u3299\u25AA\u25AB\u25FB\u25FC]\uFE0F?|[\u2615\u26EA\u26F2\u26FA\u26FD\u2693\u26F5\u231B\u23F3\u231A\u23F0\u2B50\u26C5\u2614\u26A1\u26C4\u2728\u26BD\u26BE\u26F3\u267F\u26D4\u2648-\u2653\u26CE\u23E9-\u23EC\u2B55\u2705\u274C\u274E\u2795-\u2797\u27B0\u27BF\u2753-\u2755\u2757\u25FD\u25FE\u2B1B\u2B1C\u26AA\u26AB])";
      foreach (var match in Regex.Matches(msg.Content, emojiPattern))
      {
        Emoji uEmoji = new Emoji(match.ToString());
        await msg.AddReactionAsync(uEmoji);
      }

      await Context.Message.DeleteAsync();
    }

    [Command("femoji")]
    [Summary("States an emoji")]
    public async Task Femoji(
      [Summary("The reaction (Use the emoji ID.)")] string emoji,
      [Summary("`a` to use the animated prefix")] string anim = "x")
    {
      string prefix = "<:";
      if (anim == "a")
      {
        prefix = "<a:";
      }
      emoji = prefix + emoji + ">";
      Emote res;
      if (Emote.TryParse(emoji, out res))
      {
        var msg = await Context.Channel.SendMessageAsync(emoji) as RestUserMessage;
      }
      await Context.Message.DeleteAsync();
    }

#region Word React
    private async void wreactHelper(string reaction, RestUserMessage rum)
    {
      reaction = reaction.ToUpper();

      foreach (char c in reaction)
      {
        if ('A' <= c && c <= 'Z')
        {
          int index = c - 'A';
          if (index < 0 || index > 26) continue;
          Emoji r = LookupData.Emojis[index];
          await rum.AddReactionAsync(r);
        }
        else if (c == '!' || c == '?')
        {
          if (c == '!') await rum.AddReactionAsync(new Emoji("❗"));
          if (c == '?') await rum.AddReactionAsync(new Emoji("❓"));
        }
        else
        {
          int index = c - '0';
          if (index < 0 || index > 10) continue;
          Emoji r = LookupData.Emojis[index + 26];
          await rum.AddReactionAsync(r);
        }
      }
    }

    [Command("wreact")]
    [Alias("wreact", "wr")]
    [Summary("Adds a word-reaction to the specified message")]
    public async Task Wreact(
      [Summary("The message")] ulong mid,
      [Summary("The reaction")] string reaction)
    {
      RestUserMessage rum = await Context.Channel.GetMessageAsync(mid) as RestUserMessage;
      if (rum == null) return;

      await Context.Message.DeleteAsync();

      wreactHelper(reaction, rum);
    }

    [Command("wreact")]
    [Alias("wreact", "wr")]
    [Summary("Adds a word-reaction to the previous message")]
    public async Task Wreact(
      [Summary("The reaction")] [Remainder] string reaction)
    {
      RestUserMessage rum = null;
      var items = await Context.Channel.GetMessagesAsync(2).Flatten();
      int i = 0;
      foreach (IMessage message in items)
      {
        if (i == 1)
        {
          rum = await Context.Channel.GetMessageAsync(message.Id) as RestUserMessage;
        }
        i++;
      }
      if (rum == null) return;

      await Context.Message.DeleteAsync();


      wreactHelper(reaction, rum);
    }
#endregion

    [Command("gg")]
    [Summary("**Owner only**. Kills the bot.")]
    public async Task Gg()
    {
      Task<bool> result = this.Context.IsOwner();
      if (!result.Result) return;
      await this.Context.Channel.SendMessageAsync("😭... Okay, bye for now! 👋");
      Environment.Exit(0);
    }

    [Command("owners")]
    [Summary("**Owner only**. DMs invoker with list of owners.")]
    public async Task Owners()
    {
      if (!this.Context.IsOwner().Result) return;

      List<ulong> owners = SimplePermissions.Owners;

      string msg = "Owners:\n";
      foreach (ulong owner in owners)
      {
        msg += "<@!" + owner + ">\n";
      }

      await this.Context.User.SendMessageAsync(msg);
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

        await this.Context.Message.AddReactionAsync(new Emoji("💀"));
      }
    }

    //--

    [Command("prune")]
    [Alias("purge")]
    [Summary("**Requires Manage Messages**. Deletes a specified amount of messages in the channel.")]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public async Task Prune([Summary("Number of messages to delete.")] int delnum)
    {
      var items = await Context.Channel.GetMessagesAsync(delnum + 1).Flatten();
      await this.Context.Channel.DeleteMessagesAsync(items);
    }

    [Command("timespit")]
    [Summary("Converts a time into other timezones.")]
    public async Task Timespit(
      [Summary("The time to convert")] string time,
      [Summary("The timezone of the original timezone")] string origzone,
      [Remainder] [Summary("Other timezones, separated by commas")] string zones)
    {
      //try
      zones = zones.ToUpper();
      {
        DateTime result;
        if (!DateTime.TryParse(time, out result))
        {
          await this.Context.Channel.SendMessageAsync("???");
        }

          //static: run all timezones
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
          //UNIX system requires an IANA conversion
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
          string stz = LocalExtensions.WindowsToIana(LookupData.TimeAbs[origzone]);
          otzi = TimeZoneInfo.FindSystemTimeZoneById(stz);
        }
          //Windows system uses the dotNET/Windows internal table
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
        //TODO: do not generate previously generated PNGs, it's not that much data.
        bmp.Save("Temp.png");
        await this.Context.Channel.SendFileAsync("Temp.png");
        await Task.Factory.StartNew(
          path => File.Delete((string)path), "Temp.png"
          );
      }
      catch (Exception e)
      {
        await this.Context.Channel.SendMessageAsync("Error: " + e.Message);
      }
    }
  }
}
