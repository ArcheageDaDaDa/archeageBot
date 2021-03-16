using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ArcheageBot.Config;

namespace ArcheageBot
{
    class Program
    {
        private static DiscordSocketClient mClient;
        private static CommandService mCommands;
        private static IServiceProvider mServices;

        public static void Main(string[] args)
        {
            new Program().RunBotAsync().GetAwaiter().GetResult();
        }

        public async Task RunBotAsync()
        {
            mClient = new DiscordSocketClient();
            mCommands = new CommandService();

            string botToken = Config.Config.botToken;

            mClient.Log += _client_Log;

            await RegisterCommandsAsync();

            await mClient.LoginAsync(TokenType.Bot, botToken);

            await mClient.StartAsync();

            await Task.Delay(-1);
        }

        private static Task _client_Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        public static async Task RegisterCommandsAsync()
        {
            mClient.MessageReceived += HandleCommandAsync;
            await mCommands.AddModulesAsync(Assembly.GetEntryAssembly(), mServices);
        }

        private static async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(mClient, message);
            if (message.Author.IsBot) return;

            int argPos = 0;

            if (message.HasStringPrefix(";", ref argPos))
            {
                var result = await mCommands.ExecuteAsync(context, argPos, mServices);
                if (!result.IsSuccess)
                {
                    Console.WriteLine(result.ErrorReason);
                }
                else
                {
                    return;
                }

                if (result.Error == CommandError.Exception)
                {
                    await context.Channel.SendMessageAsync("Something broke =_=;;");
                }
                else
                {
                    await context.Message.AddReactionAsync(new Emoji("❌"));
                    await context.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
            else
            {

            }
        }
    }
}
