using UnityEngine;

namespace Survivor.Progression
{
    public sealed class UpgradeCardVM
    {
        public string Id;
        public string Title;
        public string Subtitle;
        public string Description;
        public Sprite Icon;
        public Rarity Rarity;
        public string[] PreviewLines; // “+15% MoveSpeed”, “+1 Projectile”, etc.
        public bool IsDisabled;
        public string DisabledReason;
    }

    public enum Rarity { Common, Uncommon, Rare }
}
