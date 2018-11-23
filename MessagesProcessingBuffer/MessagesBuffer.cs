using System;
using System.Collections.Generic;
using System.Linq;

namespace MessagesBufferProcessor
{
    public class MessagesBuffer<T>
    {
        public const string Started = "Started";
        public const string Added = "Added";
        public const string Updated = "Updated";
        public const string Completed = "Completed";
        
        private readonly object _lock = new object();
        private readonly Dictionary<string, List<T>> _processedMessages = new Dictionary<string, List<T>>();
        private readonly Dictionary<string, List<T>> _messages = new Dictionary<string, List<T>>();

        public void ClearProcessedMessages(string subject = null)
        {
            lock (_lock)
            {
                if (subject == null)
                {
                    foreach (KeyValuePair<string, List<T>> bySubject in _processedMessages)
                    {
                        _processedMessages[bySubject.Key].Clear();
                    }
                }
                else
                {
                    if (_processedMessages.ContainsKey(subject))
                    {
                        _processedMessages[subject].Clear();
                    }
                }
            }
        }

        public bool IsEmpty()
        {
            lock (_lock)
            {
                return _messages.Count == 0;
            }
        }

        public IEnumerable<T> GetProcessedMessages(string subject)
        {
            lock (_lock)
            {
                if (_processedMessages.ContainsKey(subject))
                {
                    return _processedMessages[subject].ToList();
                }
            }

            return Enumerable.Empty<T>();
        }

        public IEnumerable<T> GetMessages(string subject, int count = 0)
        {
            lock (_lock)
            {
                if (_messages.ContainsKey(subject))
                {
                    return
                        count == 0
                            ? _messages[subject]
                            : _messages[subject].Take(count);
                }
            }

            return Enumerable.Empty<T>();
        }

        public void RemoveMessage(string subject, T message)
        {
            lock (_lock)
            {
                if (!_messages.ContainsKey(subject))
                {
                    OnEmptyListReached(GetChangedArgs(subject, $"{subject} - all messages are processed", Completed));
                    return;
                }

                _messages[subject].Remove(message);

                MarkAsProcessed(subject, message);

                if (_messages[subject].Count == 0)
                {
                    OnEmptyListReached(GetChangedArgs(subject, $"{subject} - all messages are processed", Completed));
                }

                OnChanged(GetChangedArgs(subject, "Message processed", Updated));
            }
        }

        private MessagesBufferEventArgs GetChangedArgs(string subject, string message, string type)
        {
            return new MessagesBufferEventArgs(subject)
            {
                ChangeType = type,
                Message = message,
                ToProcess = _messages[subject].Count,
                Processed = _processedMessages.ContainsKey(subject) ? _processedMessages[subject].Count : 0
            };
        }

        private void MarkAsProcessed(string subject, T message)
        {
            if (_processedMessages.ContainsKey(subject))
            {
                _processedMessages[subject].Add(message);
            }
            else
            {
                _processedMessages.Add(subject, new List<T> { message });
            }
        }

        public void AddMessage(string subject, T message)
        {
            lock (_lock)
            {
                if (_messages.ContainsKey(subject))
                {
                    if (_messages[subject].Count == 0)
                    {
                        OnFirstArrived(GetChangedArgs(subject, "First message arrived", Started));
                    }

                    _messages[subject].Add(message);
                }
                else
                {
                    _messages.Add(subject, new List<T> { message });
                    OnFirstArrived(GetChangedArgs(subject, "First message arrived", Started));
                }

                OnChanged(GetChangedArgs(subject, "New message arrived", Added));
            }
        }

        protected virtual void OnChanged(MessagesBufferEventArgs e)
        {
            EventHandler handler = Changed;
            handler?.Invoke(this, e);
        }

        protected virtual void OnEmptyListReached(MessagesBufferEventArgs e)
        {
            EventHandler handler = EmptyListReached;
            handler?.Invoke(this, e);
        }

        protected virtual void OnFirstArrived(MessagesBufferEventArgs e)
        {
            EventHandler handler = FirstMessageArrived;
            handler?.Invoke(this, e);
        }

        public event EventHandler Changed;
        public event EventHandler EmptyListReached;
        public event EventHandler FirstMessageArrived;
    }
}
