using UnityEngine;
using Survivor.Game;

namespace Survivor.Weapon
{
    public sealed class ProjectileWeapon : WeaponBase<ProjectileWeaponDef>
    {
        [SerializeField] private WeaponModDef[] mods;
        private ObjectPool _projPool;

        public override void Equip(WeaponContext context)
        {
            base.Equip(context); // sets ctx, cooldown, _getTarget
            _projPool = new ObjectPool(def.ProjectilePrefab, prewarm: 16, context.PoolRoot);

            if (mods != null)
            {
                for (int i = 0; i < mods.Length; i++)
                    if (mods[i]) mods[i].OnEquip(this);
            }
        }

        public override void Tick(float dt)
        {
            // Per-tick dynamic modifiers: reset → apply → then gate
            ResetDynamicMods();
            if (mods != null)
            {
                for (int i = 0; i < mods.Length; i++)
                    if (mods[i]) mods[i].OnTick(this, dt);
            }

            if (!Ready(dt)) return;

            Transform t = _getTarget?.Invoke();
            if (!t) return;

            Vector2 baseDir = ((Vector2)t.position - (Vector2)fireOrigin.position).normalized;
            int count = Shots();
            float spread = def.SpreadDeg * Mathf.Deg2Rad;
            float start = -spread * (count - 1) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float ang = start + spread * i;
                float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                Vector2 dir = new Vector2(
                    baseDir.x * ca - baseDir.y * sa,
                    baseDir.x * sa + baseDir.y * ca
                );

                GameObject go = _projPool.Rent(fireOrigin.position, Quaternion.identity);

                go.layer = (ctx.Team == Team.Player)
                    ? LayerMask.NameToLayer("PlayerProjectile")
                    : LayerMask.NameToLayer("EnemyProjectile");

                var p = go.GetComponent<Projectile>();
                p.SetPool(_projPool);

                int dmg = ScaledDamage();
                int pierce = def.Pierce + Pierce();
                float speed = def.ProjectileSpeed * Speed();

                p.Fire(fireOrigin.position, dir, speed, dmg, pierce, def.Lifetime);
            }
        }
    }
}
