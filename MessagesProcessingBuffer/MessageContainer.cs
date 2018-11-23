namespace MessagesBufferProcessor
{
    public class MessageContainer<T>
    {
        public MessageContainer(T message)
        {
            Message = message;
        }

        public bool Running { get; set; }
        public T Message { get; set; }
    }
}
