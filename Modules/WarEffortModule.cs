using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord.Commands;
using Tuck.Model;
using System.Collections.Generic;
using Discord;
using Hangfire;
using Tuck.Services;

namespace Tuck.Modules
{
    [Group("weffort")]
    public class WarEffortModule : ModuleBase<SocketCommandContext>
    {
        private static Dictionary<ItemType, string> _icons = new Dictionary<ItemType, string> {
            {ItemType.Woolbandage,"<:Woolbandage:727629457166172271>"},
            {ItemType.TinBar,"<:Tinbar:727628410473545749>"},
            {ItemType.ThickLeather,"<:Thickleather:727627987834634420>"},
            {ItemType.SpottedYellowtail,"<:SpottedYellowtail:727628079584903228>"},
            {ItemType.Runecloth,"<:Runecloth:727628181300838460>"},
            {ItemType.RuggedLeather,"<:Ruggedleather:727629141410709545>"},
            {ItemType.PurpleLotus,"<:PurpleLotus:727627871744688180>"},
            {ItemType.Peacebloom,"<:Peacebloom:727628699146387506>"},
            {ItemType.MithrilBar,"<:Mithrilbar:727628534817751051>"},
            {ItemType.MageweaveBandage,"<:Mageweavebandage:727629746967412778>"},
            {ItemType.HeavyLeather,"<:Heavyleather:727628970320986247>"},
            {ItemType.Firebloom,"<:Firebloom:727628840804810763>"},
            {ItemType.CopperBar,"<:Copperbar:727627789276020776>"},
            {ItemType.BakedSalmon,"<:Bakedsalmon:727629381324898314>"}
        };

        private static Dictionary<ItemType, string> _names = new Dictionary<ItemType, string> {
            {ItemType.Woolbandage,"Wool Bandage"},
            {ItemType.TinBar,"Tin Bar"},
            {ItemType.ThickLeather,"Thick Leather"},
            {ItemType.SpottedYellowtail,"Spotted Yellowtail"},
            {ItemType.Runecloth,"Runecloth"},
            {ItemType.RuggedLeather,"Rugged Leather"},
            {ItemType.PurpleLotus,"Purple Lotus"},
            {ItemType.Peacebloom,"Peacebloom"},
            {ItemType.MithrilBar,"Mithril Bar"},
            {ItemType.MageweaveBandage,"Mageweave Bandage"},
            {ItemType.HeavyLeather,"Heavy Leather"},
            {ItemType.Firebloom,"Firebloom"},
            {ItemType.CopperBar,"Copper Bar"},
            {ItemType.BakedSalmon,"Baked Salmon"}
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

                await channel.SendMessageAsync($"Hi {Context.User.Username}!\nI'm here to help with tallying up materials for the wareffort. You can register your contribution to the war effort by writing !weffort [item] [amount].\n\nI support the following types: {types}");
            }
        }

        [Command("contribute")]
        [RequireContext(ContextType.Guild)]
        public async Task Contribute(ItemType itemType, uint amount) {
            using(var context = new TuckContext()) {

                var user = Context.Guild.GetUser(Context.User.Id);
                var guild = Context.Guild;

                var contribution = context.Contributions.AsQueryable()
                    .Where(s => s.GuildId == guild.Id && s.UserId == user.Id && s.ItemType == itemType)
                    .FirstOrDefault();

                if(contribution == null)    await CreateContribution(context, user, guild, itemType, amount);
                else                        await UpdateContribution(context, contribution, amount);

            }
        }

        [Command("overview")]
        [Alias("overview guild")]
        [RequireContext(ContextType.Guild)]
        public async Task OverviewGuild() {
            using(var context = new TuckContext()) {

                var user = Context.Guild.GetUser(Context.User.Id);
                var guild = Context.Guild;

                var contributions = context.Contributions.AsQueryable()
                    .Where(s => s.GuildId == guild.Id)
                    .ToList()
                    .GroupBy(t => t.ItemType)
                    .Select(t => $"\n> {_icons[t.Key]} {_names[t.Key]} x {t.Sum(e => e.Amount)}")
                    .Aggregate("", (s1,s2) => s1 + s2);

                await ReplyAsync("**War effort contributions:**" + 
                    (string.IsNullOrEmpty(contributions) ? "\n> Nothing have been added yet..." : contributions));
            }
        }

        [Command("overview guild detailed")]
        [RequireContext(ContextType.Guild)]
        public async Task OverviewGuildDetailed() {
            using(var context = new TuckContext()) {

                var msg = "";
                foreach(var group in context.Contributions.ToList().GroupBy(t => t.Username)) {
                    var contributions = group
                        .GroupBy(t => t.ItemType)
                        .Select(t => $"\n> {_icons[t.Key]} {_names[t.Key]} x {t.Sum(e => e.Amount)}")
                        .Aggregate("", (s1,s2) => s1 + s2);
                    msg += $"\n\n__{group.Key}__{contributions}";
                }

                await ReplyAsync("**War effort contributions:**" + 
                    (string.IsNullOrEmpty(msg) ? "\n> Nothing have been added yet..." : msg));
            }
        }

        [Command("overview server")]
        [RequireContext(ContextType.Guild)]
        public async Task OverviewServer() {
            using(var context = new TuckContext()) {

                var user = Context.Guild.GetUser(Context.User.Id);
                var guild = Context.Guild;

                var contributions = context.Contributions
                    .ToList()
                    .GroupBy(t => t.ItemType)
                    .Select(t => $"\n> {_icons[t.Key]} {_names[t.Key]} x {t.Sum(e => e.Amount)}")
                    .Aggregate("", (s1,s2) => s1 + s2);

                await ReplyAsync("**War effort contributions:**" + 
                    (string.IsNullOrEmpty(contributions) ? "\n> Nothing have been added yet..." : contributions));
            }
        }

        [Command("overview server detailed")]
        [RequireContext(ContextType.Guild)]
        public async Task OverviewServerDetailed() {
            using(var context = new TuckContext()) {

                var msg = "";
                foreach(var group in context.Contributions.ToList().GroupBy(t => t.GuildId)) {
                    var guild = Context.Client.GetGuild(group.Key);
                    var contributions = group
                        .GroupBy(t => t.ItemType)
                        .Select(t => $"\n> {_icons[t.Key]} {_names[t.Key]} x {t.Sum(e => e.Amount)}")
                        .Aggregate("", (s1,s2) => s1 + s2);
                    msg += $"\n\n__{guild.Name}__{contributions}";
                }

                await ReplyAsync("**War effort contributions:**" + 
                    (string.IsNullOrEmpty(msg) ? "\n> Nothing have been added yet..." : msg));
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
            await ReplyAsync($"{_icons[itemType]} Your contribution of {amount} x {_names[itemType]} have been registered");

        }

        public async Task UpdateContribution(TuckContext context, WarEffortContribution existing, uint amount) {
            existing.Amount = amount;
            context.Update(existing);
            await context.SaveChangesAsync();
            await ReplyAsync($"{_icons[existing.ItemType]} Your contribution has been updated to {amount} x {_names[existing.ItemType]}");
        }
    }
}   