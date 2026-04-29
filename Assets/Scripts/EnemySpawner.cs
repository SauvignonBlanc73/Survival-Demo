using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public float baseSpawnInterval = 2.0f; // 初始每2秒生一个
    public float eliteProbability = 0.1f;  // 10% 概率是精英怪
    public float spawnRadius = 12f;        // 生成半径（确保在屏幕外）

    private float timer = 0f;
    private float gameTime = 0f;

    void Update()
    {
        gameTime += Time.deltaTime;
        timer += Time.deltaTime;

        // 难度曲线：每过 60 秒，生成间隔缩短 10%
        float currentInterval = baseSpawnInterval / (1 + gameTime / 60f * 0.1f);

        if (timer >= currentInterval)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    void SpawnEnemy()
    {
        bool isElite = Random.value < eliteProbability;

        // --- 核心优化部分开始 ---

        // 1. 获取摄像机的 Orthographic Size (屏幕高度的一半)
        float camHeight = Camera.main.orthographicSize;
        // 2. 根据屏幕宽高比计算宽度的一半
        float camWidth = camHeight * Camera.main.aspect;

        // 3. 计算对角线长度，作为“安全半径”（保证无论哪个方向都在屏幕外）
        // 额外加 1 或 2 个单位的缓冲距离，防止怪物模型的一半露在屏幕里
        float safeRadius = Mathf.Sqrt(camHeight * camHeight + camWidth * camWidth) + 1.5f;

        // --- 核心优化部分结束 ---

        float angle = Random.Range(0f, Mathf.PI * 2);
        Vector3 spawnPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * safeRadius;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            spawnPos += player.transform.position;
        }

        if (EnemyPool.Instance != null)
        {
            GameObject enemy = EnemyPool.Instance.GetEnemy(isElite);
            enemy.transform.position = spawnPos;

            // 💡 额外提示：如果怪物生成后不动，记得在 Enemy 脚本里写上
            // 让怪物朝向 Player 移动的逻辑
        }
    }
}