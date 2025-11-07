using UnityEngine;
using Survivor.Game;

namespace Survivor.Weapon
{
    [DisallowMultipleComponent]
    public sealed class RotatingOrbitWeapon : WeaponBase<RotatingOrbitWeaponDef>
    {
        private ObjectPool _orbPool;


        protected override void OnEquipped()
        {
            base.OnEquipped();
            _orbPool = new ObjectPool(def.OrbPrefab, prewarm: 8, ctx.PoolRoot);
        }


        public override void Tick(float dt)
        {
            // Unified prelude: resets dynamic mods, applies mods.OnTick, then cooldown gate.
            if (!BeginTickAndGate(dt)) return;

            Transform pivot = _getTarget?.Invoke();
            if (!pivot) pivot = ctx.Owner ? ctx.Owner : fireOrigin;
            if (!pivot) return;

            int count = Shots();

            // --- Area split (use ScaledArea) ---
            float Aeff = Mathf.Max(0.0001f, ScaledArea());

            float orbScale = Mathf.Clamp(
                Mathf.Lerp(1f, Aeff, def.OrbAreaBias),
                def.OrbScaleClamp.x, def.OrbScaleClamp.y);

            float radiusMul = Mathf.Clamp(
                Mathf.Lerp(1f, Mathf.Sqrt(Aeff), def.RadiusAreaBias),
                def.RadiusMulClamp.x, def.RadiusMulClamp.y);

            float radius = def.Radius * radiusMul;

            // --- Motion ---
            float revTime = Mathf.Max(0.01f, def.RotationTime);
            float revs = Mathf.Max(0.001f, def.Revolutions);
            float lifetime = revs * revTime;
            float totalAngle = revs * (2f * Mathf.PI) * (def.Clockwise ? -1f : 1f);

            float step = (count > 0) ? (2f * Mathf.PI / count) : 0f;

            int baseDamage = ScaledDamage();

            for (int i = 0; i < count; i++)
            {
                float startAng = step * i;

                GameObject go = _orbPool.Rent(pivot.position, Quaternion.identity);
                go.layer = (ctx.Team == Team.Player)
                    ? LayerMask.NameToLayer("PlayerProjectile")
                    : LayerMask.NameToLayer("EnemyProjectile");

                var orb = go.GetComponent<RotatingOrbitOrb>();
                orb.SetPool(_orbPool);


                orb.SetHitSink(this);
                orb.ConfigureCrit(Current().CritChance, Current().CritMultiplier, perHit: true);

                orb.Arm(
                    pivot: pivot,
                    radius: radius,
                    startAngleRad: startAng,
                    totalAngleRad: totalAngle,
                    lifetime: lifetime,
                    damage: baseDamage,
                    team: ctx.Team,
                    followOrigin: def.FollowOrigin,
                    toggleVis: def.ToggleRendererAndCollider,
                    maxHitsPerTarget: def.MaxHitsPerTarget,
                    orbVisualScale: orbScale,
                    motionCurve: null
                );
            }
        }
    }
}
