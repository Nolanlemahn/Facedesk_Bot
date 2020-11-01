using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using FaceDesk_Bot.Permissions;

namespace FaceDesk_Bot.FD_MainModules
{
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private static Dictionary<string, Type> HelpPages = new Dictionary<string, Type>
        {
            ["announcement"] = typeof(AnnouncementsModule),
            ["fun"] = typeof(FunModule),
            ["help"] = typeof(HelpModule),
            ["permissions"] = typeof(GranularPermissionsModule),
            ["react"] = typeof(ReactModule),
            ["system"] = typeof(SystemModule),
            ["utility"] = typeof(UtilityModule)
        };

        private string SummaryFromCommand(CommandInfo command)
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
            for (int j = 1; j < paramCounter; j++)
            {
                realSummary += "+ param" + j + " ";
            }
            realSummary += ("`\n*" + command.Summary + "*" + "\n\n");

            return realSummary;
        }

        [Command("help")]
        [Summary("Brings up the list of help pages.")]
        public async Task Help()
        {
            string ap = "Available pages:\n";
            foreach (KeyValuePair<string, Type> kvp in HelpPages)
            {
                ap += kvp.Key + '\n';
            }
            await this.Context.Channel.SendMessageAsync(ap);
        }

        [Command("help")]
        [Summary("Brings up the list of help pages.")]
        public async Task Help([Remainder] string page)
        {
            ModuleInfo mi = EntryPoint.MainCommandService.Modules.FirstOrDefault(x => {
                if (HelpPages.ContainsKey(page))
                {
                    return x.Name.Equals(HelpPages[page].ToString().Split('.').Last());
                }
                return false;
            });

            if (mi != default)
            {
                var ebh = new EmbedBuilder();
                ebh.WithTitle($"FaceDesk_Bot Help ({mi.Name})");

                List<CommandInfo> commandCopy = mi.Commands.OrderBy(c => c.Name).ToList();
                foreach (CommandInfo command in commandCopy) ebh.AddField(command.Name, SummaryFromCommand(command));

                await this.Context.DebugPublicReleasePrivate("", false, ebh.Build());
            }
            else
            {
                await this.Context.Channel.SendMessageAsync("Module code not registered.");
            }
        }

        [Command("allhelp")]
        [Summary("Prints everything this bot can do.")]
        public async Task AllHelp([Summary("The page to pull")] int page)
        {
            List<CommandInfo> commandCopy = EntryPoint.MainCommandService.Commands.OrderBy(c => c.Name).ToList();

            var ebh = new EmbedBuilder();
            ebh.WithTitle("FaceDesk_Bot Command Help");
            ebh.WithDescription($"Here are all of the commands the bot is capable of doing.\n(Page {page} of {commandCopy.Count / 25 + 1})");

            for (int i = (page - 1) * 25; i < (page * 25); i++)
            {
                CommandInfo command = commandCopy.ElementAtOrDefault(i);
                if (command == default) continue;

                ebh.AddField(command.Name, SummaryFromCommand(command));
            }

            await this.Context.DebugPublicReleasePrivate("", false, ebh.Build());
        }
    }
}