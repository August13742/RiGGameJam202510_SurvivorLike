using UnityEngine;

namespace Survivor.Enemy
{
    public sealed class EnemyChaser : EnemyBase
    {
       
        private void Start()
        {
            _target = GameObject.FindWithTag("Player")?.transform;
        }

        private void FixedUpdate()
        {
            Move();
        }
        
    }
}
