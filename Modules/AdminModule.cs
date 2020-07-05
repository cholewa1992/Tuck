using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Brothers.Modules
{
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("relay")]
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
    }
}