using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI References")]
    public Text scoreText;
    public Slider chargeSlider;
    public Slider healthSlider;      // 🔥 新增：关联血条
    public GameObject startPanel;
    public GameObject gameOverPanel;
    // 🔥 新增：倒计时的 UI 引用
    public GameObject countdownPanel;
    public TMPro.TextMeshProUGUI countdownText; // 如果你用的是旧版 Text，请换成 public Text countdownText;

    // 🔥 新增：暂停面板引用
    public GameObject pausePanel;
    // 🔥 新增：规则面板引用
    public GameObject rulesPanel;

    [Header("Settings")]
    public float lerpSpeed = 5f;
    public Animator chargeAnimator;

    private float targetCharge = 0f; // 充能目标值
    private float targetHealth = 1f; // 血量目标值 (初始为满血 100%)
    private int score = 0;
    private bool isFull = false;

    [Header("Upgrade System")]
    public int currentLevel = 1;      // 🔥 新增：当前等级（方便观察）
    public int nextLevelScore = 50;   // 第一次升级所需总分
    public int scoreStep = 50;        // 当前的升级跨度
    public int scoreStepIncrement = 20; // 🔥 新增：每次升级后，跨度增加的量（推荐 20-30）

    // 🔥 新增：记录当前是否处于暂停状态
    private bool isPaused = false;

    // 在 UIManager 类中添加这些内容

    private bool isTimerMode = false;

    // 开启计时模式：让充能条准备显示倒计时
    public void StartTimerMode()
    {
        isTimerMode = true;
    }

    // 结束计时模式：恢复充能条的正常逻辑
    public void StopTimerMode()
    {
        isTimerMode = false;
        targetCharge = 0f; // 大招结束，目标值归零
    }

    // 这是一个新方法，专门给大招协程调用，用来强制设置进度条位置
    public void UpdateChargeManual(float percent)
    {
        targetCharge = percent;
        // 在计时模式下，我们希望进度条反应极快，所以直接赋值而不进行 Lerp 平滑处理
        chargeSlider.value = percent;
    }


    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        startPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        // 🔥 新增：游戏开始时，确保规则面板是关闭的
        if (rulesPanel != null) rulesPanel.SetActive(false);

        Time.timeScale = 0f;
        if (healthSlider != null) healthSlider.value = 1f;
    }

    // 🔥 新增：打开规则面板
    public void ShowRules()
    {
        startPanel.SetActive(false); // 隐藏主菜单
        rulesPanel.SetActive(true);  // 显示规则面板
    }

    // 🔥 新增：关闭规则面板，返回主菜单
    public void HideRules()
    {
        rulesPanel.SetActive(false); // 隐藏规则面板
        startPanel.SetActive(true);  // 重新显示主菜单
    }

    // 🔥 唯一的 Update 方法
    void Update()
    {
        // 🔥 新增：按下 Esc 键呼出/关闭暂停菜单
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (isTimerMode)
        {
            // 计时模式下，targetCharge 会在 PlayerSkills 里被不断减小
            chargeSlider.value = targetCharge;
        }
        else
        {
            // 1. 充能条平滑逻辑
            if (Mathf.Abs(chargeSlider.value - targetCharge) > 0.0001f)
            {
                chargeSlider.value = Mathf.Lerp(chargeSlider.value, targetCharge, Time.unscaledDeltaTime * lerpSpeed);
            }

            // 2. 血条平滑逻辑 (可选：如果你希望血条也平滑移动)
            if (healthSlider != null && Mathf.Abs(healthSlider.value - targetHealth) > 0.0001f)
            {
                healthSlider.value = Mathf.Lerp(healthSlider.value, targetHealth, Time.unscaledDeltaTime * lerpSpeed);
            }
        }
    }

    // 🔥 新增：切换暂停状态的核心逻辑
    public void TogglePause()
    {
        // 防冲突保护：如果游戏没开始、已经结束，或者正在选升级技能，直接忽略暂停请求
        if (startPanel.activeSelf || gameOverPanel.activeSelf ||
           (UpgradeManager.Instance != null && UpgradeManager.Instance.upgradePanel.activeSelf))
        {
            return;
        }

        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f; // 暂停或恢复时间
    }

    // 🔥 新增：给暂停面板上的“继续游戏”按钮调用
    // 🔥 修改：不要立刻恢复游戏，而是启动倒计时协程
    public void ResumeGame()
    {
        StartCoroutine(ResumeCountdownRoutine());
    }

    // 🔥 新增：倒计时协程逻辑
    private System.Collections.IEnumerator ResumeCountdownRoutine()
    {
        // 1. 先把暂停菜单隐藏，防止挡住视野
        pausePanel.SetActive(false);

        // 2. 显示倒计时面板
        if (countdownPanel != null) countdownPanel.SetActive(true);

        // 3. 开始 3, 2, 1 倒数
        for (int i = 3; i > 0; i--)
        {
            if (countdownText != null) countdownText.text = i.ToString();

            // 必须用 Realtime，因为此时 Time.timeScale 还是 0！
            yield return new WaitForSecondsRealtime(1f);
        }

        // 4. 倒数结束，清理 UI
        if (countdownText != null) countdownText.text = "GO!";
        yield return new WaitForSecondsRealtime(0.5f); // 稍微展示一下 GO!

        if (countdownPanel != null) countdownPanel.SetActive(false);

        // 5. 真正恢复游戏状态
        isPaused = false;
        Time.timeScale = 1f;
    }

    // 🔥 新增：给暂停面板上的“退出结算”按钮调用
    public void QuitToGameOver()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        ShowGameOver(); // 直接调用你现有的结算逻辑
    }

    public void AddScore(int pts)
    {
        score += pts;
        scoreText.text = "Score: " + score;

        // 检查是否达到升级门槛
        if (score >= nextLevelScore)
        {
            // 1. 增加等级计数
            currentLevel++;

            // 2. 动态提升下一次升级的门槛
            // 逻辑：旧跨度(50) + 增量(20) = 新跨度(70) -> 下次阈值 = 50 + 70 = 120
            scoreStep += scoreStepIncrement;
            nextLevelScore += scoreStep;

            // 3. 调用升级管理器的菜单
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.OpenUpgradeMenu();
            }
        }
    }

    public void UpdateCharge(int current, int max)
    {
        targetCharge = (float)current / max;
        bool currentlyFull = (current >= max);

        if (currentlyFull && !isFull)
        {
            isFull = true;
            // Debug.Log("能量满了！正在尝试启动动画..."); // <--- 加这一行
            if (chargeAnimator != null) chargeAnimator.SetBool("IsFull", true);
        }
        else if (!currentlyFull && isFull)
        {
            isFull = false;
            if (chargeAnimator != null) chargeAnimator.SetBool("IsFull", false);
        }
    }

    // 🔥 新增：给 PlayerController 调用的更新血量方法
    public void UpdateHealth(int current, int max)
    {
        targetHealth = (float)current / max;
    }

    public void StartGame()
    {
        startPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}