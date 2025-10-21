using Survivor.Game;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UI.Image;

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
            switch (def.TargetingMode)
            {
                case TargetMode.Nearest:
                    {
                        Transform t = _getTarget?.Invoke();
                        if (t)
                            res.Add(((Vector2)t.position - (Vector2)fireOrigin.position).normalized);
                        break;
                    }
                case TargetMode.RandomK:
                    {
                        // Pick K random targets and aim each beam at one
                        int k = Mathf.Max(1, def.RandomPickK);
                        for (int i = 0; i < Mathf.Min(k, Shots()); i++)
                        {
                            Transform t = _getTarget?.Invoke();
                            if (t) res.Add(((Vector2)t.position - (Vector2)fireOrigin.position).normalized);
                        }
                        break;
                    }
                case TargetMode.SelfCentered:
                default:
                    {
                        // Point forward if have a facing; else 0 deg (right)
                        Vector2 dir = fireOrigin ? (Vector2)fireOrigin.right : Vector2.right;
                        for (int i = 0; i < Shots(); i++) res.Add(dir);
                        break;
                    }
            }
            // small spread fan for multishot visual variety when same target
            if (res.Count >= 2 && def.SpreadDeg > 0f)
            {
                float spread = def.SpreadDeg * Mathf.Deg2Rad;
                float start = -spread * (res.Count - 1) * 0.5f;
                Vector2 baseDir = res[0];
                res.Clear();
                for (int i = 0; i < Shots(); i++)
                {
                    float ang = start + spread * i;
                    float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                    res.Add(new Vector2(baseDir.x * ca - baseDir.y * sa,
                                        baseDir.x * sa + baseDir.y * ca));
                }
            }
            return res;
        }

        private void SpawnBeam(Vector2 dir)
        {
            GameObject go = _beamPool.Rent(fireOrigin.position, Quaternion.identity);
            var beam = go.GetComponent<BeamInstance2D>();
            if (!beam) { beam = go.AddComponent<BeamInstance2D>(); beam.SetPool(_beamPool); }

            go.layer = (ctx.Team == Team.Player)
                ? LayerMask.NameToLayer("PlayerProjectile")
                : LayerMask.NameToLayer("EnemyProjectile");

            // Damage per tick: distribute base damage over second -> per tick
            float ticksPerSec = Mathf.Max(0.01f, def.TicksPerSecond);
            int dpt = Mathf.Max(1, Mathf.RoundToInt((def.BaseDamage * (ctx?.Stats?.DamageMul ?? 1f)) / ticksPerSec));


            beam.Configure(
                origin: fireOrigin,
                dir: dir,
                length: def.BeamLength * def.AreaScale,
                width: def.BeamWidth * def.AreaScale,
                duration: def.Duration,
                tickInterval: 1f / def.TicksPerSecond,
                damagePerTick: dpt,
                sourceMat: def.BeamMaterial,
                uvScrollRate: def.UVScrollRate,
                alphaOverLife: def.AlphaOverLife,

                followOrigin: def.FollowOrigion, followDirection: def.FollowDirection
            );
        }
    }
}
