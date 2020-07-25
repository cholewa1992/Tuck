using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord.Commands;
using Tuck.Model;
using System.Collections.Generic;
using Discord;
using System.IO;

namespace Tuck.Modules
{
    [Group("weffort")]
    public class WarEffortModule : ModuleBase<SocketCommandContext>
    {
        private static Dictionary<ItemType, string> _icons = new Dictionary<ItemType, string> {
            {ItemType.WoolBandage,"<:Woolbandage:727629457166172271>"},
            {ItemType.MageweaveBandage,"<:Mageweavebandage:727629746967412778>"},
            {ItemType.RuneclothBandage,"<:Runeclothbandage:727910336409108510>"},
            {ItemType.CopperBar,"<:Copperbar:727627789276020776>"},
            {ItemType.TinBar,"<:Tinbar:727628410473545749>"},
            {ItemType.MithrilBar,"<:Mithrilbar:727628534817751051>"},
            {ItemType.HeavyLeather,"<:Heavyleather:727628970320986247>"},
            {ItemType.ThickLeather,"<:Thickleather:727627987834634420>"},
            {ItemType.RuggedLeather,"<:Ruggedleather:727629141410709545>"},
            {ItemType.Peacebloom,"<:Peacebloom:727628699146387506>"},
            {ItemType.PurpleLotus,"<:PurpleLotus:727627871744688180>"},
            {ItemType.Firebloom,"<:Firebloom:727628840804810763>"},
            {ItemType.LeanWolfSteak,"<:LeanWolfSteak:727629243164393504>"},
            {ItemType.BakedSalmon,"<:Bakedsalmon:727629381324898314>"},
            {ItemType.SpottedYellowtail,"<:SpottedYellowtail:727628079584903228>"}
        };

        private static Dictionary<ItemType, string> _names = new Dictionary<ItemType, string> {
            {ItemType.WoolBandage,"Wool Bandage"},
            {ItemType.MageweaveBandage,"Mageweave Bandage"},
            {ItemType.RuneclothBandage,"Runecloth Bandage"},
            {ItemType.CopperBar,"Copper Bar"},
            {ItemType.TinBar,"Tin Bar"},
            {ItemType.MithrilBar,"Mithril Bar"},
            {ItemType.HeavyLeather,"Heavy Leather"},
            {ItemType.ThickLeather,"Thick Leather"},
            {ItemType.RuggedLeather,"Rugged Leather"},
            {ItemType.Firebloom,"Firebloom"},
            {ItemType.PurpleLotus,"Purple Lotus"},
            {ItemType.Peacebloom,"Peacebloom"},
            {ItemType.LeanWolfSteak,"Lean Wolf Steak"},
            {ItemType.BakedSalmon,"Baked Salmon"},
            {ItemType.SpottedYellowtail,"Spotted Yellowtail"}
        };

        private static Dictionary<ItemType, uint> _quotas = new Dictionary<ItemType, uint> {
            {ItemType.WoolBandage, 250000},
            {ItemType.MageweaveBandage, 250000},
            {ItemType.RuneclothBandage, 400000},
            {ItemType.CopperBar, 90000},
            {ItemType.TinBar, 22000},
            {ItemType.MithrilBar, 18000},
            {ItemType.HeavyLeather, 60000},
            {ItemType.ThickLeather, 80000},
            {ItemType.RuggedLeather, 60000},
            {ItemType.Peacebloom, 96000},
            {ItemType.PurpleLotus, 26000},
            {ItemType.Firebloom, 19000},
            {ItemType.LeanWolfSteak, 10000},
            {ItemType.BakedSalmon, 10000},
            {ItemType.SpottedYellowtail, 17000},
        };

        [Command("help")]
        [RequireContext(ContextType.Guild)]
        public async Task Help() {
            using(var context = new TuckContext()) {

                await Context.Message.DeleteAsync();
                var channel = await Context.User.GetOrCreateDMChannelAsync();

                var types =  Enum.GetValues(typeof(ItemType))
                    .Cast<ItemType>()
                    .Select(t => $"\n> {t.ToString()}")
                    .Aggregate("", (s1,s2) => s1 + s2);

                await channel.SendMessageAsync($"Hi {Context.User.Username}!\nI'm here to help with tallying up materials for the wareffort. You can register your contribution to the war effort by writing !weffort add [item] [amount].\n\nI support the following types: {types}");
            }
        }

        [Command("add")]
        [RequireContext(ContextType.Guild)]
        public async Task AddContribution(ItemType itemType, uint amount) {
            using(var context = new TuckContext()) {

                var user = Context.Guild.GetUser(Context.User.Id);
                var guild = Context.Guild;

                var contribution = context.Contributions.AsQueryable()
                    .Where(s => s.GuildId == guild.Id && s.UserId == user.Id && s.ItemType == itemType)
                    .FirstOrDefault();

                if (contribution == null){
                    await CreateContribution(context, user, guild, itemType, amount);
                } else {
                    await UpdateContribution(context, contribution, contribution.Amount + amount);
                }
            }
        }

        [Command("remove")]
        [RequireContext(ContextType.Guild)]
        public async Task UpdateContribution(ItemType itemType, uint amount) {
            using(var context = new TuckContext()) {

                var user = Context.Guild.GetUser(Context.User.Id);
                var guild = Context.Guild;

                var contribution = context.Contributions.AsQueryable()
                    .Where(s => s.GuildId == guild.Id && s.UserId == user.Id && s.ItemType == itemType)
                    .FirstOrDefault();

                if (contribution != null && contribution.Amount > amount) {
                    await UpdateContribution(context, contribution, contribution.Amount - amount);
                } else if (contribution != null) {
                    await RemoveContribution(context, contribution);
                }
            }
        }

        [Command("remove")]
        [RequireContext(ContextType.Guild)]
        public async Task RemoveContribution(ItemType itemType) {
            using(var context = new TuckContext()) {

                var user = Context.Guild.GetUser(Context.User.Id);
                var guild = Context.Guild;

                var contribution = context.Contributions.AsQueryable()
                    .Where(s => s.GuildId == guild.Id && s.UserId == user.Id && s.ItemType == itemType)
                    .FirstOrDefault();

                if(contribution != null) {
                    await RemoveContribution(context, contribution);
                }
            }
        }

        [Command("overview")]
        [RequireContext(ContextType.Guild)]
        public async Task OverviewGuild() {
            using(var context = new TuckContext()) {
                var contributions = context.Contributions.AsQueryable()
                    .Where(s => s.GuildId == Context.Guild.Id)
                    .ToList()
                    .GroupBy(t => t.ItemType)
                    .ToDictionary(x => x.Key, x => x.Sum((t => t.Amount)));

                var embed = BuildOverview(contributions)
                    .WithAuthor($"{Context.Guild.Name} overview", Context.Guild?.IconUrl);

                await ReplyAsync("", false, embed.Build());
            }
        }

        [Command("overview server")]
        [RequireContext(ContextType.Guild)]
        public async Task OverviewServer() {
          using(var context = new TuckContext()) {
                var contributions = context.Contributions.AsQueryable()
                    .ToList()
                    .GroupBy(t => t.ItemType)
                    .ToDictionary(x => x.Key, x => x.Sum((t => t.Amount)));

                var embed = BuildOverview(contributions)
                    .WithAuthor("Server overview", Context.Guild?.IconUrl);

                await ReplyAsync("", false, embed.Build());
            }
        }

        private EmbedBuilder BuildOverview(Dictionary<ItemType, long> contributions) {
            var builder = new EmbedBuilder ()
                .WithFooter("Last updated")
                .WithCurrentTimestamp();

            var total = 0m;
            foreach(var type in (ItemType[]) Enum.GetValues(typeof(ItemType))) {  
                var icon = _icons[type];
                var name = _names[type];
                var quota = _quotas[type];
                var sum = contributions.GetValueOrDefault(type);
                var progress = (decimal) sum / quota;
                total += progress < 1 ? progress : 1; 
                builder.AddField($"{icon} {name}", $"{sum:N0} / {quota:N0} = {progress:P0}", true);
            }

            builder.AddField($"Total", $"{total/15:P3}");
            return builder;
        }

        [Command("export")]
        [RequireContext(ContextType.Guild)]
        public async Task OverviewDetailed() {
            using(var context = new TuckContext()) {
                var contributions = context.Contributions.AsQueryable()
                    .Where(s => s.GuildId == Context.Guild.Id)
                    .ToList();
                await SendCsvFile(contributions);
            }
        }

        [Command("export server")]
        [RequireContext(ContextType.Guild)]
        public async Task OverviewServerDetailed() {
            using(var context = new TuckContext()) {
                var contributions = await context.Contributions.ToListAsync();
                await SendCsvFile(contributions);
            }
        }

        private async Task SendCsvFile(List<WarEffortContribution> contributions) {

            var guildIds = contributions.Select(t => t.GuildId).ToHashSet();
            var guilds = guildIds.ToDictionary(t => t, t => Context.Client.GetGuild(t));
            
            var stream = new MemoryStream();
            var sw = new StreamWriter(stream);
            await sw.WriteLineAsync($"guild, username, itemtype, amount");
            foreach (var c in contributions.OrderBy(s => s.GuildId).ThenBy(s => s.Username))
            {
                Console.WriteLine($"{guilds[c.GuildId]?.Name ?? c.GuildId.ToString()}, {c.Username}, {c.ItemType}, {c.Amount}");
                await sw.WriteLineAsync($"{guilds[c.GuildId]?.Name ?? c.GuildId.ToString()}, {c.Username}, {c.ItemType}, {c.Amount}");
                await sw.FlushAsync();
            }
            stream.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(stream, "contributions.csv");
        }

        public async Task CreateContribution(TuckContext context, Discord.WebSocket.SocketGuildUser user, Discord.WebSocket.SocketGuild guild, ItemType itemType, uint amount) {
            var contribution = new WarEffortContribution(){
                UserId = user.Id,
                GuildId = guild.Id,
                Username = user.Nickname ?? user.Username,
                ItemType = itemType,
                Amount = amount
            };
            await context.AddAsync(contribution);
            await context.SaveChangesAsync();
            await ReplyAsync($"{_icons[itemType]} Your contribution of {amount:N0} x {_names[itemType]} have been registered");

        }

        private async Task UpdateContribution(TuckContext context, WarEffortContribution existing, uint amount) {
            existing.Amount = amount;
            context.Update(existing);
            await context.SaveChangesAsync();
            await ReplyAsync($"{_icons[existing.ItemType]} Your contribution has been updated to {amount:N0} x {_names[existing.ItemType]}");
        }

        private async Task RemoveContribution(TuckContext context, WarEffortContribution existing) {
            context.Remove(existing);
            await context.SaveChangesAsync();
            await ReplyAsync($"{_icons[existing.ItemType]} Your contribution of {_names[existing.ItemType]} has been removed");
        }

        private uint Positive(int integer) {
            return integer > 0 ? (uint) integer : 0;
        }
    }
}   