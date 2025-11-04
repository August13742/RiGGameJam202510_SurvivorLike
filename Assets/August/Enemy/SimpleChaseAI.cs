using UnityEngine;

namespace Survivor.Enemy
{
    public sealed class EnemyChaser : EnemyBase
    {
        [SerializeField] private ChaserEnemyDef def;
        private new void Awake()
        {
            base.Awake();
            _def = def;
        }
        private void Start()
        {
            _target = Game.SessionManager.Instance.GetPlayerReference().transform;
        }

        private void FixedUpdate()
        {
            Move();
        }
        
    }
}
