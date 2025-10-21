using UnityEngine;

namespace Survivor.Weapon
{
    public enum Team { Player, Enemy }
    public abstract class WeaponBase<TDef> : MonoBehaviour, IWeapon where TDef : WeaponDef
    {
        
        [SerializeField] protected TDef def;
        [SerializeField] protected Transform fireOrigin;

        protected WeaponContext ctx;
        protected float cooldown;
        protected System.Func<Transform> _getTarget;

        public virtual void Equip(WeaponContext context)
        {
            ctx = context;
            cooldown = 0f;

            _getTarget = def.TargetingMode switch
            {
                TargetMode.SelfCentered => ctx.SelfCentered ?? ctx.Target,
                TargetMode.Nearest => ctx.Nearest ?? ctx.Target,                    // fallback
                TargetMode.RandomK => () => ctx.RandomInRange?.Invoke(def.RandomPickK) ?? ctx.Target?.Invoke(),
                _ => ctx.Nearest ?? ctx.Target
            };
        }

        protected bool Ready(float dt)
        {
            cooldown -= dt;
            if (cooldown > 0f) return false;
            float cd = def.BaseCooldown * (ctx?.Stats?.CooldownMul ?? 1f);
            cooldown = Mathf.Max(0.01f, cd);
            return true;
        }

        protected int ScaledDamage()
        {
            float mul = ctx?.Stats?.DamageMul ?? 1f;
            return Mathf.Max(1, Mathf.RoundToInt(def.BaseDamage * mul));
        }

        protected float ScaledArea() => def.AreaScale * (ctx?.Stats?.AreaMul ?? 1f);
        protected int Shots() => Mathf.Max(1, def.Projectiles + (ctx?.Stats?.ProjectilesAdd ?? 0));


        public abstract void Tick(float dt);
    }




}
