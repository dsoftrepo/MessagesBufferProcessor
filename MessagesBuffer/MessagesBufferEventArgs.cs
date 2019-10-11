using System;

namespace MessagesBuffer
{
    public class MessagesBufferEventArgs : EventArgs
    {
		public MessagesBufferEventArgs(string subject)
		{
			Subject = subject;
		}

		public EventType ChangeType { get; set; }
		public string MessageId { get; set; }
		public string Name { get; set; }
		public string Subject { get; set; }
		public string Message { get; set; }
		public int ToProcess { get; set; }
		public int Processed { get; set; }
		public long DurationMs { get; set; }
	}
}
