using NUnit.Framework;

namespace OguzhanOzdemir.ProgressionDemo.Tests
{
    public sealed class ProgressionStateTests
    {
        [Test]
        public void LargeGrantCrossesMultipleLevelsAndKeepsOverflow()
        {
            ProgressionState state = new ProgressionState(new ProgressionCurve(100, 50, 10), new ProgressionSnapshot(1, 75));
            ProgressionChange change = state.AddXp(225);
            Assert.That(change.LevelsGained, Is.EqualTo(2));
            Assert.That(state.Level, Is.EqualTo(3));
            Assert.That(state.Xp, Is.EqualTo(50));
        }

        [Test]
        public void MaximumLevelCapsProgressAndRejectsFurtherMutation()
        {
            ProgressionState state = new ProgressionState(new ProgressionCurve(100, 0, 2), new ProgressionSnapshot(1, 0));
            ProgressionChange first = state.AddXp(300);
            ProgressionChange second = state.AddXp(100);
            Assert.That(first.ReachedMaximum, Is.True);
            Assert.That(state.Level, Is.EqualTo(2));
            Assert.That(state.Xp, Is.Zero);
            Assert.That(second.XpApplied, Is.Zero);
        }

        [Test]
        public void LoadedOverflowIsNormalized()
        {
            ProgressionState state = new ProgressionState(new ProgressionCurve(100, 0, 5), new ProgressionSnapshot(1, 250));
            Assert.That(state.Level, Is.EqualTo(3));
            Assert.That(state.Xp, Is.EqualTo(50));
        }
    }
}

