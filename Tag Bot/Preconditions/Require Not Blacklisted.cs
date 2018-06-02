using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using TagBot.Services;

namespace TagBot.Preconditions
{
    public class RequireNotBlacklisted : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var service = services.GetService<DatabaseService>();
            return service.GetBlacklisted(context.Guild.Id).Contains(context.User.Id)
                ? Task.FromResult(PreconditionResult.FromError("You are blacklisted you cannot use this command"))
                : Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
