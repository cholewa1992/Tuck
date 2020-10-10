using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Tuck;
using Tuck.Model;

namespace Brothers.Modules
{
    public class AdminModule : ModuleBase<SocketCommandContext>
    {

        [Command("channels")]
        [RequireContext(ContextType.DM)]
        public async Task Channels(ulong guildId) {
            if(Context.User.Id == 103492791069327360) {
                SocketGuild guild = Context.Client.GetGuild(guildId);
                foreach(var channel in guild.Channels){
                    await ReplyAsync($"{channel.Id} {channel.Name}");
                }
            }
        }

        [Command("messages")]
        [RequireContext(ContextType.DM)]
        public async Task Messages(ulong channelId) {
            if(Context.User.Id == 103492791069327360) {
                ISocketMessageChannel channel = Context.Client.GetChannel(channelId) as ISocketMessageChannel;
                var messages = new List<IMessage>();
                await channel.GetMessagesAsync().ForEachAsync(list => messages.AddRange(list));
                await DumpMessages(messages);
            }
        }

        private async Task DumpMessages(IReadOnlyCollection<IMessage> messages) {
             var stream = new MemoryStream();
             var sw = new StreamWriter(stream);
             foreach (var msg in messages)
             {
                 await sw.WriteLineAsync($"{msg.Source}: {msg.Content}");
                 await sw.FlushAsync();
             }
             stream.Seek(0, SeekOrigin.Begin);
             await Context.Channel.SendFileAsync(stream, "messages.txt");
         }

        [Command("id")]
        [RequireContext(ContextType.Guild)]
        public async Task Id() {
            await ReplyAsync($"The id of this server is {Context.Guild.Id}");
        }

        [Command("ping")]
        public async Task Ping() {
            var msg = await ReplyAsync("If you see this, then I'm missing edit message right");
            await msg.ModifyAsync(msg => msg.Content = "If you see this, then I'm missing react or external emojii right");
            var emoji = new Emoji("\uD83D\uDC4C");
            await msg.AddReactionAsync(emoji);
            await msg.ModifyAsync(msg => msg.Content = "pong!");
        }

        [Command("relay")]
        [RequireContext(ContextType.DM)]
        public async Task Relay(ulong channelId, [Remainder] string msg) {
            if(Context.User.Id == 103492791069327360) {
                ISocketMessageChannel channel = Context.Client.GetChannel(channelId) as ISocketMessageChannel;
                await channel.SendMessageAsync(msg);
            }
        }

        [Command("time")]
        public async Task Time() {
            if(Context.User.Id == 103492791069327360) {
                await ReplyAsync(DateTime.Now.ToString());
            }
        }

        [Command("subscribe")]
        [RequireContext(ContextType.DM)]
        public async Task AddSubscription(ulong guildId, ulong channelId, ulong targetId) {
            if(Context.User.Id == 103492791069327360) {
                using(var context = new TuckContext()) {

                    if(context.Subscriptions.AsQueryable().Where(s => s.GuildId == guildId && s.TargetGuildId == targetId).Any()) {
                            await ReplyAsync("A subscription already exists.");
                            return;
                    }

                    var subscription = new Subscription() {
                        GuildId = guildId,
                        ChannelId = channelId,
                        TargetGuildId = targetId
                    };

                    await context.AddAsync(subscription);
                    await context.SaveChangesAsync();
                    await ReplyAsync("The subscription was added");
                }
            }
        }
    }
}