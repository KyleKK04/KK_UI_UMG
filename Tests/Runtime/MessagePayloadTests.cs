using NUnit.Framework;
using KK.UI.UMG;

namespace KK.UI.UMG.Tests
{
    public sealed class MessagePayloadTests
    {
        [Test]
        public void SetAndGetRoundTripsTypedValue()
        {
            var payload = new MessagePayload();

            payload.Set("title", "hello");

            Assert.That(payload.Get<string>("title"), Is.EqualTo("hello"));
            Assert.That(payload.TryGet<string>("title", out var value), Is.True);
            Assert.That(value, Is.EqualTo("hello"));
        }

        [Test]
        public void GetMissingKeyThrows()
        {
            var payload = new MessagePayload();

            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => payload.Get<string>("missing"));
        }

        [Test]
        public void NullReferenceValueCanBeRead()
        {
            var payload = new MessagePayload();

            payload.Set<string>("icon", null);

            Assert.That(payload.TryGet<object>("icon", out var raw), Is.True);
            Assert.That(raw, Is.Null);
            Assert.That(payload.TryGet<string>("icon", out var text), Is.True);
            Assert.That(text, Is.Null);
        }

        [Test]
        public void NullValueDoesNotMatchNonNullableValueType()
        {
            var payload = new MessagePayload();

            payload.Set<object>("count", null);

            Assert.That(payload.TryGet<int>("count", out _), Is.False);
        }
    }
}
