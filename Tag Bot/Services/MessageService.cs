using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TagBot.Entities;

namespace TagBot.Services
{
    public class MessageService
    {
        private readonly List<MessageModel> _messages = new List<MessageModel>();

        public async Task SendMessage(SocketCommandContext context, string message, Embed embed = null)
        {
            MessagePurge();
            var sentMessage = await context.Channel.SendMessageAsync(message, embed: embed);
            _messages.Add(new MessageModel(context.User.Id, context.Channel.Id, sentMessage.Id, sentMessage.CreatedAt));
        }

        public async Task ClearMessages(SocketCommandContext context)
        {
            MessagePurge();
            var deletedMessages = new List<MessageModel>();
            foreach (var msg in _messages)
            {
                if (msg.UserId != context.User.Id || msg.ChannelId != context.Channel.Id) continue;
                var message = context.Channel.GetCachedMessage(msg.MessageId) ?? await context.Channel.GetMessageAsync(msg.MessageId);
                await context.Channel.DeleteMessageAsync(message);
                deletedMessages.Add(msg);
            }

            foreach (var deleted in deletedMessages)
            {
                _messages.Remove(deleted);
            }
        }

        private void MessagePurge()
        {
            var toDelete = new List<MessageModel>();
            foreach (var msg in _messages)
            {
                if (DateTime.UtcNow - msg.CreatedAt > TimeSpan.FromMinutes(5))
                {
                    toDelete.Add(msg);
                }
            }

            foreach (var deleted in toDelete)
            {
                _messages.Remove(deleted);
            }
        }
    }
}
