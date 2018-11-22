using System;

namespace MessagesBufferProcessor
{
    public class MessagesBufferEventArgs : EventArgs
    {
        public MessagesBufferEventArgs(string subject)
        {
            Subject = subject;
        }

        public string Subject { get; set; }
    }
}
