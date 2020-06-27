using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Tuck.Services;
using Hangfire;
using Hangfire.LiteDB;

namespace Tuck
{
    class Program
    {
        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            using (var services = ConfigureServices())
            {
                GlobalConfiguration.Configuration
                    .UseLiteDbStorage("Filename=Hangfire.db;Mode=Exclusive")
                    .UseActivator(new HangfireActivator(services));

                BackgroundJob.Enqueue(() => Console.WriteLine("Up and running"));
    
                var engine = services.GetRequiredService<BackgroundJobServer>();
                var client = services.GetRequiredService<DiscordSocketClient>();

                client.Log += LogAsync;
                services.GetRequiredService<CommandService>().Log += LogAsync;

                await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DiscordToken"));
                await client.StartAsync();

                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
                await Task.Delay(Timeout.Infinite);              
            }
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<NotificationService>()
                .AddSingleton<BackgroundJobServer>()
                .AddSingleton<HttpClient>()
                .BuildServiceProvider();
        }
    }
}