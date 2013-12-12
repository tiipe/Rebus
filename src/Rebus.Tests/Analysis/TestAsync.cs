using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Transports.Msmq;
using Task = System.Threading.Tasks.Task;

namespace Rebus.Tests.Analysis
{
    [TestFixture]
    public class TestAsync : FixtureBase
    {
        BuiltinContainerAdapter adapter;
        ManualResetEvent messageHasBeenProcessedEvent;

        protected override void DoSetUp()
        {
            adapter = new BuiltinContainerAdapter();
            messageHasBeenProcessedEvent = new ManualResetEvent(false);

            Configure.With(TrackDisposable(adapter))
                .Logging(l => l.ColoredConsole(minLevel: LogLevel.Warn))
                .Transport(t => t.UseMsmq("test_async_handlers.input", "error"))
                .Behavior(b => b.SetMaxRetriesFor<UnhandledMessageException>(0))
                .CreateBus()
                .Start(1);
        }

        [Test]
        public void CanDoIt()
        {
            adapter.Register(() => new HasAsyncHandleMethod(messageHasBeenProcessedEvent));

            adapter.Bus.SendLocal(new SomeMessage {Delay = TimeSpan.FromSeconds(1)});

            messageHasBeenProcessedEvent.WaitUntilSetOrDie(50.Seconds());
        }

        [Test]
        public void CanDoItWithVoid()
        {
            adapter.Register(() => new HasAsyncVoidHandleMethod(messageHasBeenProcessedEvent));

            adapter.Bus.SendLocal(new SomeMessage {Delay = TimeSpan.FromSeconds(1)});

            messageHasBeenProcessedEvent.WaitUntilSetOrDie(50.Seconds());
        }

    }

    public class HasAsyncHandleMethod : IHandleMessagesAsync<SomeMessage>
    {
        readonly ManualResetEvent resetEvent;

        public HasAsyncHandleMethod(ManualResetEvent resetEvent)
        {
            this.resetEvent = resetEvent;
        }

        public async Task Handle(SomeMessage message)
        {
            Console.WriteLine("Starting on {0}", Thread.CurrentThread.Name);

            await Task.Delay((int) message.Delay.TotalMilliseconds);
            
            Console.WriteLine("Landing on {0}", Thread.CurrentThread.Name);

            await Task.Delay((int) message.Delay.TotalMilliseconds);
            
            Console.WriteLine("Ending on {0}", Thread.CurrentThread.Name);

            resetEvent.Set();
        }
    }

    public class HasAsyncVoidHandleMethod : IHandleMessages<SomeMessage>
    {
        readonly ManualResetEvent resetEvent;

        public HasAsyncVoidHandleMethod(ManualResetEvent resetEvent)
        {
            this.resetEvent = resetEvent;
        }

        public async void Handle(SomeMessage message)
        {
            Console.WriteLine("Starting on {0}", Thread.CurrentThread.Name);

            await Task.Delay((int) message.Delay.TotalMilliseconds);

            Console.WriteLine("Landing on {0}", Thread.CurrentThread.Name);

            await Task.Delay((int)message.Delay.TotalMilliseconds);

            Console.WriteLine("Ending on {0}", Thread.CurrentThread.Name);

            resetEvent.Set();
        }
    }

    public class SomeMessage
    {
        public TimeSpan Delay { get; set; }
    }
}