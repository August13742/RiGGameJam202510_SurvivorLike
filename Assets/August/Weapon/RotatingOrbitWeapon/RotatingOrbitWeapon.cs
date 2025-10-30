using UnityEngine;
using Survivor.Game;

namespace Survivor.Weapon
{
    [RequireComponent(typeof(PrefabStamp), typeof(Collider2D),typeof(Renderer))]
    [DisallowMultipleComponent]
    public sealed class RotatingOrbitWeapon : WeaponBase<RotatingOrbitWeaponDef>
    {
        private ObjectPool _orbPool;

        public override void Equip(WeaponContext context)
        {
            base.Equip(context);
            _orbPool = new ObjectPool(def.OrbPrefab, prewarm: 8, context.PoolRoot);
        }

        public override void Tick(float dt)
        {
            if (!Ready(dt)) return;

            // Self-centered pivot (owner or fireOrigin)
            Transform pivot = _getTarget?.Invoke(); // SelfCentered is mapped in base
            if (!pivot) pivot = ctx.Owner ? ctx.Owner : fireOrigin;
            if (!pivot) return;

            int count = Shots(); // maps def.Projectiles -> number of orbiters
            float radius = ScaledArea() * def.Radius;
            float revTime = Mathf.Max(0.01f, def.RotationTime);
            float angVel = (2f * Mathf.PI) / revTime; // rad/sec (CCW)
            float lifetime = Mathf.Max(0.01f, def.Revolutions * revTime);

            // Even angular spacing
            float step = (count > 0) ? (2f * Mathf.PI / count) : 0f;

            for (int i = 0; i < count; i++)
            {
                float startAng = step * i; // evenly spread

                GameObject go = _orbPool.Rent(pivot.position, Quaternion.identity);

                // Team layer
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
                    angVelRad: angVel,
                    lifetime: lifetime,
                    damage: dmg,
                    team: ctx.Team,
                    followOrigin: def.FollowOrigin,
                    toggleVis: def.ToggleRendererAndCollider,
                    maxHitsPerTarget: def.MaxHitsPerTarget
                );
            }
            // cooldown handled by Ready(); nothing else to do here
        }
    }
}
