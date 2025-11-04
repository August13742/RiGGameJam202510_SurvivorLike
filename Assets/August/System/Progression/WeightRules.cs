using UnityEngine;

namespace Survivor.Progression.Rule
{
    public abstract class WeightRule : ScriptableObject
    {
        public abstract float GetMultiplier(ProgressionContext ctx, UpgradeDef def);
    }

}
