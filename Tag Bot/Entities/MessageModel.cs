using System;

namespace TagBot.Entities
{
    public class MessageModel
    {
        public ulong UserId { get; }
        public ulong ChannelId { get; }
        public ulong MessageId { get; }
        public DateTimeOffset CreatedAt { get; }

        public MessageModel(ulong userId, ulong channelId, ulong messageId, DateTimeOffset createdAt)
        {
            UserId = userId;
            ChannelId = channelId;
            MessageId = messageId;
            CreatedAt = createdAt;
        }
    }
}
