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
                g.CreateCommand("say")
                .MinPermissions((int)PermissionLevel.BotOwner)
                .Description("Make the bot speak!")
                .Parameter("text", ParameterType.Unparsed)
                .Do(async e =>
                {
                    await e.Channel.SendMessage(e.GetArg("text"));
                });
            });
        }
    }
}
