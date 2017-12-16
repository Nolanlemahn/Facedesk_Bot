using System;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Rest;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using FaceDesk_Bot.FD_MainModules;

namespace FaceDesk_Bot
{
  public enum PreprocessType
  {
    NO_PREPROC,
    Direct,
    Roles
  }

  public class EntryPoint
  {
    //Sorry.
    public static bool Debug = 
#if DEBUG
       true;
#else
       false;
#endif

    public static CommandService MainCommandService;
    public static DiscordSocketClient Client;
    public static SqliteConnection Connection;
    public static bool Lockdown = false;
    private IServiceProvider _services;

    private static void Main(string[] args) => new EntryPoint().StartAsync().GetAwaiter().GetResult();

    public async Task StartAsync()
    {
      // Find where we are executing from
      string assemblyFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
      Console.WriteLine(assemblyFolder);

      // Database!
      EntryPoint.Connection = new SqliteConnection("" + 
        new SqliteConnectionStringBuilder
        {
          DataSource = "nogit_data.db"
        });
      //EntryPoint.Connection.Open();

      // Grab the token
      string token = System.IO.File.ReadAllText(Path.Combine(assemblyFolder, "nogit_token.txt"));

      Client = new DiscordSocketClient();
      MainCommandService = new CommandService();

      _services = new ServiceCollection()
        .AddSingleton(Client)
        .AddSingleton(MainCommandService)
        .BuildServiceProvider();

      await InstallCommandsAsync();

      await Client.LoginAsync(TokenType.Bot, token);
      await Client.StartAsync();
      
      await Client.SetGameAsync("dead");

      await Task.Delay(-1);
    }

    public async Task InstallCommandsAsync()
    {
      Client.ReactionAdded += HandleReactionAsync;

      Client.MessageReceived += HandleCommandAsync;

      await MainCommandService.AddModuleAsync(typeof(FD_MainModules.UtilityModule));

      await MainCommandService.AddModulesAsync(Assembly.GetEntryAssembly());
    }

    private PreprocessType ShouldPreprocessMessage(SocketUserMessage msg)
    {
      if (msg.Channel is SocketDMChannel && !msg.Author.IsBot)
      {
        return PreprocessType.Direct;
      }
      //TODO: other conditions?
      return PreprocessType.NO_PREPROC;
    }

    private async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel socketMessageChannel, SocketReaction reaction)
    {
      Console.WriteLine("Got a reaction.");
      return;
    }

    private async Task HandleCommandAsync(SocketMessage messageParam)
    {
      var message = messageParam as SocketUserMessage;
      if (message == null) return;

      int argPos = 0;


      PreprocessType runPreprocess = ShouldPreprocessMessage(message);
      if (runPreprocess == PreprocessType.NO_PREPROC &&
        !(
          message.HasCharPrefix('^', ref argPos) ||
          message.HasMentionPrefix(Client.CurrentUser, ref argPos)
        )
      ) return;

      var context = new SocketCommandContext(Client, message);
      RestApplication application = await context.Client.GetApplicationInfoAsync();

      if (runPreprocess != PreprocessType.NO_PREPROC)
      {
        Console.WriteLine("Preprocessing...");
        if (EntryPoint.Lockdown && context.User.Id != application.Owner.Id)
        {
          await context.Channel.SendMessageAsync("We are on lockdown.");
          return;
        }
        switch (runPreprocess)
        {
          case PreprocessType.Direct:
            //???
            break;
        }
      }
      else
      {
        if (EntryPoint.Lockdown && context.User.Id != application.Owner.Id)
        {
          await context.Channel.SendMessageAsync("We are on lockdown.");
          return;
        }

        var result = await MainCommandService.ExecuteAsync(context, argPos, _services);
        if (result.Error == CommandError.UnknownCommand) return;
        if (!result.IsSuccess)
        {
          await application.Owner.SendMessageAsync(result.ErrorReason);
        }
      }
    }
  }
}