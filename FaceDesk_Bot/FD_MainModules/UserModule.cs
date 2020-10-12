using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace FaceDesk_Bot.FD_MainModules
{
  public class UserModule : ModuleBase<SocketCommandContext>
  {
    private string _suffix = " | afk";

    [Command("afk")]
    [Summary("Toggle user name's AFK suffix.")]
    [RequireBotPermission(Discord.GuildPermission.ManageNicknames)]
    public async Task Afk()
    {
      await this.Context.Message.DeleteAsync();


      SocketGuildUser sgu = this.Context.Guild.GetUser(this.Context.User.Id);

      string oldNick = sgu.Nickname;
      if (string.IsNullOrEmpty(oldNick))
      {
        oldNick = sgu.Username;
        await addSuffix();
      }
      else
      {
        if (oldNick.EndsWith(_suffix)) await removeSuffix();
        else await addSuffix();
      }


      async Task addSuffix()
      {
        await sgu.ModifyAsync(nu =>
        {
          nu.Nickname = $"{oldNick}{_suffix}";
        });
      }

      async Task removeSuffix()
      {
        await sgu.ModifyAsync(nu =>
        {
          nu.Nickname = oldNick.Substring(0, oldNick.Length - _suffix.Length);
        });
      }
    }
  }
}
