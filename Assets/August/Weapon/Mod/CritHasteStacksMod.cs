using UnityEngine;
using Survivor.Status;

namespace Survivor.Weapon
{
    [CreateAssetMenu(menuName = "Defs/Weapons/Mods/CritHasteStacks")]
    public sealed class CritHasteStacksMod : WeaponModDef
    {
        public float AttackSpeedPerStack = 0.05f; // +5% per stack
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

            // Clamp to MaxStacks by checking current stacks
            float current = bm.Sum(KEY);
            int currStacks = Mathf.RoundToInt(current / AttackSpeedPerStack);

            if (currStacks < MaxStacks)
            {
                bm.AddTimedStack(KEY, AttackSpeedPerStack, Duration);
            }
        }

        public override void OnTick(IWeapon weapon, float dt)
        {
            var bm = EnsureBuffManager(weapon);
            if (!bm) return;

            float bonus = bm.Sum(KEY); // 0..(MaxStacks*AttackSpeedPerStack)

            // Only apply if we have stacks
            if (bonus > 0f && weapon is IModTarget wt)
            {
                wt.GetAndMutateDynamicMods(dyn =>
                {
                    dyn.CooldownMul = 1f / (1f + bonus);
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