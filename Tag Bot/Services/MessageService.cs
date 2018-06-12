using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TagBot.Entities;

namespace TagBot.Services
{
    public class MessageService
    {
        private readonly List<MessageModel> _messages = new List<MessageModel>();
        private ulong _currentMessage;

        public async Task SendMessageAsync(SocketCommandContext context, string message, Embed embed = null)
        {
            CleanseOldMessages();
            if (_messages.Any(x => x.ExecutingMessageId == _currentMessage))
            {
                var targetMessage = _messages.FirstOrDefault(x => x.ExecutingMessageId == _currentMessage);
                var retrievedMessage = context.Channel.GetCachedMessage(targetMessage.MessageId) ??
                                       await context.Channel.GetMessageAsync(targetMessage.MessageId);
                if (retrievedMessage is null) return;
                await (retrievedMessage as IUserMessage).ModifyAsync(x =>
                {
                    x.Content = message;
                    x.Embed = embed;
                });
            }
            else
            {
                var sentMessage = await context.Channel.SendMessageAsync(message, embed: embed);
                var newMessage = new MessageModel(_currentMessage, context.User.Id, context.Channel.Id, sentMessage.Id, sentMessage.CreatedAt);
                _messages.Add(newMessage);
            }
        }

        public async Task ClearMessages(SocketCommandContext context)
        {
            CleanseOldMessages();
            var foundMessages = _messages.Where(x => x.UserId == context.User.Id && x.ChannelId == context.Channel.Id).ToList();
            foreach (var foundMessage in foundMessages)
            {
                _messages.Remove(foundMessage);
                var retrievedMessage = context.Channel.GetCachedMessage(foundMessage.MessageId) ??
                                       await context.Channel.GetMessageAsync(foundMessage.MessageId);
                if (retrievedMessage == null) continue;
                await retrievedMessage.DeleteAsync();
            }
        }

        private void CleanseOldMessages()
        {
            var oldMessages = _messages.Where(x => x.CreatedAt.AddMinutes(5) < DateTime.UtcNow).ToList();
            foreach (var oldMessage in oldMessages)
            {
                _messages.Remove(oldMessage);
            }
        }

        public void SetCurrentMessage(ulong receivedMessageId)
        {
            _currentMessage = receivedMessageId;
        }
    }
}
