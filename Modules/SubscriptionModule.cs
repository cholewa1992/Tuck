using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
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

        [Command("alert")]
        [RequireContext(ContextType.Guild)]
        public async Task Alert(AlertAction action, SocketRole role) {
            using(var context = new TuckContext()) {
                Subscription subscription;
                try {
                    subscription = context.Subscriptions
                        .AsQueryable()
                        .Single(t => t.GuildId == Context.Guild.Id);
                } catch (InvalidOperationException) {
                    await ReplyAsync("You need to have an active subscription to enable alerts.");
                    return;
                }

                switch (action) {
                    case AlertAction.add:
                        await AddAlert(context, subscription, role.Id);
                        break;
                    case AlertAction.remove:
                        await RemoveAlert(context, subscription);
                        break;
                }
            }
        }

        private async Task AddAlert(TuckContext context, Subscription subscription, ulong role) {
            if (subscription.SubscriberAlert != null) {
                await ReplyAsync(string.Format(
                    "Replacing alerts for {0} with {1}.",
                    MentionUtils.MentionRole(subscription.SubscriberAlert ?? 0),
                    MentionUtils.MentionRole(role)
                ));
            } else {
                await ReplyAsync(string.Format(
                    "Enabled alerts for {0}.",
                    MentionUtils.MentionRole(role)
                ));
            }

            subscription.SubscriberAlert = role;
            context.Update(subscription);
            await context.SaveChangesAsync();
        }

        private async Task RemoveAlert(TuckContext context, Subscription subscription) {
            if (subscription.SubscriberAlert == null) {
                await ReplyAsync("No alert is currently active.");
                return;
            }

            subscription.SubscriberAlert = null;
            context.Update(subscription);
            await context.SaveChangesAsync();
        }
    }
}   