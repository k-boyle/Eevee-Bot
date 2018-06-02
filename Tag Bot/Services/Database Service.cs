using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TagBot.Entities;

namespace TagBot.Services
{
    public class DatabaseService
    {
        private const string LogSource = "Database";
        private const string DatabaseDir = @".\Database.db";
        private readonly Func<LogMessage, Task> _logMethod;
        private readonly DiscordSocketClient _client;

        public DatabaseService(Func<LogMessage, Task> logMethod, DiscordSocketClient client)
        {
            _logMethod = logMethod;
            _client = client;
        }

        private readonly Dictionary<ulong, List<TagObject>> _currentTags = new Dictionary<ulong, List<TagObject>>();
        private readonly Dictionary<ulong, List<ulong>> _approvedUsers = new Dictionary<ulong, List<ulong>>();
        private readonly Dictionary<ulong, List<ulong>> _blacklistedUsers = new Dictionary<ulong, List<ulong>>();

        public void Initialise()
        {
            using (var db = new LiteDatabase(DatabaseDir))
            {
                if (!db.CollectionExists("guilds"))
                {
                    _logMethod.Invoke(new LogMessage(LogSeverity.Error, LogSource, "Guild collection does not exist"));
                    return;
                }

                var guilds = db.GetCollection<GuildObject>("guilds");
                foreach (var guild in _client.Guilds)
                {
                    var dbGuild = guilds.FindOne(x => x.GuildId == guild.Id);
                    _currentTags.Add(guild.Id, dbGuild.Tags);
                    _approvedUsers.Add(guild.Id, dbGuild.ApprovedUsers);
                    _blacklistedUsers.Add(guild.Id, dbGuild.BlacklistedUsers);
                }
            }
        }

        public async Task AddNewGuild(ulong guildId)
        {
            using (var db = new LiteDatabase(DatabaseDir))
            {
                var guilds = db.GetCollection<GuildObject>("guilds");
                guilds.EnsureIndex($"{guildId}");
                await _logMethod.Invoke(new LogMessage(LogSeverity.Info, LogSource,
                    $"Inserting {guildId} into the database"));
                guilds.Insert(new GuildObject(guildId, (await _client.GetApplicationInfoAsync()).Owner.Id, _client.GetGuild(guildId).OwnerId));
            }
        }

        public void AdddNewTag(SocketCommandContext context, string tagName, string tagValue)
        {
            var newTag = new TagObject(context.User.Id, tagName, tagValue);
            _currentTags[context.Guild.Id].Add(newTag);
            UpdateTags(context);
        }

        public void DeleteTag(SocketCommandContext context, string tagName)
        {
            var targetTag = _currentTags[context.Guild.Id].FirstOrDefault(x => x.TagName == tagName);
            _currentTags[context.Guild.Id].Remove(targetTag);
            UpdateTags(context);
        }

        public void ModifyTag(SocketCommandContext context, string tagName, string newValue)
        {
            _currentTags[context.Guild.Id].FirstOrDefault(x => x.TagName == tagName).TagValue = newValue;
            UpdateTags(context);
        }

        public void AddApproved(SocketCommandContext context, ulong userId)
        {
            _approvedUsers[context.Guild.Id].Add(userId);
            UpdateApproved(context);
        }

        public void RemoveApproved(SocketCommandContext context, ulong userId)
        {
            _approvedUsers[context.Guild.Id].Remove(userId);
            UpdateApproved(context);
        }

        private void UpdateApproved(SocketCommandContext context)
        {
            using (var db = new LiteDatabase(DatabaseDir))
            {
                var guilds = db.GetCollection<GuildObject>("guilds");
                var guild = guilds.FindOne(x => x.GuildId == context.Guild.Id);
                guild.ApprovedUsers = _approvedUsers[context.Guild.Id];
                guilds.Update(guild);
            }
        }

        public void AddBlacklisted(SocketCommandContext context, ulong userId)
        {
            _blacklistedUsers[context.Guild.Id].Add(userId);
            UpdateBlacklisted(context);
        }

        public void RemoveBlacklsited(SocketCommandContext context, ulong userId)
        {
            _blacklistedUsers[context.Guild.Id].Remove(userId);
            UpdateBlacklisted(context);
        }

        private void UpdateBlacklisted(SocketCommandContext context)
        {
            using (var db = new LiteDatabase(DatabaseDir))
            {
                var guilds = db.GetCollection<GuildObject>("guilds");
                var guild = guilds.FindOne(x => x.GuildId == context.Guild.Id);
                guild.BlacklistedUsers = _blacklistedUsers[context.Guild.Id];
                guilds.Update(guild);
            }
        }

        private void UpdateTags(SocketCommandContext context)
        {
            using (var db = new LiteDatabase(DatabaseDir))
            {
                var guilds = db.GetCollection<GuildObject>("guilds");
                var guild = guilds.FindOne(x => x.GuildId == context.Guild.Id);
                guild.Tags = _currentTags[context.Guild.Id];
                guilds.Update(guild);
            }
        }

        public IEnumerable<TagObject> GetTags(ulong guildId)
        {
            return _currentTags[guildId];
        }

        public List<ulong> GetApproved(ulong guildId)
        {
            return _approvedUsers[guildId];
        }

        public List<ulong> GetBlacklisted(ulong guildId)
        {
            return _blacklistedUsers[guildId];
        }
    }
}
