using System;
using NUnit.Framework;
using KK.UI.UMG.MessageBus;

namespace KK.UI.UMG.Tests
{
    public sealed class UIMessageBusTests
    {
        [Test]
        public void PublishCallsSubscribedHandlerSynchronously()
        {
            var channel = UniqueChannel("publish");
            var called = false;
            var subscription = UIMessageBus.Subscribe(channel, (receivedChannel, payload) =>
            {
                called = true;
                Assert.That(receivedChannel, Is.EqualTo(channel));
                Assert.That(payload.Get<string>("message"), Is.EqualTo("hello"));
            });

            try
            {
                var payload = new MessagePayload();
                payload.Set("message", "hello");

                UIMessageBus.Publish(channel, payload);

                Assert.That(called, Is.True);
            }
            finally
            {
                subscription.Dispose();
            }
        }

        [Test]
        public void DisposeUnsubscribesHandler()
        {
            var channel = UniqueChannel("dispose");
            var calls = 0;
            var subscription = UIMessageBus.Subscribe(channel, (receivedChannel, payload) => calls++);

            subscription.Dispose();
            subscription.Dispose();
            UIMessageBus.Publish(channel);

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void DisposingInsideHandlerDoesNotBreakCurrentPublishSnapshot()
        {
            var channel = UniqueChannel("snapshot");
            var calls = 0;
            IDisposable first = null;
            first = UIMessageBus.Subscribe(channel, (receivedChannel, payload) =>
            {
                calls++;
                first.Dispose();
            });
            var second = UIMessageBus.Subscribe(channel, (receivedChannel, payload) => calls++);

            try
            {
                UIMessageBus.Publish(channel);

                Assert.That(calls, Is.EqualTo(2));
            }
            finally
            {
                first.Dispose();
                second.Dispose();
            }
        }

        [Test]
        public void HandlerExceptionDoesNotStopFollowingHandlers()
        {
            var channel = UniqueChannel("exception");
            var calls = 0;
            var first = UIMessageBus.Subscribe(channel, (receivedChannel, payload) => throw new InvalidOperationException("bus test"));
            var second = UIMessageBus.Subscribe(channel, (receivedChannel, payload) => calls++);
            var previousExceptionLogger = UIMessageBus.ExceptionLogger;
            Exception loggedException = null;

            try
            {
                UIMessageBus.ExceptionLogger = ex => loggedException = ex;
                UIMessageBus.Publish(channel);

                Assert.That(calls, Is.EqualTo(1));
                Assert.That(loggedException, Is.TypeOf<InvalidOperationException>());
                Assert.That(loggedException.Message, Is.EqualTo("bus test"));
            }
            finally
            {
                UIMessageBus.ExceptionLogger = previousExceptionLogger;
                first.Dispose();
                second.Dispose();
            }
        }

        [Test]
        public void DuplicateSubscriptionOnlyCallsHandlerOnce()
        {
            var channel = UniqueChannel("duplicate");
            var calls = 0;
            Action<string, MessagePayload> handler = (receivedChannel, payload) => calls++;
            var first = UIMessageBus.Subscribe(channel, handler);
            var second = UIMessageBus.Subscribe(channel, handler);

            try
            {
                UIMessageBus.Publish(channel);

                Assert.That(calls, Is.EqualTo(1));
            }
            finally
            {
                first.Dispose();
                second.Dispose();
            }
        }

        [Test]
        public void InvalidChannelThrows()
        {
            Assert.Throws<ArgumentException>(() => UIMessageBus.Subscribe("bad.channel", (channel, payload) => { }));
            Assert.Throws<ArgumentException>(() => UIMessageBus.Publish("ui.TestBox.side.event"));
        }

        private static string UniqueChannel(string suffix)
        {
            return $"ui.TestBox.in.{suffix}_{Guid.NewGuid():N}";
        }
    }
}
