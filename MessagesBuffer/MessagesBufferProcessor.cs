using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace MessagesBuffer
{
	public class MessagesBufferProcessor<TMessage> : IMessagesBuffer<TMessage>
	{
		private int _runningTasks;
		private readonly Dictionary<string, IDisposable> _subscriptions = new Dictionary<string, IDisposable>();
		private readonly MessagesBuffer<MessageContainer<TMessage>, TMessage> _messagesBuffer = new MessagesBuffer<MessageContainer<TMessage>, TMessage>();
		private readonly int _observingInterval;
		private readonly int _processingBatchSize;
		private Action<TMessage> _processingAction;
		private Func<TMessage, bool> _canProcess;

		public event EventHandler BufferChanged
		{
			add => _messagesBuffer.Changed += value;
			remove => _messagesBuffer.Changed -= value;
		}

		public event EventHandler BufferStarted
		{
			add => _messagesBuffer.FirstMessageArrived += value;
			remove => _messagesBuffer.FirstMessageArrived -= value;
		}

		public event EventHandler BufferEmpty
		{
			add => _messagesBuffer.EmptyListReached += value;
			remove => _messagesBuffer.EmptyListReached -= value;
		}

		public MessagesBufferProcessor(int observingInterval, int processingBatchSize)
		{
			_observingInterval = observingInterval == 0 ? 2000 : observingInterval;
			_processingBatchSize = processingBatchSize == 0 ? 4 : processingBatchSize;
			_messagesBuffer.EmptyListReached += OnEmptyReached;
			_messagesBuffer.FirstMessageArrived += OnFirstMessageArrived;
		}

		public void RegisterCanProcessFunc(Func<TMessage, bool> func)
		{
			_canProcess = func;
		}

		public void RegisterProcessingAction(Action<TMessage> action)
		{
			_processingAction = action;
		}

		public void PushNewMessage(string subject, TMessage message, string messageId, string name, string messageTag = null)
		{
			if (string.IsNullOrWhiteSpace(subject))
			{
				throw new NullReferenceException("[MessageBuffer] - Processor subject required. Batch / Package / Aggregate name or id is missing");
			}

			if (_processingAction == null)
			{
				throw new NullReferenceException("[MessageBuffer] - Processing action not registered, unable to process message");
			}

			_messagesBuffer.AddMessage(subject, new MessageContainer<TMessage>(message, messageId, name, messageTag));
		}

		public void ClearProcessedMessages(string subject = null)
		{
			_messagesBuffer.ClearProcessedMessages(subject);
		}

		public IEnumerable<TMessage> GetProcessedMessages(string subject = null)
		{
			return _messagesBuffer.GetProcessedMessages(subject).Select(x => x.Message);
		}

		public IEnumerable<TMessage> GetMessages(string subject = null)
		{
			return _messagesBuffer.GetMessages(subject).Select(x => x.Message);
		}

		private void OnFirstMessageArrived(object sender, EventArgs e)
		{
			if (!(e is MessagesBufferEventArgs args)) return;

			if (_subscriptions.ContainsKey(args.Subject) && _subscriptions[args.Subject] == null)
			{
				_subscriptions[args.Subject] = Subscription(args);
			}

			if (!_subscriptions.ContainsKey(args.Subject))
			{
				_subscriptions.Add(args.Subject, Subscription(args));
			}
		}

		private IDisposable Subscription(MessagesBufferEventArgs args)
		{
			return Observable.Interval(TimeSpan.FromMilliseconds(_observingInterval)).Subscribe(tick =>
			{
				if (_runningTasks >= _processingBatchSize) return;

				IEnumerable<MessageContainer<TMessage>> toProcess =
					_messagesBuffer
						.GetMessages(args.Subject, _processingBatchSize - _runningTasks)
						.Where(x => !x.Running)
						.ToList();

				foreach (MessageContainer<TMessage> container in toProcess)
				{
					if (_canProcess != null && !_canProcess.Invoke(container.Message)) continue;

					var timer = new Stopwatch();
					timer.Start();

					container.Running = true;
					Task.Run(() =>
					{
						_runningTasks++;
						_processingAction.Invoke(container.Message);
						timer.Stop();
						container.ProcessedInMs = timer.ElapsedMilliseconds;
					})
					.ContinueWith(x =>
					{
						_messagesBuffer.RemoveMessage(args.Subject, container);
						_runningTasks--;
					});
				}
			});
		}

		private void OnEmptyReached(object sender, EventArgs e)
		{
			if (!(e is MessagesBufferEventArgs args)) return;
			if (!_subscriptions.ContainsKey(args.Subject) || _subscriptions[args.Subject] == null) return;
			_subscriptions[args.Subject].Dispose();
			_subscriptions[args.Subject] = null;
		}
	}
}
