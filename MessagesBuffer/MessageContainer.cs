namespace MessagesBuffer
{
	public class MessageContainer<T>
	{
		public MessageContainer(T message, string id, string name, string tag = null)
		{
			Message = message;
			Name = name;
			Tag = tag;
			Id = id;
		}
		public string Id { get; set; }
		public string Tag { get; set; }
		public string Name { get; set; }
		public bool Running { get; set; }
		public T Message { get; set; }
		public long ProcessedInMs { get; set; }
	}
}
