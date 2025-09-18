using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;
using System.Collections;

/// <summary>
/// 게임 전체를 관리하는 메인 매니저
/// 게임 시작, 초기화, 게임 오버 등을 처리합니다
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("게임 설정")]
    [SerializeField] private bool autoStartGame = true; // 게임 자동 시작
    [SerializeField] private float gameStartDelay = 2f; // 게임 시작 딜레이

    [Header("게임 상태")]
    private bool isGameStarted = false;
    private bool isGamePaused = false;
    private bool isGameOver = false;

    [Header("시스템 참조")]
    private WaveManager waveManager;

    [Header("플레이어 리소스")]
    [SerializeField] private int initialGold = 200;
    private int currentGold;

    /// <summary>
    /// 골드 변경 이벤트 (UI 업데이트용)
    /// </summary>
    public event Action<int> OnGoldChanged;

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeSystems();
    }

    private void Start()
    {
        if (autoStartGame)
        {
            StartCoroutine(StartGameAfterDelay(gameStartDelay));
        }
    }

    private void Update()
    {
        // 게임 일시정지 토글 (ESC 키)
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }

        // 게임 재시작 (R 키)
        if (isGameOver && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartGame();
        }
    }

    /// <summary>
    /// 게임 시스템들 초기화
    /// </summary>
    private void InitializeSystems()
    {
        // WaveManager 찾기
        waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager == null)
        {
            Debug.LogError("WaveManager를 찾을 수 없습니다!");
        }

        // TowerPlacementSystem은 더 이상 직접 참조하지 않음

        // 골드 초기화
        currentGold = initialGold;
    }

    /// <summary>
    /// 게임 시작 (딜레이 후)
    /// </summary>
    private System.Collections.IEnumerator StartGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        StartGame();
    }

    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame()
    {
        if (isGameStarted) return;

        isGameStarted = true;
        isGameOver = false;


        // 웨이브 매니저는 수동으로 시작됨 (플레이어 버튼 클릭)
        // waveManager.StartWave(0); // 제거됨

        // 타워 배치 시스템은 별도로 관리됨

        // 게임 시작 이벤트 호출
        OnGameStarted();
    }

    /// <summary>
    /// 게임 일시정지 토글
    /// </summary>
    public void TogglePause()
    {
        isGamePaused = !isGamePaused;
        Time.timeScale = isGamePaused ? 0f : 1f;


        if (isGamePaused)
        {
            OnGamePaused();
        }
        else
        {
            OnGameResumed();
        }
    }

    /// <summary>
    /// 게임 오버
    /// </summary>
    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        isGameStarted = false;


        // 웨이브 중지
        if (waveManager != null)
        {
            // 웨이브 중지 로직 (필요시 구현)
        }

        OnGameOver();
    }

    /// <summary>
    /// 게임 재시작 (씬 리로딩 대신 상태 리셋)
    /// </summary>
    public void RestartGame()
    {
        StartCoroutine(RestartGameCoroutine());
    }

    /// <summary>
    /// 게임 재시작 코루틴
    /// </summary>
    private IEnumerator RestartGameCoroutine()
    {
        // 게임 상태 초기화
        isGameOver = false;
        isGameStarted = false;
        currentGold = initialGold;

        // WaveManager 리셋
        if (waveManager != null)
        {
            waveManager.ResetWaveManager();
        }

        // TowerPlacementSystem 리셋 (필요시 구현)
        // ...

        // 모든 적 제거
        BaseEnemy[] enemies = FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }

        // 모든 타워 제거 (업그레이드된 타워들도 포함)
        BaseTower[] towers = FindObjectsByType<BaseTower>(FindObjectsSortMode.None);
        foreach (var tower in towers)
        {
            if (tower != null)
            {
                Destroy(tower.gameObject);
            }
        }

        // 플레이어 위치 리셋 (필요시)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 플레이어 초기 위치로 이동 (필요시 구현)
            // player.transform.position = initialPlayerPosition;
        }

        // 잠시 대기하여 오브젝트들이 완전히 파괴되도록 함
        yield return new WaitForSeconds(0.1f);

        // 게임 재시작
        StartGame();

        Debug.Log("게임이 완전히 리셋되고 재시작되었습니다.");
    }

    /// <summary>
    /// 게임 승리
    /// </summary>
    public void GameWin()
    {

        OnGameWin();
    }

    #region 이벤트 핸들러들

    private void OnGameStarted()
    {
        // 게임 시작 시 처리할 내용들
    }

    private void OnGamePaused()
    {
        // 게임 일시정지 시 처리할 내용들
    }

    private void OnGameResumed()
    {
        // 게임 재개 시 처리할 내용들
    }

    private void OnGameOver()
    {
        // 게임 오버 시 처리할 내용들
    }

    private void OnGameWin()
    {
        // 게임 승리 시 처리할 내용들
    }

    #endregion

    #region 퍼블릭 메소드들

    /// <summary>
    /// 현재 게임 상태 확인
    /// </summary>
    public bool IsGameStarted() => isGameStarted;
    public bool IsGamePaused() => isGamePaused;
    public bool IsGameOver() => isGameOver;

    /// <summary>
    /// 골드 관리
    /// </summary>
    public int CurrentGold => currentGold;

    public bool CanAfford(int cost) => currentGold >= cost;

    public bool SpendGold(int cost)
    {
        if (!CanAfford(cost)) return false;

        currentGold -= cost;
        OnGoldChanged?.Invoke(currentGold); // 골드 변경 이벤트 호출
        return true;
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        OnGoldChanged?.Invoke(currentGold); // 골드 변경 이벤트 호출
    }

    /// <summary>
    /// 게임 속도 설정 (디버그용)
    /// </summary>
    public void SetGameSpeed(float speed)
    {
        Time.timeScale = speed;
    }

    #endregion
}
