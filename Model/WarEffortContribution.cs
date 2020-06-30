using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using System.Collections.Generic;

namespace Tuck.Model
{
    public class WarEffortContribution
    { 
        public ulong Id { get; set; }
        public ulong UserId { get; set; }
        public string Username { get; set; }
        public ulong GuildId { get; set; }
        public ItemType ItemType { get; set; }
        public uint Amount { get; set; }
    }
}   