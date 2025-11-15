using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AugustsUtility.Telegraph.TelegraphDefinition;

namespace AugustsUtility.Telegraph
{
    public static class Telegraph
    {
        // --- Circle ---
        public static Coroutine Circle(MonoBehaviour host, Vector3 pos, float radius, float duration, Color? color = null, Action onFinished = null)
        {
            TelegraphParams p = new TelegraphParams
            {
                Shape = TelegraphShape.Circle,
                WorldPosProvider = () => pos, // Wrap static pos in a lambda
                IsDynamic = false,
                Duration = duration,
                Radius = radius,
                Color = color ?? Color.red
            };
            return TelegraphManager.Instance.PlayTelegraphCoroutine(host, p, onFinished);
        }

        public static Coroutine Circle(MonoBehaviour host, Func<Vector3> posProvider, float radius, float duration, Color? color = null, Action onFinished = null)
        {
            TelegraphParams p = new TelegraphParams
            {
                Shape = TelegraphShape.Circle,
                WorldPosProvider = posProvider,
                IsDynamic = true,
                Duration = duration,
                Radius = radius,
                Color = color ?? Color.red
            };
            return TelegraphManager.Instance.PlayTelegraphCoroutine(host, p, onFinished);
        }

        // --- Box ---
        public static Coroutine Box(MonoBehaviour host, Vector3 pos, Vector2 size, float duration, float angleDeg = 0f, Color? color = null, Action onFinished = null)
        {
            TelegraphParams p = new TelegraphParams
            {
                Shape = TelegraphShape.Box,
                WorldPosProvider = () => pos,
                IsDynamic = false,
                Duration = duration,
                Size = size,
                AngleDeg = angleDeg,
                Color = color ?? Color.yellow
            };
            return TelegraphManager.Instance.PlayTelegraphCoroutine(host, p, onFinished);
        }

        public static Coroutine Box(MonoBehaviour host, Func<Vector3> posProvider, Vector2 size, float duration, float angleDeg = 0f, Color? color = null, Action onFinished = null)
        {
            TelegraphParams p = new TelegraphParams
            {
                Shape = TelegraphShape.Box,
                WorldPosProvider = posProvider,
                IsDynamic = true,
                Duration = duration,
                Size = size,
                AngleDeg = angleDeg,
                Color = color ?? Color.yellow
            };
            return TelegraphManager.Instance.PlayTelegraphCoroutine(host, p, onFinished);
        }

        // --- Sector ---
        public static Coroutine Sector(MonoBehaviour host, Vector3 pos, float radius, float angleDeg, float arcDeg, float duration, Color? color = null, Action onFinished = null)
        {
            TelegraphParams p = new TelegraphParams
            {
                Shape = TelegraphShape.Sector,
                WorldPosProvider = () => pos,
                IsDynamic = false,
                Duration = duration,
                Radius = radius,
                AngleDeg = angleDeg,
                ArcDeg = arcDeg,
                Color = color ?? Color.blue
            };
            return TelegraphManager.Instance.PlayTelegraphCoroutine(host, p, onFinished);
        }

        public static Coroutine Sector(MonoBehaviour host, Func<Vector3> posProvider, float radius, float angleDeg, float arcDeg, float duration, Color? color = null, Action onFinished = null)
        {
            TelegraphParams p = new TelegraphParams
            {
                Shape = TelegraphShape.Sector,
                WorldPosProvider = posProvider,
                IsDynamic = true,
                Duration = duration,
                Radius = radius,
                AngleDeg = angleDeg,
                ArcDeg = arcDeg,
                Color = color ?? Color.blue
            };
            return TelegraphManager.Instance.PlayTelegraphCoroutine(host, p, onFinished);
        }
    }

    /// <summary>
    /// A builder for creating and playing a sequence of telegraphs.
    /// Correctly waits for each step to complete before starting the next.
    /// </summary>
    public sealed class TelegraphSequence
    {
        private readonly MonoBehaviour _host;
        private readonly List<IEnumerator> _steps = new();

        private TelegraphSequence(MonoBehaviour host) => _host = host;

        public static TelegraphSequence Begin(MonoBehaviour host) => new TelegraphSequence(host);

        public TelegraphSequence Circle(Vector3 pos, float radius, float duration, Color? color = null)
        {
            TelegraphParams p = new()
            {
                Shape = TelegraphShape.Circle,
                WorldPosProvider = () => pos,
                IsDynamic = false,
                Duration = duration,
                Radius = radius,
                Color = color ?? Color.red
            };
            _steps.Add(TelegraphManager.Instance.PlayRoutine(p, null));
            return this;
        }

        public TelegraphSequence Sector(Vector3 pos, float radius, float angleDeg, float arcDeg, float duration, Color? color = null)
        {
            TelegraphParams p = new()
            {
                Shape = TelegraphShape.Sector,
                WorldPosProvider = () => pos,
                IsDynamic = false,
                Duration = duration,
                Radius = radius,
                AngleDeg = angleDeg,
                ArcDeg = arcDeg,
                Color = color ?? Color.blue
            };
            _steps.Add(TelegraphManager.Instance.PlayRoutine(p, null));
            return this;
        }

        public TelegraphSequence Wait(float delay)
        {
            _steps.Add(WaitRoutine(delay));
            return this;
        }

        public Coroutine Play()
        {
            return _host.StartCoroutine(RunSequence());
        }

        private IEnumerator WaitRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
        }

        private IEnumerator RunSequence()
        {
            foreach (var step in _steps)
            {
                yield return _host.StartCoroutine(step);
            }
        }
    }
}