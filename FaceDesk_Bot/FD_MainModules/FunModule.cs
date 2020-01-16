using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;

namespace FaceDesk_Bot.FD_MainModules
{
  class FunModule : ModuleBase<SocketCommandContext>
  {
    public static List<string> BallGlobalPositiveResponses; //+
    public static List<string> BallGlobalNegativeResponses; //-
    public static List<string> BallGlobalNeutralResponses;  //!, ?

    private Random ballRandom = new Random();

    [Command("8balldbg")]
    [Summary("**Owner only**. Shows all 8ball result.")]
    public async Task BallShow()
    {
      bool result = await this.Context.IsOwner(); if (!result) return;

      var ballResults = await FunStorage.Get8BallFor(this.Context.Guild.Id);

      string msg = "";
      foreach(var subcategory in ballResults)
      {
        foreach(var lineitem in subcategory)
        {
          msg += lineitem + '\n';
          Console.WriteLine(lineitem);
        }
      }

      await this.ReplyAsync(msg);
    }

    private static char[] _positiveChars = { '+' };
    private static char[] _negativeChars = { '-' };

    [Command("8balladd")]
    [Summary("**Admin only**. Adds an 8ball result.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task BallAdd(char ballSymbol, [Remainder] string msg)
    {
      msg = msg + " [" + ballSymbol + "]";

      ulong gid = this.Context.Guild.Id;
      string ballType = "neutrals";
      if(_positiveChars.Contains(ballSymbol)) ballType = "positives";
      else if(_negativeChars.Contains(ballSymbol)) ballType = "negatives";

      bool success = await FunStorage.Insert8ballFor(gid, ballType, msg);

      if (success) await this.Context.Message.AddReactionAsync(new Emoji("👌"));
      else await this.Context.Message.AddReactionAsync(new Emoji("👎"));
    }

    [Command("8ball")]
    [Summary("Shakes the 8ball.")]
    public async Task BallShake()
    {
      SocketUser author = this.Context.Message.Author;
      // Get the question - arbitrarily, question must have been in the previous 10 messages (parsing for politeness)
      IEnumerable<IMessage> previousMessages = await this.Context.Channel.GetMessagesAsync(10).Flatten();
      List<IMessage> authoredMessages = new List<IMessage>();

      foreach (IMessage previousMessage in previousMessages)
      {
        if (previousMessage.Author.Id == author.Id) authoredMessages.Add(previousMessage);
      }

      bool beNice = false;
      authoredMessages.Remove(authoredMessages.First());
      // "please" indicates politeness
      if (authoredMessages.Count > 0 && authoredMessages.First().Content.ToUpper().Contains("PLEASE"))
      {
        beNice = true;
      }

      string eightballPath = Path.Combine(EntryPoint.RunningFolder, "8ball");
      string rawPosResponses = File.ReadAllText(Path.Combine(eightballPath, "positive.txt"));
      string rawNegResponses = File.ReadAllText(Path.Combine(eightballPath, "negative.txt"));
      string rawNeutResponses = File.ReadAllText(Path.Combine(eightballPath, "neutral.txt"));

      BallGlobalPositiveResponses = rawPosResponses.Split('\n').ToList();
      BallGlobalNegativeResponses = rawNegResponses.Split('\n').ToList();

      var ballResults = await FunStorage.Get8BallFor(this.Context.Guild.Id);

      List<string> localPosResponses = ballResults[0]; localPosResponses.AddRange(BallGlobalPositiveResponses);
      List<string> localNegResponses = ballResults[1]; localNegResponses.AddRange(BallGlobalNegativeResponses);

      List<List<string>> responses = new List<List<string>>
      {
        localPosResponses,
        localNegResponses,
      };

      // If we aren't being nice in response to politeness, add the neutral responses
      if (!beNice)
      {
        BallGlobalNeutralResponses = rawNeutResponses.Split('\n').ToList();
        List<string> localNeutResponses = ballResults[2]; localNeutResponses.AddRange(BallGlobalNeutralResponses);
        responses.Append(localNeutResponses);
        responses.Append(localNeutResponses); //This is not a typo.
      }

      List<string> randList =
        // Get a random 8ball list
        responses[ballRandom.Next(responses.Count)];

      // Select a random response
      string rand = randList[ballRandom.Next(randList.Count)];

      string shakingMessage = "*shooka shooka...*";
      if (beNice) shakingMessage += "_❤️..._";

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

    [Command("vore")]
    [Summary("Vores the given user.")]
    public async Task Vore(
      [Summary("The user to vore")] [Remainder]
      SocketGuildUser user)
    {
      if (!string.IsNullOrEmpty(user.Nickname))
        await Vore(user.Nickname);

      else
        await Vore(user.Username);
    }


    [Command("vore")]
    [Summary("Vores the given message.")]
    public async Task Vore(
      [Summary("The message to vore")] [Remainder] string msg)
    {
      if (msg.Length < 3)
      {
        await ReplyAsync("That doesn't look very tasty. No thanks. *(Character minimum of 3 not met.)*");
        return;
      }

      string voreEmoteRaw = "<:turtlevore:585605828283858944>";
      string stareEmoteRaw = "<:turtlestare:585542857218326579>";

      Emote voreEmoji;
      Emote stareEmoji;

      if (!Emote.TryParse(voreEmoteRaw, out voreEmoji) ||
          !Emote.TryParse(stareEmoteRaw, out stareEmoji))
      {
        await ReplyAsync("😢 Looking up the emojis failed...");
        return;
      }

      msg = msg.Replace("_", "\\_");

      string lengthValidationCopy = "";
      for (int i = msg.Length; i > 0; i--)
      {
        // TODO: Naive, breaks with multi-coded glyphs. LU C# utf-8 codepoint handling. 
        if (i < 4)
        {
          lengthValidationCopy += "_" + msg.Substring(0, i) + "_" + voreEmoteRaw;
        }
        else
        {
          lengthValidationCopy += msg.Substring(0, i - 3) + "_" + msg.Substring(i - 3, 3) + "_" + voreEmoteRaw;
        }
      }


      if (lengthValidationCopy.Length > 1900) //or so.
      {
        await ReplyAsync("What are you trying to make me vore? Your life story? No thanks. *(Character limit reached.)*");
        return;
      }
      else
      {
        string currMsg = msg + " " + stareEmoji + "\n";
        IUserMessage editableMessage = await ReplyAsync(currMsg);
        // put in an arbitrary delay between edits
        await Task.Delay(750);

        for (int i = msg.Length; i > 0; i--)
        {
          // TODO: Naive, breaks with multi-coded glyphs. LU C# utf-8 codepoint handling. 
          if (i < 4)
          {
            currMsg += "_" + msg.Substring(0, i) + "_ " + voreEmoji;
          }
          else
          {
            currMsg += msg.Substring(0, i - 3) + "_" + msg.Substring(i - 3, 3) + "_ " + voreEmoji;
          }
          currMsg += "\n";

          string copyCurrMsg = currMsg;
          await editableMessage.ModifyAsync(msgProps => msgProps.Content = copyCurrMsg);

          // put in an arbitrary delay between edits
          await Task.Delay(750);
        }

        currMsg += stareEmoji;
        await editableMessage.ModifyAsync(msgProps => msgProps.Content = currMsg);
      }
    }

  }

  class FunStorage
  {
    private static FirestoreDb _fs;

    public static CollectionReference Db => _fs.Collection("discord_bot");
    private static string[] _ballResultTypes = { "positives", "negatives", "neutrals" };


    public static async Task<List<List<string>>> Get8BallFor(ulong guildID)
    {
      CollectionReference channelCollection = Db.Document(Convert.ToString(guildID)).Collection("8ball");

      DocumentReference messages = channelCollection.Document("messages");
      DocumentSnapshot messagesSnapshot = await messages.GetSnapshotAsync();

      List<List<string>> returnable = new List<List<string>>();
      foreach (string btype in _ballResultTypes)
      {
        if (messagesSnapshot.TryGetValue(btype, out List<string> got)) returnable.Add(got);
        else returnable.Add(new List<string>());
      }

      return returnable;
    }

    public static async Task<bool> Insert8ballFor(ulong guildID, string btype, string msg)
    {
      try
      {
        CollectionReference channelCollection = Db.Document(Convert.ToString(guildID)).Collection("8ball");

        DocumentReference messages = channelCollection.Document("messages");
        DocumentSnapshot messagesSnapshot = await messages.GetSnapshotAsync();

        if (messagesSnapshot.TryGetValue(btype, out List<string> got))
        {
          got.Add(msg);
        }
        else got = new List<string>() { msg };
        Dictionary<string, List<string>> update = new Dictionary<string, List<string>> { [btype] = got };
        WriteResult wrire = await messages.SetAsync(update, SetOptions.MergeAll);

        return true;
      }
      catch
      {
        return false;
      }
    }

    public static void Setup(FirestoreDb firestore)
    {
      _fs = firestore;
    }
  }
}
