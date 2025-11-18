using UnityEngine;
using Survivor.Enemy.FSM;

namespace Survivor.Game
{
    [CreateAssetMenu(menuName = "Defs/Boss", fileName = "NewBossDef")]
    public sealed class BossDef : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private BossController prefab;
        [SerializeField] private Sprite portrait;

        public string Id => id;
        public string DisplayName => displayName;
        public BossController Prefab => prefab;
        public Sprite Portrait => portrait;
    }
}
