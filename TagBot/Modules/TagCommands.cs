using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;
using TagBot.Preconditions;
using TagBot.Services;

namespace TagBot.Modules
{
    public class TagCommands : ModuleBase<SocketCommandContext>
    {
        private readonly DatabaseService _service;
        private readonly CommandService _commands;
        private readonly MessageService _message;

        public TagCommands(DatabaseService service, CommandService commands, MessageService message)
        {
            _service = service;
            _commands = commands;
            _message = message;
        }

        [Command("tag"), Alias("t"), Name("Tag"), Summary("Get a tag for this guild")]
        public async Task GetTag([Name("Name of tag"), Summary("The name of the tag you want to fetch"), Remainder] string tagName)
        {
            var currentTags = _service.GetTags(Context.Guild.Id);
            var targetTag = currentTags.FirstOrDefault(x => x.TagName == tagName.ToLower());
            if (targetTag is null)
            {
                var levenTags = currentTags.Where(x => TagHelper.CalcLevenshteinDistance(tagName.ToLower(), x.TagName) < 5);
                var containsTags = currentTags.Where(x => x.TagName.Contains(tagName.ToLower()));
                var totalTags = levenTags.Union(containsTags);

                await _message.SendMessageAsync(Context, $"{(totalTags.Any() ? "Tag not found did you mean?\n" + $"{string.Join(", ", totalTags.Select(x => $"{x.TagName}"))}" : "No tags found")}");
                return;
            }
            
            await _message.SendMessageAsync(Context, targetTag.TagValue);
        }

        [Command("search"), Alias("s"), Summary("Search for a tag")]
        public async Task SearchTags([Name("Search For"), Summary("Your search query"), Remainder] string tagName)
        {
            var currentTags = _service.GetTags(Context.Guild.Id);
            var levenTags = currentTags.Where(x => TagHelper.CalcLevenshteinDistance(tagName.ToLower(), x.TagName) < 5);
            var containsTags = currentTags.Where(x => x.TagName.Contains(tagName.ToLower()));
            var totalTags = levenTags.Union(containsTags);
            await _message.SendMessageAsync(Context, $"{(totalTags.Any() ? "Tags found;\n" + $"{string.Join(", ", totalTags.Select(x => $"{x.TagName}"))}" : "No tags found")}");
        }

        [Command("tags"), Alias("l"), Name("List Tags"), Summary("Lists all the tags for the guild")]
        public async Task GetTags()
        {
            var currentTags = _service.GetTags(Context.Guild.Id).OrderBy(x => x.TagName);
            await _message.SendMessageAsync(Context,
                $"{(currentTags.Any() ? $"Available tags\n" + $"{string.Join(", ", currentTags.Select(x => $"{x.TagName}"))}" : "No available tags")}");
        }

        [Command("help"), Alias("h")]
        public async Task GetHelp()
        {
            var availableCommands = _commands.Commands.Where(x => x.Name != "help");
            var i = 0;
            await _message.SendMessageAsync(Context,
                $"Available Commands:\n{string.Join("\n", availableCommands.Select(x => $"{++i} - ev?**{x.Aliases.FirstOrDefault()}** {(x.Parameters.Any() ? $"{string.Join(" ", x.Parameters.Select(y => $"`{y.Name}`"))}" : "")}"))}");
        }

        [Command("create"), Name("Create Tag"), Summary("Creates a new tag for the guild. This requires an approved user"), RequireApproved]
        public async Task CreateTag([Name("Tag Name"), Summary("The name of the tag")]string tagName, [Name("Tag Value"), Summary("What you want the tag to say in response"), Remainder] string tagValue)
        {
            var currentTags = _service.GetTags(Context.Guild.Id);
            if (currentTags.Any(x => x.TagName == tagName.ToLower()))
            {
                await _message.SendMessageAsync(Context, "This tag already exists");
                return;
            }
            _service.AdddNewTag(Context, tagName.ToLower(), tagValue);
            await _message.SendMessageAsync(Context, $"{tagName} has been created");
        }

        [Command("delete"), Name("Delete Tag"), Summary("Delete a tag on the guild. This requires an approved user"), RequireApproved]
        public async Task DeleteTag([Name("Tag To Delete"), Summary("The name of the tag that you want to delete"), Remainder] string tagName)
        {
            var currentTags = _service.GetTags(Context.Guild.Id);
            var targetTag = currentTags.FirstOrDefault(x => x.TagName == tagName.ToLower());
            if (targetTag is null)
            {
                await _message.SendMessageAsync(Context, "This tag does not exists");
                return;
            }
            _service.DeleteTag(Context, tagName.ToLower());
            await _message.SendMessageAsync(Context, "Tag has been deleted");
        }

        [Command("modify"), Name("Modify Tag"), Summary("Modify a tag on the guild. This requires an approved user"), RequireApproved]
        public async Task ModifyTag([Name("Tag Name"), Summary("Name of the tag you want to modify")]string tagName, [Name("New Tag Value"), Summary("The new value that you want to respond with"), Remainder] string newValue)
        {
            var currentTags = _service.GetTags(Context.Guild.Id);
            var targetTag = currentTags.FirstOrDefault(x => x.TagName == tagName.ToLower());
            if (targetTag is null)
            {
                await _message.SendMessageAsync(Context, "Tag could not be found");
                return;
            }
            _service.ModifyTag(Context, tagName.ToLower(), newValue);
            await _message.SendMessageAsync(Context, "Tag has been modified");
        }

        [Command("approve"), Name("Approve User"), Summary("Add a user to the approved users list. This requires the bot owner"), RequireOwner]
        public async Task ApproveUser([Name("User To Approve"), Summary("The mention/name/id of the user you want to approve"), Remainder]SocketGuildUser toApprove)
        {
            var current = _service.GetApproved(Context.Guild.Id);
            if (current.Contains(toApprove.Id))
            {
                await _message.SendMessageAsync(Context, "This user is already approved");
                return;
            }
            _service.AddApproved(Context, toApprove.Id);
            await _message.SendMessageAsync(Context, "User has been approved");
        }

        [Command("unapprove"), Name("Unapprove User"), Summary("Remove a user from the approved users list. This requires the bot owner"), RequireOwner]
        public async Task UnapproveUser([Name("User To Unapprove"), Summary("The mention/name/id of the user you want to unapprove"), Remainder]SocketGuildUser toUnapprove)
        {
            var current = _service.GetApproved(Context.Guild.Id);
            if (!current.Contains(toUnapprove.Id))
            {
                await _message.SendMessageAsync(Context, "This user isn't approved");
                return;
            }
            _service.RemoveApproved(Context, toUnapprove.Id);
            await _message.SendMessageAsync(Context, "User has been unapproved");
        }

        [Command("approved"), Name("List Approved"), Summary("See all of the approved users for this guild")]
        public async Task ListApproved()
        {
            var users = _service.GetApproved(Context.Guild.Id).Select(x => $"`{(Context.Guild.GetUser(x).Nickname ?? Context.Guild.GetUser(x).Username).Replace("`", "")}`");
            await _message.SendMessageAsync(Context, $"Approved users are:\n{string.Join(", ", users)}");
        }

        [Command("cleanse"), Alias("c"), Name("Cleanse Messages"), Summary("Removes all the responses by Eevee Bot to you in the last 5 minutes")]
        public async Task Cleanse()
        {
            await _message.ClearMessages(Context);
        }
    }
}
