using LiteDB;
using System.Collections.Generic;
using System.Net;

namespace TagBot.Entities
{
    public class GuildObject
    {
        public GuildObject(ulong guildId, ulong BotOwnerId, ulong GuildOwnerId)
        {
            GuildId = guildId;
            ApprovedUsers.Add(BotOwnerId);
            if(GuildOwnerId != BotOwnerId)
                ApprovedUsers.Add(GuildOwnerId);
        }

        public GuildObject() { }

        [BsonId(false)]
        public ulong GuildId { get; set; }

        public List<TagObject> Tags { get; set; } = new List<TagObject>();
        public List<ulong> ApprovedUsers { get; set; } = new List<ulong>();
    }

    public class TagObject
    {
        public TagObject(ulong creatorId, string tagName, string tagValue)
        {
            CreatorId = creatorId;
            TagName = tagName;
            TagValue = tagValue;
        }

        public TagObject() { }

        public ulong CreatorId { get; set; }
        public string TagName { get; set; }
        public string TagValue { get; set; }
    }
}
