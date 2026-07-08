using System;
using NUnit.Framework;
using KK.UI.UMG;

namespace KK.UI.UMG.Tests
{
    public sealed class ViewModelStoreTests
    {
        [Test]
        public void TakeDirtyReturnsUpdatedFieldsOnce()
        {
            var store = new ViewModelStore();

            store.Update("Message", "hello");

            var dirty = store.TakeDirty();
            Assert.That(dirty, Has.Count.EqualTo(1));
            Assert.That(dirty[0].FieldId, Is.EqualTo("Message"));
            Assert.That(dirty[0].Value, Is.EqualTo("hello"));
            Assert.That(store.TakeDirty(), Is.Empty);
        }

        [Test]
        public void DisposeClearsAndBlocksFurtherWrites()
        {
            var store = new ViewModelStore();
            store.Update("Message", "hello");

            store.Dispose();

            Assert.Throws<ObjectDisposedException>(() => store.Update("Message", "goodbye"));
            Assert.Throws<ObjectDisposedException>(() => store.TakeDirty());
        }

        [Test]
        public void DisposeCanBeCalledMoreThanOnce()
        {
            var store = new ViewModelStore();

            store.Dispose();
            store.Dispose();

            Assert.Pass();
        }

        [Test]
        public void TakeDirtyReusesBuffer()
        {
            var store = new ViewModelStore();
            store.Update("Message", "hello");

            var first = store.TakeDirty();
            store.Update("Message", "goodbye");
            var second = store.TakeDirty();

            Assert.That(ReferenceEquals(first, second), Is.True);
            Assert.That(second, Has.Count.EqualTo(1));
            Assert.That(second[0].Value, Is.EqualTo("goodbye"));
        }
    }
}
