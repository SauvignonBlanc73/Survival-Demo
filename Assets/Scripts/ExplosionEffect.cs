using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    public float expandSpeed = 50f; // 扩大速度（设快一点，体现爆炸的瞬间冲击力）
    public float fadeSpeed = 1f;    // 变透明速度

    // 这个变量由释放大招的脚本动态传过来
    [HideInInspector]
    public float targetScale = 1f;

    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.zero; // 初始极小
        Destroy(gameObject, 5f); // 5秒后自动销毁
    }

    void Update()
    {
        // 1. 迅速变大，但最高只长到 targetScale
        // 使用 MoveTowards 可以确保缩放值平滑且精准地停在目标大小
        transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.one * targetScale, expandSpeed * Time.deltaTime);

        // 2. 逐渐变透明
        Color c = sr.color;
        c.a -= fadeSpeed * Time.deltaTime;
        sr.color = c;
    }
}