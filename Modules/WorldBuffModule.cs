using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord.Commands;
using Tuck.Model;
using System.Collections.Generic;
using Discord;
using Hangfire;
using Tuck.Services;

namespace Tuck.Modules
{
    [Group("wbuff")]
    public class WorldBuffModule : ModuleBase<SocketCommandContext>
    {
        private static List<string> _types = new List<string> {"rend", "ony", "nef", "zg"};
        private static Dictionary<BuffType, int> _cooldown = new Dictionary<BuffType, int> {
            {BuffType.Nef, 8},
            {BuffType.Ony, 6},
            {BuffType.Rend, 2}
        };

        private static Dictionary<BuffType, string> _icons = new Dictionary<BuffType, string> {
            {BuffType.Ony, "<:ony1:724665644641026068>"},
            {BuffType.Nef, "<:nef1:724665563967782923>"},
            {BuffType.Zg, "<:zg1:724665670402310194>"},
            {BuffType.Rend, "<:WCB1:724665604245684284>"},
        };

        [Command("list")]
        [RequireContext(ContextType.Guild)]
        public async Task PostBuffOverview() {
            using(var context = new TuckContext()) {
                await ReplyAsync(GetBuffPost(context, Context.Guild.Id));
            }
        }

        [Command("overview")]
        [RequireContext(ContextType.Guild)]
        public async Task PostBuffOverviewFromSubscribedGuilds() {
            using(var context = new TuckContext()) {
                var subscriptions = context.Subscriptions.AsQueryable()
                    .Where(s => s.GuildId == Context.Guild.Id)
                    .ToList();

                foreach(var subscription in subscriptions) {
                     await ReplyAsync(GetBuffPost(context, subscription.TargetGuildId));
                }
            }
        }

        [Command("add")]
        [RequireContext(ContextType.Guild)]
        public async Task RegisterBuff(BuffType type, DateTime time) {
            await RegisterBuff(type, time, Context.Guild.GetUser(Context.User.Id).Nickname);
        }  

        [Command("add")]
        [RequireContext(ContextType.Guild)]
        public async Task RegisterBuff(BuffType type, DateTime time, [Remainder] string username) {

            if(time < DateTime.Now) {
                await ReplyAsync($"On my watch the time is already {DateTime.Now.ToString("HH:mm")}. You can't add buffs earlier than that.");
                return;
            }

            using(var context = new TuckContext()) {

                var buff = new BuffInstance {
                    GuildId = Context.Guild.Id,
                    UserId = Context.User.Id,
                    Time = time,
                    Type = type,
                    Username = username
                };

                if(_cooldown.ContainsKey(buff.Type)) {

                    var from = buff.Time.AddHours(-_cooldown[buff.Type]);
                    var to = buff.Time.AddHours(_cooldown[buff.Type]);

                    var overlaps = context.Buffs.AsQueryable()
                        .Where(t => t.GuildId == Context.Guild.Id)
                        .Where(t => t.Type == buff.Type)
                        .Where(t => t.Time > from && t.Time < to)
                        .OrderBy(t => t.Time)
                        .ToList();
                    
                    if(overlaps.Count > 0) {
                        var warning = $"The buff you just added will clash with buffs already added:";
                        foreach(var overlap in overlaps) {
                            warning += $"\n> {overlap.Time.ToString("HH:mm")} by {overlap.Username} (<@{overlap.UserId}>)";
                        }
                        warning += $"\n\n To remove the buff you added, write !wbuff remove {buff.Type} {buff.Time.ToString("HH:mm")}";

                        var channel = await Context.User.GetOrCreateDMChannelAsync();
                        await channel.SendMessageAsync(warning);
                    }
                }

                // Adding a job for later to make notifications
                var notification = $"{_icons[buff.Type]} {buff.Type} is popping in {(buff.Time - DateTime.Now).Minutes} minutes by {buff.Username} @here";
                buff.JobId = NotificationService.PushNotification(buff.GuildId, notification, buff.Time - DateTime.Now - TimeSpan.FromMinutes(5));

                // Saving the buff instance
                await context.AddAsync(buff);
                await context.SaveChangesAsync();

                // Posting an update message in all subscribing channels
                var update  = $"{_icons[buff.Type]} {buff.Type} will be popped {buff.Time.ToString("dddd")} at {buff.Time.ToString("HH:mm")} by {buff.Username} (Added by <@{buff.UserId}>).";
                NotificationService.PushNotification(buff.GuildId, update);

                // Posting the updated schedule in this channel
                await ReplyAsync(GetBuffPost(context, buff.GuildId));
            }
        }

        [Command("remove")]
        [RequireContext(ContextType.Guild)]
        public async Task RemoveBuff(BuffType type, DateTime time) {
            using(var context = new TuckContext()) {
                var mathces = context.Buffs.AsQueryable()
                    .Where(t => t.GuildId == Context.Guild.Id)
                    .Where(t => t.Type == type && t.Time == time)
                    .ToList();

                foreach(var buff in mathces) {
                    NotificationService.CancelNotification(buff.JobId);
                    var update  = $"{_icons[buff.Type]} {buff.Type} that was planned for {buff.Time.ToString("dddd")} at {buff.Time.ToString("HH:mm")} has been cancelled by <@{Context.User.Id}>.";
                    NotificationService.PushNotification(buff.GuildId, update);
                }

                context.RemoveRange(mathces);
                await context.SaveChangesAsync();
                await ReplyAsync(GetBuffPost(context, Context.Guild.Id));
            }
        }

        [Command("subscribe")]
        [RequireContext(ContextType.Guild)]
        public async Task AddSubscription(ulong guildId) {
            using(var context = new TuckContext()) {
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
        public async Task Subscription(ulong guildId) {
            using(var context = new TuckContext()) {
                var mathces = context.Subscriptions.AsQueryable()
                    .Where(t => t.GuildId == Context.Guild.Id)
                    .Where(t => t.TargetGuildId == guildId);
                context.RemoveRange(mathces);
                await context.SaveChangesAsync();
                await ReplyAsync("The subscription was removed");
            }
        }

        [Command("clear")]
        [RequireContext(ContextType.Guild)]
        public async Task RemoveAllBuffs() {
            if(Context.User.Id == 103492791069327360) {
                using(var context = new TuckContext()) {
                    var mathces = context.Buffs.AsQueryable()
                        .Where(t => t.GuildId == Context.Guild.Id);

                    foreach(var buff in mathces) {
                        NotificationService.CancelNotification(buff.JobId);
                        var update  = $"{_icons[buff.Type]} {buff.Type} that was planned for {buff.Time.ToString("dddd")} at {buff.Time.ToString("HH:mm")} has been cancelled by <@{Context.User.Id}>.";
                        NotificationService.PushNotification(buff.GuildId, update);
                    }

                    context.RemoveRange(mathces);
                    await context.SaveChangesAsync();
                }
            }
        }

        private string GetBuffPost(TuckContext context, ulong guildId) {
            var buffs = context.Buffs.AsQueryable()
                .Where(t => t.GuildId == guildId && DateTime.Now < t.Time)
                .ToList();

            var msg = "**World buff schedule:**\n";
            if(buffs.Count() > 0) {
                foreach(var type in buffs.GroupBy(t => t.Type)) {
                    var times = type.OrderBy(t => t.Time).Select(e => $"{e.Time.ToString("HH:mm")} ({e.Username})");
                    msg += $"> {_icons[type.Key]} " + String.Join(", ", times) + "\n";
                }
            } else {
                msg += "> Nothing have been added yet...";
            }
            return msg;
        }
    }
}   