using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AugustsUtility.Telegraph.TelegraphDefinition;
namespace AugustsUtility.Telegraph
{
    public static class Telegraph
    {
        public static Coroutine Circle(MonoBehaviour host, Vector3 pos,
                                        float radius, float duration,
                                        Color? color = null,
                                        Action onFinished = null)
        {
            TelegraphParams p = new TelegraphParams
            {
                Shape = TelegraphShape.Circle,
                WorldPos = pos,
                Duration = duration,
                Radius = radius,
                Size = Vector2.zero,
                AngleDeg = 0f,
                ArcDeg = 0f,
                Color = color ?? Color.red
            };

            return TelegraphManager.Instance.PlayTelegraphCoroutine(host, p, onFinished);
        }

        public static Coroutine Box(MonoBehaviour host, Vector3 pos,
                                    Vector2 size, float duration,
                                    float angleDeg = 0f,
                                    Color? color = null,
                                    Action onFinished = null)
        {
            TelegraphParams p = new TelegraphParams
            {
                Shape = TelegraphShape.Box,
                WorldPos = pos,
                Duration = duration,
                Radius = 0f,
                Size = size,
                AngleDeg = angleDeg,
                ArcDeg = 0f,
                Color = color ?? Color.yellow
            };

            return TelegraphManager.Instance.PlayTelegraphCoroutine(host, p, onFinished);
        }

        public static Coroutine Sector(MonoBehaviour host, Vector3 pos,
                                        float radius, float arcDeg,
                                        float angleDeg, float duration,
                                        Color? color = null,
                                        Action onFinished = null)
        {
            TelegraphParams p = new TelegraphParams
            {
                Shape = TelegraphShape.Sector,
                WorldPos = pos,
                Duration = duration,
                Radius = radius,
                Size = Vector2.zero,
                AngleDeg = angleDeg,
                ArcDeg = arcDeg,
                Color = color ?? Color.cyan
            };

            return TelegraphManager.Instance.PlayTelegraphCoroutine(host, p, onFinished);
        }

    }


    public sealed class TelegraphSequence
    {
        private readonly MonoBehaviour _host;
        private readonly List<Action> _steps = new();

        private TelegraphSequence(MonoBehaviour host) => _host = host;

        public static TelegraphSequence Begin(MonoBehaviour host) => new TelegraphSequence(host);

        public TelegraphSequence Circle(Vector3 pos, float radius, float duration, Color? color = null)
        {
            _steps.Add(() =>
            {
                TelegraphParams p = new()
                {
                    Shape = TelegraphShape.Circle,
                    WorldPos = pos,
                    Duration = duration,
                    Radius = radius,
                    Size = Vector2.zero,
                    AngleDeg = 0f,
                    Color = color ?? Color.red
                };
                TelegraphManager.Instance.PlayTelegraphCoroutine(_host, p, null);
            }); 
            return this;
        }

        public Coroutine Play()
        {
            return _host.StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            foreach (var step in _steps)
            {
                step();
                yield return new WaitForSeconds(0.1f); // Small delay between telegraphs, adjust as needed
            }
        }
    }
}