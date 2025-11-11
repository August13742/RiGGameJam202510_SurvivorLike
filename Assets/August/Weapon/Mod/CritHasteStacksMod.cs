using UnityEngine;
using Survivor.Status;

namespace Survivor.Weapon
{
    [CreateAssetMenu(menuName = "Defs/Weapons/Mods/CritHasteStacks")]
    public sealed class CritHasteStacksMod : WeaponModDef
    {
        public float AttackSpeedPerStack = 0.05f; // +5% AS = +5% cooldown reduction
        public int MaxStacks = 10;
        public float Duration = 6f;
        private const string KEY = "mod.crithaste";

        public override void OnEquip(IWeapon weapon)
        {
            EnsureBuffManager(weapon);
        }

        public override void OnCrit(IWeapon weapon, Vector2 pos)
        {
            var bm = EnsureBuffManager(weapon);
            if (!bm) return;

            float current = bm.Sum(KEY);
            int currStacks = Mathf.RoundToInt(current / AttackSpeedPerStack);

            if (currStacks < MaxStacks)
                bm.AddTimedStack(KEY, AttackSpeedPerStack, Duration);
        }

        public override void OnTick(IWeapon weapon, float dt)
        {
            var bm = EnsureBuffManager(weapon);
            if (!bm) return;

            float bonus = bm.Sum(KEY); // 0..(MaxStacks*AttackSpeedPerStack)
            if (bonus <= 0f) return;

            if (weapon is IModTarget wt)
            {
                wt.GetAndMutateDynamicMods(dyn =>
                {
                    // cooldownReduction is linear: +0.10 => -10% CD
                    dyn.CooldownReduction += bonus;
                });
            }
        }

        private BuffManager EnsureBuffManager(IWeapon weapon)
        {
            var owner = GetOwner(weapon);
            if (!owner) return null;

            var bm = owner.GetComponent<BuffManager>();
            if (!bm) bm = owner.gameObject.AddComponent<BuffManager>();
            return bm;
        }
    }
}
