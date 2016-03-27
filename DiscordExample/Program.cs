using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;

namespace DiscordExample
{
    class Program
    {
        public static void Main(string[] args) => new Program().Start(args);

        private const string AppName = "DiscordExample"; // Change this to the name of your bot

        private DiscordClient _client;

        private Regex invite = new Regex(@"https?:\/\/discord.gg\/([a-zA-Z0-9\-]+)", RegexOptions.Compiled);

        private void Start(string[] args)
        {
            _client = new DiscordClient(x =>
            {
                x.AppName = AppName;
                x.MessageCacheSize = 10;
                x.EnablePreUpdateEvents = true;
            })
            .UsingCommands(x =>
            {
                x.AllowMentionPrefix = true;
                x.HelpMode = HelpMode.Public;
            })
            .UsingPermissionLevels((u, c) => (int)GetPermissions(u, c))
            .UsingModules();

            _client.AddModule<StandardModule>("Standard", ModuleFilter.None);

            _client.MessageReceived += async (s, e) =>
            {
                //if (!e.Message.IsAuthor)
                //    await e.Channel.SendMessage($"{e.User.Name} said: {e.Message.RawText}");

                if (!e.Message.IsAuthor && e.Channel.IsPrivate)
                {
                    MatchCollection mc = invite.Matches(e.Message.Text.Replace("://discordapp.com/invite/", "://discord.gg"));

                    foreach (Match m in mc)
                    {
                        Invite inv = null;

                        try
                        {
                            inv = await _client.GetInvite(m.Groups[0].Value);
                        }
                        catch (Discord.Net.HttpException ex)
                        {
                            if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                                await e.Channel.SendMessage("I'm banned from that server.");

                            continue;
                        }

                        if (inv != null && !inv.IsRevoked && inv.Server != null)
                        {

                            if (!_client.Servers.Any(x => x.Id == inv.Server.Id))
                            {
                                await inv.Accept();
                                await e.Channel.SendMessage("Joined!");
                            }
                            else
                                await e.Channel.SendMessage("Already joined.");
                        }
                        else
                            await e.Channel.SendMessage("Invalid invite.");
                    }
                }
            };
            
            _client.ExecuteAndWait(async () =>
            {
                await _client.Connect("email", "password");
            });
        }

        private static PermissionLevel GetPermissions(User u, Channel c)
        {
            if (u.Id == 0000) // Replace this with your own UserId
                return PermissionLevel.BotOwner;

            if (!c.IsPrivate)
            {
                if (u == c.Server.Owner)
                    return PermissionLevel.ServerOwner;

                var serverPerms = u.ServerPermissions;
                if (serverPerms.ManageRoles || u.Roles.Select(x => x.Name.ToLower()).Contains("bot commander"))
                    return PermissionLevel.ServerAdmin;
                if (serverPerms.ManageMessages && serverPerms.KickMembers && serverPerms.BanMembers)
                    return PermissionLevel.ServerModerator;

                var channelPerms = u.GetPermissions(c);
                if (channelPerms.ManagePermissions)
                    return PermissionLevel.ChannelAdmin;
                if (channelPerms.ManageMessages)
                    return PermissionLevel.ChannelModerator;
            }
            return PermissionLevel.User;
        }
    }
}
