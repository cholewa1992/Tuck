using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord.Commands;
using Tuck.Model;
using System.Collections.Generic;
using Discord.WebSocket;
using Discord;
using Discord.Rest;

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
            {BuffType.Rend, "<:WCB1:724665604245684284>"}
        };

        [Command("list")]
        [Alias("overview")]
        [RequireContext(ContextType.Guild)]
        public async Task PostBuffOverview() {
            using(var context = new TuckContext()) {
                var subscription = context.Subscriptions.AsQueryable()
                    .Where(s => s.GuildId == Context.Guild.Id)
                    .FirstOrDefault();

                var embed = GetBuffPost(context, subscription?.TargetGuildId ?? Context.Guild.Id);
                var msg  = await ReplyAsync("", false, embed);

                if(subscription != null && Context.Channel.Id == subscription.ChannelId) { 
                    subscription.LastMessage = msg.Id;
                    context.Update(subscription);
                    await context.SaveChangesAsync();
                }
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
                    .Where(s => s.GuildId == Context.Guild.Id && s.TargetGuildId != s.GuildId)
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

                // Saving the buff instance
                await context.AddAsync(buff);
                await context.SaveChangesAsync();
                await Context.Message.AddReactionAsync(Emote.Parse(_icons[buff.Type]));

                // Posting an update message in all subscribing channels
                await MakeNotification(context, buff.GuildId, GetBuffPost(context, buff.GuildId));
            }
        }

        [Command("remove")]
        [RequireContext(ContextType.Guild)]
        public async Task RemoveBuff(BuffType type, DateTime time) {
            using(var context = new TuckContext()) {
                var buff = context.Buffs.AsQueryable()
                    .Where(t => t.GuildId == Context.Guild.Id)
                    .Where(t => t.Type == type && t.Time == time)
                    .FirstOrDefault();

                if(buff != null){
                    context.Remove(buff);
                    await context.SaveChangesAsync();
                    await Context.Message.AddReactionAsync(Emote.Parse(_icons[buff.Type]));

                    // Posting an update message in all subscribing channels
                    await MakeNotification(context, buff.GuildId, GetBuffPost(context, Context.Guild.Id));
                }
            }
        }

        private async Task MakeNotification(TuckContext context, ulong guildId, Embed embed) {
            var subscriptions = context.Subscriptions.AsQueryable()
                    .Where(s => s.TargetGuildId == guildId)
                    .ToList();

            foreach(var subscription in subscriptions) {
                try{
                    if(subscription.LastMessage == null) await PostMessage(context, subscription, embed);
                    else await UpdateMessage(context, subscription, embed);
                } catch (Exception e) {
                    var guild = Context.Client.GetGuild(subscription.GuildId);
                    Console.WriteLine($"Notification for guildId={subscription.GuildId}, guildName={guild.Name}, owner={guild.OwnerId} failed.");
                    Console.WriteLine(e.Message);
                }
            }
        }

        private async Task UpdateMessage(TuckContext context, Subscription subscription, Embed embed) {
            var channel = Context.Client.GetChannel(subscription.ChannelId) as ISocketMessageChannel;
            var message = await channel.GetMessageAsync(subscription.LastMessage.Value) as RestUserMessage;
            await message.ModifyAsync(msg => msg.Embed = embed);
        }

        private async Task PostMessage(TuckContext context, Subscription subscription, Embed embed) {
            var channel = Context.Client.GetChannel(subscription.ChannelId) as ISocketMessageChannel;
            var message = await channel.SendMessageAsync("", false, embed);
            subscription.LastMessage = message.Id;
            context.Update(subscription);
            await context.SaveChangesAsync();
        }

        private Embed GetBuffPost(TuckContext context, ulong guildId) {
            var guild = Context.Client.GetGuild(guildId);
            return new EmbedBuilder ()
                .WithAuthor("World buff schedule", guild?.IconUrl)
                .AddField(GetBuffsToday(context, guildId))
                .AddField(GetBuffsTomorrow(context, guildId))
                .AddField("\u200B", "The world boss schedule is moderated by the guild masters and officers of Dreadmist. To queue a buff, reach our to one of the officers in your guild and have them add it in the global discord channel.\u200B")
                .WithFooter("Last updated")
                .WithCurrentTimestamp()
                .Build();
        }

        private EmbedFieldBuilder GetBuffsToday(TuckContext context, ulong guildId) {
            var buffs = context.Buffs.AsQueryable()
                .Where(t => t.GuildId == guildId && DateTime.Today == t.Time.Date)
                .OrderBy(t => t.Time).ThenBy(t => t.Type)
                .ToList();
            return new EmbedFieldBuilder {
                Name = "Today",
                Value = GetBuffsAsString(buffs),
                IsInline = true
            };
        }

        private EmbedFieldBuilder GetBuffsTomorrow(TuckContext context, ulong guildId) {
            var buffs = context.Buffs.AsQueryable()
                .Where(t => t.GuildId == guildId && DateTime.Today.AddDays(1) == t.Time.Date)
                .OrderBy(t => t.Time).ThenBy(t => t.Type)
                .ToList();
            return new EmbedFieldBuilder {
                Name = "Tomorrow",
                Value = GetBuffsAsString(buffs),
                IsInline = true
            };
        }

        private string GetBuffsAsString(IEnumerable<BuffInstance> buffs) {
            var buffMsg = buffs
                .Select(t => $"> {_icons[t.Type]} {t.Time.ToString("HH:mm")} by {t.Username}")
                .Aggregate("", (s1,s2) => s1 + "\n" + s2);
            return string.IsNullOrEmpty(buffMsg) ? "> Noting added" : buffMsg;
        }
    }
}   