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
        private readonly Dictionary<ulong, ulong?> _messageCache = new Dictionary<ulong, ulong?>();
        private ulong _currentId;
        private bool _editCmd;

        public async Task SendMessage(SocketCommandContext context, string message, Embed embed = null)
        {
            MessagePurge();
            if (_editCmd)
            {
                var idToEdit = _messageCache[_currentId].Value;
                var fetchedMessage = context.Channel.GetCachedMessage(idToEdit) ?? await context.Channel.GetMessageAsync(idToEdit);
                if (fetchedMessage is null) return;
                await (fetchedMessage as IUserMessage).ModifyAsync(x => x.Content = message);
                return;
            }
            var sentMessage = await context.Channel.SendMessageAsync(message, embed: embed);
            _messages.Add(new MessageModel(context.User.Id, context.Channel.Id, sentMessage.Id, sentMessage.CreatedAt));
            _messageCache[_currentId] = sentMessage.Id;
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

        public void MessageReceived(ulong senderMessageId)
        {
            _currentId = 0;
            _editCmd = false;
            if (_messageCache.ContainsKey(senderMessageId))
            {
                if(_messageCache[senderMessageId] != null)
                    _editCmd = true;
                return;
            }
            _messageCache.Add(senderMessageId, null);
        }

        public void CommandReceived(ulong commandMessageId)
        {
            _currentId = commandMessageId;
        }
    }
}
