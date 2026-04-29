using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance;

    public GameObject normalPrefab; // 普通怪预制体
    public GameObject elitePrefab;  // 精英怪预制体
    public int initialSize = 20;

    private Queue<GameObject> normalQueue = new Queue<GameObject>();
    private Queue<GameObject> eliteQueue = new Queue<GameObject>();

    void Awake()
    {
        Instance = this;
        // 预生产两种怪物
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewEnemy(normalPrefab, normalQueue);
            CreateNewEnemy(elitePrefab, eliteQueue);
        }
    }

    void CreateNewEnemy(GameObject prefab, Queue<GameObject> queue)
    {
        GameObject obj = Instantiate(prefab);
        obj.SetActive(false);
        queue.Enqueue(obj);
    }

    // 根据类型获取敌人
    public GameObject GetEnemy(bool isElite)
    {
        Queue<GameObject> targetQueue = isElite ? eliteQueue : normalQueue;
        GameObject prefab = isElite ? elitePrefab : normalPrefab;

        if (targetQueue.Count > 0)
        {
            GameObject obj = targetQueue.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        else
        {
            return Instantiate(prefab);
        }
    }

    public void ReturnEnemy(GameObject obj, bool isElite)
    {
        obj.SetActive(false);
        if (isElite) eliteQueue.Enqueue(obj);
        else normalQueue.Enqueue(obj);
    }
}