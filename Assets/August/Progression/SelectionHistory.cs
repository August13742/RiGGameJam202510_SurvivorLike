using System.Collections.Generic;

namespace Survivor.Progression
{
    public sealed class SelectionHistory
    {
        private readonly Dictionary<string, int> _picks = new();
        private readonly HashSet<string> _capped = new();

        public int Count(string id) => _picks.TryGetValue(id, out var n) ? n : 0;
        public void RecordPick(string id) => _picks[id] = Count(id) + 1;
        public void MarkCapped(string id) => _capped.Add(id);
        public bool IsCapped(string id) => _capped.Contains(id);
    }
}
