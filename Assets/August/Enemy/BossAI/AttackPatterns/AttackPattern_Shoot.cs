using Survivor.Game;
using System.Collections;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(fileName = "New Pattern_Shoot", menuName = "Defs/Boss Attacks/Pattern_Shoot")]
    public class AttackPattern_Shoot : AttackPattern
    {
        [Header("Properties")]
        [SerializeField] private string shootAnimationName = "Shoot";
        [SerializeField] private GameObject ProjectilePrefab;
        [SerializeField] private float Damage = 5f;

        [SerializeField] private float Speed = 14f;
        [SerializeField] private float Life = 3.0f;

        [Header("SFX")]
        [SerializeField] private SFXResource fireSFX;

        [Header("Repetitions")]
        [SerializeField] private int ArrowPerRep = 3;
        [SerializeField] private float SpreadMultiplier = 3f;     // degrees base multiplier for fan width
        [SerializeField] private bool Homing = false;
        [SerializeField] private float HomingDuration = 1.5f;
        [SerializeField] private int Repetition = 3;
        [SerializeField] private bool RepIsProbabilistic = false;
        [SerializeField, Range(0f, 1f)] private float ProbDecayPerShot = 0.25f;

        // --- Local tunables
        [SerializeField] private float windupSeconds = 0.15f;
        [SerializeField] private float interArrowDelay = 0.05f;
        [SerializeField] private float interRepDelay = 0.25f;

        // Enrage multipliers (mild; adjust to taste)
        [SerializeField] private float enrageArrowMul = 1.5f;
        [SerializeField, Range(0f, 1f)] private float enrageDecayReduction = 0.5f; // 50% less decay when enraged
        [SerializeField] private float enrageRateMul = 1.25f; // faster cadence when enraged
        bool _enraged = false;

        public override IEnumerator Execute(BossController controller)
        {
            if (ProjectilePrefab == null)
                yield break;

            _enraged = controller.IsEnraged;
            //  Play anim + windup
            var anim = controller.Animator;
            if (anim != null && !string.IsNullOrEmpty(shootAnimationName))
                anim.Play(shootAnimationName);

            
            float rateMul = _enraged ? enrageRateMul : 1f;
            float arrowMul = _enraged ? enrageArrowMul : 1f;

            yield return new WaitForSeconds(windupSeconds / rateMul);

            // 3) Determine repetition policy
            if (RepIsProbabilistic)
            {
                yield return ShootProbabilistic(controller, arrowMul, rateMul);
            }
            else
            {
                for (int rep = 0; rep < Mathf.Max(0, Repetition); rep++)
                {
                    yield return FireOneRepetition(controller, Mathf.RoundToInt(ArrowPerRep * arrowMul));
                    yield return new WaitForSeconds(interRepDelay / rateMul);
                }
            }
        }

        private IEnumerator ShootProbabilistic(BossController controller, float arrowMul, float rateMul)
        {
            // Start at p=1.0 �� always shoot once; then decay after each repetition
            float p = 1f;
            int guard = 0; // absolute cap to prevent pathological infinite loops
            const int hardCap = 32;

            while (Random.value <= p && guard++ < hardCap)
            {
                yield return FireOneRepetition(controller, Mathf.RoundToInt(ArrowPerRep * arrowMul));
                p = Mathf.Max(0f, p - ProbDecayPerShot * (_enraged ? enrageDecayReduction : 1f));
                yield return new WaitForSeconds(interRepDelay / rateMul);


            }
        }

        private IEnumerator FireOneRepetition(BossController controller, int arrowCount)
        {
            if (arrowCount <= 0) yield break;

            AudioManager.Instance?.PlaySFX(fireSFX);
            // Aim vector
            Vector2 origin = controller.transform.position;
            Transform target = controller.PlayerTransform;
            Vector2 toTarget = ((Vector2)target.position - origin).normalized;

            // Spread in degrees: base fan width scales with SpreadMultiplier and arrowCount
            // With N arrows, distribute across [-halfSpread, +halfSpread]
            float baseSpreadDeg = Mathf.Max(0f, SpreadMultiplier * 10f); // �g10�h as base step; knob scales it
            float halfSpread = (arrowCount > 1) ? 0.5f * baseSpreadDeg : 0f;

            for (int i = 0; i < arrowCount; i++)
            {
                float t = (arrowCount == 1) ? 0.5f : i / (float)(arrowCount - 1); // 0..1
                float ang = Mathf.Lerp(-halfSpread, +halfSpread, t);
                Vector2 dir = Rotate(toTarget, ang * Mathf.Deg2Rad);

                SpawnProjectile(controller, origin, dir);

                if (i < arrowCount - 1)
                    yield return new WaitForSeconds(interArrowDelay);
            }
        }

        private void SpawnProjectile(BossController controller, Vector2 origin, Vector2 dir)
        {
            var go = Object.Instantiate(ProjectilePrefab, origin, Quaternion.identity);

            // required
            var bullet = go.GetComponent<Weapon.EnemyProjectile2D>();
            if (bullet == null)
            {
                Debug.LogWarning("ProjectilePrefab missing EnemyProjectile2D.");
                Destroy(go);
                return;
            }

            Transform tgt = controller.PlayerTransform;

            bullet.Fire(origin, dir, Speed, Damage, Life, tgt, homingOverride: Homing, homingSecondsOverride: HomingDuration);
        }

        private static Vector2 Rotate(Vector2 v, float radians)
        {
            float c = Mathf.Cos(radians), s = Mathf.Sin(radians);
            return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
        }

    }
}
