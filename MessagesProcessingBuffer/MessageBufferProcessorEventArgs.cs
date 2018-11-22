using System;

namespace MessagesBuffer
{
    public class MessagesBufferProcessorEventArgs<T> : EventArgs
    {
        public MessagesBufferProcessorEventArgs(string subject)
        {
            Subject = subject;
        }

        public string Subject { get; set; }
        public string Message { get; set; }
        public int ToProcess { get; set; }
        public int Processed { get; set; }
    }
}
