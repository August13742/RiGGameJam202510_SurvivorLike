using Survivor.Game;
using System.Collections.Generic;
using UnityEngine;


namespace Survivor.Weapon
{
    public sealed class BeamWeapon : WeaponBase<BeamWeaponDef>
    {
        private ObjectPool _beamPool;

        public override void Equip(WeaponContext context)
        {
            base.Equip(context);
            _beamPool = new ObjectPool(def.BeamPrefab, prewarm: 8, context.PoolRoot);
        }

        public override void Tick(float dt)
        {
            if (!Ready(dt)) return;

            // Get aim(s)
            var aims = GatherAimDirections();
            if (aims.Count == 0) return;

            foreach (var dir in aims)
                SpawnBeam(dir);
        }

        private List<Vector2> GatherAimDirections()
        {
            var res = new List<Vector2>(Shots());
            int shots = Shots();

            switch (def.TargetingMode)
            {
                case TargetMode.Nearest:
                    {
                        Transform t = _getTarget?.Invoke();
                        if (t)
                        {
                            Vector2 baseDir = ((Vector2)t.position - (Vector2)fireOrigin.position).normalized;
                            // Apply spread if multishot
                            if (shots > 1 && def.SpreadDeg > 0f)
                            {
                                res.AddRange(ApplySpread(baseDir, shots));
                            }
                            else
                            {
                                for (int i = 0; i < shots; i++)
                                    res.Add(baseDir);
                            }
                        }
                        break;
                    }
                case TargetMode.RandomK:
                    {
                        // Pick one random target per beam (K = shots)
                        for (int i = 0; i < shots; i++)
                        {
                            Transform t = ctx.RandomInRange?.Invoke(1);
                            if (t)
                                res.Add(((Vector2)t.position - (Vector2)fireOrigin.position).normalized);
                        }
                        break;
                    }
                case TargetMode.SelfCentered:
                default:
                    {
                        Vector2 baseDir = fireOrigin ? (Vector2)fireOrigin.right : Vector2.right;
                        // Apply spread if multishot
                        if (shots > 1 && def.SpreadDeg > 0f)
                        {
                            res.AddRange(ApplySpread(baseDir, shots));
                        }
                        else
                        {
                            for (int i = 0; i < shots; i++)
                                res.Add(baseDir);
                        }
                        break;
                    }
            }

            return res;
        }

        private IEnumerable<Vector2> ApplySpread(Vector2 baseDir, int count)
        {
            float spread = def.SpreadDeg * Mathf.Deg2Rad;
            float start = -spread * (count - 1) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float ang = start + spread * i;
                float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                yield return new Vector2(
                    baseDir.x * ca - baseDir.y * sa,
                    baseDir.x * sa + baseDir.y * ca
                );
            }
        }

        private void SpawnBeam(Vector2 dir)
        {
            GameObject go = _beamPool.Rent(fireOrigin.position, Quaternion.identity);
            var beam = go.GetComponent<BeamInstance2D>();
            if (!beam) { beam = go.AddComponent<BeamInstance2D>(); beam.SetPool(_beamPool); }

            go.layer = (ctx.Team == Team.Player)
                ? LayerMask.NameToLayer("PlayerProjectile")
                : LayerMask.NameToLayer("EnemyProjectile"); 

            // Treat TicksPerSecond in the def as TOTAL TICKS over the beam's lifetime.
            int totalTicks = Mathf.Max(1, def.TicksPerSecond);

            // Distribute base damage across the beam lifetime (not per-second).
            float dmgMul = ctx?.Stats?.DamageMul ?? 1f;
            int dpt = Mathf.Max(1, Mathf.RoundToInt((def.BaseDamage * dmgMul) / totalTicks));

            beam.Configure(
                origin: fireOrigin,
                dir: dir,
                length: def.BeamLength * def.AreaScale,
                width: def.BeamWidth * def.AreaScale,
                duration: def.Duration,
                desiredTicks: totalTicks,
                tickInterval: def.Duration / totalTicks,
                damagePerTick: dpt,
                sourceMat: def.BeamMaterial,
                uvScrollRate: def.UVScrollRate, 
                alphaOverLife: def.AlphaOverLife,
                followOrigin: def.FollowOrigion
            );
        }
    }
}
