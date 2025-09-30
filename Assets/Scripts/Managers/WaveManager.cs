using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using System.Linq;

/// <summary>
/// 웨이브 매니저 - 적 생성 및 웨이브 관리 시스템
/// 자동/수동 웨이브 진행, 적 생성, UI 업데이트를 담당합니다.
/// </summary>
public class WaveManager : MonoBehaviour
{
    #region 싱글톤 패턴
    public static WaveManager Instance { get; private set; }
    #endregion

    #region 상수 정의
    private const float DEFAULT_WAVE_INTERVAL = 10f;
    private const float DEFAULT_MESSAGE_DURATION = 2f;
    #endregion

    #region 직렬화된 필드
    [Header("웨이브 설정")]
    private WaveData[] waves; // 런타임에 Resources에서 로드됨
    [SerializeField] private Transform[] spawnPoints;

    [Header("기본 적 설정")]
    [SerializeField] private GameObject defaultEnemyPrefab;

    [Header("UI 설정")]
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private TextMeshProUGUI waveTimeText;

    [Header("이벤트")]
    [SerializeField] private UnityEngine.Events.UnityEvent onWaveStart;
    [SerializeField] private UnityEngine.Events.UnityEvent onWaveEnd;
    [SerializeField] private UnityEngine.Events.UnityEvent onAllWavesComplete;
    #endregion

    #region 프라이빗 필드
    // 웨이브 상태 관리
    private int currentWaveIndex = 0;
    private bool isWaveActive = false;
    private bool isWaveReady = false;
    private int enemiesRemainingInWave = 0;
    private int totalEnemiesSpawned = 0;

    // 타이머 관리
    private float nextWaveTimer = 0f;
    private bool isWaitingForNextWave = false;

    // 적 관리
    private int currentEnemyCount = 0; // 현재 살아있는 적 수
    #endregion

    #region Unity 생명주기 메소드
    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 런타임에서 WaveData 로드
        LoadWaveDataFromResources();

        // WaveData 배열 검증
        if (waves == null || waves.Length == 0)
        {
            Debug.LogError("WaveManager: WaveData를 로드하지 못했습니다!");
            return;
        }

        // 게임 초기화
        StartGameInitialization();
    }

    private void Update()
    {
        CheckWaveCompletion();
        UpdateWaveTimer();
        UpdateUI();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
    #endregion

    #region 초기화 메소드
    /// <summary>
    /// 런타임에서 WaveData를 Resources 폴더에서 로드
    /// </summary>
    private void LoadWaveDataFromResources()
    {
        WaveData[] loadedWaves = Resources.LoadAll<WaveData>("WaveData");

        if (loadedWaves == null || loadedWaves.Length == 0)
        {
            Debug.LogError("WaveManager: Resources/WaveData 폴더에서 WaveData 파일을 찾을 수 없습니다!");
            waves = new WaveData[0];
            return;
        }

        // 웨이브 파일들을 숫자 순서대로 정렬
        waves = loadedWaves.OrderBy(w =>
        {
            if (w == null || string.IsNullOrEmpty(w.waveName)) return 0;

            // "Wave 1", "Wave 2", ... 형태에서 숫자 추출
            string name = w.waveName;
            if (name.StartsWith("Wave "))
            {
                string numberPart = name.Substring(5); // "Wave " 제거
                if (int.TryParse(numberPart, out int waveNumber))
                {
                    return waveNumber;
                }
            }

            // 파싱 실패시 문자열 그대로 사용
            return 0;
        }).ToArray();

        // 로드 결과 확인
        Debug.Log($"WaveManager: {waves.Length}개의 WaveData 로드 및 정렬됨");
        for (int i = 0; i < Mathf.Min(waves.Length, 5); i++) // 처음 5개만 로그
        {
            if (waves[i] != null)
            {
                Debug.Log($"  [{i}] {waves[i].waveName} (적 수: {waves[i].enemyCount})");
            }
        }
        if (waves.Length > 5)
        {
            Debug.Log($"  ... 그리고 {waves.Length - 5}개 더");
        }
    }

    /// <summary>
    /// 게임 초기화 시작
    /// </summary>
    private void StartGameInitialization()
    {
        // 웨이브 인덱스 초기화 확인
        currentWaveIndex = 0;
        Debug.Log($"게임 초기화: currentWaveIndex = {currentWaveIndex}, 총 웨이브 수 = {waves.Length}");

        ShowControlTutorial();
        StartWaveCountdown(15f); // 15초 후 웨이브 시작
    }

    /// <summary>
    /// 조작법 튜토리얼 표시
    /// </summary>
    private void ShowControlTutorial()
    {
        string controlKeysMessage = "조작법 안내:\n" +
                                   "esc 눌러서 멈춘 뒤 읽으세용\n" +
                                   "TAB - 타워 생성 및 업그레이드, 책상 옆에서 누르세요\n" +
                                   "E - 책상 이동, 책상 옆에서 누르세요\n" +
                                   "화살표키 - 이동\n" +
                                   "SPACE - 웨이브 바로 시작";

        ShowWarningMessage(controlKeysMessage, 9f);
    }
    #endregion

    #region 업데이트 관련 메소드
    /// <summary>
    /// 웨이브 완료 조건 확인
    /// </summary>
    private void CheckWaveCompletion()
    {
        if (isWaveActive && currentEnemyCount == 0 && enemiesRemainingInWave == 0)
        {
            EndCurrentWave();
        }
    }

    /// <summary>
    /// 웨이브 타이머 업데이트
    /// </summary>
    private void UpdateWaveTimer()
    {
        if (isWaitingForNextWave)
        {
            // 스페이스바 입력으로 즉시 웨이브 시작
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Debug.Log($"스페이스바 입력! 웨이브 즉시 시작 (남은 시간: {nextWaveTimer:F1}초)");
                nextWaveTimer = 0f;
                ShowWarningMessage("웨이브 즉시 시작!", 1f);
            }
            else
            {
                nextWaveTimer -= Time.deltaTime;
            }

            // 타이머 만료 확인
            if (nextWaveTimer <= 0f)
            {
                nextWaveTimer = 0f;
                isWaitingForNextWave = false;
                isWaveReady = true;
                StartNextWave();
            }
        }
    }

    /// <summary>
    /// 웨이브 카운트다운 시작
    /// </summary>
    private void StartWaveCountdown(float duration)
    {
        isWaitingForNextWave = true;
        nextWaveTimer = Mathf.Max(0f, duration);
        Debug.Log($"⏰ 웨이브 카운트다운 시작: {nextWaveTimer}초");
    }
    #endregion

    #region 웨이브 관리 메소드
    /// <summary>
    /// 특정 웨이브 시작
    /// </summary>
    public void StartWave(int waveIndex)
    {
        if (waveIndex >= waves.Length)
        {
            Debug.LogError($"웨이브 시작 실패: 유효하지 않은 웨이브 인덱스 {waveIndex} (총 {waves.Length}개 웨이브)");
            return;
        }

        Debug.Log($"=== 웨이브 {waveIndex + 1} 시작 ===");
        if (waves[waveIndex] != null)
        {
            Debug.Log($"웨이브 데이터: {waves[waveIndex].waveName}, 적 수: {waves[waveIndex].enemyCount}");
        }
        else
        {
            Debug.LogError($"waves[{waveIndex}]가 null입니다!");
        }

        ShowWaveStartMessage(waveIndex);
        InitializeWaveState(waveIndex);
        NotifyWaveStart();
        StartEnemySpawning(waveIndex);
    }

    /// <summary>
    /// 웨이브 시작 메시지 표시
    /// </summary>
    private void ShowWaveStartMessage(int waveIndex)
    {
        string waveStartMessage = $"웨이브 {waveIndex + 1} 시작!";
        ShowWarningMessage(waveStartMessage, DEFAULT_MESSAGE_DURATION);
    }

    /// <summary>
    /// 웨이브 상태 초기화
    /// </summary>
    private void InitializeWaveState(int waveIndex)
    {
        currentWaveIndex = waveIndex;
        WaveData currentWave = waves[waveIndex];

        isWaveActive = true;
        isWaveReady = false;
        enemiesRemainingInWave = currentWave.enemyCount;
        totalEnemiesSpawned = 0;
        ResetEnemyCount();
    }

    /// <summary>
    /// 웨이브 시작 이벤트 알림
    /// </summary>
    private void NotifyWaveStart()
    {
        if (onWaveStart != null)
        {
            onWaveStart.Invoke();
        }

        if (enemyCountText != null)
        {
            enemyCountText.text = $"남은 적: {currentEnemyCount}";
        }
    }

    /// <summary>
    /// 적 생성 프로세스 시작
    /// </summary>
    private void StartEnemySpawning(int waveIndex)
    {
        WaveData currentWave = waves[waveIndex];
        StartCoroutine(SpawnEnemies(currentWave));
    }
    #endregion

    /// <summary>
    /// 다음 웨이브 자동 시작
    /// </summary>
    public void StartNextWave()
    {
        if (!isWaveReady || isWaveActive || isWaitingForNextWave || currentWaveIndex >= waves.Length)
        {
            Debug.Log($"StartNextWave 실패: isWaveReady={isWaveReady}, isWaveActive={isWaveActive}, isWaitingForNextWave={isWaitingForNextWave}, currentWaveIndex={currentWaveIndex}, waves.Length={waves.Length}");
            return;
        }

        Debug.Log($"웨이브 {currentWaveIndex + 1} 시작 시도 (총 {waves.Length}개 웨이브 중)");
        if (waves.Length > currentWaveIndex && waves[currentWaveIndex] != null)
        {
            Debug.Log($"시작할 웨이브: {waves[currentWaveIndex].waveName}");
        }

        isWaveReady = false;
        isWaitingForNextWave = false;
        StartWave(currentWaveIndex);
    }

    /// <summary>
    /// 적 생성 코루틴
    /// </summary>
    private IEnumerator SpawnEnemies(WaveData wave)
    {
        int spawnedCount = 0;

        while (spawnedCount < wave.enemyCount)
        {
            Transform spawnPoint;
            if (spawnPoints.Length > 0)
            {
                spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            }
            else
            {
                Debug.LogError("스폰 포인트가 설정되지 않았습니다!");
                yield break;
            }

            GameObject enemyPrefab = wave.enemyPrefab != null ? wave.enemyPrefab : defaultEnemyPrefab;

            if (enemyPrefab == null)
            {
                Debug.LogError($"적 프리팹이 설정되지 않았습니다! WaveData의 enemyPrefab을 설정하거나 WaveManager의 defaultEnemyPrefab을 설정하세요.");
                Debug.LogError($"WaveData: {wave.waveName}, WaveManager: {this.name}");
                yield break;
            }

            Vector3 spawnPosition = FindValidSpawnPosition(spawnPoint.position);
            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, spawnPoint.rotation);

            // 적 속성 적용
            BasicEnemy enemyComponent = enemy.GetComponent<BasicEnemy>();
            if (enemyComponent != null)
            {
                if (wave.healthMultiplier != 1f)
                    enemyComponent.ApplyHealthMultiplier(wave.healthMultiplier);
                if (wave.speedMultiplier != 1f)
                    enemyComponent.ApplySpeedMultiplier(wave.speedMultiplier);
            }

            // 레이어 설정
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1)
                enemy.layer = enemyLayer;

            currentEnemyCount++;
            totalEnemiesSpawned++;
            spawnedCount++;
            enemiesRemainingInWave--;

            yield return new WaitForSeconds(wave.spawnInterval);
        }

        // 모든 적이 생성될 때까지 대기
        while (currentEnemyCount > 0)
        {
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// 현재 웨이브 종료 처리
    /// </summary>
    private void EndCurrentWave()
    {
        Debug.Log($"웨이브 {currentWaveIndex + 1} 완료");
        isWaveActive = false;

        if (onWaveEnd != null)
        {
            onWaveEnd.Invoke();
        }

        // 다음 웨이브 준비 또는 게임 종료
        if (IsNextWaveAvailable())
        {
            PrepareNextWave();
        }
        else
        {
            HandleGameCompletion();
        }
    }

    /// <summary>
    /// 다음 웨이브 존재 여부 확인
    /// </summary>
    private bool IsNextWaveAvailable()
    {
        currentWaveIndex++;
        return currentWaveIndex < waves.Length;
    }

    /// <summary>
    /// 다음 웨이브 준비
    /// </summary>
    private void PrepareNextWave()
    {
        StartWaveCountdown(DEFAULT_WAVE_INTERVAL);
        string clearMessage = $"웨이브 {currentWaveIndex} 클리어! 다음 웨이브 준비 중...\n[SPACE] 즉시 시작";
        ShowWarningMessage(clearMessage, 6f);
    }

    /// <summary>
    /// 게임 완료 처리
    /// </summary>
    private void HandleGameCompletion()
    {
        ShowWarningMessage("모든 웨이브 클리어! 게임 종료!", 5f);
        OnAllWavesComplete();
    }

    /// <summary>
    /// 적 카운트 초기화
    /// </summary>
    private void ResetEnemyCount()
    {
        currentEnemyCount = 0;
    }

    /// <summary>
    /// WaveManager 상태 완전 리셋
    /// </summary>
    public void ResetWaveManager()
    {
        currentWaveIndex = 0;
        isWaveActive = false;
        isWaveReady = false;
        isWaitingForNextWave = false;
        currentEnemyCount = 0;
        totalEnemiesSpawned = 0;
        nextWaveTimer = 0f;
        enemiesRemainingInWave = 0;

        UpdateUI();

        if (waves.Length > 0)
        {
            StartWaveCountdown(DEFAULT_WAVE_INTERVAL);
        }
    }

    /// <summary>
    /// 모든 웨이브 완료 처리
    /// </summary>
    private void OnAllWavesComplete()
    {
        isWaveReady = false;

        if (onAllWavesComplete != null)
        {
            onAllWavesComplete.Invoke();
        }
    }

    private void UpdateUI()
    {
        if (waveText != null)
        {
            waveText.text = $"웨이브: {currentWaveIndex + 1}/{waves.Length}";
        }

        if (enemyCountText != null)
        {
            if (isWaveActive)
            {
                enemyCountText.text = $"남은 적: {currentEnemyCount}";
            }
            else if (isWaitingForNextWave)
            {
                enemyCountText.text = $"남은 적: 모두 처치됨";
            }
            else if (isWaveReady)
            {
                enemyCountText.text = $"웨이브 준비 완료";
            }
            else
            {
                enemyCountText.text = $"웨이브 대기 중";
            }
        }
        else
        {
            Debug.LogWarning("enemyCountText가 설정되지 않았습니다. WaveManager의 Inspector에서 연결해주세요.");
        }

        if (waveTimeText != null)
        {
            if (isWaveActive)
            {
                waveTimeText.text = $"웨이브 진행 중";
            }
            else if (isWaitingForNextWave)
            {
                int displayTime = Mathf.Max(0, Mathf.CeilToInt(nextWaveTimer));
                waveTimeText.text = $"다음 웨이브까지: {displayTime}초";
            }
            else if (isWaveReady)
            {
                waveTimeText.text = $"웨이브 시작 대기 중";
            }
            else
            {
                waveTimeText.text = $"게임 준비 중";
            }
        }
        else
        {
            Debug.LogWarning("waveTimeText가 설정되지 않았습니다. WaveManager의 Inspector에서 연결해주세요.");
        }

        // 경고 텍스트 관리
        if (warningText != null && isWaveActive && currentWaveIndex > 0)
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameOver())
            {
                warningText.gameObject.SetActive(false);
            }
        }
    }

    public void OnEnemyDeath(GameObject enemy)
    {
        if (enemy == null)
        {
            Debug.LogError("OnEnemyDeath: enemy가 null입니다!");
            return;
        }

        if (currentEnemyCount > 0)
        {
            currentEnemyCount--;

            BasicEnemy enemyComponent = enemy.GetComponent<BasicEnemy>();
            if (enemyComponent != null)
            {
                int goldReward = GetGoldRewardForEnemy(enemy);
                GameManager.Instance?.AddGold(goldReward);
                Debug.Log($"적 사망: {enemy.name} (+{goldReward}G)");
            }

            UpdateUI();
        }
        else
        {
            Debug.LogWarning($"적 카운트가 이미 0인데 사망 이벤트 발생: {enemy.name}");
        }
    }

    private int GetGoldRewardForEnemy(GameObject enemy)
    {
        int baseGold = GetBaseGoldForEnemy(enemy);

        if (currentWaveIndex >= 0 && currentWaveIndex < waves.Length && waves[currentWaveIndex] != null)
        {
            WaveData currentWave = waves[currentWaveIndex];
            baseGold = Mathf.RoundToInt(baseGold * currentWave.goldRewardMultiplier);
        }

        return baseGold;
    }

    private int GetBaseGoldForEnemy(GameObject enemy)
    {
        string enemyName = enemy.name.ToLower();

        if (enemyName.Contains("basic"))
        {
            return 20;
        }
        else if (enemyName.Contains("attacking"))
        {
            return 30;
        }
        else if (enemyName.Contains("zombie"))
        {
            return 40;
        }
        else if (enemyName.Contains("boss"))
        {
            return 100;
        }

        return 20; // 기본 보상
    }

    /// <summary>
    /// NavMesh 위의 유효한 스폰 위치를 찾습니다
    /// </summary>
    private Vector3 FindValidSpawnPosition(Vector3 desiredPosition)
    {
        // 1. 원하는 위치가 NavMesh 위인지 먼저 확인
        if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        // 2. NavMesh 위가 아니라면 주변을 검색
        float searchRadius = 2.0f;
        float stepSize = 0.5f;

        for (float radius = stepSize; radius <= searchRadius; radius += stepSize)
        {
            int pointsPerCircle = Mathf.CeilToInt(radius * 2 * Mathf.PI / stepSize);

            for (int i = 0; i < pointsPerCircle; i++)
            {
                float angle = (2 * Mathf.PI * i) / pointsPerCircle;
                Vector3 checkPosition = desiredPosition + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );

                if (NavMesh.SamplePosition(checkPosition, out hit, 1.0f, NavMesh.AllAreas))
                {
                    Debug.Log($"[{gameObject.name}] 스폰 위치 조정: {desiredPosition:F1} → {hit.position:F1} (반경: {radius:F1}m)");
                    return hit.position;
                }
            }
        }

        // 3. 그래도 찾지 못하면 원래 위치 반환
        Debug.LogWarning($"[{gameObject.name}] 유효한 NavMesh 스폰 위치를 찾을 수 없습니다! 원래 위치 사용: {desiredPosition:F1}");
        return desiredPosition;
    }

    /// <summary>
    /// 모든 스폰 포인트에서 플레이어까지의 경로가 유효한지 확인
    /// </summary>
    private bool AreAllSpawnPointsValid()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("플레이어 오브젝트를 찾을 수 없습니다! Player 태그가 설정되어 있는지 확인하세요.");
            return false;
        }

        Transform playerTransform = playerObj.transform;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("스폰 포인트가 설정되지 않았습니다! WaveManager의 Spawn Points에 스폰 포인트를 추가하세요.");
            return false;
        }

        if (!NavMesh.SamplePosition(playerTransform.position, out NavMeshHit playerHit, 1.0f, NavMesh.AllAreas))
        {
            Debug.LogError("플레이어 위치에 NavMesh가 존재하지 않습니다!");
            Debug.LogError("Window > AI > Navigation 에서 NavMesh를 Bake했는지 확인하세요.");
            return false;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform spawnPoint = spawnPoints[i];
            if (spawnPoint == null)
            {
                Debug.LogError($"스폰 포인트 {i}가 null입니다!");
                return false;
            }

            if (!NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            {
                Debug.LogError($"스폰 포인트 {i} ({spawnPoint.name})가 NavMesh 위에 없습니다!");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 현재 웨이브 정보 반환
    /// </summary>
    public int GetCurrentWave()
    {
        return currentWaveIndex + 1;
    }

    /// <summary>
    /// 웨이브 진행 중인지 확인
    /// </summary>
    public bool IsWaveActive()
    {
        return isWaveActive;
    }

    /// <summary>
    /// 현재 웨이브 준비 상태 확인
    /// </summary>
    public bool IsWaveReady()
    {
        return isWaveReady;
    }

    /// <summary>
    /// 남은 웨이브 수 반환
    /// </summary>
    public int GetRemainingWaves()
    {
        if (waves == null) return 0;
        return waves.Length - currentWaveIndex;
    }

    public void ShowWarningMessage(string message, float duration = DEFAULT_MESSAGE_DURATION)
    {
        if (warningText == null)
        {
            warningText = FindFirstObjectByType<TextMeshProUGUI>();
            if (warningText == null)
            {
                Debug.LogError("WarningText를 찾을 수 없습니다.");
                return;
            }
        }

        if (currentMessageCoroutine != null)
        {
            StopCoroutine(currentMessageCoroutine);
        }

        warningText.text = message;
        warningText.gameObject.SetActive(true);
        currentMessageCoroutine = StartCoroutine(HideMessageAfterDelay(duration));
    }

    private IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
        }
    }

    private Coroutine currentMessageCoroutine;
}