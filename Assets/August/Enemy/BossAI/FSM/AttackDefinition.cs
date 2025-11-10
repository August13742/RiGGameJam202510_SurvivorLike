using UnityEngine;
namespace Survivor.Enemy.FSM
{
    public enum AttackType
    {
        Simple,
        Dashing,
        RangedSequence
    }

    [System.Serializable]
    public class AttackDefinition
    {
        [Header("General")]
        public AttackType Type = AttackType.Simple;
        public string CooldownTag;
        public float Cooldown = 3.0f;
        public float Weight = 1.0f;

        [Header("Simple Animation")]
        public string AnimationName;
        public float AnimationDuration = 1.0f;

        [Header("Dashing Attack")]
        public string DashAnimationName = "Attack1";
        public float DashTelegraphTime = 0.5f;
        public float DashSpeed = 20f;
        public float DashDuration = 0.4f;
        public int DashCount = 2;
        public int EnragedDashCount = 3; // Used if health is low
        public float HealthThresholdForEnrage = 0.5f;

        [Header("Ranged Sequence")]
        public GameObject ProjectilePrefab;
        public Transform FirePoint;
        public string RangedAimAnimation = "AttackRanged_Aim";
        public string RangedFireAnimation = "AttackRanged_Fire";
        public float AimDuration = 0.8f;
        public int ShotCount = 3;
        public float DelayBetweenShots = 0.3f;
    }
}