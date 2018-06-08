using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using TagBot.Services;

namespace TagBot.Preconditions
{
    public class RequireApproved : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var service = services.GetService<DatabaseService>();
            return service.GetApproved(context.Guild.Id).Contains(context.User.Id)
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError("You do not have permission to use this command"));
        }
    }
}
