using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AugustsUtility.Telegraph.TelegraphDefinition;
namespace AugustsUtility.Telegraph
{
    public sealed class TelegraphManager : MonoBehaviour
    {
        public static TelegraphManager Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private TelegraphInstance circlePrefab;
        [SerializeField] private TelegraphInstance boxPrefab;
        [SerializeField] private TelegraphInstance sectorPrefab;

        private readonly Dictionary<TelegraphShape, Stack<TelegraphInstance>> _pool =
            new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            //DontDestroyOnLoad(gameObject); // optional, make true global, else stay as scene global

            foreach (TelegraphShape shape in Enum.GetValues(typeof(TelegraphShape)))
                _pool[shape] = new Stack<TelegraphInstance>();
        }

        private TelegraphInstance GetInstance(TelegraphShape shape)
        {
            var stack = _pool[shape];
            if (stack.Count > 0)
                return stack.Pop();

            TelegraphInstance prefab = shape switch
            {
                TelegraphShape.Circle => circlePrefab,
                TelegraphShape.Box => boxPrefab,
                TelegraphShape.Sector => sectorPrefab,
                _ => circlePrefab
            };

            var inst = Instantiate(prefab, transform);
            inst.gameObject.SetActive(false);
            return inst;
        }

        private void ReturnInstance(TelegraphShape shape, TelegraphInstance inst)
        {
            inst.gameObject.SetActive(false);
            _pool[shape].Push(inst);
        }

        // --- Public API ---

        public Coroutine PlayTelegraphCoroutine(MonoBehaviour host, TelegraphParams p, Action onFinished = null)
        {
            return host.StartCoroutine(PlayRoutine(p, onFinished));
        }

        private IEnumerator PlayRoutine(TelegraphParams p, Action onFinished)
        {
            bool done = false;

            TelegraphInstance inst = GetInstance(p.Shape);
            inst.Begin(p, () =>
            {
                done = true;
                ReturnInstance(p.Shape, inst);
                onFinished?.Invoke();
            });

            while (!done)
                yield return null;
        }
    }
}