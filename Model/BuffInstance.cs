using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuck.Model
{
    public class BuffInstance
    { 
        public ulong Id { get; set; }
        public ulong UserId { get; set; }
        public string Username { get; set; }
        public ulong GuildId { get; set; }
        public DateTime Time { get; set; }
        public BuffType Type { get; set; }
        public bool Ninja { get; set; }

        [NotMapped]
        public bool Conflicting { get; set; }
    }
}   