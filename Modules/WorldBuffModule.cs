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

        private static Dictionary<BuffType, string> _names = new Dictionary<BuffType, string> {
            {BuffType.Ony, "Onyxia"},
            {BuffType.Nef, "Nefarian"},
            {BuffType.Zg, "Zandalar"},
            {BuffType.Rend, "Rend"}
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
                await ReplyAsync($"On my watch the time is already {DateTime.Now.ToString("HH:mm:ss")}. You can't add buffs earlier than that. If you ment to add it for tomorrow, then write {time:HH:mm}+1.");
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

                var conflict = false;
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
                        conflict = true;
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

                try {
                    await Context.Message.AddReactionAsync(Emote.Parse(_icons[buff.Type]));
                    if(conflict) {
                        await Context.Message.AddReactionAsync(new Emoji("⚠️"));
                    }
                } catch (Discord.Net.HttpException) {
                    // If custom emoji's returns an error, add a thumbs up instead.
                    await Context.Message.AddReactionAsync(new Emoji("👍"));
                }

                // Posting an update message in all subscribing channels
                await MakeNotification(context, buff.GuildId, GetBuffPost(context, buff.GuildId));
            }
        }

        [Command("ninja")]
        [RequireContext(ContextType.Guild)]
        public async Task RegisterNinjaPop(BuffType type, DateTime time) {
            var user = Context.Guild.GetUser(Context.User.Id);
            await RegisterNinjaPop(type, time, user.Nickname ?? user.Username);
        }  

        [Command("ninja")]
        [RequireContext(ContextType.Guild)]
        public async Task RegisterNinjaPop(BuffType type, DateTime time, [Remainder] string username) {

            if(time > DateTime.Now) {
                await ReplyAsync($"On my watch the time is already {DateTime.Now.ToString("HH:mm:ss")}. You can't add a ninja pop that didn't happen yet.");
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
                    Ninja = true,
                    Username = username
                };

                // Saving the buff instance
                await context.AddAsync(buff);
                await context.SaveChangesAsync();

                try {
                    await Context.Message.AddReactionAsync(Emote.Parse(_icons[buff.Type]));
                } catch (Discord.Net.HttpException) {
                    // If custom emoji's returns an error, add a thumbs up instead.
                    await Context.Message.AddReactionAsync(new Emoji("👍"));
                }

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
                    
                    try {
                        await Context.Message.AddReactionAsync(Emote.Parse(_icons[buff.Type]));
                    } catch (Discord.Net.HttpException) {
                        // If custom emoji's returns an error, add a thumbs up instead.
                        await Context.Message.AddReactionAsync(new Emoji("👍"));
                    }

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
                try {
                    if(subscription.LastMessage == null) await PostMessage(context, subscription, embed);
                    else await UpdateMessage(context, subscription, embed);
                    if (subscription.SubscriberAlert != null) await NotifySubscriber(context, subscription);
                } catch (Exception e) {
                    // Write in the log that posting to the channel failed.
                    var guild = Context.Client.GetGuild(subscription.GuildId);
                    Console.WriteLine($"Notification for guildId={subscription.GuildId}, guildName={guild?.Name ?? "unkown"}, owner={guild?.OwnerId.ToString() ?? "unkown"} failed.");
                    Console.WriteLine(e.Message);
                    if(guild == null){
                        await RemoveSubsciption(guild, subscription);
                    } 
                }
            }
        }

        private async Task RemoveSubsciption(SocketGuild guild, Subscription subscription) {
            using(var context = new TuckContext()) {
                // Remove the subscription that failed.
                context.Subscriptions.Remove(subscription);
                await context.SaveChangesAsync();

                // Notify in the bother tuck channel that an error happend.
                var channel = Context.Client.GetChannel(subscription.ChannelId) as ISocketMessageChannel;
                await ReplyAsync($"I removed the subscription for guildId={subscription.GuildId} because the guild no longer exists.");
		        Console.WriteLine($"Removed subscription with id={subscription.Id}");
            }
        }
        
        private async Task NotifySubscriber(TuckContext context, Subscription subscription) {
            var channel = Context.Client.GetChannel(subscription.ChannelId) as ISocketMessageChannel;
            var message = await channel.SendMessageAsync(string.Format(
                "{0}World buff list has been updated. ({1})",
                // SubscriberAlert can never be null here either.. So fallback is just to cast to int(64)
                MentionUtils.MentionRole(subscription.SubscriberAlert ?? 0) + ": ",
                // CultureInfo param should be refactored to EU/NA/etc when multi-realm support is added.
                DateTime.Now.ToString("HH:mm", new System.Globalization.CultureInfo("fr-FR"))
            ));

            // If a previous alert exists, delete it.
            ulong lastAlert = subscription.LastAlert ?? 0;
            if (lastAlert != 0) {
                var lastMsg = await channel.GetMessageAsync(lastAlert);
                if (lastMsg != null) await lastMsg.DeleteAsync();
            }

            // Store the new message Id.
            subscription.LastAlert = message.Id;
            context.Update(subscription);
            await context.SaveChangesAsync();
        }

        private async Task UpdateMessage(TuckContext context, Subscription subscription, Embed embed) {
            var channel = Context.Client.GetChannel(subscription.ChannelId) as ISocketMessageChannel;
            var message = await channel.GetMessageAsync(subscription.LastMessage.Value) as RestUserMessage;

            // Fallback incase message doesn't exist, create new one.
            if (message == null) await PostMessage(context, subscription, embed);
            else await message.ModifyAsync(msg => msg.Embed = embed);
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
            var builder = new EmbedBuilder ()
                .WithAuthor("World buff schedule", guild?.IconUrl)
                .WithFooter("Last updated")
                .WithCurrentTimestamp();

            var buffs = GetBuffs(context, guildId);
            var lastPops = new Dictionary<BuffType, DateTime>();

            foreach(var buff in buffs.OrderBy(t => t.Time)) {
                if(lastPops.ContainsKey(buff.Type) && _cooldown.ContainsKey(buff.Type) ) {
                    var lastPop = lastPops[buff.Type];
                    var cooldown = _cooldown[buff.Type];
                    buff.Conflicting = buff.Time < lastPop.AddHours(cooldown);
                }
                lastPops[buff.Type] = buff.Time;
            }
        
            var buffsByDay = buffs
                .Where(t => t.Time.Date >= DateTime.Today)
                .GroupBy(t => t.Time.Date)
                .OrderBy(g => g.Key);

            foreach(var day in buffsByDay) {
                builder.AddField($"{day.Key:dddd, dd MMMM}", GetBuffsAsString(day));
            }

            builder.AddField("\u200B", "https://discord.gg/NKNgEsp\u200B");
            return builder.Build();
        }

        private ICollection<BuffInstance> GetBuffs(TuckContext context, ulong guildId) {
            return context.Buffs.AsQueryable()
                .Where(t => t.GuildId == guildId && DateTime.Today.AddHours(-8) <= t.Time)
                .ToList();
        }

        private string GetBuffsAsString(IEnumerable<BuffInstance> buffs) {
            var buffMsg = buffs
                .OrderBy(t => t.Time).ThenBy(t => t.Type)
                .Select(t => $"> {GetBuffIcon(t)} {t.Time.ToString("HH:mm")} {_names[t.Type]} by {t.Username}")
                .Aggregate("", (s1,s2) => s1 + "\n" + s2);
            return string.IsNullOrEmpty(buffMsg) ? "> Nothing added" : buffMsg;
        }

        private string GetBuffIcon(BuffInstance buff) {
            if(buff.Ninja) return ":ninja:";
            if(buff.Conflicting) return ":warning:";
            return _icons[buff.Type];
        }
    }
}   
