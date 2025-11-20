using Survivor.Game;
using System.Collections;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(
        fileName = "New SummonPixieRoadblockPattern",
        menuName = "Defs/Boss Attacks/Summon Pixie Roadblock")]
    public sealed class AttackPattern_SummonPixieRoadblock : AttackPattern
    {
        [Header("Pixie")]
        [SerializeField] private GameObject pixiePrefab;
        [SerializeField] private PixieConfig pixieConfig;
        [SerializeField] private int maxSimultaneousPixies = 2; 

        [Header("Spawn")]
        [SerializeField] private float spawnRadius = 2.5f;

        [Header("SFX")]
        [SerializeField] private SFXResource fireSFX;

        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null ||
                controller.PlayerTransform == null ||
                pixiePrefab == null ||
                pixieConfig == null)
            {
                yield break;
            }

            // cap how many are alive at once
            int existing = 0;
            PixieRoadblockCaster[] all =
                Object.FindObjectsByType<PixieRoadblockCaster>(FindObjectsSortMode.None);

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].Owner == controller)
                    existing++;
            }

            if (existing >= maxSimultaneousPixies)
                yield break;

            
            Vector2 bossPos = controller.BehaviorPivotWorld;
            Vector2 playerPos = controller.PlayerTransform.position;

            // Spawn slightly biased towards side of player so it’s not stacked
            Vector2 dirToPlayer = (playerPos - bossPos);
            if (dirToPlayer.sqrMagnitude < 0.0001f)
                dirToPlayer = Vector2.right;
            dirToPlayer.Normalize();

            Vector2 side = new Vector2(-dirToPlayer.y, dirToPlayer.x);
            float sideSign = Random.value < 0.5f ? -1f : 1f;

            Vector2 spawnPos = bossPos
                             + dirToPlayer * (spawnRadius * 0.5f)
                             + side * (spawnRadius * 0.5f * sideSign);

            GameObject go = Object.Instantiate(pixiePrefab, spawnPos, Quaternion.identity);
            PixieRoadblockCaster pixie = go.GetComponent<PixieRoadblockCaster>();
            if (pixie != null)
            {
                pixie.Init(controller, controller.PlayerTransform, pixieConfig);
            }

            AudioManager.Instance?.PlaySFX(fireSFX);
            // fire-and-forget
            yield break;
        }
    }
}
