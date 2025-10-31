using System.Collections.Generic;
using UnityEngine;

namespace Survivor.Progression
{
    public sealed class OfferBuilder
    {
        public UpgradeDef[] LastDefs { get; private set; } = System.Array.Empty<UpgradeDef>();

        public UpgradeCardVM[] BuildOffer(ProgressionContext ctx, UpgradeDef[] pool, int n)
        {
            var candidates = new List<(UpgradeDef def, float w)>(pool.Length);
            foreach (var def in pool)
            {
                if (!def) continue;
                float w = def.ComputeWeight(ctx);
                if (w > 0f) candidates.Add((def, w));
            }

            // If everything finite is exhausted, auto-include infinite PlayerStat options.
            if (candidates.Count == 0)
            {
                foreach (var def in pool)
                    if (def && def.IsInfinite) candidates.Add((def, Mathf.Max(0.01f, def.BaseWeight)));
            }

            var picks = WeightedSampleWithoutReplacement(candidates, n);
            LastDefs = picks.ToArray();

            var outCards = new UpgradeCardVM[picks.Count];
            for (int i = 0; i < picks.Count; i++)
                outCards[i] = ToVM(ctx, picks[i]);
            return outCards;
        }

        private static List<UpgradeDef> WeightedSampleWithoutReplacement(List<(UpgradeDef def, float w)> src, int count)
        {
            var list = new List<(UpgradeDef def, float w)>(src);
            var rng = Random.value;
            var picks = new List<UpgradeDef>(count);

            for (int k = 0; k < count && list.Count > 0; k++)
            {
                float total = 0f; foreach (var t in list) total += t.w;
                float pick = Random.Range(0f, total);
                float acc = 0f;
                int idx = 0;
                for (; idx < list.Count; idx++)
                {
                    acc += list[idx].w;
                    if (acc >= pick) break;
                }
                picks.Add(list[idx].def);
                list.RemoveAt(idx);
            }
            return picks;
        }

        private static UpgradeCardVM ToVM(ProgressionContext ctx, UpgradeDef def)
        {
            // Dry-run Apply() is expensive/mutative; instead, each def’s Description should already summarize.
            // You can add an optional Preview() API if you want exact numbers.
            return new UpgradeCardVM
            {
                Id = def.Id,
                Title = def.Title,
                Subtitle = def.Kind.ToString(),
                Description = def.Description,
                Icon = def.Icon,
                Rarity = def.Rarity,
                PreviewLines = System.Array.Empty<string>(),
                IsDisabled = !def.IsAvailable(ctx),
                DisabledReason = !def.IsAvailable(ctx) ? "Unavailable" : null
            };
        }
    }
}
