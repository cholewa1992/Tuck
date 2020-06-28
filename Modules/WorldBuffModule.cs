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
        [Alias("overview")]
        [RequireContext(ContextType.Guild)]
        public async Task PostBuffOverview() {
            using(var context = new TuckContext()) {
                var subscription = context.Subscriptions.AsQueryable()
                    .Where(s => s.GuildId == Context.Guild.Id)
                    .FirstOrDefault();

                await ReplyAsync(GetBuffPost(context, subscription?.TargetGuildId ?? Context.Guild.Id));
            }
        }

        [Command("add")]
        [RequireContext(ContextType.Guild)]
        public async Task RegisterBuff(BuffType type, DateTime time) {
            var user = Context.Guild.GetUser(Context.User.Id);
            await RegisterBuff(type, time, user.Nickname ?? user.Username);
        }  

        [Command("add")]
        [RequireContext(ContextType.Guild)]
        public async Task RegisterBuff(BuffType type, DateTime time, [Remainder] string username) {

            if(time < DateTime.Now) {
                await ReplyAsync($"On my watch the time is already {DateTime.Now.ToString("HH:mm")}. You can't add buffs earlier than that.");
                return;
            }

            using(var context = new TuckContext()) {

                var subscription = context.Subscriptions.AsQueryable()
                    .Where(s => s.GuildId == Context.Guild.Id)
                    .Any();

                if(subscription) {
                    await ReplyAsync("You cannot add buffs if a subscription exists");
                    return;
                }

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
                var notification = $"{_icons[buff.Type]} {buff.Type} is being popped at {buff.Time.ToString("HH:mm")} by {buff.Username}";
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
                var match = context.Buffs.AsQueryable()
                    .Where(t => t.GuildId == Context.Guild.Id)
                    .Where(t => t.Type == type && t.Time == time)
                    .FirstOrDefault();

                if(match != null){
                    NotificationService.CancelNotification(match.JobId);
                    var update  = $"{_icons[match.Type]} {match.Type} that was planned for {match.Time.ToString("dddd")} at {match.Time.ToString("HH:mm")} has been cancelled by <@{Context.User.Id}>.";
                    NotificationService.PushNotification(match.GuildId, update);
                    context.Remove(match);
                    await context.SaveChangesAsync();
                    await ReplyAsync(GetBuffPost(context, Context.Guild.Id));
                }
            }
        }

        private string GetBuffPost(TuckContext context, ulong guildId) {
            var buffs = context.Buffs.AsQueryable()
                .Where(t => t.GuildId == guildId && DateTime.Now < t.Time)
                .OrderBy(t => t.Time).ThenBy(t => t.Type)
                .ToList()
                .Select(t => $"\n> {_icons[t.Type]} {t.Time.ToString("HH:mm")} by {t.Username}")
                .Aggregate("", (s1,s2) => s1 + s2);

            return "**World buff schedule:**" + 
                (string.IsNullOrEmpty(buffs) ? "\n> Nothing have been added yet..." : buffs);
        }
    }
}   