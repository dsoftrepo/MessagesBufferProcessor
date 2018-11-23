using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace MessagesBufferProcessor
{
    public class MessagesBufferProcessor<TMessage>
    {
        private int _runningTasks;
        private readonly Dictionary<string, IDisposable> _subscriptions = new Dictionary<string, IDisposable>();
        private readonly MessagesBuffer<MessageContainer<TMessage>> _messagesBuffer = new MessagesBuffer<MessageContainer<TMessage>>();
        private readonly int _observingInterval;
        private readonly int _processingBatchSize;
        private Action<TMessage> _processingAction;

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
            _observingInterval = observingInterval;
            _processingBatchSize = processingBatchSize;
            _messagesBuffer.EmptyListReached += OnEmptyReached;
            _messagesBuffer.FirstMessageArrived += OnFirstMessageArrived;
        }

        public void RegisterProcessingAction(Action<TMessage> action)
        {
            _processingAction = action;
        }

        public void PushNewMessage(string subject, TMessage message)
        {
            _messagesBuffer.AddMessage(subject, new MessageContainer<TMessage>(message));
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
                    container.Running = true;

                    Task.Run(() =>
                    {
                        _runningTasks++;
                        _processingAction.Invoke(container.Message);
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
            if (_subscriptions.ContainsKey(args.Subject) && _subscriptions[args.Subject] != null)
            {
                _subscriptions[args.Subject].Dispose();
                _subscriptions[args.Subject] = null;
            }
        }
    }
}
