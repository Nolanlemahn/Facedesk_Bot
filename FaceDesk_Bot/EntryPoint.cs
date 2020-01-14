using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;

using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Rest;

using Microsoft.Extensions.DependencyInjection;

using Google.Cloud.Firestore;

//
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
    public static FirestoreDb Firestore;

    public static char Prefix = '^';
    private IServiceProvider _services;

    private static void Main(string[] args) => new EntryPoint().StartAsync().GetAwaiter().GetResult();

    private void FirebaseHookups()
    {
      GranularPermissionsStorage.Setup(Firestore);
      FunStorage.Setup(Firestore);
      AnnouncementsStorage.Setup(Firestore);
    }

    public async Task StartAsync()
    {
      // Find where we are executing from
      RunningFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
      Console.WriteLine("Running from: " + RunningFolder);

      // Database!
      string projectid = File.ReadAllText(Path.Combine(RunningFolder, "nogit_firestoreproject.txt"));
      Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "nogit_serviceaccount.json"); //this is unforgivable
      Firestore = FirestoreDb.Create(projectid);
      FirebaseHookups();
      Console.WriteLine("Connected to Firestore (Hopefully).");

      // Grab the token
      string token = File.ReadAllText(Path.Combine(RunningFolder, "nogit_token.txt"));
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
      Console.WriteLine("Logged in: " + Client.LoginState);

      SimplePermissions.LoadOwners();

      Console.WriteLine("Permissions loaded.");

      await Client.StartAsync();
      Console.WriteLine("Client started.");

      LookupData.Init();

      await Client.SetGameAsync("dead");

      await Task.Delay(-1);
    }

    public async Task InstallCommandsAsync()
    {
      Client.ReactionAdded += HandleReactionAsync;

      //Client.MessageReceived += AutoReact.AutoReactAsync;
      Client.MessageReceived += HandleCommandAsync;
      Client.MessageReceived += Responses.Responses.HandleCommandAsync;

      await MainCommandService.AddModuleAsync(typeof(FD_MainModules.AnnouncementsModule));
      await MainCommandService.AddModuleAsync(typeof(FD_MainModules.UtilityModule));
      await MainCommandService.AddModuleAsync(typeof(FD_MainModules.FunModule));
      await MainCommandService.AddModuleAsync(typeof(Permissions.SimplePermissionsModule));
      await MainCommandService.AddModuleAsync(typeof(Permissions.GranularPermissionsModule));

      await MainCommandService.AddModuleAsync(typeof(FD_MainModules.CNModule));

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

    private async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel socketMessageChannel, SocketReaction reaction)
    {
      //TODO: switch
      //Console.WriteLine(reaction.Emote.ToString());
      return;
    }

    private async Task HandleCommandAsync(SocketMessage messageParam)
    {
      var message = messageParam as SocketUserMessage;
      if (message == null) return;
      var context = new SocketCommandContext(Client, message);

      int argPos = 0;

      SocketGuildUser me = context.Guild.Users.Where(x => x.Id == Client.CurrentUser.Id).ToList()[0];

      PreprocessType runPreprocess = ShouldPreprocessMessage(message);
      if (runPreprocess == PreprocessType.NO_PREPROC &&
        !(
          (me != null && message.HasStringPrefix(me.Nickname + ", ", ref argPos)) ||
          message.HasCharPrefix(EntryPoint.Prefix, ref argPos) ||
          message.HasMentionPrefix(Client.CurrentUser, ref argPos)
        )
      ) return;

      RestApplication application = await context.Client.GetApplicationInfoAsync();

      if (runPreprocess != PreprocessType.NO_PREPROC)
      {
        Console.WriteLine("Preprocessing...");
        if (context.User.Id != application.Owner.Id)

        switch (runPreprocess)
        {
          case PreprocessType.Direct:
            //???
            break;
        }
      }
      else
      {
        // Verify Auth
        bool isAuthed = await GranularPermissionsStorage.GetAuthStatusFor(context.Guild.Id);
        if(!isAuthed)
        {

          // Check for Auth attempt
          string cmd = messageParam.Content.Substring(argPos);
          Match m1 = Regex.Match(cmd, @"(auth )+(\w+)+$");
          if (m1.Success)
          {
            int codeIndex = cmd.IndexOf("auth ");
            string code = cmd.Substring(codeIndex);
            await GranularPermissionsStorage.TryAuthFromContext(context, code);
            return;
          }
          Match m2 = Regex.Match(cmd, @"(authorize )+(\w+)+$");
          if(m2.Success)
          {
            int codeIndex = cmd.IndexOf("authorize ");
            string code = cmd.Substring(codeIndex);
            await GranularPermissionsStorage.TryAuthFromContext(context, code);
            return;
          }

          await context.Channel.SendMessageAsync("This server is not authorized.");
          return;
        }

        var result = await MainCommandService.ExecuteAsync(context, argPos, _services);
        if (result.Error == CommandError.UnknownCommand) return;
        if (!result.IsSuccess)
        {
          await application.Owner.SendMessageAsync("```" + messageParam.Content + "```\n" + result.ErrorReason);
        }
      }
    }
  }
}