using System;
using System.Threading.Tasks;
using Discord.Commands;
using Tuck.Model;

namespace Tuck.Readers {
    public class ItemTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            ItemType result;
            if (Enum.TryParse(input, true, out result))
                return Task.FromResult(TypeReaderResult.FromSuccess(result));

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input could not be parsed as a item type."));
        }
    }
}