using System;

namespace MessagesBuffer
{
	public interface IMessagesBuffer<TMessage>
	{
		event EventHandler BufferStarted;
		event EventHandler BufferEmpty;
		event EventHandler BufferChanged;

		void RegisterCanProcessFunc(Func<TMessage, bool> func);
		void RegisterProcessingAction(Action<TMessage> action);
		void PushNewMessage(string subject, TMessage eventMessage, string messageId, string name, string messageTag = null);
		void ClearProcessedMessages(string subject = null);
	}
}
