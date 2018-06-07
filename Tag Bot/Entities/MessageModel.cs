using System;
using System.Collections.Generic;
using System.Text;

namespace TagBot.Entities
{
    public class MessageModel
    {
        public ulong userId { get; set; }
        public ulong channelId { get; set; }
        public ulong messageId { get; set; }
        public DateTime timeout { get; set; }
        public DateTimeOffset createdAt { get; set; }

        public MessageModel(ulong userId, ulong channelId, ulong messageId, DateTime timeout, DateTimeOffset createdAt)
        {
            this.userId = userId;
            this.channelId = channelId;
            this.messageId = messageId;
            this.timeout = timeout;
            this.createdAt = createdAt;
        }
    }
}
