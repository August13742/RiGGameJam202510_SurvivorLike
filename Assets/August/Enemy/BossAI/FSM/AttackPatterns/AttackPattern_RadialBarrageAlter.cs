using Survivor.Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(fileName = "New RadialBarrageAlter", menuName = "Defs/Boss Attacks/Radial Barrage Alter")]
    public sealed class AttackPattern_RadialBarrageAlter : AttackPattern
    {
        [Header("Projectile & Damage")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float damage = 5f;
        [SerializeField] private float speed = 12f;
        [SerializeField] private float life = 4f;
        [SerializeField] private bool homing = false;
        [SerializeField] private float homingDuration = 0.75f;

        [Header("Barrage Shape")]
        [SerializeField] private int bulletsPerWave = 24;
        [SerializeField] private float ringRadius = 1.5f;      // local offset from root

        [Header("Timeline")]
        [SerializeField] private float windupSeconds = 0.25f;  // pre-spawn telegraph
        [SerializeField] private float interWaveDelay = 0.25f; // after release before next wave

        [Header("Repetitions")]
        [SerializeField] private bool repIsProbabilistic = false;
        [SerializeField] private int repetitions = 3;                // used if not probabilistic
        [SerializeField, Range(0f, 1f)] private float probDecayPerWave = 0.25f; // p -= this
        [SerializeField] private int hardCap = 6;

        [Header("Enrage")]
        [SerializeField] private float enrageRateMul = 1.15f;   // faster timeline
        [SerializeField] private float enrageBulletMul = 1.10f; // +10% bullets (rounded)
        [SerializeField, Range(0f, 1f)] private float enrageDecayReduction = 0.5f;

        [Header("Animation (optional)")]
        [SerializeField] private string castAnim = "Shoot";
        bool _enraged;
        public override IEnumerator Execute(BossController controller)
        {
            if (projectilePrefab == null || controller == null || controller.PlayerTransform == null)
                yield break;

            _enraged = controller.IsEnraged;

            // Enrage scalars
            float rateMul = _enraged ? enrageRateMul : 1f;
            int bullets = Mathf.Max(1, Mathf.RoundToInt(bulletsPerWave * (_enraged ? enrageBulletMul : 1f)));
            float decay = repIsProbabilistic
                ? probDecayPerWave * (_enraged ? enrageDecayReduction : 1f)
                : probDecayPerWave;

            // Telegraph
            if (controller.Animator && !string.IsNullOrEmpty(castAnim))
                controller.Animator.Play(castAnim);
            yield return new WaitForSeconds(windupSeconds / rateMul);

            if (repIsProbabilistic)
            {
                float p = 1f;
                int guard = 0;
                while (Random.value <= p && guard++ < hardCap)
                {
                    FireOneWave(controller, bullets);
                    p = Mathf.Max(0f, p - decay);
                    yield return new WaitForSeconds(interWaveDelay / rateMul);
                }
            }
            else
            {
                int reps = Mathf.Max(0, repetitions);
                for (int i = 0; i < reps; i++)
                {
                    FireOneWave(controller, bullets);
                    if (i < reps - 1)
                        yield return new WaitForSeconds(interWaveDelay / rateMul);
                }
            }
        }

        private Transform GetOrCreateProjectileRoot(BossController controller)
        {
            Transform rootTF = controller.transform.Find("RadialRoot");
            if (rootTF == null)
            {
                rootTF = new GameObject("RadialRoot").transform;
                rootTF.SetPositionAndRotation(controller.transform.position, Quaternion.identity);
                rootTF.SetParent(controller.transform, worldPositionStays: true);
                return rootTF.transform;
            }
            return rootTF;
        }
        private void FireOneWave(BossController controller, int bullets)
        {
            // Create a transient root under the boss to host the pattern
            Transform root = GetOrCreateProjectileRoot(controller);
            var spawned = new List<Weapon.EnemyBullet2D>(bullets);

            for (int i = 0; i < bullets; i++)
            {
                float ang = Random.Range(0f, 360f);
                Vector2 local = Polar(ringRadius, ang * Mathf.Deg2Rad);

                // Instantiate and position correctly
                var go = Instantiate(projectilePrefab, root);
                go.transform.SetLocalPositionAndRotation(local, Quaternion.Euler(0, 0, ang));

                var bullet = go.GetComponent<Weapon.EnemyBullet2D>();
                if (bullet == null)
                {
                    Debug.LogWarning("Projectile prefab missing EnemyBullet2D; destroying.");
                    Destroy(go);
                    continue;
                }

                bullet.enabled = false;

                spawned.Add(bullet);
            }

            // Release: re-enable the script, and fire
            Transform player = controller.PlayerTransform;
            for (int i = 0; i < spawned.Count; i++)
            {
                var b = spawned[i];
                if (b == null) continue;

                Transform bt = b.transform;
                Vector2 worldPos = bt.position;
                Vector2 outward = ((Vector2)bt.position - (Vector2)root.position).normalized;

                bt.SetParent(null, worldPositionStays: true);
                // kill any reflection artifacts
                var ls = bt.localScale;
                ls.x = Mathf.Abs(ls.x);
                ls.y = Mathf.Abs(ls.y);
                bt.localScale = ls;

                b.enabled = true;

                b.Fire(worldPos, outward, speed, damage, life, player, overrideHomingSeconds: homing ? homingDuration : 0f);
            }

        }

        private static Vector2 Polar(float r, float rad)
        {
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            return new Vector2(r * c, r * s);
        }
    }
}
