using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Survivor.Progression
{
    public sealed class OfferBuilder
    {
        // Store the last offered defs so Pick() can find them.
        public UpgradeDef[] LastDefs { get; private set; }

        private readonly List<UpgradeDef> _choices = new ();

        public UpgradeCardVM[] BuildOffer(ProgressionContext ctx, UpgradeDef[] pool, int count)
        {
            _choices.Clear();

            float totalWeight = 0f;
            var weightedPool = new List<(UpgradeDef def, float weight)>();

            // 1. Calculate weights for all available upgrades in the pool
            foreach (var def in pool)
            {
                float weight = def.ComputeWeight(ctx);
                if (weight > 0)
                {
                    weightedPool.Add((def, weight));
                    totalWeight += weight;
                }
            }

            // 2. Perform weighted random selection without replacement
            for (int i = 0; i < count && weightedPool.Count > 0; i++)
            {
                float pick = Random.Range(0, totalWeight);
                float current = 0f;
                for (int j = 0; j < weightedPool.Count; j++)
                {
                    current += weightedPool[j].weight;
                    if (current >= pick)
                    {
                        var (def, weight) = weightedPool[j];
                        _choices.Add(def);

                        //Debug.Log($"choice appended: {def.Id}");
                        totalWeight -= weight;
                        weightedPool.RemoveAt(j);
                        break;
                    }
                }
            }

            LastDefs = _choices.ToArray();

            // 3. Convert the chosen UpgradeDefs into ViewModels for UI
            return LastDefs.Select(def => new UpgradeCardVM
            {
                Id = def.Id,
                Title = def.Title,
                Description = def.Description,
                Icon = def.Icon,
                Rarity = def.Rarity,
                PreviewLines = def.GetPreviewLines(ctx)
            }).ToArray();
        }
    }
}