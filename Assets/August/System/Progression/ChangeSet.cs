using System.Collections.Generic;

namespace Survivor.Progression
{
    public sealed class ChangeSet
    {
        public readonly List<string> PreviewLines = new();
        public void Add(string line) => PreviewLines.Add(line);
    }
}
