using System;
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
            public Func<Vector3> WorldPosProvider; // Position is now a delegate
            public bool IsDynamic;                 // Determines if position is updated per-frame

            public float Duration;      // sec
            public float Radius;        // for circle / sector
            public Vector2 Size;        // for box
            public float AngleDeg;      // for sector / oriented box
            public float ArcDeg;        // for sector width
            public Color Color;
        }
    }
}