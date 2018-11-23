using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace MessagesBufferProcessor
{
    public class MessagesBufferProcessor<TMessage>
    {
        private readonly Dictionary<string, IDisposable> _subscriptions = new Dictionary<string, IDisposable>();
        private readonly MessagesBuffer<TMessage> _messagesBuffer = new MessagesBuffer<TMessage>();
        private readonly int _observingInterval;
        private readonly int _processingBatchSize;
        private readonly Action<TMessage> _processingAction;

        public event EventHandler MessageBufferChanged
        {
            add => _messagesBuffer.Changed += value;
            remove => _messagesBuffer.Changed -= value;
        }

        public MessagesBufferProcessor(int observingInterval, int processingBatchSize, Action<TMessage> processingAction)
        {
            _observingInterval = observingInterval;
            _processingBatchSize = processingBatchSize;
            _processingAction = processingAction;
            _messagesBuffer.EmptyListReached += OnEmptyReached;
            _messagesBuffer.FirstMessageArrived += OnFirstMessageArrived;
        }

        public void AddMessage(string subject, TMessage message)
        {
            _messagesBuffer.AddMessage(subject, message);
        }

        public void GetMessages(string subject = null)
        {
            _messagesBuffer.GetMessages(subject);
        }

        public void ClearProcessedMessages(string subject = null)
        {
            _messagesBuffer.ClearProcessedMessages(subject);
        }

        public void GetProcessedMessages(string subject = null)
        {
            _messagesBuffer.GetMessages(subject);
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
                IEnumerable<TMessage> toProcess = _messagesBuffer.GetMessages(args.Subject, _processingBatchSize);

                foreach (TMessage message in toProcess)
                {
                    Task.Run(() =>
                        {
                            _processingAction.Invoke(message);
                        })
                        .ContinueWith(x =>
                        {
                            _messagesBuffer.RemoveMessage(args.Subject, message);
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
