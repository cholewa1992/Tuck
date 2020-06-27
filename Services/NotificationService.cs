using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Tuck.Model;

namespace Tuck.Services
{
    public class NotificationService
    {
        DiscordSocketClient _client;

        public NotificationService(IServiceProvider services) {
            _client = services.GetRequiredService<DiscordSocketClient>();
        }

        public static string PushNotification(ulong guildId, string msg) {
            return BackgroundJob.Enqueue<NotificationService>(ns => ns.HandleNotification(guildId, msg));
        }

        public static string PushNotification(ulong guildId, string msg, TimeSpan time) {
            return BackgroundJob.Schedule<NotificationService>(ns => ns.HandleNotification(guildId, msg), time);
        }

        public static bool CancelNotification(string jobId) {
            return jobId != null && BackgroundJob.Delete(jobId);
        }
        
        private IEnumerable<Subscription> getSubscriptions(ulong guildId){
             using(var context = new TuckContext()){
                return context.Subscriptions.AsQueryable()
                    .Where(s => s.TargetGuildId == guildId)
                    .ToList();
             }
        }

        public async Task HandleNotification(ulong guildId, string msg) {
            foreach(var subscription in getSubscriptions(guildId)) {
                var channel = _client.GetChannel(subscription.ChannelId) as ISocketMessageChannel;
                await channel.SendMessageAsync(msg);
            }
        }
    }
}