using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

using FaceDesk_Bot.Permissions;

namespace FaceDesk_Bot.FD_MainModules
{
  class SystemModule : ModuleBase<SocketCommandContext>
  {
    [Command("memory")]
    [Alias("ram")]
    [Summary("Shows RAM usage.")]
    public async Task Memory()
    {
      var proc = Process.GetCurrentProcess();
      var mem = proc.WorkingSet64;
      await this.Context.Channel.SendMessageAsync(string.Format("🖥️ Using {0:n3} MB", mem / 1024.0 / 1024.0));
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
    }

    [Command("gg")]
    [Summary("**Owner only**. Kills the bot.")]
    public async Task Gg()
    {
      bool result = await this.Context.IsOwner(); if (!result) return;

      await this.Context.Channel.SendMessageAsync("😭... Okay, bye for now! 👋");
      Environment.Exit(0);
    }

    [Command("owners")]
    [Summary("**Owner only**. DMs invoker with list of owners.")]
    public async Task Owners()
    {
      bool result = await this.Context.IsOwner(); if (!result) return;

      List<ulong> owners = SimplePermissions.Owners;

      string msg = "Owners:\n";
      foreach (ulong owner in owners)
      {
        msg += "<@!" + owner + ">\n";
      }

      await this.Context.User.SendMessageAsync(msg);
    }
  }
}