using System;

namespace MessagesBufferProcessor
{
    public class MessagesBufferEventArgs : EventArgs
    {
        public MessagesBufferEventArgs(string subject)
        {
            Subject = subject;
        }

        public string ChangeType { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public int ToProcess { get; set; }
        public int Processed { get; set; }
    }
}
