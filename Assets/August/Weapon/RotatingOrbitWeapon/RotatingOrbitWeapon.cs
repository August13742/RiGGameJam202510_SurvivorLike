using UnityEngine;
using Survivor.Game;

namespace Survivor.Weapon
{
    [DisallowMultipleComponent]
    public sealed class RotatingOrbitWeapon : WeaponBase<RotatingOrbitWeaponDef>
    {
        [SerializeField] private WeaponModDef[] mods;
        private ObjectPool _orbPool;

        public override void Equip(WeaponContext context)
        {
            base.Equip(context);
            _orbPool = new ObjectPool(def.OrbPrefab, prewarm: 8, context.PoolRoot);

            if (mods != null)
            {
                for (int i = 0; i < mods.Length; i++)
                    if (mods[i]) mods[i].OnEquip(this);
            }
        }

        public override void Tick(float dt)
        {
            // Per-tick dynamic modifiers: reset → apply → then gate with Ready()
            ResetDynamicMods();
            if (mods != null)
            {
                for (int i = 0; i < mods.Length; i++)
                    if (mods[i]) mods[i].OnTick(this, dt);
            }

            if (!Ready(dt)) return;

            Transform pivot = _getTarget?.Invoke();
            if (!pivot) pivot = ctx.Owner ? ctx.Owner : fireOrigin;
            if (!pivot) return;

            int count = Shots();

            // --- Area split (use ScaledArea) ---
            float Aeff = Mathf.Max(0.0001f, ScaledArea());

            // Orb visual size (dominant, linear) → clamp
            float orbScale = Mathf.Clamp(
                Mathf.Lerp(1f, Aeff, def.OrbAreaBias),
                def.OrbScaleClamp.x, def.OrbScaleClamp.y);

            // Orbit radius (tamed, sqrt) → clamp
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

            for (int i = 0; i < count; i++)
            {
                float startAng = step * i;

                GameObject go = _orbPool.Rent(pivot.position, Quaternion.identity);
                go.layer = (ctx.Team == Team.Player)
                    ? LayerMask.NameToLayer("PlayerProjectile")
                    : LayerMask.NameToLayer("EnemyProjectile");

                var orb = go.GetComponent<RotatingOrbitOrb>();
                orb.SetPool(_orbPool);

                int dmg = ScaledDamage();

                orb.Arm(
                    pivot: pivot,
                    radius: radius,
                    startAngleRad: startAng,
                    totalAngleRad: totalAngle,
                    lifetime: lifetime,
                    damage: dmg,
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
