using UnityEngine;

namespace Survivor.Game
{
    [CreateAssetMenu(menuName = "Defs/Boss Rush Run", fileName = "NewBossRushRun")]
    public sealed class BossRushRunDef : ScriptableObject
    {
        [SerializeField] private BossDef[] sequence;
        [SerializeField] private int startingLevels = 0;
        [SerializeField] private int startingGold = 0;

        public BossDef[] Sequence => sequence;
        public int StartingLevels => startingLevels;
        public int StartingGold => startingGold;
    }
}
