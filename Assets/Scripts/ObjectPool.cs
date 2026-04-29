using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    // 使用单例模式，方便全局调用
    public static ObjectPool Instance;

    public GameObject bulletPrefab; // 子弹预制体
    public int poolSize = 50;       // 池子初始大小

    // 使用队列 (Queue) 来管理子弹，先进先出
    private Queue<GameObject> bulletPool = new Queue<GameObject>();

    void Awake()
    {
        Instance = this;
        // 游戏一开始，提前造好 50 发子弹隐藏起来
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(bulletPrefab);
            obj.SetActive(false); // 隐藏备用
            bulletPool.Enqueue(obj); // 放入队列
        }
    }

    // 当需要子弹时，调用这个方法
    public GameObject GetBullet()
    {
        if (bulletPool.Count > 0)
        {
            GameObject obj = bulletPool.Dequeue(); // 从队列拿出一个
            obj.SetActive(true); // 激活它
            return obj;
        }
        else
        {
            // 如果池子空了（比如射速太快），临时新建一个（面试时提一句：这叫动态扩容）
            GameObject obj = Instantiate(bulletPrefab);
            return obj;
        }
    }

    // 当子弹击中目标或飞出屏幕外时，调用这个方法回收
    public void ReturnBullet(GameObject obj)
    {
        obj.SetActive(false); // 隐藏它
        bulletPool.Enqueue(obj); // 重新放回池子排队
    }
}