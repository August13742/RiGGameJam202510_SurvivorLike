using UnityEngine;
using System.Collections.Generic;

namespace Survivor.Weapon
{
    public enum Team { Player, Enemy }

    public abstract class WeaponBase<TDef> : MonoBehaviour,
        IUpgradeableWeapon, IHitEventSink, IModTarget where TDef : WeaponDef
    {
        [SerializeField] protected TDef def;
        [SerializeField] protected Transform fireOrigin;
        [SerializeField] protected WeaponModDef[] mods;
        [SerializeField] protected SFXResource fireSFX;
        [SerializeField] protected SFXResource equipSFX;

        protected WeaponContext ctx;
        protected float cooldown;
        protected System.Func<Transform> _getTarget;

        // Permanent from upgrades
        protected readonly List<WeaponLevelBonus> permanentBonuses = new();

        // Per-tick, reset every Tick
        protected RuntimeModState dynamicMods = new();

        public WeaponContext GetContext() => ctx;
        public TDef GetDef() => def;

        private readonly EffectiveWeaponStats _tickCachedStats = new EffectiveWeaponStats();
        private int _lastTickFrame = -1;

        // ---- IWeapon --------------------------------------------------------
        void IWeapon.Equip(WeaponDef baseDef, WeaponContext context)
        {
            if (baseDef is not TDef typed)
                throw new System.ArgumentException(
                    $"[{GetType().Name}] expects {typeof(TDef).Name}, got {baseDef?.GetType().Name ?? "null"}");

            def = typed;
            ctx = context;

            AudioManager.Instance?.PlaySFX(equipSFX);

            OnEquipped();
        }

        protected virtual void Equip(TDef typedDef, WeaponContext context)
        {
            def = typedDef ?? throw new System.ArgumentNullException(nameof(typedDef));
            ctx = context;
            OnEquipped();
        }

        protected virtual void OnEquipped()
        {
            fireOrigin = ctx.FireOrigin;
            cooldown = 0f;

            _getTarget = def.TargetingMode switch
            {
                TargetMode.SelfCentered => ctx.SelfCentered ?? ctx.Target,
                TargetMode.Nearest => ctx.Nearest ?? ctx.Target,
                TargetMode.RandomK => () => ctx.RandomInRange?.Invoke(def.Projectiles) ?? ctx.Target?.Invoke(),
                _ => ctx.Nearest ?? ctx.Target
            };

            if (mods != null)
                for (int i = 0; i < mods.Length; i++)
                    if (mods[i]) mods[i].OnEquip(this);
        }

        // ---- Tick scaffolding ----------------------------------------------
        /// Call at the very start of Tick.
        protected bool BeginTickAndGate(float dt)
        {
            // 1) reset per-tick mods
            ResetDynamicMods();

            // 2) let mods apply per-tick effects
            if (mods != null)
            {
                for (int i = 0; i < mods.Length; i++)
                    if (mods[i]) mods[i].OnTick(this, dt);
            }

            // 3) cooldown
            cooldown -= dt;
            if (cooldown > 0f) return false;

            // 4) set next cooldown using effective snapshot
            var stats = Current(); // snapshot from def + permanent + dynamic
            cooldown = Mathf.Max(0.01f, stats.Cooldown);
            return true;
        }

        protected bool Ready(float dt)
        {
            cooldown -= dt;
            if (cooldown > 0f) return false;
            var stats = Current();
            cooldown = Mathf.Max(0.01f, stats.Cooldown);
            return true;
        }

        // ---- Crit helpers ---------------------------------------------------
        protected bool RollCrit()
        {
            return Random.value < Mathf.Clamp01(Current().CritChance);
        }

        protected int ApplyCrit(int baseDamage, bool crit)
        {
            if (!crit) return baseDamage;
            float m = Mathf.Max(1f, Current().CritMultiplier);
            return Mathf.Max(1, Mathf.RoundToInt(baseDamage * m));
        }

        // ---- Mod event fan-out ---------------------------------------------
        public void OnHit(float damage, Vector2 pos, bool crit)
        {
            if (mods == null) return;
            for (int i = 0; i < mods.Length; i++)
            {
                var mod = mods[i];
                if (!mod) continue;
                mod.OnHit(this, damage, pos, crit);
                if (crit) mod.OnCrit(this, pos);
            }
        }

        public void OnKill(Vector2 pos)
        {
            if (mods == null) return;
            for (int i = 0; i < mods.Length; i++)
                if (mods[i]) mods[i].OnKill(this, pos);
        }

        // ---- Effective snapshot & convenience accessors --------------------
        protected EffectiveWeaponStats Current()
        {
            // Check if we already computed stats for this exact frame
            if (_lastTickFrame == Time.frameCount)
            {
                return _tickCachedStats;
            }

            _lastTickFrame = Time.frameCount; // Mark as computed for this frame

            int basePierce = 0;
            if (def is ProjectileWeaponDef pdef) basePierce = pdef.Pierce;

            // combine permanent bonuses with a single-frame "bonus" view over dynamic mods
            var combined = _tmpCombined;
            combined.Clear();
            // fold permanent
            for (int i = 0; i < permanentBonuses.Count; i++) Accumulate(ref combined, permanentBonuses[i]);
            // fold dynamic
            Accumulate(ref combined, dynamicMods);
            Compute(_tickCachedStats, def, combined, basePierce);

            return _tickCachedStats;
        }

        protected int ScaledDamage() => Mathf.Max(1, Mathf.RoundToInt(Current().Damage));
        protected float ScaledArea() => Current().AreaScale;
        protected int Shots() => Current().Projectiles;
        protected int Pierce() => Current().Pierce;
        protected float SpeedFactor() => Current().SpeedFactor;

        // ---- IModTarget -----------------------------------------------------
        public void SetDynamicMods(RuntimeModState modsState)
        {
            if (modsState != null) dynamicMods = modsState;
        }

        public void ResetDynamicMods() => dynamicMods.Clear();

        public RuntimeModState GetAndMutateDynamicMods(System.Action<RuntimeModState> mut = null)
        {
            mut?.Invoke(dynamicMods);
            return dynamicMods;
        }

        // ---- IUpgradeableWeapon --------------------------------------------
        public bool Owns(WeaponDef otherDef) => def == otherDef;

        public void ApplyUpgrade(WeaponLevelBonus bonus)
        {
            if (bonus == null || bonus.IsNoop()) return;
            permanentBonuses.Add(bonus);
        }

        public EffectiveWeaponStats GetCurrentStats() => Current();

        // ---- Aggregation helpers -------------------------------------------
        private readonly Accum _tmpCombined = new();

        private struct Accum
        {
            public float dmgMul, cdRed, areaMul, speedMul;
            public int projAdd, pierceAdd;
            public float critChanceAdd, critMultAdd;
            public void Clear()
            {
                dmgMul = cdRed = areaMul = speedMul = 0f;
                projAdd = pierceAdd = 0;
                critChanceAdd = critMultAdd = 0f;
            }
        }

        private static void Accumulate(ref Accum a, WeaponLevelBonus b)
        {
            a.dmgMul += b.DamageMultiplierBonus;
            a.cdRed += b.CooldownReduction;
            a.areaMul += b.AreaScaleBonus;
            a.speedMul += b.SpeedMultiplierBonus;
            a.projAdd += b.ProjectileCountBonus;
            a.pierceAdd += b.PierceCountBonus;
            a.critChanceAdd += b.CritChanceBonus;
            a.critMultAdd += b.CritDamageMultiplierBonus;
        }

        private static void Accumulate(ref Accum a, RuntimeModState d)
        {
            a.dmgMul += d.DamageMultiplierBonus;
            a.cdRed += d.CooldownReduction;
            a.areaMul += d.AreaScaleBonus;
            a.speedMul += d.SpeedMultiplierBonus;
            a.projAdd += d.ProjectileCountBonus;
            a.pierceAdd += d.PierceCountBonus;
            a.critChanceAdd += d.CritChanceBonus;
            a.critMultAdd += d.CritDamageMultiplierBonus;
        }

        private void Compute(EffectiveWeaponStats target, WeaponDef def, in Accum a, int typeBasePierce)
        {
            float damage = def.BaseDamage * (1f - 0f + (1f * a.dmgMul)); // base × (1+Σ)
            float cooldown = def.BaseCooldown * Mathf.Max(0.01f, 1f - a.cdRed);
            float area = def.AreaScale * (1f + a.areaMul);
            int proj = Mathf.Max(1, def.Projectiles + a.projAdd);
            int pierce = Mathf.Max(0, typeBasePierce + a.pierceAdd);
            float cChance = Mathf.Clamp01(def.BaseCritChance + a.critChanceAdd);
            float cMult = Mathf.Max(1f, def.BaseCritMultiplier + a.critMultAdd);

            target.Damage = damage;
            target.Cooldown = cooldown;
            target.AreaScale = area;
            target.Projectiles = proj;
            target.Pierce = pierce;
            target.CritChance = cChance;
            target.CritMultiplier = cMult;
            target.SpeedFactor = 1f + a.speedMul;
            target.DamageMultiplierFromBase = (def.BaseDamage > 0f) ? damage / def.BaseDamage : 1f;
            target.CooldownMultiplierFromBase = (def.BaseCooldown > 0f) ? cooldown / def.BaseCooldown : 1f;
            target.AreaMultiplierFromBase = (def.AreaScale > 0f) ? area / def.AreaScale : 1f;

        }

        public abstract void Tick(float dt);
    }
}
