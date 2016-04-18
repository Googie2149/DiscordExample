using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;

namespace DiscordExample
{
    class StandardModule : IModule
    {
        private ModuleManager _m;

        public void Install(ModuleManager manager)
        {
            _m = manager;

            _m.CreateCommands("", g =>
            {
                g.MinPermissions((int)PermissionLevel.User);

                g.CreateCommand("say")
                .MinPermissions((int)PermissionLevel.BotOwner) // An unrestricted say command is a bad idea
                .Description("Make the bot speak!")
                .Parameter("text", ParameterType.Unparsed)
                .Do(async e =>
                {
                    await e.Channel.SendMessage(e.GetArg("text"));
                });

                g.CreateCommand("params")
                .Description("Multiple paramter test")
                .Parameter("text", ParameterType.Multiple)
                .Do(async e =>
                {
                    StringBuilder output = new StringBuilder();

                    output.AppendLine("Parameters:");

                    for (int i = 0; i < e.Args.Length; i++)
                    {
                        output.AppendLine($"{i}: {e.Args[i]}");
                    }

                    await e.Channel.SendMessage(output.ToString());
                });
            });
        }
    }
}
