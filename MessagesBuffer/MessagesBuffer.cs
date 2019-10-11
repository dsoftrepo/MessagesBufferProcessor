using System;
using System.Collections.Generic;
using System.Linq;

namespace MessagesBuffer
{
	public class MessagesBuffer<T, TMessage> where T : MessageContainer<TMessage>
	{
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
					if (!_processedMessages.ContainsKey(subject)) return;

					_processedMessages.Remove(subject);
					_messages.Remove(subject);
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
				if (!_messages.ContainsKey(subject) && _processedMessages.ContainsKey(subject))
				{
					OnEmptyListReached(GetChangedArgs(message.Id, message.Name, subject, $"[{subject}] - All [{_processedMessages[subject].Count}] items processed", _processedMessages[subject].Sum(x => x.ProcessedInMs), EventType.Completed));
					return;
				}

				_messages[subject].Remove(message);

				MarkAsProcessed(subject, message);

				OnChanged(GetChangedArgs(message.Id, message.Name, subject, $"[{message.Tag}] processed", message.ProcessedInMs, EventType.Updated));

				if (_messages[subject].Count != 0) return;

				OnEmptyListReached(GetChangedArgs(message.Id, message.Name, subject, $"[{subject}] - Processing queue completed, processed [{_processedMessages[subject].Count}] items", _processedMessages[subject].Sum(x => x.ProcessedInMs), EventType.Completed));
			}
		}

		private MessagesBufferEventArgs GetChangedArgs(string messageId, string name, string subject, string message, long duration, EventType type)
		{
			return new MessagesBufferEventArgs(subject)
			{
				MessageId = messageId,
				Name = name,
				ChangeType = type,
				Message = message,
				ToProcess = _messages.ContainsKey(subject) ? _messages[subject].Count : 0,
				Processed = _processedMessages.ContainsKey(subject) ? _processedMessages[subject].Count : 0,
				DurationMs = duration
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
						OnFirstArrived(GetChangedArgs(message.Id, message.Name, subject, $"[{subject}] - Processing queue re-activated for next batch", message.ProcessedInMs, EventType.Updated));
					}

					_messages[subject].Add(message);
				}
				else
				{
					_messages.Add(subject, new List<T> { message });
					OnFirstArrived(GetChangedArgs(message.Id, message.Name, subject, $"[{subject}] - Processing queue initialized", message.ProcessedInMs, EventType.Started));
				}

				OnChanged(GetChangedArgs(message.Id, message.Name, subject, $"[{message.Tag}] received for processing", message.ProcessedInMs, EventType.Added));
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
