using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;
using Tuck.Model;

namespace Tuck.Readers {
    public class DateTimeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var regex = new Regex(@"^(0[0-9]|1[0-9]|2[0-3])(?::|\.)([0-5][0-9])(?:\+([1-9][0-9]*))?$");

            var match = regex.Match(input);
            if(!match.Success){
                var errorMessage = "Invalid format. The allowed format is: 12:45, 12.45, 12:45+1";
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, errorMessage));
            } 

            var hour = int.Parse(match.Groups[1].Value);
            var minute = int.Parse(match.Groups[2].Value);

            if(hour > 23) new Exception("Invalid hour. There're only 24 hours in a day... stupid.");
            if(minute > 59) new Exception("Invalid minute. There're only 60 minuts an hour... stupid.");
            
            var now = DateTime.Now;
            var time = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);

            if(match.Groups[3].Success){
                var offset = int.Parse(match.Groups[3].Value);
                time = time.AddDays(offset);
            }

            return Task.FromResult(TypeReaderResult.FromSuccess(time));
        }
    }
}