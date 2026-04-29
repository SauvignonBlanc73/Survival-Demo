using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 2f; // 子弹最多飞 2 秒
    private float timer = 0f;

    // 🔥 新增：声明一个事件，用来告诉主角“我打中人了！”
    public static event System.Action OnHitEnemy;

    void Update()
    {
        // 1. 一直往前飞
        transform.Translate(Vector3.right * speed * Time.deltaTime);

        // 2. 计时器累加
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            // 3. 时间到了没打中，把自己还给对象池
            ObjectPool.Instance.ReturnBullet(this.gameObject);
        }
    }

    // 每次从对象池拿出来激活时，计时器清零
    void OnEnable()
    {
        timer = 0f;
    }

    // 🔥 新增：物理触发器检测（必须和 Update、OnEnable 平级）
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(1);

                // 🔥 【失真】核心判定
                // Random.value 返回 0.0 到 1.0 之间的随机数
                if (Random.value < UpgradeManager.stunChance)
                {
                    enemy.Stun(1.0f); // 定身 1 秒
                    // Debug.Log("触发失真！敌人已被定身");
                }
            }

            OnHitEnemy?.Invoke();
            ObjectPool.Instance.ReturnBullet(this.gameObject);
        }
    }
}