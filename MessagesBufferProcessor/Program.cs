using System;
using System.Threading;
using MessagesBufferProcessor;

namespace MessagesBufferProcessorApp
{
    internal class Program
    {
        private static void Main()
        {
            Console.WriteLine("press 'a', 's', 'z' key to add new message to buffer");

            void MessageProcessAction(int x)
            {
                Thread.Sleep(new Random().Next(1000));
            }

            var messageBufferProcessor = new MessagesBufferProcessor<int>(1000, 2, MessageProcessAction);

            messageBufferProcessor.MessageBufferChanged += MessageBufferProcessor_MessageBufferChanged;

            var input = new char();

            while (input!='q')
            {
                input = Console.ReadKey(true).KeyChar;
                switch (input)
                {
                    case 'a':
                        messageBufferProcessor.AddMessage("Sip1", new Random().Next(10));
                        break;
                    case 's':
                        messageBufferProcessor.AddMessage("Sip2", new Random().Next(10));
                        break;
                    case 'z':
                        messageBufferProcessor.AddMessage("Sip3", new Random().Next(10));
                        break;
                    case 'c':
                        messageBufferProcessor.ClearProcessedMessages();
                        break;
                }
            }
        }

        private static void MessageBufferProcessor_MessageBufferChanged(object sender, EventArgs e)
        {
            if (e is MessagesBufferProcessorEventArgs<int> args)
            {
                Console.WriteLine("{0} : processed {1} , to process : {2}", args.Subject, args.Processed, args.ToProcess);

                if (args.ToProcess == 0)
                {
                    Console.WriteLine("{0} : Done", args.Subject);
                }
            }
        }
    }
}
