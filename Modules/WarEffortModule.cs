using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord.Commands;
using Tuck.Model;
using System.Collections.Generic;
using Discord;

namespace Tuck.Modules
{
    [Group("weffort")]
    public class WarEffortModule : ModuleBase<SocketCommandContext>
    {
        private static Dictionary<ItemType, string> _icons = new Dictionary<ItemType, string> {
            {ItemType.WoolBandage,"<:Woolbandage:727629457166172271>"},
            {ItemType.TinBar,"<:Tinbar:727628410473545749>"},
            {ItemType.ThickLeather,"<:Thickleather:727627987834634420>"},
            {ItemType.SpottedYellowtail,"<:SpottedYellowtail:727628079584903228>"},
            {ItemType.RuneclothBandage,"<:Runeclothbandage:727910336409108510>"},
            {ItemType.RuggedLeather,"<:Ruggedleather:727629141410709545>"},
            {ItemType.PurpleLotus,"<:PurpleLotus:727627871744688180>"},
            {ItemType.Peacebloom,"<:Peacebloom:727628699146387506>"},
            {ItemType.MithrilBar,"<:Mithrilbar:727628534817751051>"},
            {ItemType.MageweaveBandage,"<:Mageweavebandage:727629746967412778>"},
            {ItemType.HeavyLeather,"<:Heavyleather:727628970320986247>"},
            {ItemType.Firebloom,"<:Firebloom:727628840804810763>"},
            {ItemType.CopperBar,"<:Copperbar:727627789276020776>"},
            {ItemType.BakedSalmon,"<:Bakedsalmon:727629381324898314>"},
            {ItemType.LeanWolfSteak,"<:LeanWolfSteak:727629243164393504>"}
        };

        private static Dictionary<ItemType, string> _names = new Dictionary<ItemType, string> {
            {ItemType.WoolBandage,"Wool Bandage"},
            {ItemType.TinBar,"Tin Bar"},
            {ItemType.ThickLeather,"Thick Leather"},
            {ItemType.SpottedYellowtail,"Spotted Yellowtail"},
            {ItemType.RuneclothBandage,"Runecloth Bandage"},
            {ItemType.RuggedLeather,"Rugged Leather"},
            {ItemType.PurpleLotus,"Purple Lotus"},
            {ItemType.Peacebloom,"Peacebloom"},
            {ItemType.MithrilBar,"Mithril Bar"},
            {ItemType.MageweaveBandage,"Mageweave Bandage"},
            {ItemType.HeavyLeather,"Heavy Leather"},
            {ItemType.Firebloom,"Firebloom"},
            {ItemType.CopperBar,"Copper Bar"},
            {ItemType.BakedSalmon,"Baked Salmon"},
            {ItemType.LeanWolfSteak,"Lean Wolf Steak"}
        };

        private static Dictionary<ItemType, uint> _quotas = new Dictionary<ItemType, uint> {
            {ItemType.WoolBandage, 250000},
            {ItemType.TinBar, 22000},
            {ItemType.ThickLeather, 80000},
            {ItemType.SpottedYellowtail, 17000},
            {ItemType.RuneclothBandage, 400000},
            {ItemType.RuggedLeather, 60000},
            {ItemType.PurpleLotus, 26000},
            {ItemType.Peacebloom, 96000},
            {ItemType.MithrilBar, 18000},
            {ItemType.MageweaveBandage, 250000},
            {ItemType.HeavyLeather, 60000},
            {ItemType.Firebloom, 19000},
            {ItemType.CopperBar, 90000},
            {ItemType.BakedSalmon, 10000},
            {ItemType.LeanWolfSteak, 10000}
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
        [Alias("overview guild")]
        [RequireContext(ContextType.Guild)]
        public async Task OverviewGuild() {
            using(var context = new TuckContext()) {
                await ReplyAsync("**War effort contributions:**");
                await PostGuildOverview(context, Context.Guild.Id);
            }
        }

        private async Task PostGuildOverview(TuckContext context, ulong guildId) {
            var contributions = context.Contributions.AsQueryable()
                .Where(s => s.GuildId == guildId)
                .ToList()
                .GroupBy(t => t.ItemType)
                .ToDictionary(x => x.Key, x => x.Sum((t => t.Amount)));

            var msg = "";
            foreach(var type in (ItemType[]) Enum.GetValues(typeof(ItemType))) {  
                var icon = _icons[type];
                var name = _names[type];
                var quota = _quotas[type];
                var sum = contributions.GetValueOrDefault(type);
                var progress = (decimal) sum / quota;
                msg += $"\n> {icon} {name}: {sum:N0} / {quota:N0} = {progress:P3}";
            }
            await ReplyAsync(msg);

        }

        [Command("overview detailed")]
        [Alias("overview guild detailed")]
        [RequireContext(ContextType.Guild)]
        public async Task OverviewGuildDetailed() {
            using(var context = new TuckContext()) {
                await ReplyAsync("**War effort contributions:**");
                await PostGuildOverviewDetailed(context, Context.Guild.Id);
            }
        }

        private async Task PostGuildOverviewDetailed(TuckContext context, ulong guildId) {

            var users = context.Contributions.AsQueryable()
                .Where(s => s.GuildId == guildId)
                .ToList()
                .GroupBy(t => t.Username);

            foreach(var user in users) {

                var contributions = user
                    .GroupBy(t => t.ItemType)
                    .ToDictionary(x => x.Key, x => x.Sum((t => t.Amount)));

                var msg = $"__{user.Key}__";
                foreach(var type in contributions) {
                    var icon = _icons[type.Key];
                    var name = _names[type.Key];
                    var sum = type.Value;
                    msg += $"\n> {icon} {name}: {sum:N0}";
                }
                await ReplyAsync(msg);
            }
        }

        [Command("overview server")]
        [RequireContext(ContextType.Guild)]
        public async Task OverviewServer() {
          using(var context = new TuckContext()) {
                await ReplyAsync("**War effort contributions:**");
                await PostServerOverview(context);
            }
        }

        private async Task PostServerOverview(TuckContext context) {
            var contributions = context.Contributions.AsQueryable()
                .ToList()
                .GroupBy(t => t.ItemType)
                .ToDictionary(x => x.Key, x => x.Sum((t => t.Amount)));

            var msg = "";
            foreach(var type in (ItemType[]) Enum.GetValues(typeof(ItemType))) {
                var icon = _icons[type];
                var name = _names[type];
                var quota = _quotas[type];
                var sum = contributions.GetValueOrDefault(type);
                var progress = (decimal) sum / quota;
                msg += $"\n> {icon} {name}: {sum:N0} / {quota:N0} = {progress:P3}";
            }
            await ReplyAsync(msg);
        }

        [Command("overview server detailed")]
        [RequireContext(ContextType.Guild)]
        public async Task OverviewServerDetailed() {
            using(var context = new TuckContext()) {
                await ReplyAsync("**War effort contributions:**");
                await PostServerOverviewDetailed(context);
            }
        }

        private async Task PostServerOverviewDetailed(TuckContext context) {

            var guilds = context.Contributions.AsQueryable()
                .ToList()
                .GroupBy(t => t.GuildId);

            foreach(var guild in guilds) {

                var contributions = guild
                    .GroupBy(t => t.ItemType)
                    .ToDictionary(x => x.Key, x => x.Sum((t => t.Amount)));

                var msg = $"__{Context.Client.GetGuild(guild.Key).Name}__";
                foreach(var type in contributions) {
                    var icon = _icons[type.Key];
                    var name = _names[type.Key];
                    var sum = type.Value;
                    msg += $"\n> {icon} {name}: {sum:N0}";
                }
                await ReplyAsync(msg);
            }
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