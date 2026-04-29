using UnityEngine;

public class AutoWeapon : MonoBehaviour
{
    [Header("手动射击设置")]
    public float manualFireRate = 5f; // 手动射速（每秒发射5发）
    private float manualTimer = 0f;

    [Header("大招强化(自动)设置")]
    public float autoFireRate = 2f;   // 自动追踪射速
    public float attackRange = 100f;  // 索敌范围
    private float autoTimer = 0f;

    // 🔥 状态开关：是否处于大招强化状态？
    public bool isEnhanced = false;

    void Update()
    {
        // 1. 【手动射击逻辑】
        // 使用 Input.GetMouseButton(0) 代替 Down，这样只要按住左键，它就会一直返回 true，实现连续射击
        manualTimer += Time.deltaTime;
        if (Input.GetMouseButton(0) && manualTimer >= 1f / manualFireRate)
        {
            FireManualBullet();
            manualTimer = 0f;
        }

        // 2. 【自动射击逻辑】（仅在强化状态 isEnhanced 为 true 时执行）
        if (isEnhanced)
        {
            autoTimer += Time.deltaTime;
            if (autoTimer >= 1f / autoFireRate)
            {
                FindAndShootNearest();
                autoTimer = 0f;
            }
        }
    }

    // 手动开火方法
    void FireManualBullet()
    {
        // 获取鼠标在游戏世界里的准确位置
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0; // 强制拍扁到 2D 平面

        // 复用之前的发射逻辑
        ShootAt(mousePos);
    }

    // -------- 下方保留你原有的索敌和发射逻辑 --------

    void FindAndShootNearest()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return;

        GameObject nearestEnemy = null;
        float minDistanceSqr = attackRange * attackRange;
        Vector3 currentPos = transform.position;

        foreach (GameObject enemy in enemies)
        {
            if (enemy.activeInHierarchy)
            {
                Vector3 viewportPos = Camera.main.WorldToViewportPoint(enemy.transform.position);
                bool isInsideScreen = viewportPos.x >= -0.05f && viewportPos.x <= 1.05f &&
                                      viewportPos.y >= -0.05f && viewportPos.y <= 1.05f;

                if (!isInsideScreen) continue;

                float distanceSqr = (enemy.transform.position - currentPos).sqrMagnitude;
                if (distanceSqr < minDistanceSqr)
                {
                    minDistanceSqr = distanceSqr;
                    nearestEnemy = enemy;
                }
            }
        }

        if (nearestEnemy != null)
        {
            ShootAt(nearestEnemy.transform.position);
        }
    }

    void ShootAt(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        GameObject bullet = ObjectPool.Instance.GetBullet();
        bullet.transform.position = transform.position;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}