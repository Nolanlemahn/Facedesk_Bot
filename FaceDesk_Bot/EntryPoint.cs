using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using FaceDesk_Bot.Permissions;

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

    public static string RunningFolder;
    public static CommandService MainCommandService;
    public static DiscordSocketClient Client;
    public static SqliteConnection Connection;
    public static bool Lockdown = false;
    private IServiceProvider _services;

    private static void Main(string[] args) => new EntryPoint().StartAsync().GetAwaiter().GetResult();

    public async Task StartAsync()
    {
      // Find where we are executing from
      RunningFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
      Console.WriteLine(RunningFolder);

      // Database!
      EntryPoint.Connection = new SqliteConnection("" + 
        new SqliteConnectionStringBuilder
        {
          DataSource = "nogit_data.db"
        });
      //EntryPoint.Connection.Open();

      // Grab the token
      string token = System.IO.File.ReadAllText(Path.Combine(RunningFolder, "nogit_token.txt"));
      Console.WriteLine("Key read.");

      Client = new DiscordSocketClient();
      MainCommandService = new CommandService();

      _services = new ServiceCollection()
        .AddSingleton(Client)
        .AddSingleton(MainCommandService)
        .BuildServiceProvider();
      Console.WriteLine("Services started.");

      await InstallCommandsAsync();
      Console.WriteLine("Commands installed.");

      await Client.LoginAsync(TokenType.Bot, token);
      Console.WriteLine("Logged in.");

      SimplePermissions.LoadOwners();
      Console.WriteLine("Permissions loaded.");

      await Client.StartAsync();
      Console.WriteLine("Client started.");

      await Client.SetGameAsync("dead");

      await Task.Delay(-1);
    }

    public async Task InstallCommandsAsync()
    {
      Client.ReactionAdded += HandleReactionAsync;

      Client.MessageReceived += AutoReact.AutoReactAsync;
      Client.MessageReceived += HandleCommandAsync;

      await MainCommandService.AddModuleAsync(typeof(FD_MainModules.UtilityModule));
      await MainCommandService.AddModuleAsync(typeof(FD_MainModules.FunModule));

      //await MainCommandService.AddModulesAsync(Assembly.GetEntryAssembly());
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

#pragma warning disable 1998
    private async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel socketMessageChannel, SocketReaction reaction)
#pragma warning restore 1998
    {
      Console.WriteLine("Got a reaction.");
      Console.WriteLine(reaction.Emote.ToString());
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