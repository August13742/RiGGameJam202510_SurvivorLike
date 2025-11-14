using System.Collections;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    public abstract class AttackPattern : ScriptableObject
    {
        // returns an IEnumerator so it can be run as a coroutine.
        public abstract IEnumerator Execute(BossController controller);
    }
}