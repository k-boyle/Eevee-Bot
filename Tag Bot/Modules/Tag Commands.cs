using System;
using System.Linq;
using Discord.Commands;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using TagBot.Preconditions;
using TagBot.Services;
using Discord.Addons.Interactive;

namespace TagBot.Modules
{
    public class TagCommands : InteractiveBase<SocketCommandContext>
    {
        private readonly DatabaseService _service;
        private readonly CommandService _commands;

        public TagCommands(DatabaseService service, CommandService commands)
        {
            _service = service;
            _commands = commands;
        }

        [Command("tag"), Alias("t"), Name("Tag"), Summary("Get a tag for this guild")]
        public async Task GetTag([Name("Name of tag"), Summary("The name of the tag you want to fetch"), Remainder] string tagName)
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

        [Command("tags", RunMode = RunMode.Async), Name("List Tags"), Summary("Lists all the tags for the guild")]
        public async Task GetTags()
        {
            var currentTags = _service.GetTags(Context.Guild.Id);
            var msg = await Context.Channel.SendMessageAsync($"{(currentTags.Any() ? $"Available tags\n" + $"{string.Join(", ", currentTags.Select(x => $"{x.TagName}"))}" : "No available tags")}");
            await Task.Delay(TimeSpan.FromSeconds(30));
            await msg.DeleteAsync();
        }

        [Command("help", RunMode = RunMode.Async)]
        public async Task GetHelp()
        {
            var availableCommands = _commands.Commands.Where(x => x.Name != "help");

            var builder = new EmbedBuilder
            {
                Title = $"{Context.Client.CurrentUser.Username}'s help",
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    Name = (Context.User as SocketGuildUser).Nickname ?? Context.User.Username
                },
                Color = Color.Blue,
                ThumbnailUrl = Context.Client.CurrentUser.GetAvatarUrl() ?? Context.Client.CurrentUser.GetDefaultAvatarUrl(),
                Timestamp = DateTime.UtcNow
            };
            foreach (var cmd in availableCommands)
            {
                builder.AddField(f =>
                {
                    f.Name = $"**{cmd.Name}**";
                    f.Value = $"Summary: {cmd.Summary}\n" +
                              $"Usage: *dnet {cmd.Aliases.FirstOrDefault()} {(cmd.Parameters.Any() ? $"{string.Join(" ", cmd.Parameters.Select(y => $"`{y.Name}`{(y.Summary != null ? $"\n{y.Name} - {y.Summary}" : "")}"))}" : "")}";
                });
            }

            var msg = await Context.Channel.SendMessageAsync("", embed: builder.Build());
            await Task.Delay(TimeSpan.FromSeconds(30));
            await msg.DeleteAsync();
        }

        [Command("create"), Name("Create Tag"), Summary("Creates a new tag for the guild. This requires an approved user"), RequireApproved]
        public async Task CreateTag([Name("Tag Name")]string tagName, [Name("Tag Value"), Remainder] string tagValue)
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

        [Command("delete"), Name("Delete Tag"), Summary("Delete a tag on the guild. This requires an approved user"), RequireApproved]
        public async Task DeleteTag([Name("Tag To Delete"), Remainder] string tagName)
        {
            var currentTags = _service.GetTags(Context.Guild.Id);
            var targetTag = currentTags.FirstOrDefault(x => x.TagName == tagName.ToLower());
            if (targetTag is null)
            {
                await Context.Channel.SendMessageAsync("This tag does not exists");
                return;
            }
            
            if (targetTag.CreatorId != Context.User.Id && !_service.GetApproved(Context.Guild.Id).Contains(Context.User.Id))
            {
                await Context.Channel.SendMessageAsync("You do not have permission to delete this tag");
                return;
            }
            _service.DeleteTag(Context, tagName.ToLower());
            await Context.Channel.SendMessageAsync("Tag has been deleted");
        }

        [Command("modify"), Name("Modify Tag"), Summary("Modify a tag on the guild. This requires an approved user"), RequireApproved]
        public async Task ModifyTag([Name("Tag Name")]string tagName, [Name("New Tag Value"), Remainder] string newValue)
        {
            var currentTags = _service.GetTags(Context.Guild.Id);
            var targetTag = currentTags.FirstOrDefault(x => x.TagName == tagName.ToLower());
            if (targetTag is null)
            {
                await Context.Channel.SendMessageAsync("Tag could not be found");
                return;
            }

            if (targetTag.CreatorId != Context.User.Id && !_service.GetApproved(Context.Guild.Id).Contains(Context.User.Id))
            {
                await Context.Channel.SendMessageAsync("You do not have permissions to edit this tag");
                return;
            }
            _service.ModifyTag(Context, tagName.ToLower(), newValue);
            await Context.Channel.SendMessageAsync("Tag has been modified");
        }

        [Command("approve"), Name("Approve User"), Summary("Add a user to the approved users list. This requires the bot owner"), RequireOwner]
        public async Task ApproveUser([Name("User To Approve"), Remainder]SocketGuildUser toApprove)
        {
            var current = _service.GetApproved(Context.Guild.Id);
            if (current.Contains(toApprove.Id))
            {
                await Context.Channel.SendMessageAsync("This user is already approved");
                return;
            }
            _service.AddApproved(Context, toApprove.Id);
            await Context.Channel.SendMessageAsync("User has been approved");
        }

        [Command("unapprove"), Name("Unapprove User"), Summary("Remove a user from the approved users list. This requires the bot owner"), RequireOwner]
        public async Task UnapproveUser([Name("User To Unapprove"), Remainder]SocketGuildUser toUnapprove)
        {
            var current = _service.GetApproved(Context.Guild.Id);
            if (!current.Contains(toUnapprove.Id))
            {
                await Context.Channel.SendMessageAsync("This user isn't approved");
                return;
            }
            _service.RemoveApproved(Context, toUnapprove.Id);
            await Context.Channel.SendMessageAsync("User has been unapproved");
        }
    }
}
