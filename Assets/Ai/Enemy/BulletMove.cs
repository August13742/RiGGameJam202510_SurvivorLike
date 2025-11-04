using Survivor.Enemy;
using Survivor.Game;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BulletMove : MonoBehaviour
{

    private float _speed = 3f;
    private float _lifeTime = 7f;

    private Rigidbody2D _rb;
    Vector2 _direction;

    Transform _target;

    [SerializeField] private EnemyDef _def;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        _target = GameObject.FindWithTag("Player")?.transform;
        _direction = ((Vector2)_target.position - (Vector2)transform.position).normalized;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _rb.MovePosition(_rb.position + _direction * _speed * Time.fixedDeltaTime);

        _lifeTime -= Time.fixedDeltaTime;

        if (_lifeTime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.tag == "Player" || col.gameObject.tag == "PlayerProjectile")
        {
            if (!col.TryGetComponent<HealthComponent>(out var target))
                Destroy(gameObject);
               
            target.Damage(_def.ContactDamage / 2);
        }
    }
}
