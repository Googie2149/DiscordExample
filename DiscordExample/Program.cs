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
                // x.PrefixChar = '!';
                // Please don't use !, there's a million bots that already do.
                x.HelpMode = HelpMode.Public;
                x.ExecuteHandler += (s, e) => _client.Log.Info("Command", $"[{((e.Server != null) ? e.Server.Name : "Private")}{((!e.Channel.IsPrivate) ? $"/#{e.Channel.Name}" : "")}] <@{e.User.Name}> {e.Command.Text} {((e.Args.Length > 0) ? "| " + string.Join(" ", e.Args) : "")}");
                x.ErrorHandler = CommandError;
            })
            .UsingPermissionLevels((u, c) => (int)GetPermissions(u, c))
            .UsingModules();

            _client.Log.Message += (s, e) => WriteLog(e);
            _client.MessageReceived += (s, e) => 
            {
                if (e.Message.IsAuthor)
                    _client.Log.Info("<<Message", $"[{((e.Server != null) ? e.Server.Name : "Private")}{((!e.Channel.IsPrivate) ? $"/#{e.Channel.Name}" : "")}] <@{e.User.Name}> {e.Message.Text}");
                else
                    _client.Log.Info(">>Message", $"[{((e.Server != null) ? e.Server.Name : "Private")}{((!e.Channel.IsPrivate) ? $"/#{e.Channel.Name}" : "")}] <@{e.User.Name}> {e.Message.Text}");
            };

            _client.AddModule<StandardModule>("Standard", ModuleFilter.None);
            
            _client.ExecuteAndWait(async () =>
            {
                await _client.Connect("token");

                _client.Log.Info("Connected", $"Connected as {_client.CurrentUser.Name} (Id {_client.CurrentUser.Id})");
            });
        }

        private static PermissionLevel GetPermissions(User u, Channel c)
        {
            if (u.Id == 102528327251656704) // Replace this with your own UserId
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

        private async void CommandError(object sender, CommandErrorEventArgs e)
        {
            if (e.ErrorType == CommandErrorType.Exception)
            {
                _client.Log.Error("Command", e.Exception);
                await e.Channel.SendMessage($"Error: {e.Exception.GetBaseException().Message}");
            }
            else if (e.ErrorType == CommandErrorType.BadPermissions)
            {
                if (e.Exception?.Message == "This module is currently disabled.")
                {
                    await e.Channel.SendMessage($"The `{e.Command?.Category}` module is currently disabled.");
                    return;
                }
                else if (e.Exception != null)
                {
                    await e.Channel.SendMessage(e.Exception.Message);
                    return;
                }

                if (e.Command?.IsHidden == true)
                    return;

                await e.Channel.SendMessage($"You don't have permission to access that command!");
            }
            else if (e.ErrorType == CommandErrorType.BadArgCount)
            {
                await e.Channel.SendMessage("Error: Invalid parameter count.");
            }
            else if (e.ErrorType == CommandErrorType.InvalidInput)
            {
                await e.Channel.SendMessage("Error: Invalid input! Make sure your quotes match up correctly!");
            }
            else if (e.ErrorType == CommandErrorType.UnknownCommand)
            {
                // Only set up a response in here if you stick with a mention prefix
            }
        }

        private void WriteLog(LogMessageEventArgs e)
        {
            //Color
            ConsoleColor color;
            switch (e.Severity)
            {
                case LogSeverity.Error: color = ConsoleColor.Red; break;
                case LogSeverity.Warning: color = ConsoleColor.Yellow; break;
                case LogSeverity.Info: color = ConsoleColor.White; break;
                case LogSeverity.Verbose: color = ConsoleColor.Gray; break;
                case LogSeverity.Debug: default: color = ConsoleColor.DarkGray; break;
            }

            //Exception
            string exMessage;
            Exception ex = e.Exception;
            if (ex != null)
            {
                while (ex is AggregateException && ex.InnerException != null)
                    ex = ex.InnerException;
                exMessage = $"{ex.Message}";
                if (exMessage != "Reconnect failed: HTTP/1.1 503 Service Unavailable")
                    exMessage += $"\n{ex.StackTrace}";
            }
            else
                exMessage = null;

            //Source
            string sourceName = e.Source?.ToString();

            //Text
            string text;
            if (e.Message == null)
            {
                text = exMessage ?? "";
                exMessage = null;
            }
            else
                text = e.Message;

            if (sourceName == "Command")
                color = ConsoleColor.Cyan;
            else if (sourceName == "<<Message")
                color = ConsoleColor.Green;


            //Build message
            StringBuilder builder = new StringBuilder(text.Length + (sourceName?.Length ?? 0) + (exMessage?.Length ?? 0) + 5);
            if (sourceName != null)
            {
                builder.Append('[');
                builder.Append(sourceName);
                builder.Append("] ");
            }
            builder.Append($"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}] ");
            for (int i = 0; i < text.Length; i++)
            {
                //Strip control chars
                char c = text[i];
                if (c == '\n' || !char.IsControl(c) || c != (char)8226) // (char)8226 beeps like \a, this catches that
                    builder.Append(c);
            }
            if (exMessage != null)
            {
                builder.Append(": ");
                builder.Append(exMessage);
            }

            text = builder.ToString();
            if (e.Severity <= LogSeverity.Info)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(text);
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine(text);
#endif
        }
    }
}
