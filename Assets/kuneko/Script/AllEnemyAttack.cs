using UnityEngine;
using Survivor.Game;

public class AllEnemyAttack : MonoBehaviour
{
    [SerializeField] private ContactFilter2D filter2d = default;
    //[SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Vector2 size;
    [SerializeField] private int damage;
    //検知した敵の数
    private Collider2D[] cols = new Collider2D[256];
    private Camera cam;
    //[SerializeField] private EnemyDef enemyDef;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void LayerEnemyAttack()
    {
        //filter2dで得られたコライダーをcolsに格納　返り値は得られたコライダーの個数
        int hitCount = Physics2D.OverlapBox(transform.position, size, 0f, filter2d, cols);
        if (hitCount <= 0) return;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = cols[i];
            if (!col) continue;
            //カメラに写っているか判定(座標をビューポートポイントに変換)
            Vector3 vp = cam.WorldToViewportPoint(col.transform.position);
            bool visible = (vp.x >= 0 && vp.x <= 1 && vp.y >= 0 && vp.y <= 1);
            if (visible)
            {
                if (!col.TryGetComponent<HealthComponent>(out var target)) return;
                target.Damage(damage,transform.position);
            } else
            {
                continue;
            }
        } 
    }
}
