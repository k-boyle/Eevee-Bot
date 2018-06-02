using System;
using System.Linq;
using Discord.Commands;
using System.Threading.Tasks;
using Discord.WebSocket;
using TagBot.Preconditions;
using TagBot.Services;

namespace TagBot.Modules
{
    public class TagCommands : ModuleBase<SocketCommandContext>
    {
        private readonly DatabaseService _service;

        public TagCommands(DatabaseService service)
        {
            _service = service;
        }

        [Command("tag")]
        public async Task GetTag([Remainder] string tagName)
        {
            var currentTags = _service.GetTags(Context.Guild.Id);
            var targetTag = currentTags.FirstOrDefault(x => x.TagName == tagName.ToLower());

            if (targetTag is null)
            {
                var levenTags = currentTags.Where(x => CalcLevenshteinDistance(tagName.ToLower(), x.TagName) < 5);
                var containsTags = currentTags.Where(x => x.TagName.Contains(tagName.ToLower()));
                var totalTags = levenTags.Union(containsTags);

                await Context.Channel.SendMessageAsync($"{(totalTags.Any() ? $"Tag not found did you mean?\n" + $"{string.Join(", ", totalTags.Select(x => $"{x.TagName}"))}" : "No tags found")}");
                return;
            }

            await Context.Channel.SendMessageAsync(targetTag.TagValue);
        }

        private static int CalcLevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;

            var lengthA = a.Length;
            var lengthB = b.Length;
            var distances = new int[lengthA + 1, lengthB + 1];
            for (var i = 0; i <= lengthA; distances[i, 0] = i++) ;
            for (var j = 0; j <= lengthB; distances[0, j] = j++) ;

            for (var i = 1; i <= lengthA; i++)
            for (var j = 1; j <= lengthB; j++)
            {
                var cost = b[j - 1] == a[i - 1] ? 0 : 1;
                distances[i, j] = Math.Min
                (
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost
                );
            }
            return distances[lengthA, lengthB];
        }

        [Command("tags")]
        public async Task GetTags()
        {
            var currentTags = _service.GetTags(Context.Guild.Id);
            await Context.Channel.SendMessageAsync($"{(currentTags.Any() ? $"Available tags\n" + $"{string.Join(", ", currentTags.Select(x => $"{x.TagName}"))}" : "No available tags")}");
        }

        [Command("help")]
        public async Task GetHelp()
        {

        }

        [Command("create"), RequireNotBlacklisted]
        public async Task CreateTag(string tagName, [Remainder] string tagValue)
        {
            var currentTags = _service.GetTags(Context.Guild.Id);
            if (currentTags.Any(x => x.TagName == tagName.ToLower()))
            {
                await Context.Channel.SendMessageAsync("This tag already exists");
                return;
            }
            _service.AdddNewTag(Context, tagName.ToLower(), tagValue);
            await Context.Channel.SendMessageAsync($"{tagName} has been created");
        }

        [Command("delete"), RequireNotBlacklisted]
        public async Task DeleteTag([Remainder] string tagName)
        {
            var currentTags = _service.GetTags(Context.Guild.Id);
            var targetTag = currentTags.FirstOrDefault(x => x.TagName == tagName.ToLower());
            if (targetTag is null)
            {
                await Context.Channel.SendMessageAsync("This tag does not exists");
                return;
            }
            
            if (targetTag.CreatorId != Context.User.Id && !_service.GetUsers(Context.Guild.Id).Contains(Context.User.Id))
            {
                await Context.Channel.SendMessageAsync("You do not have permission to delete this tag");
                return;
            }
            _service.DeleteTag(Context, tagName.ToLower());
            await Context.Channel.SendMessageAsync("Tag has been deleted");
        }

        [Command("modify"), RequireNotBlacklisted]
        public async Task ModifyTag(string tagName, [Remainder] string newValue)
        {
            var currentTags = _service.GetTags(Context.Guild.Id);
            var targetTag = currentTags.FirstOrDefault(x => x.TagName == tagName.ToLower());
            if (targetTag is null)
            {
                await Context.Channel.SendMessageAsync("Tag could not be found");
                return;
            }

            if (targetTag.CreatorId != Context.User.Id && !_service.GetUsers(Context.Guild.Id).Contains(Context.User.Id))
            {
                await Context.Channel.SendMessageAsync("You do not have permissions to edit this tag");
                return;
            }
            _service.ModifyTag(Context, tagName.ToLower(), newValue);
            await Context.Channel.SendMessageAsync("Tag has been modified");
        }

        [Command("approve"), RequireOwner]
        public async Task ApproveUser(SocketGuildUser toApprove)
        {

        }

        [Command("unapprove"), RequireOwner]
        public async Task UnapproveUser(SocketGuildUser toUnapprove)
        {

        }

        [Command("blacklist"), RequireApproved]
        public async Task BlacklistMember(SocketGuildUser toBlacklist)
        {

        }

        [Command("unblacklist"), RequireApproved]
        public async Task UnBlacklistMember(SocketGuildUser toUnBlacklist)
        {

        }
    }
}
