using System;

namespace OguzhanOzdemir.ProgressionDemo
{
    [Serializable]
    public sealed class ProgressionCurve
    {
        public int BaseXp { get; }
        public int GrowthPerLevel { get; }
        public int MaxLevel { get; }

        public ProgressionCurve(int baseXp = 600, int growthPerLevel = 250, int maxLevel = 12)
        {
            if (baseXp <= 0) throw new ArgumentOutOfRangeException(nameof(baseXp));
            if (growthPerLevel < 0) throw new ArgumentOutOfRangeException(nameof(growthPerLevel));
            if (maxLevel < 1) throw new ArgumentOutOfRangeException(nameof(maxLevel));
            BaseXp = baseXp;
            GrowthPerLevel = growthPerLevel;
            MaxLevel = maxLevel;
        }

        public int RequiredXp(int level)
        {
            if (level < 1 || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return checked(BaseXp + ((level - 1) * GrowthPerLevel));
        }
    }

    [Serializable]
    public struct ProgressionSnapshot
    {
        public int level;
        public int xp;

        public ProgressionSnapshot(int level, int xp)
        {
            this.level = level;
            this.xp = xp;
        }
    }

    public struct ProgressionChange
    {
        public int LevelsGained { get; }
        public int XpApplied { get; }
        public bool ReachedMaximum { get; }

        public ProgressionChange(int levelsGained, int xpApplied, bool reachedMaximum)
        {
            LevelsGained = levelsGained;
            XpApplied = xpApplied;
            ReachedMaximum = reachedMaximum;
        }
    }

    public sealed class ProgressionState
    {
        private readonly ProgressionCurve _curve;

        public int Level { get; private set; }
        public int Xp { get; private set; }
        public bool IsMaximumLevel => Level >= _curve.MaxLevel;
        public int RequiredXp => IsMaximumLevel ? 0 : _curve.RequiredXp(Level);
        public float NormalizedProgress => IsMaximumLevel ? 1f : (float)Xp / RequiredXp;

        public ProgressionState(ProgressionCurve curve, ProgressionSnapshot snapshot)
        {
            _curve = curve ?? throw new ArgumentNullException(nameof(curve));
            Level = Math.Max(1, Math.Min(snapshot.level, curve.MaxLevel));
            Xp = Math.Max(0, snapshot.xp);
            NormalizeLoadedState();
        }

        public ProgressionChange AddXp(int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (IsMaximumLevel) return new ProgressionChange(0, 0, true);

            int applied = amount;
            int gained = 0;
            Xp = checked(Xp + amount);
            while (!IsMaximumLevel && Xp >= _curve.RequiredXp(Level))
            {
                Xp -= _curve.RequiredXp(Level);
                Level++;
                gained++;
            }
            if (IsMaximumLevel) Xp = 0;
            return new ProgressionChange(gained, applied, IsMaximumLevel);
        }

        public ProgressionSnapshot Snapshot() => new ProgressionSnapshot(Level, Xp);

        private void NormalizeLoadedState()
        {
            while (!IsMaximumLevel && Xp >= _curve.RequiredXp(Level))
            {
                Xp -= _curve.RequiredXp(Level);
                Level++;
            }
            if (IsMaximumLevel) Xp = 0;
        }
    }
}

