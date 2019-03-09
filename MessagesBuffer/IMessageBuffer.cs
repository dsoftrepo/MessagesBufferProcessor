using System;

namespace MessagesBuffer
{
    public interface IMessagesBuffer<TAssetEvent>
    {
        event EventHandler BufferStarted;
        event EventHandler BufferEmpty;
        event EventHandler BufferChanged;
        void RegisterProcessingAction(Action<TAssetEvent> action);
        void PushNewMessage(string sipName, TAssetEvent eventMessage);
        void ClearProcessedMessages(string subject = null);
    }
}
