using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public int maxHp = 100;
    private int currentHp;

    private SpriteRenderer sr;
    private Color originalColor;

    // --- 新增：用于限制范围的变量 ---
    private Vector2 screenBounds;
    private float playerWidth;
    private float playerHeight;

    void Awake()
    {
        currentHp = maxHp;
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
    }

    void Start()
    {
        // 1. 获取主摄像机，计算屏幕在世界空间中的边界坐标
        Camera mainCam = Camera.main;
        // 将屏幕的像素坐标（右上角）转换为世界坐标
        screenBounds = mainCam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCam.transform.position.z));

        // 2. 计算物体的半宽和半高，防止玩家只有中心点在屏幕内而身体出一半
        playerWidth = sr.bounds.extents.x;
        playerHeight = sr.bounds.extents.y;
    }

    void Update()
    {
        // 移动逻辑
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        // 计算这一帧的移动向量
        Vector3 movement = new Vector3(moveX, moveY, 0).normalized * moveSpeed * Time.deltaTime;
        transform.Translate(movement);

        // --- 核心修改：在移动后，强制限制坐标 ---
        Vector3 currentPos = transform.position;

        // 限制 X 轴范围：[左边界 + 半宽, 右边界 - 半宽]
        currentPos.x = Mathf.Clamp(currentPos.x, screenBounds.x * -1 + playerWidth, screenBounds.x - playerWidth);

        // 限制 Y 轴范围：[下边界 + 半高, 上边界 - 半高]
        currentPos.y = Mathf.Clamp(currentPos.y, screenBounds.y * -1 + playerHeight, screenBounds.y - playerHeight);

        // 将修正后的位置重新赋给玩家
        transform.position = currentPos;
    }

    public void TakeDamage(int amount)
    {
        currentHp -= amount;

        StopAllCoroutines();
        StartCoroutine(FlashRed());

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(currentHp, maxHp);
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator FlashRed()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = originalColor;
    }

    void Die()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOver();
        }
        gameObject.SetActive(false);
    }
}