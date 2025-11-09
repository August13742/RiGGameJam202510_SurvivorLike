using UnityEngine;
using Survivor.Game;

namespace Survivor.Weapon
{
    public sealed class ProjectileWeapon : WeaponBase<ProjectileWeaponDef>
    {
        private ObjectPool<Projectile> _projPool;

        protected override void OnEquipped()
        {
            base.OnEquipped();
            _projPool = new ObjectPool<Projectile>(def.ProjectilePrefab, prewarm: 64, ctx.PoolRoot);
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

                Projectile projectile = _projPool.Rent(fireOrigin.position, Quaternion.identity);
                projectile.gameObject.layer = (ctx.Team == Team.Player) ? LayerMask.NameToLayer("PlayerProjectile") : LayerMask.NameToLayer("EnemyProjectile");


                projectile.SetHitSink(this);

                int pierceFinal = def.Pierce + Pierce();
                float speedFinal = def.ProjectileSpeed * Current().SpeedFactor;

                // Let projectile do per-hit crits with current effective chance/mul.
                projectile.ConfigureCrit(Current().CritChance, Current().CritMultiplier, perHit: true);

                projectile.Fire(fireOrigin.position, dir, speedFinal, baseDamage, pierceFinal, def.Lifetime,def.AreaScale);
            }
        }
    }
}
