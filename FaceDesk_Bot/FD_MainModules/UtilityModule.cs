using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;

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

    [Command("medit")]
    [Summary("**Owner only**. Edits a message for which the bot is the author.")]
    public async Task Medit(
      [Summary("The message id")] ulong msgid,
      [Summary("The new message text")] [Remainder] string newmsg)
    {
      bool result = await this.Context.IsOwner(); if (!result) return;

      if (this.Context.Guild.GetUser(EntryPoint.Client.CurrentUser.Id).GetPermissions(this.Context.Channel as IGuildChannel).ManageMessages)
        await this.Context.Message.DeleteAsync();

      var message = (RestUserMessage)await this.Context.Channel.GetMessageAsync(msgid);
      await message.ModifyAsync(msg => msg.Content = newmsg);
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

  

    [Command("cleanup")]
    [Summary("**Admin only**. (Bot requires Admin.) Kicks all users without roles.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireBotPermission(GuildPermission.Administrator)]
    public async Task Cleanup()
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

    //--
    [Command("1timeinvite")]
    [Alias("1ti")]
    [Summary("**Requires Create Invite**. (Bot requires Create Invite.) Creates a 1-time 24-hour invite.")]
    [RequireUserPermission(GuildPermission.CreateInstantInvite)]
    [RequireBotPermission(GuildPermission.CreateInstantInvite)]
    public async Task Invite()
    {
      SocketTextChannel stc = this.Context.Channel as SocketTextChannel;
      if(stc != default)
      {
        RestInviteMetadata invite = await stc.CreateInviteAsync(
          86400,
          1,
          false,
          true);

        await this.Context.DebugPublicReleasePrivate(invite.Url, false);
        await this.Context.Message.AddReactionAsync(new Emoji("👍"));
      }
      else await this.Context.Message.AddReactionAsync(new Emoji("❓"));
    }


    [Command("prune")]
    [Alias("purge")]
    [Summary("**Requires Channelmod or Manage Messages**. (Bot requires Manage Messages.) Deletes a specified amount of messages in the channel.")]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    public async Task Prune([Summary("Number of messages to delete.")] int delnum)
    {
      if (Context.User is SocketGuildUser sgu)
      {
        bool manageMessagesChannel()
        {
          SocketGuildChannel sgc = Context.Guild.Channels.FirstOrDefault(x => x.Id == Context.Channel.Id);

          if (sgc != default)
          {
            return sgu.GetPermissions(sgc).ManageMessages;
          }

          return false;
        }

        if (!manageMessagesChannel() && !sgu.GuildPermissions.ManageMessages)
        {
          List<ulong> mods = await GranularPermissionsStorage.GetChannelmodsFor(this.Context.Guild.Id, this.Context.Channel as ISocketMessageChannel);

          if (!mods.Contains(this.Context.User.Id))
          {
            await this.Context.Channel.SendMessageAsync("You are not able to manage messages on this server/channel: **" + this.Context.Channel.Name + "**");
            return;
          }
        }
      }

      var items = await Context.Channel.GetMessagesAsync(delnum + 1).Flatten();
      await this.Context.Channel.DeleteMessagesAsync(items);
    }

    [Command("timezone")]
    [Summary("Converts a time into other timezones.")]
    public async Task Timezone(
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

    [Command("delete_role")]
    [Alias("delr", "nuker")]
    [Summary("**Admin only**. DELETES A ROLE.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireBotPermission(GuildPermission.Administrator)]
    public async Task DeleteRole(SocketRole role)
    {
      string message = $"Attempting to delete `{role.Name}`, `{role.Id}`.";

      await this.Context.Channel.SendMessageAsync(message);
      await role.DeleteAsync();
    }

    [Command("zero_out")]
    [Alias("zero_out", "zo")]
    [Summary("**Admin only**. Zeros out a role.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireBotPermission(GuildPermission.Administrator)]
    public async Task ZeroOut(SocketRole role)
    {
      GuildPermissions noPerms = GuildPermissions.None;
      await role.ModifyAsync(x => x.Permissions = noPerms);

      IReadOnlyCollection<SocketGuildUser> allUsers = this.Context.Guild.Users;

      var message = await this.Context.Channel.SendMessageAsync("Please wait...");
      string content = "";

      foreach (SocketGuildUser user in allUsers)
      {
        if (user.Roles.Contains(role))
        {
          content += user.Username + "." + user.Discriminator + " is losing that role.\n";
          await message.ModifyAsync(msg => msg.Content = content);
          await user.RemoveRoleAsync(role);
        }
      }

      await Context.Message.AddReactionAsync(new Emoji("👌"));
    }

    [Command("orphaned")]
    [Alias("batman", "orph")]
    [Summary("Checks for orphaned roles.")]
    public async Task Orphaned()
    {
      IReadOnlyCollection<SocketRole> allRoles = Context.Guild.Roles;

      string newMsg = "";
      Task<RestUserMessage> startTask = Context.Channel.SendMessageAsync("Starting...");
      await startTask;
      foreach (SocketRole role in allRoles)
      {
        if (!role.Members.Any())
        {
          newMsg += (role.Mention + " has no children.\n");
          await startTask.Result?.ModifyAsync(msg => msg.Content = newMsg);
        }
      }

      await Context.Message.AddReactionAsync(new Emoji("👌"));
    }

    [Command("massassign")]
    [Alias("opera")]
    [Summary("**Admin only**. __Very slow__. Gives a role to absolutely everyone.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireBotPermission(GuildPermission.Administrator)]
    public async Task Opera(SocketRole role)
    {
      IReadOnlyCollection<SocketGuildUser> allUsers = this.Context.Guild.Users;

      foreach (SocketGuildUser user in allUsers)
      {
        await user.AddRoleAsync(role);
      }

      await Context.Message.AddReactionAsync(new Emoji("👌"));
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
