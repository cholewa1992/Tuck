using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Tuck;
using System.Linq;

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
    }
}