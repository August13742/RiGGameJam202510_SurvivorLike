using UnityEngine;

namespace Survivor.Weapon { 

    public enum Team{Player,Enemy}
    public abstract class WeaponBase<TDef> : MonoBehaviour, IUpgradeableWeapon, IHitEventSink, IModTarget where TDef : WeaponDef
    {
        [SerializeField] protected TDef def;
        [SerializeField] protected Transform fireOrigin;

        [SerializeField] protected WeaponModDef[] mods;

        protected WeaponContext ctx;
        protected float cooldown;
        protected System.Func<Transform> _getTarget;

        protected WeaponStats weaponStats = new (); // permanent
        protected WeaponStats dynamicMods = new (); // per-tick, reset every Tick

        public WeaponContext GetContext() => ctx;
        public TDef GetDef() => def;

        public virtual void Equip(WeaponContext context)
        {
            ctx = context;
            cooldown = 0f;

            // Initialize weapon stats from def
            weaponStats.CritChance = def.BaseCritChance;
            weaponStats.CritMultiplier = def.BaseCritMultiplier;

            _getTarget = def.TargetingMode switch
            {
                TargetMode.SelfCentered => ctx.SelfCentered ?? ctx.Target,
                TargetMode.Nearest => ctx.Nearest ?? ctx.Target,
                TargetMode.RandomK => () => ctx.RandomInRange?.Invoke(def.Projectiles) ?? ctx.Target?.Invoke(),
                _ => ctx.Nearest ?? ctx.Target
            };

            // Mods: OnEquip for all weapons
            if (mods != null)
            {
                for (int i = 0; i < mods.Length; i++)
                    if (mods[i]) mods[i].OnEquip(this);
            }
        }

        // --- Tick scaffolding shared by all weapons -------------------------

        /// Call this at the very start of Tick.
        protected bool BeginTickAndGate(float dt)
        {
            // 1) reset per-tick
            ResetDynamicMods();

            // 2) let mods apply per-tick effects (e.g., haste stacks)
            if (mods != null)
            {
                for (int i = 0; i < mods.Length; i++)
                    if (mods[i]) mods[i].OnTick(this, dt);
            }

            // 3) cooldown decrement with current effective multiplier
            cooldown -= dt;
            if (cooldown > 0f) return false;
            
            // 4) Fire allowed - set new cooldown using effective multiplier
            float cd = def.BaseCooldown * GetEffectiveCooldownMul();
            cooldown = Mathf.Max(0.01f, cd);
            return true;
        }

        protected bool Ready(float dt)
        {
            cooldown -= dt;
            if (cooldown > 0f) return false;
            float cd = def.BaseCooldown * GetEffectiveCooldownMul();
            cooldown = Mathf.Max(0.01f, cd);
            return true;
        }

        // --- Crit helpers ----------------------------------------------------
        protected bool RollCrit()
        {
            return Random.value < Mathf.Clamp01(GetEffectiveCritChance());
        }

        protected int ApplyCrit(int baseDamage, bool crit)
        {
            if (!crit) return baseDamage;
            float m = Mathf.Max(1f, GetEffectiveCritMultiplier());
            return Mathf.Max(1, Mathf.RoundToInt(baseDamage * m));
        }

        // --- Mod event fan-out ----------------------------------------------
        public void OnHit(float damage, Vector2 pos, bool crit)
        {
            if (mods == null) return;
            for (int i = 0; i < mods.Length; i++)
            {
                var m = mods[i];
                if (!m) continue;
                m.OnHit(this, damage, pos, crit);
                if (crit) m.OnCrit(this, pos);
            }
        }

        public void OnKill(Vector2 pos)
        {
            if (mods == null) return;
            for (int i = 0; i < mods.Length; i++)
                if (mods[i]) mods[i].OnKill(this, pos);
        }

        // --- Effective stats ------------------------------------
        protected int ScaledDamage() => Mathf.Max(1, Mathf.RoundToInt(def.BaseDamage * GetEffectiveDamageMul()));
        protected float ScaledArea() => def.AreaScale * GetEffectiveAreaMul();
        protected int Shots() => Mathf.Max(1, def.Projectiles + GetEffectiveProjectilesAdd());
        protected int Pierce() => GetEffectivePierceAdd();
        protected float Speed() => GetEffectiveSpeedMul();

        protected float GetEffectiveCooldownMul() => weaponStats.CooldownMul * dynamicMods.CooldownMul;
        protected float GetEffectiveDamageMul() => weaponStats.DamageMul * dynamicMods.DamageMul;
        protected float GetEffectiveAreaMul() => weaponStats.AreaMul * dynamicMods.AreaMul;
        protected int GetEffectiveProjectilesAdd() => weaponStats.ProjectilesAdd + dynamicMods.ProjectilesAdd;
        protected int GetEffectivePierceAdd() => weaponStats.PierceAdd + dynamicMods.PierceAdd;
        protected float GetEffectiveSpeedMul() => weaponStats.SpeedMul * dynamicMods.SpeedMul;
        protected float GetEffectiveCritChance() => weaponStats.CritChance + dynamicMods.CritChance;
        protected float GetEffectiveCritMultiplier() => Mathf.Max(weaponStats.CritMultiplier, dynamicMods.CritMultiplier);

        public void SetDynamicMods(WeaponStats modsStats) { if (modsStats != null) dynamicMods = modsStats; }
        public void ResetDynamicMods() { dynamicMods = new WeaponStats(); }
        public WeaponStats GetAndMutateDynamicMods(System.Action<WeaponStats> mut = null)
        {
            mut?.Invoke(dynamicMods);
            return dynamicMods;
        }

        // IUpgradeableWeapon
        public bool Owns(WeaponDef otherDef) => def == otherDef;
        public void ApplyUpgrade(WeaponStats delta)
        {
            if (delta == null) return;
            weaponStats.CooldownMul *= delta.CooldownMul;
            weaponStats.DamageMul *= delta.DamageMul;
            weaponStats.AreaMul *= delta.AreaMul;
            weaponStats.ProjectilesAdd += delta.ProjectilesAdd;
            weaponStats.PierceAdd += delta.PierceAdd;
            weaponStats.SpeedMul *= delta.SpeedMul;
            weaponStats.CritChance += delta.CritChance;
            weaponStats.CritMultiplier = Mathf.Max(weaponStats.CritMultiplier, delta.CritMultiplier);
        }
        public WeaponStats GetCurrentStats() => new WeaponStats
        {
            CooldownMul = weaponStats.CooldownMul,
            DamageMul = weaponStats.DamageMul,
            AreaMul = weaponStats.AreaMul,
            ProjectilesAdd = weaponStats.ProjectilesAdd,
            PierceAdd = weaponStats.PierceAdd,
            SpeedMul = weaponStats.SpeedMul,
            CritChance = weaponStats.CritChance,
            CritMultiplier = weaponStats.CritMultiplier
        };

        public abstract void Tick(float dt);
    }
}
