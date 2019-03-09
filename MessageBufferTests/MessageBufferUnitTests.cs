using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using MessagesBuffer;

namespace TDR.Utilities.UnitTests
{
    [TestFixture]
    public class MessageBufferUnitTests
    {
        [Test]
        public void PushNewMessageShouldThrowExceptionIfProcessingActionNotSet()
        {
            //Arrange
            var buffer = new MessagesBufferProcessor<int>(100, 2);
            //Act//Assert
            Assert.Throws<NullReferenceException>(() =>
            {
                buffer.PushNewMessage("test", 100);
            });
        }

        [Test]
        public void PushNewMessageShouldAddNewMessageToListAndObservableShouldStartSubscription()
        {
            //Arrange
            var started = false;
            MessagesBufferProcessor<int> buffer = GetBuffer();
            buffer.BufferStarted += (sender, e) =>
            {
                if (e is MessagesBufferEventArgs args)
                {
                    started = true;
                    Assert.AreEqual(EventType.Started, args.ChangeType);
                    Assert.AreEqual(0, args.Processed);
                }
                else
                {
                    Assert.Fail("Wrong EventArgs type");
                }
            };

            //Act
            buffer.PushNewMessage("test", 100);

            //Assert
            Assert.IsTrue(started);
            Assert.AreEqual(1, buffer.GetMessages("test").Count());
        }

        [Test]
        public void PushNewMessageShouldAddNewMessageToListAndTriggerChangedEvent()
        {
            //Arrange
            var count = 0;
            var emptyEvent = new AutoResetEvent(false);
            MessagesBufferProcessor<int> buffer = GetBuffer();
            buffer.BufferChanged += (sender, e) =>
            {
                if (e is MessagesBufferEventArgs args)
                {
                    count++;
                }

                if (count == 4)
                {
                    emptyEvent.Set();
                }
            };

            //Act//Assert
            buffer.PushNewMessage("test", 100);
            Assert.AreEqual(1, count);
            Assert.AreEqual(1, buffer.GetMessages("test").Count());
            buffer.PushNewMessage("test", 100);
            Assert.AreEqual(2, count);
            Assert.AreEqual(2, buffer.GetMessages("test").Count());
            Assert.IsTrue(emptyEvent.WaitOne(500), "should take less than 500 ms");
        }

        [Test]
        public void ProcessingActionShouldTriggerChangedEvent()
        {
            //Arrange
            var count = 0;
            var emptyEvent = new AutoResetEvent(false);
            MessagesBufferProcessor<int> buffer = GetBuffer();
            buffer.BufferChanged += (sender, e) =>
            {
                if (e is MessagesBufferEventArgs args)
                {
                    count++;
                }

                if (count == 8)
                {
                    emptyEvent.Set();
                }
            };

            //Act
            for (var i = 0; i < 4; i++)
            {
                buffer.PushNewMessage("test", 50);
            }

            //Assert
            Assert.AreEqual(4, buffer.GetMessages("test").Count());
            Assert.IsTrue(emptyEvent.WaitOne(500), "should take less than 500 ms");
            Assert.IsTrue(!buffer.GetMessages("test").Any());
            Assert.AreEqual(0, buffer.GetMessages("test").Count());
            Assert.AreEqual(4, buffer.GetProcessedMessages("test").Count());
        }

        [Test]
        public void PushNewMessagesShouldInitObservableAndProcessMessages()
        {
            //Arrange
            var emptyEvent = new AutoResetEvent(false);
            MessagesBufferProcessor<int> buffer = GetBuffer();
            buffer.BufferEmpty += (sender, e) =>
            {
                if (e is MessagesBufferEventArgs args)
                {
                    emptyEvent.Set();
                }
            };

            //Act
            for (var i = 0; i < 4; i++)
            {
                buffer.PushNewMessage("test", 100);
            }

            //Assert
            Assert.AreEqual(4, buffer.GetMessages("test").Count());
            Assert.IsTrue(emptyEvent.WaitOne(1000), "should take less than 1000 ms");
            Assert.IsFalse(buffer.GetMessages("test").Any());
        }

        [Test]
        public void PushNewMessagesShouldInitObservableAndProcessMessagesForAllSubjects()
        {
            //Arrange
            var subjects = new List<string>();
            var emptyEvent = new AutoResetEvent(false);

            MessagesBufferProcessor<int> buffer = GetBuffer();
            buffer.BufferEmpty += (sender, e) =>
            {
                if (e is MessagesBufferEventArgs args)
                {
                    subjects.Add(args.Subject);
                }
                else
                {
                    Assert.Fail($"Expected {nameof(MessagesBufferEventArgs)} but got {e.GetType().Name}");
                }

                if (subjects.Count == 3)
                {
                    emptyEvent.Set();
                }
            };

            //Act
            for (var i = 0; i < 4; i++)
            {
                buffer.PushNewMessage("test_1", 100);
                buffer.PushNewMessage("test_2", 100);
                buffer.PushNewMessage("test_3", 100);
            }

            //Assert
            Assert.AreEqual(4, buffer.GetMessages("test_1").Count());
            Assert.AreEqual(4, buffer.GetMessages("test_2").Count());
            Assert.AreEqual(4, buffer.GetMessages("test_3").Count());
            Assert.IsTrue(emptyEvent.WaitOne(1000), "should take less than 1000 ms");
            Assert.AreEqual(3, subjects.Count);
        }

        [Test]
        public void SubjectsShouldRunIndependently()
        {
            //Arrange
            var subjects = new Dictionary<string, long>();
            var emptyEvent = new AutoResetEvent(false);
            var sw = new Stopwatch();
            sw.Start();

            MessagesBufferProcessor<int> buffer = GetBuffer();
            buffer.BufferEmpty += (sender, e) =>
            {
                if (e is MessagesBufferEventArgs args)
                {
                    sw.Stop();
                    subjects.Add(args.Subject, sw.ElapsedMilliseconds);
                    sw.Start();

                    if (subjects.Count == 3)
                    {
                        emptyEvent.Set();
                    }
                }
                else
                {
                    Assert.Fail($"Expected {nameof(MessagesBufferEventArgs)} but got {e.GetType().Name}");
                }
            };

            //Act
            for (var i = 0; i < 8; i++)
            {
                buffer.PushNewMessage("test_1", 100);
                buffer.PushNewMessage("test_2", 200);
                buffer.PushNewMessage("test_3", 400);
            }

            //Assert
            Assert.AreEqual(8, buffer.GetMessages("test_1").Count());
            Assert.AreEqual(8, buffer.GetMessages("test_2").Count());
            Assert.AreEqual(8, buffer.GetMessages("test_3").Count());
            Assert.IsTrue(emptyEvent.WaitOne(4000), "should take less than 4000 ms");
            Assert.AreEqual(3, subjects.Count);
            Assert.IsTrue(subjects["test_1"] < 3000, $"test_1 time {subjects["test_1"]}");
            Assert.IsTrue(subjects["test_2"] < 3600, $"test_2 time {subjects["test_2"]}");
            Assert.IsTrue(subjects["test_3"] < 4000, $"test_3 time {subjects["test_3"]}");
        }

        [Test]
        public void PushNewMessagesShouldInitObservableAndProcessMessagesWithinCorrectTime()
        {
            //Arrange
            var emptyEvent = new AutoResetEvent(false);
            var sw = new Stopwatch();
            sw.Start();

            MessagesBufferProcessor<int> buffer = GetBuffer();
            buffer.BufferEmpty += (sender, e) =>
            {
                sw.Stop();

                if (e is MessagesBufferEventArgs args)
                {
                    Assert.AreEqual(0, buffer.GetMessages("test").Count());
                }
                else
                {
                    Assert.Fail($"Expected {nameof(MessagesBufferEventArgs)} but got {e.GetType().Name}");
                }

                emptyEvent.Set();
            };

            //Act
            for (var i = 0; i < 4; i++)
            {
                buffer.PushNewMessage("test", 50);
            }

            //Assert
            Assert.AreEqual(4, buffer.GetMessages("test").Count());
            Assert.IsTrue(emptyEvent.WaitOne(500), "should take less than 500 ms");
        }

        [Test]
        public void ClearProcessedMessagesShouldDoIt()
        {
            var emptyEvent = new AutoResetEvent(false);
            var sw = new Stopwatch();
            sw.Start();

            //Arrange
            MessagesBufferProcessor<int> buffer = GetBuffer();
            buffer.BufferEmpty += (sender, e) =>
            {
                sw.Stop();
                if (e is MessagesBufferEventArgs args)
                {
                    Assert.AreEqual(0, buffer.GetMessages("test").Count());
                    Assert.AreEqual(4, buffer.GetProcessedMessages("test").Count());
                    buffer.ClearProcessedMessages("test");
                    Assert.AreEqual(0, buffer.GetProcessedMessages("test").Count());
                }
                else
                {
                    Assert.Fail($"Expected {nameof(MessagesBufferEventArgs)} but got {e.GetType().Name}");
                }
                emptyEvent.Set();
            };

            //Act
            for (var i = 0; i < 4; i++)
            {
                buffer.PushNewMessage("test", 50);
            }

            //Assert
            Assert.AreEqual(4, buffer.GetMessages("test").Count());
            Assert.AreEqual(0, buffer.GetProcessedMessages("test").Count());
            Assert.IsTrue(emptyEvent.WaitOne(500), "should take less than 500 ms");
        }

        private static MessagesBufferProcessor<int> GetBuffer()
        {
            void Wait(int x)
            {
                Thread.Sleep(x);
            }

            var buffer = new MessagesBufferProcessor<int>(100, 4);
            buffer.RegisterProcessingAction(Wait);

            return buffer;
        }
    }
}
