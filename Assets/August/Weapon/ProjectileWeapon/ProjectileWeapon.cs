using UnityEngine;
using Survivor.Game;

namespace Survivor.Weapon
{
    public sealed class ProjectileWeapon : WeaponBase<ProjectileWeaponDef>
    {
        private ObjectPool _projPool;

        public override void Equip(WeaponContext context)
        {
            base.Equip(context); // sets ctx, cooldown, _getTarget
            _projPool = new ObjectPool(def.ProjectilePrefab, prewarm: 32, context.PoolRoot);
        }

        public override void Tick(float dt)
        {
            if (!BeginTickAndGate(dt)) return;

            Transform t = _getTarget?.Invoke();
            if (!t) return;

            Vector2 baseDir = ((Vector2)t.position - (Vector2)fireOrigin.position).normalized;
            int count = Shots();
            float spread = def.SpreadDeg * Mathf.Deg2Rad;
            float start = -spread * (count - 1) * 0.5f;

            // Base damage roll is *not* crit yet; crit is per-hit inside Projectile.
            int baseDamage = ScaledDamage();

            for (int i = 0; i < count; i++)
            {
                float ang = start + spread * i;
                float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                Vector2 dir = new(baseDir.x * ca - baseDir.y * sa, baseDir.x * sa + baseDir.y * ca);

                GameObject go = _projPool.Rent(fireOrigin.position, Quaternion.identity);
                go.layer = (ctx.Team == Team.Player) ? LayerMask.NameToLayer("PlayerProjectile") : LayerMask.NameToLayer("EnemyProjectile");

                var p = go.GetComponent<Projectile>();
                p.SetPool(_projPool);
                p.SetHitSink(this);

                int pierceFinal = def.Pierce + Pierce();
                float speedFinal = def.ProjectileSpeed * Speed();

                // Let projectile do per-hit crits with current effective chance/mul.
                p.ConfigureCrit(GetEffectiveCritChance(), GetEffectiveCritMultiplier(), perHit: true);

                p.Fire(fireOrigin.position, dir, speedFinal, baseDamage, pierceFinal, def.Lifetime);
            }
        }
    }
}
