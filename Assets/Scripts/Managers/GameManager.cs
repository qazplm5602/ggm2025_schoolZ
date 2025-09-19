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

        // 게임 속도를 원래대로 복원
        SetGameSpeed(1f);

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
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.ShowWarningMessage("게임 오버!", 3f);
        }

        SetGameSpeed(0.05f);
        StartCoroutine(GameOverSequence());
    }

    /// <summary>
    /// 게임 오버 처리 시퀀스
    /// </summary>
    private IEnumerator GameOverSequence()
    {
        // 메시지 표시 시간만큼 대기
        yield return new WaitForSeconds(0.05f);

        // 게임 종료
        QuitGame();
    }

    /// <summary>
    /// 게임 종료 메소드 (GameManager에서 담당)
    /// </summary>
    private void QuitGame()
    {
        Debug.Log("GameManager: 게임을 종료합니다...");

        // 모든 코루틴 즉시 중지
        StopAllCoroutines();

        // 에디터에서 실행 중인지 빌드에서 실행 중인지 확인
        #if UNITY_EDITOR
            // Unity 에디터에서는 플레이 모드 종료
            Debug.Log("GameManager: Unity 에디터 플레이 모드를 종료합니다.");
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // 빌드된 게임에서는 애플리케이션 종료
            Debug.Log("GameManager: 빌드된 게임을 종료합니다.");
            Application.Quit();
        #endif

        Debug.Log("GameManager: 게임 종료 프로세스 완료");
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
