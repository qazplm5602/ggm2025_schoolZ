using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;

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
    private const float WARNING_MESSAGE_DURATION = 5f;
    private const float WAVE_START_MESSAGE_DURATION = 2f;
    private const float WAVE_CLEAR_MESSAGE_DURATION = 6f;
    private const float GAME_CLEAR_MESSAGE_DURATION = 5f;
    private const float UI_FADE_DURATION = 0.3f;
    private const float UI_SCALE_AMOUNT = 0.8f;
    #endregion

    #region 직렬화된 필드
    [Header("웨이브 설정")]
    [SerializeField] private WaveData[] waves;
    [SerializeField] private Transform[] spawnPoints;

    [Header("기본 적 설정")]
    [SerializeField] private GameObject defaultEnemyPrefab;

    [Header("UI 설정")]
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private TextMeshProUGUI warningText;

    [Header("WaveTime 표시")]
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

    // 적 관리 (간단한 카운트 방식)
    private int currentEnemyCount = 0;  // 현재 살아있는 적 수
    #endregion

    #region Unity 생명주기 메소드
    private void Awake()
    {
        InitializeSingleton();
    }

    private void Start()
    {
        InitializeWaveSystem();
    }

    private void Update()
    {
        // 디버깅: 현재 상태 로그 (필요시 활성화)
        // Debug.Log($"Update 상태: isWaveReady={isWaveReady}, isWaveActive={isWaveActive}, isWaitingForNextWave={isWaitingForNextWave}, currentWaveIndex={currentWaveIndex}, timer={nextWaveTimer:F1}");

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
    /// 싱글톤 패턴 초기화
    /// </summary>
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 웨이브 시스템 초기화
    /// </summary>
    private void InitializeWaveSystem()
    {
        if (waves.Length > 0)
        {
            // 첫 번째 웨이브도 카운트다운 적용
            StartWaveCountdown(DEFAULT_WAVE_INTERVAL);
            ShowWarningMessage("게임 시작! 첫 번째 웨이브 준비 중...\n[SPACE] 즉시 시작", WARNING_MESSAGE_DURATION);
        }
    }
    #endregion

    #region 업데이트 관련 메소드
    /// <summary>
    /// 적 카운트 초기화 (새로운 웨이브 시작 시)
    /// </summary>
    private void ResetEnemyCount()
    {
        currentEnemyCount = 0;
        // Debug.Log("🔄 적 카운트 초기화 완료");
    }

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
                nextWaveTimer = 0f; // 음수 방지
                // 타이머 만료 시 상태 설정 (명확하게)
                isWaitingForNextWave = false;
                isWaveReady = true;

                StartNextWave();
            }
            else if (nextWaveTimer <= 1f) // 1초 남았을 때 로그
            {
                // Debug.Log($"웨이브 타이머: {nextWaveTimer:F1}초 남음");
            }
        }
    }

    /// <summary>
    /// 웨이브 카운트다운 시작
    /// </summary>
    private void StartWaveCountdown(float duration)
    {
        isWaitingForNextWave = true;
        nextWaveTimer = Mathf.Max(0f, duration); // 음수 방지
        Debug.Log($"⏰ 웨이브 카운트다운 시작: {nextWaveTimer}초");
    }
    #endregion

    #region 경고 메시지 시스템
    /// <summary>
    /// 경고 메시지 표시 (Dotween 애니메이션 적용)
    /// </summary>
    public void ShowWarningMessage(string message, float duration = WARNING_MESSAGE_DURATION)
    {
        if (warningText == null)
        {
            Debug.LogWarning("WarningText가 설정되지 않았습니다.");
            return;
        }

        ShowAnimatedWarningMessage(message, duration);
    }

    /// <summary>
    /// 애니메이션 효과가 적용된 경고 메시지 표시
    /// </summary>
    private void ShowAnimatedWarningMessage(string message, float duration)
    {
        // 기존 코루틴 정리
        StopCoroutine("HideWarningAfterDelay");

        warningText.text = message;
        warningText.gameObject.SetActive(true);

        // CanvasGroup 준비
        CanvasGroup canvasGroup = GetOrAddCanvasGroup(warningText);

        // 초기 상태 설정
        ResetWarningMessageState(canvasGroup);

        // 애니메이션 시퀀스 생성 및 실행
        CreateWarningMessageSequence(canvasGroup, duration).Play();
    }

    /// <summary>
    /// CanvasGroup 컴포넌트 가져오기 또는 추가
    /// </summary>
    private CanvasGroup GetOrAddCanvasGroup(TextMeshProUGUI textComponent)
    {
        CanvasGroup canvasGroup = textComponent.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = textComponent.gameObject.AddComponent<CanvasGroup>();
        }
        return canvasGroup;
    }

    /// <summary>
    /// 경고 메시지 초기 상태 설정
    /// </summary>
    private void ResetWarningMessageState(CanvasGroup canvasGroup)
    {
        canvasGroup.alpha = 0f;
        warningText.transform.localScale = new Vector3(UI_SCALE_AMOUNT, UI_SCALE_AMOUNT, 1f);
    }

    /// <summary>
    /// 경고 메시지 애니메이션 시퀀스 생성
    /// </summary>
    private Sequence CreateWarningMessageSequence(CanvasGroup canvasGroup, float duration)
    {
        Sequence sequence = DOTween.Sequence();

        // 페이드 인 + 스케일 업
        sequence.Append(CreateFadeInAnimation(canvasGroup));
        sequence.Join(CreateScaleUpAnimation());

        // 표시 유지 시간
        sequence.AppendInterval(duration);

        // 페이드 아웃 + 스케일 다운
        sequence.Append(CreateFadeOutAnimation(canvasGroup));
        sequence.Join(CreateScaleDownAnimation());

        // 애니메이션 완료 처리
        sequence.OnComplete(() => ResetWarningMessageAfterAnimation(canvasGroup));

        return sequence;
    }

    /// <summary>
    /// 페이드 인 애니메이션 생성
    /// </summary>
    private Tween CreateFadeInAnimation(CanvasGroup canvasGroup)
    {
        return canvasGroup.DOFade(1f, UI_FADE_DURATION).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 스케일 업 애니메이션 생성
    /// </summary>
    private Tween CreateScaleUpAnimation()
    {
        return warningText.transform.DOScale(1f, UI_FADE_DURATION).SetEase(Ease.OutBack);
    }

    /// <summary>
    /// 페이드 아웃 애니메이션 생성
    /// </summary>
    private Tween CreateFadeOutAnimation(CanvasGroup canvasGroup)
    {
        return canvasGroup.DOFade(0f, UI_FADE_DURATION).SetEase(Ease.InQuad);
    }

    /// <summary>
    /// 스케일 다운 애니메이션 생성
    /// </summary>
    private Tween CreateScaleDownAnimation()
    {
        return warningText.transform.DOScale(0.9f, UI_FADE_DURATION).SetEase(Ease.InBack);
    }

    /// <summary>
    /// 애니메이션 완료 후 경고 메시지 상태 초기화
    /// </summary>
    private void ResetWarningMessageAfterAnimation(CanvasGroup canvasGroup)
    {
        warningText.gameObject.SetActive(false);
        canvasGroup.alpha = 1f;
        warningText.transform.localScale = Vector3.one;
    }
    #endregion

    /// <summary>
    /// 경고 메시지 숨기기
    /// </summary>
    private void HideWarningMessage()
    {
        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 일정 시간 후 경고 메시지 숨기기
    /// </summary>
    private IEnumerator HideWarningAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideWarningMessage();
    }

    #region 웨이브 관리 메소드
    /// <summary>
    /// 특정 웨이브 시작
    /// </summary>
    public void StartWave(int waveIndex)
    {
        if (!IsValidWaveIndex(waveIndex))
        {
            return;
        }

        // Debug.Log($"웨이브 {waveIndex + 1} 시작 시도...");
        ShowWaveStartMessage(waveIndex);

        // 사전 검증
        ValidateWaveStartRequirements();

        // 웨이브 상태 초기화
        InitializeWaveState(waveIndex);

        // 이벤트 및 UI 업데이트
        NotifyWaveStart();

        // 적 생성 시작
        StartEnemySpawning(waveIndex);
    }

    /// <summary>
    /// 웨이브 인덱스 유효성 검증
    /// </summary>
    private bool IsValidWaveIndex(int waveIndex)
    {
        if (waveIndex >= waves.Length)
        {
            Debug.LogError($"웨이브 시작 실패: 유효하지 않은 웨이브 인덱스 {waveIndex} (총 {waves.Length}개 웨이브)");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 웨이브 시작 메시지 표시
    /// </summary>
    private void ShowWaveStartMessage(int waveIndex)
    {
        string waveStartMessage = $"웨이브 {waveIndex + 1} 시작!";
        ShowWarningMessage(waveStartMessage, WAVE_START_MESSAGE_DURATION);
    }

    /// <summary>
    /// 웨이브 시작 요구사항 검증
    /// </summary>
    private void ValidateWaveStartRequirements()
    {
        if (defaultEnemyPrefab == null)
        {
            Debug.LogWarning("기본 적 프리팹이 설정되지 않았습니다.");
        }

        if (!AreAllSpawnPointsValid())
        {
            Debug.LogWarning("경로 검증 실패: 스폰 포인트에서 플레이어까지의 경로가 유효하지 않습니다.");
            ShowWarningMessage("경로 오류 감지! NavMesh를 확인하세요.", 4f);
        }
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

        // 적 카운트 초기화
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
        // Debug.Log($"웨이브 {waveIndex + 1} 시작됨! 적 {currentWave.enemyCount}마리 생성 예정");
        StartCoroutine(SpawnEnemies(currentWave));
    }
    #endregion

    /// <summary>
    /// 다음 웨이브 자동 시작
    /// </summary>
    public void StartNextWave()
    {
        // 각 조건을 개별적으로 체크해서 디버깅
        bool condition1 = !isWaveReady;
        bool condition2 = isWaveActive;
        bool condition3 = isWaitingForNextWave;
        bool condition4 = currentWaveIndex >= waves.Length;

        if (condition1 || condition2 || condition3 || condition4)
        {
            return;
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
            // 동시 적 수 제한 제거됨 - spawnInterval마다 적 생성

            // 스폰 포인트 선택 - 기본적으로 랜덤 스폰 사용
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

            // 적 프리팹 결정 (WaveData 우선, 없으면 기본 프리팹 사용)
            GameObject enemyPrefab = wave.enemyPrefab != null ? wave.enemyPrefab : defaultEnemyPrefab;

            if (enemyPrefab == null)
            {
                Debug.LogError($"적 프리팹이 설정되지 않았습니다! WaveData의 enemyPrefab을 설정하거나 WaveManager의 defaultEnemyPrefab을 설정하세요.");
                Debug.LogError($"WaveData: {wave.waveName}, WaveManager: {this.name}");
                yield break;
            }

            // 적 생성
            GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

            // 적 카운트 증가 (싱글톤 방식)
            currentEnemyCount++;

            // 적 생성 로그 (필요시 활성화)
            // Debug.Log($"적 생성됨: {enemy.name} (현재 살아있는 적: {currentEnemyCount})");

            // 적 속성 적용 (특별 효과)
            ApplyWaveEffectsToEnemy(enemy, wave);

            totalEnemiesSpawned++;
            spawnedCount++;
            enemiesRemainingInWave--;


            // 다음 적 생성까지 대기
            yield return new WaitForSeconds(wave.spawnInterval);
        }

        // 모든 적이 사망할 때까지 대기
        while (currentEnemyCount > 0)
        {
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// 웨이브 효과를 적에게 적용
    /// </summary>
    private void ApplyWaveEffectsToEnemy(GameObject enemy, WaveData wave)
    {
        BaseEnemy enemyComponent = enemy.GetComponent<BaseEnemy>();
        if (enemyComponent != null)
        {
            if (wave.healthMultiplier != 1f)
            {
                enemyComponent.ApplyHealthMultiplier(wave.healthMultiplier);
            }

            if (wave.speedMultiplier != 1f)
            {
                enemyComponent.ApplySpeedMultiplier(wave.speedMultiplier);
            }
        }

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
        {
            enemy.layer = enemyLayer;
        }
    }

    /// <summary>
    /// 현재 웨이브 종료 처리
    /// </summary>
    private void EndCurrentWave()
    {
        LogWaveCompletion();
        SetWaveInactive();

        NotifyWaveEnd();

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
    /// 웨이브 완료 로깅
    /// </summary>
    private void LogWaveCompletion()
    {
        Debug.Log($"🏁 웨이브 {currentWaveIndex + 1} 종료! (생성된 적: {totalEnemiesSpawned}, 남은 적: {currentEnemyCount})");
    }

    /// <summary>
    /// 웨이브 비활성화
    /// </summary>
    private void SetWaveInactive()
    {
        isWaveActive = false;
    }

    /// <summary>
    /// 웨이브 종료 이벤트 알림
    /// </summary>
    private void NotifyWaveEnd()
    {
        if (onWaveEnd != null)
        {
            onWaveEnd.Invoke();
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
        Debug.Log($"다음 웨이브 준비 시작: 웨이브 {currentWaveIndex + 1}");
        StartWaveCountdown(DEFAULT_WAVE_INTERVAL);
        string clearMessage = $"웨이브 {currentWaveIndex} 클리어! 다음 웨이브 준비 중...\n[SPACE] 즉시 시작";
        ShowWarningMessage(clearMessage, WAVE_CLEAR_MESSAGE_DURATION);
    }

    /// <summary>
    /// 게임 완료 처리
    /// </summary>
    private void HandleGameCompletion()
    {
        ShowWarningMessage("모든 웨이브 클리어! 게임 종료!", GAME_CLEAR_MESSAGE_DURATION);
        OnAllWavesComplete();
    }

    /// <summary>
    /// WaveManager 상태 완전 리셋
    /// </summary>
    public void ResetWaveManager()
    {
        // 웨이브 상태 초기화
        currentWaveIndex = 0;
        isWaveActive = false;
        isWaveReady = false;
        isWaitingForNextWave = false;
        currentEnemyCount = 0;
        totalEnemiesSpawned = 0;
        nextWaveTimer = 0f;
        enemiesRemainingInWave = 0;

        // UI 초기화
        UpdateUI();

        // 웨이브 카운트다운 초기화
        if (waves.Length > 0)
        {
            StartWaveCountdown(DEFAULT_WAVE_INTERVAL);
        }

        Debug.Log("WaveManager가 완전히 리셋되었습니다.");
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
                // 간단한 카운트 방식으로 적 수 표시
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

        // WaveTime 텍스트 업데이트
        if (waveTimeText != null)
        {
            if (isWaveActive)
            {
                // 현재 웨이브의 진행 시간 표시
                waveTimeText.text = $"웨이브 진행 중";
            }
            else if (isWaitingForNextWave)
            {
                // 타이머가 0초 이하가 되지 않도록 최대값 0으로 제한
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

        // 경고 텍스트 관리 - 웨이브 진행 중에는 숨김
        if (warningText != null && isWaveActive)
        {
            warningText.gameObject.SetActive(false);
        }
    }


    public void OnEnemyDeath(GameObject enemy)
    {
        // enemy가 null이거나 파괴되었는지 확인
        if (enemy == null)
        {
            Debug.LogError("OnEnemyDeath: enemy가 null입니다!");
            return;
        }

        // 적 카운트 감소 (싱글톤 방식)
        if (currentEnemyCount > 0)
        {
            currentEnemyCount--;

            BaseEnemy enemyComponent = enemy.GetComponent<BaseEnemy>();
            if (enemyComponent != null)
            {
                // 적 사망 시 즉시 골드 보상 지급
                int goldReward = GetGoldRewardForEnemy(enemy);
                int goldBefore = GameManager.Instance?.CurrentGold ?? 0;
                AddGoldToPlayer(goldReward);
                int goldAfter = GameManager.Instance?.CurrentGold ?? 0;

                Debug.Log($"적 사망 골드 획득: {enemy.name} → +{goldReward}G (이전: {goldBefore}G → 현재: {goldAfter}G)");
            }
            else
            {
                Debug.LogWarning($"사망한 적에 BaseEnemy 컴포넌트가 없음: {enemy.name}");
            }

            // 적 사망 시 UI 즉시 업데이트
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

        if (currentWaveIndex >= 0 && currentWaveIndex < waves.Length)
        {
            WaveData currentWave = waves[currentWaveIndex];
            if (currentWave != null)
            {
                baseGold = Mathf.RoundToInt(baseGold * currentWave.goldRewardMultiplier);
            }
        }

        return baseGold;
    }

    private int GetBaseGoldForEnemy(GameObject enemy)
    {
        string enemyName = enemy.name.ToLower();

        if (enemyName.Contains("basic"))
        {
            return 20; // 기본 적 (2배 증가)
        }
        else if (enemyName.Contains("attacking"))
        {
            return 30; // 공격형 적 (2배 증가)
        }
        else if (enemyName.Contains("zombie"))
        {
            return 40; // 좀비 등 특별한 적 (2배 증가)
        }
        else if (enemyName.Contains("boss"))
        {
            return 100; // 보스 적 (2배 증가)
        }

        return 20; // 기본 보상 (2배 증가)
    }

    private void AddGoldToPlayer(int amount)
    {
        GameManager.Instance?.AddGold(amount);
    }

    private bool IsPathValid(Transform spawnPoint, Transform player)
    {
        if (spawnPoint == null || player == null)
            return false;

        if (!NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"스폰 포인트 {spawnPoint.name}가 NavMesh 위에 있지 않습니다!");
            return false;
        }

        if (!NavMesh.SamplePosition(player.position, out NavMeshHit playerHit, 1.0f, NavMesh.AllAreas))
        {
            Debug.LogWarning("플레이어가 NavMesh 위에 있지 않습니다!");
            return false;
        }

        NavMeshPath path = new NavMeshPath();
        bool pathFound = NavMesh.CalculatePath(spawnPoint.position, player.position, NavMesh.AllAreas, path);

        if (!pathFound || path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning($"스폰 포인트 {spawnPoint.name}에서 플레이어까지의 경로를 찾을 수 없습니다!");
            Debug.LogWarning($"경로 상태: {path.status}, 코너 수: {path.corners.Length}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 모든 스폰 포인트에서 플레이어까지의 경로가 유효한지 확인
    /// </summary>
    private bool AreAllSpawnPointsValid()
    {
        // 플레이어 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("플레이어 오브젝트를 찾을 수 없습니다! Player 태그가 설정되어 있는지 확인하세요.");
            return false;
        }

        Transform playerTransform = playerObj.transform;

        // 스폰 포인트가 설정되어 있는지 확인
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("스폰 포인트가 설정되지 않았습니다! WaveManager의 Spawn Points에 스폰 포인트를 추가하세요.");
            return false;
        }

        // NavMesh가 존재하는지 기본 확인
        if (!NavMesh.SamplePosition(playerTransform.position, out NavMeshHit playerHit, 1.0f, NavMesh.AllAreas))
        {
            Debug.LogError("플레이어 위치에 NavMesh가 존재하지 않습니다!");
            Debug.LogError("Window > AI > Navigation 에서 NavMesh를 Bake했는지 확인하세요.");
            return false;
        }

        // 각 스폰 포인트에서 플레이어까지의 경로 확인
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform spawnPoint = spawnPoints[i];
            if (spawnPoint == null)
            {
                Debug.LogError($"스폰 포인트 {i}가 null입니다!");
                return false;
            }

            if (!IsPathValid(spawnPoint, playerTransform))
            {
                Debug.LogError($"스폰 포인트 {i} ({spawnPoint.name})에서 플레이어까지의 경로가 유효하지 않습니다!");
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
        return waves.Length - currentWaveIndex;
    }
}

/// <summary>
/// 웨이브 데이터 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "WaveData", menuName = "Tower Defense/Wave Data", order = 1)]
public class WaveData : ScriptableObject
{
    [Header("웨이브 기본 정보")]
    public string waveName = "웨이브";
    public string description = "웨이브 설명";

    [Header("적 생성 설정")]
    [Tooltip("적 프리팹 (설정하지 않으면 WaveManager의 기본 프리팹 사용)")]
    public GameObject enemyPrefab; // 적 프리팹 (선택적)
    public int enemyCount = 10; // 생성할 적 수
    public float spawnInterval = 1f; // 생성 간격 (초)

    [Header("적 배치 설정")]
    // useRandomSpawn과 maxEnemiesAtOnce는 제거됨 - 기본값 사용

    [Header("특별 효과")]
    public float speedMultiplier = 1f; // 적 속도 배율
    public float healthMultiplier = 1f; // 적 체력 배율
    public float goldRewardMultiplier = 1f; // 골드 보상 배율

    public float GetTotalDuration()
    {
        return enemyCount * spawnInterval;
    }

    public string GetWaveSummary()
    {
        return $"{waveName}: {enemyCount}마리, {spawnInterval}초 간격, 총 {GetTotalDuration():F1}초";
    }
}
