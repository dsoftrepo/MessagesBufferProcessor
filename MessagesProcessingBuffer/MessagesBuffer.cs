using System;
using System.Collections.Generic;
using System.Linq;

namespace MessagesBufferProcessor
{
    public class MessagesBuffer<T>
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, List<T>> _processedMessages = new Dictionary<string, List<T>>();
        private readonly Dictionary<string, List<T>> _messages = new Dictionary<string, List<T>>();

        public void ClearProcessedMessages(string subject=null)
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

        public IEnumerable<T> GetMessages(string subject, int count = 0)
        {
            lock (_lock)
            {
                if (_messages.ContainsKey(subject))
                {
                    return
                        count == 0
                            ? _messages[subject].ToList()
                            : _messages[subject].Take(count).ToList();
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
                    OnEmptyListReached(new MessagesBufferProcessorEventArgs<T>(subject)
                    {
                        Message = "Done",
                        ToProcess = 0,
                        Processed = _processedMessages.ContainsKey(subject) ? _processedMessages[subject].Count : 0
                    });

                    return;
                }

                _messages[subject].Remove(message);

                if (_processedMessages.ContainsKey(subject))
                {
                    _processedMessages[subject].Add(message);
                }
                else
                {
                    _processedMessages.Add(subject, new List<T>{message});
                }

                if (_messages[subject].Count == 0)
                {
                    OnEmptyListReached(new MessagesBufferProcessorEventArgs<T>(subject));
                }

                OnChanged(new MessagesBufferProcessorEventArgs<T>(subject)
                {
                    Message = "Message processed",
                    ToProcess = _messages[subject].Count,
                    Processed = _processedMessages[subject].Count
                });
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
                        OnFirstArrived(new MessagesBufferEventArgs(subject));
                    }

                    _messages[subject].Add(message);
                }
                else
                {
                    _messages.Add(subject, new List<T>{ message });
                    OnFirstArrived(new MessagesBufferEventArgs(subject));
                }
           
                OnChanged(new MessagesBufferProcessorEventArgs<T>(subject)
                {
                    Message = "New message arrived",
                    ToProcess = _messages[subject].Count,
                    Processed = _processedMessages.ContainsKey(subject) ? _processedMessages[subject].Count : 0
                });
            }
        }

        protected virtual void OnChanged(MessagesBufferProcessorEventArgs<T> e)
        {
            EventHandler handler = Changed;
            handler?.Invoke(this, e);
        }

        protected virtual void OnEmptyListReached(MessagesBufferProcessorEventArgs<T> e)
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
