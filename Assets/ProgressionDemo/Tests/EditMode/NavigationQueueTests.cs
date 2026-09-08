using NUnit.Framework;

namespace OguzhanOzdemir.ProgressionDemo.Tests
{
    public sealed class NavigationQueueTests
    {
        [Test]
        public void AdvanceCommandsRemainFifoAcrossAnimationBoundaries()
        {
            NavigationQueue queue = new NavigationQueue(5);
            queue.Request(NavigationIntent.Advance);
            queue.Request(NavigationIntent.Advance);
            Assert.That(queue.TryBeginNext(out NavigationMove first), Is.True);
            Assert.That(first.To, Is.EqualTo(1));
            Assert.That(queue.TryBeginNext(out _), Is.False);
            queue.Complete(first);
            Assert.That(queue.TryBeginNext(out NavigationMove second), Is.True);
            Assert.That(second.From, Is.EqualTo(1));
            Assert.That(second.To, Is.EqualTo(2));
        }

        [Test]
        public void FinishSupersedesPendingAdvanceCommands()
        {
            NavigationQueue queue = new NavigationQueue(7, 2);
            queue.Request(NavigationIntent.Advance);
            queue.Request(NavigationIntent.Advance);
            queue.Request(NavigationIntent.Finish);
            Assert.That(queue.PendingCount, Is.EqualTo(1));
            Assert.That(queue.TryBeginNext(out NavigationMove move), Is.True);
            Assert.That(move.Intent, Is.EqualTo(NavigationIntent.Finish));
            Assert.That(move.To, Is.EqualTo(6));
        }
    }
}

