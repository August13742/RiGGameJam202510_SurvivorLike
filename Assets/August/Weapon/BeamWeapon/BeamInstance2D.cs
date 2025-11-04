using System.Collections.Generic;
using UnityEngine;
using Survivor.Game;

namespace Survivor.Weapon
{
    [DisallowMultipleComponent]
    public sealed class BeamInstance2D : MonoBehaviour, IPoolable
    {
        [SerializeField] private LineRenderer _lr; // auto-created if null

        // Params per fire
        private Transform _origin;
        private Vector2 _dir;
        private float _length;
        private float _width;
        private float _duration;
        private int _desiredDamageTicks;
        private float _tickInterval;
        private int _damagePerTick;

        // State
        private float _tLeft;
        private int _tickFired = 0;
        private ObjectPool _pool;
        private readonly Collider2D[] _hits = new Collider2D[64];
        private readonly HashSet<HealthComponent> _seen = new(32);

        // VFX
        private Material _matInstance;
        private float _uvOffset;
        private float _uvScrollRate;
        private AnimationCurve _alphaCurve;
        private int _colorPropID;

        // Tracking
        private bool _followOrigin;
        private Vector2 _startSnapshot;
        private Vector2 _dirSnapshot;
        private Vector2 _targetPosSnapshot;

        private IHitEventSink _sink;
        private float _critChance = 0f;
        private float _critMul = 1f;
        private bool _critPerTick = true;
        private ContactFilter2D _filter;

        private void Awake()
        {
            if (_lr == null)
            {
                _lr = gameObject.AddComponent<LineRenderer>();
                _lr.positionCount = 2;
                _lr.numCornerVertices = 2;
                _lr.numCapVertices = 2;
                _lr.textureMode = LineTextureMode.Tile;
                _lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _lr.receiveShadows = false;
                _lr.alignment = LineAlignment.TransformZ;
                _lr.generateLightingData = false;
                _lr.sortingLayerName = "Projectiles";
            }
            _colorPropID = Shader.PropertyToID("_Color");
        }

        public void SetPool(ObjectPool pool) => _pool = pool;

        public void SetHitSink(IHitEventSink sink) => _sink = sink;

        public void ConfigureCrit(float chance, float mul, bool perTick)
        {
            _critChance = Mathf.Clamp01(chance);
            _critMul = Mathf.Max(1f, mul);
            _critPerTick = perTick;
        }

        /// <summary>Call this once from the weapon so beam hits the correct target layers.</summary>
        public void SetTargetMask(LayerMask mask)
        {
            _filter = new ContactFilter2D { useTriggers = true };
            _filter.SetLayerMask(mask);
        }

        public void Configure(
            Transform origin, Vector2 dir, float length, float width,
            float duration, int desiredTicks, float tickInterval, int damagePerTick,
            Material sourceMat, float uvScrollRate, AnimationCurve alphaOverLife,
            bool followOrigin = true)
        {
            _origin = origin;
            _dir = dir.sqrMagnitude > 0 ? dir.normalized : Vector2.right;
            _length = Mathf.Max(0.01f, length);
            _width = Mathf.Max(0.01f, width);
            _duration = Mathf.Max(0.01f, duration);

            _desiredDamageTicks = Mathf.Max(1, desiredTicks);
            _tickInterval = tickInterval;

            _damagePerTick = Mathf.Max(0, damagePerTick);
            _alphaCurve = alphaOverLife ?? AnimationCurve.Linear(0, 1, 1, 1);
            _tLeft = _duration;
            _tickFired = 0;
            _uvOffset = 0f;
            _uvScrollRate = uvScrollRate;

            _followOrigin = followOrigin;

            _startSnapshot = origin ? (Vector2)origin.position : (Vector2)transform.position;
            _dirSnapshot = _dir;
            _targetPosSnapshot = _startSnapshot + (_dir * _length);

            if (_matInstance == null) _matInstance = new Material(sourceMat);
            else _matInstance.CopyPropertiesFromMaterial(sourceMat);

            _lr.material = _matInstance;
            _lr.startWidth = _width;
            _lr.endWidth = _width;

            UpdateVisual(0f, 1f);
        }

        private void OnEnable() { if (_lr) _lr.enabled = true; }
        private void OnDisable() { if (_lr) _lr.enabled = false; }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _tLeft -= dt;
            if (_tLeft <= 0f) { Despawn(); return; }

            float elapsed = _duration - _tLeft;
            int ticksShouldHaveFired = Mathf.Min(
                _desiredDamageTicks,
                Mathf.FloorToInt(elapsed / _tickInterval) + 1
            );

            while (_tickFired < ticksShouldHaveFired)
                DoDamageTick();

            UpdateVisual(dt, _alphaCurve.Evaluate(1f - (_tLeft / _duration)));
        }

        private Vector2 GetCurrentDirection()
        {
            if (_followOrigin && _origin)
            {
                Vector2 toTarget = _targetPosSnapshot - (Vector2)_origin.position;
                return toTarget.sqrMagnitude > 1e-8f ? toTarget.normalized : _dirSnapshot;
            }
            return _dirSnapshot;
        }

        private void DoDamageTick()
        {
            _tickFired++;

            Vector2 start = _followOrigin
                ? (_origin ? (Vector2)_origin.position : (Vector2)transform.position)
                : _startSnapshot;

            Vector2 currentDir = GetCurrentDirection();

            Vector2 mid = start + currentDir * (_length * 0.5f);

            int count = Physics2D.OverlapBox(
                point: mid,
                size: new Vector2(_length, _width),
                angle: Mathf.Atan2(currentDir.y, currentDir.x) * Mathf.Rad2Deg,
                contactFilter: _filter,
                results: _hits);

            _seen.Clear();
            for (int i = 0; i < count; i++)
            {
                var c = _hits[i];
                if (!c.TryGetComponent<HealthComponent>(out var hp)) continue;
                if (!_seen.Add(hp)) continue; // prevent duplicate per tick

                float dealt = _damagePerTick;
                bool crit = false;
                if (_critPerTick)
                {
                    crit = (Random.value < _critChance);
                    if (crit) dealt = Mathf.Round(dealt * _critMul * 10f) / 10f;
                }

                hp.Damage(dealt,crit);

                _sink?.OnHit(dealt, c.transform.position, crit);
                if (hp.IsDead) _sink?.OnKill(c.transform.position);
            }
        }

        private void UpdateVisual(float dt, float alpha)
        {
            Vector3 a = _followOrigin
                ? (_origin ? _origin.position : transform.position)
                : (Vector3)_startSnapshot;

            Vector2 currentDir = GetCurrentDirection();
            Vector3 b = a + (Vector3)(currentDir * _length);

            _lr.SetPosition(0, a);
            _lr.SetPosition(1, b);

            if (_matInstance)
            {
                _uvOffset += _uvScrollRate * dt;
                _matInstance.mainTextureOffset = new Vector2(_uvOffset, 0f);

                if (_matInstance.HasProperty(_colorPropID))
                {
                    Color c = _matInstance.GetColor(_colorPropID);
                    c.a = alpha;
                    _matInstance.SetColor(_colorPropID, c);
                }
            }
        }

        private void Despawn()
        {
            if (_lr) _lr.enabled = false;
            if (_pool != null) _pool.Return(gameObject);
            else gameObject.SetActive(false);
        }

        public void OnSpawned() { }
        public void OnDespawned() { }
    }
}
