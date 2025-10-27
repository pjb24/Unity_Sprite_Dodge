using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임의 전체 상태를 관리하는 매니저 클래스입니다.
/// - 플레이어 생명, 무적, UI, 아이템, 랭킹, 게임 오버 등 모든 게임 흐름을 제어합니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// 싱글톤 인스턴스. 어디서든 GameManager.Instance로 접근 가능합니다.
    /// </summary>
    public static GameManager Instance { get; private set; }

    [Header("플레이어 설정")]
    [Tooltip("게임에서 조작할 플레이어 스크립트")]
    [SerializeField] private PlayerMove player;

    [Tooltip("초기 생명 수")]
    [SerializeField] private int startingLives = 1;

    [Tooltip("피격 후 무적 유지 시간(초)")]
    [SerializeField] private float invincibilityDuration = 1.5f;

    [Header("UI")]
    [Tooltip("생명 수를 표시하는 Text")]
    [SerializeField] private TextMeshProUGUI lifeText;

    [Tooltip("생존 시간을 표시하는 Text")]
    [SerializeField] private TextMeshProUGUI timeText;

    [Tooltip("격려 메시지 컨테이너")]
    [SerializeField] private GameObject messagePanel;

    [Tooltip("격려 메시지를 표시할 Text")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Tooltip("게임 오버 패널")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("게임 오버 시 점수를 표시하는 Text")]
    [SerializeField] private TextMeshProUGUI gameOverScoreText;

    [Tooltip("랭킹을 표시하는 Text")]
    [SerializeField] private TextMeshProUGUI rankingText;

    [Header("격려 메시지 설정")]
    [Tooltip("10초마다 순환될 격려 메시지 목록")]
    [SerializeField]
    private string[] encouragementMessages =
    {
            "좋아요! 계속 버텨봐요!",
            "집중력을 유지하세요!",
            "조금만 더!",
            "리듬을 잃지 마세요!"
        };

    [Tooltip("격려 메시지가 유지되는 시간(초)")]
    [SerializeField] private float encouragementDuration = 2.5f;

    [Header("하트 아이템")]
    [Tooltip("생명을 회복하는 하트 아이템 프리팹")]
    [SerializeField] private GameObject heartItemPrefab;

    [Tooltip("하트 아이템이 등장하는 간격(초)")]
    [SerializeField] private float heartSpawnInterval = 15f;

    [Tooltip("하트 아이템이 사라지기까지 유지되는 시간(초)")]
    [SerializeField] private float heartLifetime = 6f;

    [Header("연동 컴포넌트")]
    [Tooltip("탄환 생성기를 연결해 게임 오버 시 멈춥니다.")]
    [SerializeField] private BulletSpawner bulletSpawner;

    // 리더보드(랭킹) 점수 리스트
    private readonly List<float> leaderboard = new List<float>();
    // PlayerPrefs에 저장할 키
    private const string LeaderboardKey = "GM_LEADERBOARD";
    // 리더보드에 표시할 최대 점수 개수
    private const int MaxLeaderboardEntries = 5;

    // 게임 경과 시간(초)
    private float elapsedTime;
    // 현재 플레이어 생명 수
    private int lives;
    // 게임 오버 상태 여부
    private bool isGameOver;
    // 다음 격려 메시지 표시까지 남은 시간(초)
    private float nextEncouragementTime = 10f;
    // 격려 메시지 표시 타이머
    private float encouragementTimer;
    // 하트 아이템 생성 타이머
    private float heartTimer;
    // 플레이어 무적 상태 여부
    private bool isInvincible;
    // 무적 남은 시간(초)
    private float invincibleTimer;

    /// <summary>
    /// 게임이 진행 중인지 여부
    /// </summary>
    public bool IsGameRunning => !isGameOver;
    /// <summary>
    /// 현재까지 생존한 시간(초)
    /// </summary>
    public float ElapsedTime => elapsedTime;

    /// <summary>
    /// 싱글톤 인스턴스 설정 및 주요 컴포넌트 연결, 리더보드 불러오기
    /// </summary>
    private void Awake()
    {
        // 이미 인스턴스가 있으면 중복 제거
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 플레이어 및 탄환 생성기 자동 연결
        if (player == null)
        {
            player = FindObjectOfType<PlayerMove>();
        }
        if (bulletSpawner == null)
        {
            bulletSpawner = FindObjectOfType<BulletSpawner>();
        }

        // 리더보드 불러오기
        LoadLeaderboard();
    }

    /// <summary>
    /// 게임 시작 시 초기화
    /// </summary>
    private void Start()
    {
        StartGame();
    }

    /// <summary>
    /// 매 프레임마다 게임 상태 업데이트
    /// </summary>
    private void Update()
    {
        // 게임 오버 상태일 때 엔터키로 재시작 가능
        if (!IsGameRunning)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                RestartGame();
            }
            return;
        }

        // 경과 시간 증가
        elapsedTime += Time.deltaTime;
        // 생존 시간 UI 갱신
        UpdateTimerUI();

        // 격려 메시지, 하트 아이템, 무적 상태 처리
        HandleEncouragement();
        HandleHeartSpawn();
        UpdateInvincibility();
    }

    /// <summary>
    /// 게임을 시작할 때 모든 변수와 UI를 초기화
    /// </summary>
    private void StartGame()
    {
        Time.timeScale = 1f;
        isGameOver = false;
        elapsedTime = 0f;
        lives = Mathf.Max(1, startingLives);
        nextEncouragementTime = 10f;
        encouragementTimer = 0f;
        heartTimer = 0f;
        isInvincible = false;
        invincibleTimer = 0f;

        UpdateLifeUI();
        UpdateTimerUI();
        UpdateLeaderboardUI();

        // 격려 메시지, 게임 오버 패널 숨기기
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        // 탄환 생성기 활성화
        if (bulletSpawner != null)
        {
            bulletSpawner.enabled = true;
        }
    }

    /// <summary>
    /// 플레이어가 데미지를 입었을 때 호출
    /// </summary>
    public void DamagePlayer()
    {
        // 게임 진행 중이 아니거나 무적 상태면 무시
        if (!IsGameRunning || isInvincible)
        {
            return;
        }

        // 생명 감소
        lives = Mathf.Max(0, lives - 1);
        UpdateLifeUI();

        player.PlayHitAnimation(true);

        // 생명이 0 이하이면 게임 오버 처리
        if (lives <= 0)
        {
            HandleGameOver();
        }
        else
        {
            // 무적 상태 부여
            isInvincible = true;
            invincibleTimer = invincibilityDuration;
        }
    }

    /// <summary>
    /// 플레이어 생명 추가(하트 아이템 획득 등)
    /// </summary>
    /// <param name="amount">추가할 생명 수(기본값 1)</param>
    public void AddLife(int amount = 1)
    {
        // 게임 진행 중이 아니거나 잘못된 값이면 무시
        if (!IsGameRunning || amount <= 0)
        {
            return;
        }

        lives += amount;
        UpdateLifeUI();
    }

    /// <summary>
    /// 게임 오버 처리 및 UI, 랭킹 갱신
    /// </summary>
    private void HandleGameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f;

        // 탄환 생성기 비활성화
        if (bulletSpawner != null)
        {
            bulletSpawner.enabled = false;
        }

        // 리더보드에 점수 추가
        TryAddScore(elapsedTime);

        // 격려 메시지 숨기기
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }
        encouragementTimer = 0f;

        // 게임 오버 패널 표시
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        // 게임 오버 점수 표시
        if (gameOverScoreText != null)
        {
            gameOverScoreText.text = $"생존 시간: {elapsedTime:F1}초";
        }
    }

    /// <summary>
    /// 일정 시간마다 격려 메시지 표시 및 숨김 처리
    /// </summary>
    private void HandleEncouragement()
    {
        // 경과 시간이 다음 격려 메시지 시간에 도달하면 메시지 표시
        if (elapsedTime >= nextEncouragementTime)
        {
            ShowEncouragement();
            nextEncouragementTime += 10f;
        }

        // 메시지 표시 타이머가 남아있으면 감소시키고, 시간이 다 되면 숨김
        if (encouragementTimer > 0f)
        {
            encouragementTimer -= Time.deltaTime;
            if (encouragementTimer <= 0f)
            {
                HideEncouragement();
            }
        }
    }

    /// <summary>
    /// 격려 메시지 패널을 활성화하고 랜덤 메시지 표시
    /// </summary>
    private void ShowEncouragement()
    {
        if (messagePanel == null || messageText == null || encouragementMessages == null || encouragementMessages.Length == 0)
        {
            return;
        }

        var message = encouragementMessages[Random.Range(0, encouragementMessages.Length)];
        messageText.text = message;
        messagePanel.SetActive(true);
        encouragementTimer = encouragementDuration;
    }

    /// <summary>
    /// 격려 메시지 패널을 숨김
    /// </summary>
    private void HideEncouragement()
    {
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }
    }

    /// <summary>
    /// 일정 시간마다 하트 아이템을 화면 내 랜덤 위치에 생성
    /// </summary>
    private void HandleHeartSpawn()
    {
        if (heartItemPrefab == null)
        {
            return;
        }

        heartTimer += Time.deltaTime;
        // 하트 생성 간격이 안 됐으면 리턴
        if (heartTimer < heartSpawnInterval)
        {
            return;
        }

        heartTimer = 0f;

        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        // 화면 내 랜덤 위치 계산
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector2 randomPosition = new Vector2(Random.Range(-halfWidth * 0.8f, halfWidth * 0.8f), Random.Range(-halfHeight * 0.8f, halfHeight * 0.8f));

        // 하트 아이템 생성
        GameObject heart = Instantiate(heartItemPrefab, randomPosition, Quaternion.identity);
        HeartItem item = heart.GetComponent<HeartItem>();
        if (item != null)
        {
            // 하트 아이템에 유지 시간 설정
            item.Configure(heartLifetime);
        }
        else
        {
            // HeartItem 컴포넌트가 없으면 일정 시간 후 자동 삭제
            Destroy(heart, heartLifetime);
        }
    }

    /// <summary>
    /// 플레이어 무적 상태 타이머 갱신 및 해제
    /// </summary>
    private void UpdateInvincibility()
    {
        if (!isInvincible)
        {
            return;
        }

        invincibleTimer -= Time.deltaTime;
        if (invincibleTimer <= 0f)
        {
            isInvincible = false;
            player.PlayHitAnimation(false);
        }
    }

    /// <summary>
    /// 생명 수 UI 갱신 (하트 이모지로 시각화)
    /// </summary>
    private void UpdateLifeUI()
    {
        if (lifeText == null)
        {
            return;
        }

        if (lives <= 0)
        {
            lifeText.text = "Life : 0";
            return;
        }

        // 생명 수만큼 하트 이모지 표시(최대 10개)
        string heartDisplay = new string('\u2665', Mathf.Clamp(lives, 0, 10));
        lifeText.text = $"Life : {lives}  {heartDisplay}";
    }

    /// <summary>
    /// 생존 시간 UI 갱신
    /// </summary>
    private void UpdateTimerUI()
    {
        if (timeText == null)
        {
            return;
        }

        timeText.text = $"Time : {elapsedTime:F1}s";
    }

    /// <summary>
    /// 리더보드에 점수 추가 및 저장, UI 갱신
    /// </summary>
    /// <param name="score">추가할 점수(생존 시간)</param>
    private void TryAddScore(float score)
    {
        leaderboard.Add(score);
        leaderboard.Sort((a, b) => b.CompareTo(a)); // 내림차순 정렬
        if (leaderboard.Count > MaxLeaderboardEntries)
        {
            leaderboard.RemoveRange(MaxLeaderboardEntries, leaderboard.Count - MaxLeaderboardEntries);
        }

        SaveLeaderboard();
        UpdateLeaderboardUI();
    }

    /// <summary>
    /// PlayerPrefs에서 리더보드 점수 불러오기
    /// </summary>
    private void LoadLeaderboard()
    {
        leaderboard.Clear();
        string saved = PlayerPrefs.GetString(LeaderboardKey, string.Empty);
        if (string.IsNullOrEmpty(saved))
        {
            return;
        }

        string[] parts = saved.Split('|');
        foreach (string part in parts)
        {
            if (float.TryParse(part, out float value))
            {
                leaderboard.Add(value);
            }
        }

        leaderboard.Sort((a, b) => b.CompareTo(a));
        if (leaderboard.Count > MaxLeaderboardEntries)
        {
            leaderboard.RemoveRange(MaxLeaderboardEntries, leaderboard.Count - MaxLeaderboardEntries);
        }
    }

    /// <summary>
    /// PlayerPrefs에 리더보드 점수 저장
    /// </summary>
    private void SaveLeaderboard()
    {
        string data = string.Join("|", leaderboard.Select(v => v.ToString("F3")));
        PlayerPrefs.SetString(LeaderboardKey, data);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 리더보드 UI 갱신 (상위 5개 점수 표시)
    /// </summary>
    private void UpdateLeaderboardUI()
    {
        if (rankingText == null)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("TOP 5");
        for (int i = 0; i < MaxLeaderboardEntries; i++)
        {
            if (i < leaderboard.Count)
            {
                builder.AppendLine($"{i + 1}. {leaderboard[i]:F1}초");
            }
            else
            {
                builder.AppendLine($"{i + 1}. ---");
            }
        }

        rankingText.text = builder.ToString();
    }

    /// <summary>
    /// 현재 씬을 다시 로드하여 게임 재시작
    /// </summary>
    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
