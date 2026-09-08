using System;
using System.Collections.Generic;

namespace OguzhanOzdemir.ProgressionDemo
{
    public enum NavigationIntent
    {
        Advance,
        Finish
    }

    public struct NavigationMove
    {
        public int From { get; }
        public int To { get; }
        public NavigationIntent Intent { get; }

        public NavigationMove(int from, int to, NavigationIntent intent)
        {
            From = from;
            To = to;
            Intent = intent;
        }
    }

    public sealed class NavigationQueue
    {
        private readonly Queue<NavigationIntent> _pending = new Queue<NavigationIntent>();
        private readonly int _waypointCount;

        public int CurrentIndex { get; private set; }
        public bool IsMoving { get; private set; }
        public int PendingCount => _pending.Count;

        public NavigationQueue(int waypointCount, int initialIndex = 0)
        {
            if (waypointCount < 2) throw new ArgumentOutOfRangeException(nameof(waypointCount));
            if (initialIndex < 0 || initialIndex >= waypointCount)
                throw new ArgumentOutOfRangeException(nameof(initialIndex));
            _waypointCount = waypointCount;
            CurrentIndex = initialIndex;
        }

        public void Request(NavigationIntent intent)
        {
            if (intent == NavigationIntent.Finish) _pending.Clear();
            _pending.Enqueue(intent);
        }

        public bool TryBeginNext(out NavigationMove move)
        {
            move = default;
            if (IsMoving || _pending.Count == 0) return false;
            NavigationIntent intent = _pending.Dequeue();
            int target = intent == NavigationIntent.Finish
                ? _waypointCount - 1
                : Math.Min(CurrentIndex + 1, _waypointCount - 1);
            move = new NavigationMove(CurrentIndex, target, intent);
            IsMoving = target != CurrentIndex;
            return true;
        }

        public void Complete(NavigationMove move)
        {
            if (!IsMoving) throw new InvalidOperationException("No move is in progress.");
            if (move.From != CurrentIndex || move.To < 0 || move.To >= _waypointCount)
                throw new InvalidOperationException("Move does not match the active queue state.");
            CurrentIndex = move.To;
            IsMoving = false;
        }
    }
}

