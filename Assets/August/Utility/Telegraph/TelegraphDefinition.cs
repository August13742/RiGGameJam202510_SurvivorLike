using UnityEngine;
namespace AugustsUtility.Telegraph
{
    public class TelegraphDefinition : MonoBehaviour
    {
        public enum TelegraphShape
        {
            Circle,
            Box,
            Sector,
        }

        [System.Serializable]
        public struct TelegraphParams
        {
            public TelegraphShape Shape;
            public Vector3 WorldPos;
            public float Duration;      // sec
            public float Radius;        // for circle / sector
            public Vector2 Size;        // for box
            public float AngleDeg;      // for sector / oriented box
            public Color Color;
        }
    }
}

