using UnityEngine;

namespace Survivor.Game
{
    [CreateAssetMenu(menuName = "Defs/Boss Rush Run", fileName = "NewBossRushRun")]
    public sealed class BossRushRunDef : ScriptableObject
    {
        [Header("Sequence / Starting State")]
        [SerializeField] private BossDef[] sequence;
        [SerializeField] private int startingLevels = 0;
        [SerializeField] private int startingGold = 0;

        [Header("HP Scaling (per index)")]
        [Tooltip("If true, each subsequent boss gets extra Max HP based on its index in the sequence.")]
        [SerializeField] private bool enableHpScaling = true;

        [Tooltip("Extra HP per index.\nIndex 0: x(1 + 0 * k)\nIndex 1: x(1 + 1 * k)\nIndex 2: x(1 + 2 * k) etc.")]
        [SerializeField] private float hpScalePerIndex = 0.25f;

        public BossDef[] Sequence => sequence;
        public int StartingLevels => startingLevels;
        public int StartingGold => startingGold;

        public bool EnableHpScaling => enableHpScaling;
        public float HpScalePerIndex => hpScalePerIndex;
    }
}
