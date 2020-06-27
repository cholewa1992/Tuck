using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using System.Collections.Generic;

namespace Tuck.Model
{
    public class Subscription
    { 
        public ulong Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong TargetGuildId { get; set; }
    }
}   