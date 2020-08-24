using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord.Commands;
using Tuck.Model;

namespace Tuck.Modules
{
    [Group("wbuff")]
    public class SubscriptionModule : ModuleBase<SocketCommandContext>
    {
        [Command("subscribe")]
        [RequireContext(ContextType.Guild)]
        public async Task AddSubscription(ulong guildId) {
            using(var context = new TuckContext()) {
                
                if(context.Subscriptions.AsQueryable().Where(s => s.GuildId == Context.Guild.Id).Any()) {
                     await ReplyAsync("The subscription was **not** added. A subscription already exists. Do unsubscribe to change the server subscribe too.");
                     return;
                }

                var subscription = new Subscription() {
                    GuildId = Context.Guild.Id,
                    ChannelId = Context.Channel.Id,
                    TargetGuildId = guildId
                };

                await context.AddAsync(subscription);
                await context.SaveChangesAsync();
                await ReplyAsync("The subscription was added");
            }
        }

        [Command("unsubscribe")]
        [RequireContext(ContextType.Guild)]
        public async Task Subscription() {
            using(var context = new TuckContext()) {
                var matches = context.Subscriptions.AsQueryable().Where(t => t.GuildId == Context.Guild.Id);
                context.RemoveRange(matches);
                await context.SaveChangesAsync();
                await ReplyAsync("The subscription was removed");
            }
        }
    }
}   