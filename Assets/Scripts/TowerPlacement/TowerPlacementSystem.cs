using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 클릭 가능한 오브젝트 인터페이스
/// </summary>
public interface IClickable
{
    event System.Action OnClicked;
}

public enum UIMode
{
    Placement,  
    Upgrade     
}

public class TowerPlacementSystem : MonoBehaviour
{
    public static TowerPlacementSystem Instance { get; private set; }

    [SerializeField] private GameObject mainUI;

    [SerializeField] private GameObject[] actionObjects;

    [SerializeField] private TextMeshProUGUI goldText;

    [SerializeField] private GameObject[] towerPrefabs; // 타워 프리팹들

    private TowerPlacementZone activeZone;
    private System.Collections.Generic.List<TowerPlacementZone> nearbyZones = new System.Collections.Generic.List<TowerPlacementZone>(); // 가까운 Zone들
    private Transform playerTransform; 
    private BaseTower selectedTower; 
    private bool isUIAnimating = false;
    private UIMode currentMode; 
    private const float SLOW_TIME_SCALE = 0.3f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 플레이어 Transform 찾기
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }
    }

    private void Start()
    {
        if (mainUI != null)
        {
            mainUI.SetActive(false);
        }

        SetupActionObjects();

        if (GameManager.Instance != null)
        {
            UpdateGoldDisplay(GameManager.Instance.CurrentGold);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGoldChanged += UpdateGoldDisplay;
        }

        currentMode = UIMode.Placement;
    }

    public bool IsUIActive => mainUI != null && mainUI.activeSelf;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleUI();
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (mainUI != null && mainUI.activeSelf)
            {
                HideMainUI();
            }
        }
    }

    private void ToggleUI()
    {
        if (mainUI != null && mainUI.activeSelf)
        {
            HideMainUI();
        }
        else
        {
            ForceShowUI();
        }
    }

    private void ForceShowUI()
    {
        isUIAnimating = false;
        activeZone = null;
        selectedTower = null;

        if (mainUI != null)
        {
            mainUI.SetActive(false);
            CanvasGroup canvasGroup = mainUI.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
            mainUI.transform.localScale = Vector3.one;
        }

        UpdateNearbyZones();
        var occupiedZones = nearbyZones.FindAll(zone => zone.IsOccupied());

        if (occupiedZones.Count > 0)
        {
            currentMode = UIMode.Upgrade;
            ShowUpgradeUIForOccupiedZones(occupiedZones);
        }
        else if (nearbyZones.Count > 0)
        {
            var closestZone = FindClosestEmptyZone();
            if (closestZone != null)
            {
                activeZone = closestZone;
                currentMode = UIMode.Placement;
                ShowMainUI();
            }
            else
            {
                Debug.Log("주변에 타워 설치 위치가 없습니다.");
            }
        }
        else
        {
            Debug.Log("주변에 타워 설치 위치가 없습니다.");
        }
    }
    

    private TowerPlacementZone FindClosestEmptyZone()
    {
        if (nearbyZones.Count == 0 || playerTransform == null) return null;

        TowerPlacementZone closestZone = null;
        float closestDistance = float.MaxValue;

        foreach (TowerPlacementZone zone in nearbyZones)
        {
            if (!zone.IsOccupied()) // 빈 Zone만
            {
                float distance = Vector3.Distance(zone.transform.position, playerTransform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestZone = zone;
                }
            }
        }

        return closestZone;
    }

    private void SetupActionObjects()
    {
        if (actionObjects == null || actionObjects.Length == 0) return;

        for (int i = 0; i < actionObjects.Length; i++)
        {
            int objectIndex = i; // 클로저 문제 해결
            GameObject actionObj = actionObjects[i];

            if (actionObj != null)
            {
                // ClickableObject 컴포넌트가 있다면 이벤트 연결 및 초기화
                var clickableObj = actionObj.GetComponent<ClickableObject>();
                if (clickableObj != null)
                {
                    // 인덱스 설정
                    clickableObj.SetObjectIndex(objectIndex);

                    // 클릭 이벤트 연결 (OnClickedWithIndex 사용)
                    clickableObj.OnClickedWithIndex += HandleActionObject;
                }
                else
                {
                    // ClickableObject가 없으면 EventTrigger로 대체
                    SetupEventTrigger(actionObj, objectIndex);
                }
            }
        }
    }

    private void SetupEventTrigger(GameObject actionObj, int objectIndex)
    {
        var eventTrigger = actionObj.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = actionObj.AddComponent<EventTrigger>();
        }

        eventTrigger.triggers.Clear();

        var clickEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };
        clickEntry.callback.AddListener((data) => HandleActionObject(objectIndex));
        eventTrigger.triggers.Add(clickEntry);
    }

    private void HandleActionObject(int objectIndex)
    {
        if (isUIAnimating)
        {
            Debug.Log("UI 애니메이션 진행 중이므로 액션 무시");
            return;
        }

        Debug.Log($"HandleActionObject 호출: objectIndex={objectIndex}, currentMode={currentMode}");

        switch (currentMode)
        {
            case UIMode.Placement:
                Debug.Log("배치 모드로 타워 배치 시도");
                TryPlaceTower(objectIndex);
                break;
            case UIMode.Upgrade:
                Debug.Log("업그레이드 모드로 타워 업그레이드 시도");
                TryUpgradeTower(objectIndex);
                break;
            default:
                Debug.LogError($"알 수 없는 UI 모드: {currentMode}");
                break;
        }
    }



    private void ShowUpgradeUIForOccupiedZones(System.Collections.Generic.List<TowerPlacementZone> occupiedZones)
    {
        if (occupiedZones.Count == 0 || mainUI == null)
        {
            Debug.LogWarning("업그레이드 UI 표시 실패: 필수 컴포넌트가 null이거나 occupiedZones가 비어있음");
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogError("업그레이드 UI 표시 실패: playerTransform이 null입니다!");
            return;
        }

        var upgradeableTowers = new System.Collections.Generic.List<BaseTower>();
        foreach (var zone in occupiedZones)
        {
            var tower = zone.GetPlacedTower();
            if (tower != null)
            {
                Debug.Log($"Zone {zone.name}: tower={tower.name}, canUpgrade={tower.towerData?.canUpgrade ?? false}");
                if (tower.towerData?.canUpgrade == true)
                {
                    upgradeableTowers.Add(tower);
                }
            }
            else
            {
                Debug.Log($"Zone {zone.name}: 타워가 배치되지 않음");
            }
        }

        Debug.Log($"업그레이드 가능한 타워 수: {upgradeableTowers.Count}");

        if (upgradeableTowers.Count == 0)
        {
            Debug.LogWarning("업그레이드 UI 표시 실패: 업그레이드 가능한 타워가 없습니다");
            return;
        }

        BaseTower closestTower = null;
        float closestDistance = float.MaxValue;

        foreach (var tower in upgradeableTowers)
        {
            float distance = Vector3.Distance(tower.transform.position, playerTransform.position);
            Debug.Log($"타워 {tower.name}: 거리={distance:F2}");
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTower = tower;
            }
        }

        if (closestTower == null)
        {
            Debug.LogError("업그레이드 UI 표시 실패: closestTower가 null입니다");
            return;
        }

        selectedTower = closestTower;
        ShowMainUI();
    }

    private void SetSlowMotion(bool enable)
    {
        if (enable)
        {
            Time.timeScale = SLOW_TIME_SCALE;
            Time.fixedDeltaTime = 0.02f * SLOW_TIME_SCALE; 
        }
        else
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f; 
        }
    }

    private void ShowMainUI()
    {
        if (mainUI == null) return;

        // === UI 활성화 시작 - 모든 움직임 즉시 완전 정지 ===
        Debug.Log("=== UI 활성화 시작 - 모든 움직임 즉시 완전 정지 ===");
        Debug.Log("=== CRITICAL: UI 모드 진입 - CharacterController 잔여 움직임 제거 시작 ===");
        Debug.Log($"UI 활성화 전 플레이어 위치: {playerTransform?.position ?? Vector3.zero}");

        // === CRITICAL FIX: UI 활성화 전에 원래 플레이어 위치 저장 ===
        // UI를 열기 전에 플레이어의 실제 게임 플레이 위치를 저장해야 함
        if (playerTransform != null && !IsUIActive)
        {
            originalPlayerPosition = playerTransform.position;
            originalPlayerRotation = playerTransform.rotation;
            originalCameraPosition = Camera.main?.transform.position ?? Vector3.zero;
            originalCameraRotation = Camera.main?.transform.rotation ?? Quaternion.identity;
            // Debug.Log($"SAVE: UI 활성화 전 원래 플레이어 위치 저장: {originalPlayerPosition}");
            // Debug.Log($"SAVE: UI 활성화 전 원래 카메라 위치: {originalCameraPosition}");
        }

        // === 플레이어 위치 유지 ===
        // UI를 열 때 플레이어 위치를 변경하지 않음

        // 1. 먼저 위치 고정 (Time.timeScale 변경 전)
        LockPlayerAndCamera();
        SetSlowMotion(true);
        StabilizePhysics();

        // 애니메이션 진행 중 표시
        isUIAnimating = true;

        Debug.Log($"=== UI 완전 활성화됨 - 플레이어 위치: {lockedPlayerPosition}, 카메라 위치: {lockedCameraPosition} ===");
        Debug.Log($"UI 활성화 후 플레이어 위치: {playerTransform?.position ?? Vector3.zero}");
        Debug.Log("=== 모든 시스템 즉시 정지 완료 - UI 안전 모드 활성화 ===");

        // UI 내용 먼저 설정
        switch (currentMode)
        {
            case UIMode.Placement:
                ShowPlacementUI();
                break;
            case UIMode.Upgrade:
                ShowUpgradeUI();
                break;
        }

        // Dotween을 사용한 부드러운 UI 표시 애니메이션 (실제 시간 기준으로 동작 - TimeScale 무시)
        CanvasGroup canvasGroup = mainUI.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = mainUI.AddComponent<CanvasGroup>();
        }

        // 초기 상태 설정
        mainUI.SetActive(true);
        canvasGroup.alpha = 0f;
        mainUI.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        // 애니메이션 시퀀스 생성 (실제 시간 기준으로 동작 - TimeScale 무시)
        Sequence showSequence = DOTween.Sequence();
        showSequence.SetUpdate(UpdateType.Normal, true); // 실제 시간 기준으로 업데이트

        // 스케일 업 + 페이드 인 (TimeScale 무시)
        showSequence.Append(mainUI.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
        showSequence.Join(canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad));

        // 애니메이션 완료 시 플래그 해제
        showSequence.OnComplete(() => {
            isUIAnimating = false;
        });

        // UI가 표시되는 즉시 슬로우모션 적용 (애니메이션 중에도)
        SetSlowMotion(true);

        showSequence.Play();

        // 마우스 커서 표시
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }


    private void ShowPlacementUI()
    {
        if (mainUI == null || actionObjects == null) return;

        // 점유된 zone에서는 타워 생성 UI를 표시하지 않음
        if (activeZone != null && activeZone.IsOccupied())
        {
            return;
        }

        // 액션 오브젝트 텍스트들 업데이트
        UpdateActionObjectTextsForPlacement();

    }

    private void UpdateActionObjectTextsForPlacement()
    {
        for (int i = 0; i < actionObjects.Length; i++)
        {
            GameObject actionObj = actionObjects[i];
            if (actionObj != null)
            {
                if (i < towerPrefabs.Length)
                {
                    var clickableObj = actionObj.GetComponent<ClickableObject>();
                    if (clickableObj != null)
                    {
                        clickableObj.SetActive(true);

                        var towerData = towerPrefabs[i].GetComponent<BaseTower>()?.towerData;
                        Sprite towerIcon = (towerData != null) ? towerData.towerIcon : null;

                        string displayName = towerData?.towerName ?? $"Tower {i}";
                        int displayCost = towerData?.cost ?? 100;
                        string description = towerData?.description ?? "";

                        clickableObj.UpdateTowerDisplay(displayName, displayCost, description, towerIcon);
                    }
                    else
                    {
                        var towerData = towerPrefabs[i].GetComponent<BaseTower>()?.towerData;
                        string displayName = towerData?.towerName ?? $"Tower {i}";
                        int displayCost = towerData?.cost ?? 100;
                        string description = towerData?.description ?? "";

                        actionObj.SetActive(true);
                        SetActionObjectText(actionObj, $"{displayName}\n{displayCost}G");
                    }
                }
                else
                {
                    // ClickableObject를 사용해서 비활성화
                    var clickableObj = actionObj.GetComponent<ClickableObject>();
                    if (clickableObj != null)
                    {
                        clickableObj.SetActive(false);
                    }
                    else
                    {
                        actionObj.SetActive(false);
                    }
                }
            }
        }
    }

    private void ShowUpgradeUI()
    {
        Debug.Log($"ShowUpgradeUI 호출: selectedTower={selectedTower != null}, mainUI={mainUI != null}, actionObjects={actionObjects != null}");

        if (selectedTower == null || mainUI == null || actionObjects == null)
        {
            Debug.LogError("ShowUpgradeUI 실패: 필수 컴포넌트가 null입니다");
            return;
        }

        var upgradeOptions = selectedTower.GetAvailableUpgradeOptions();
        Debug.Log($"업그레이드 옵션 수: {upgradeOptions?.Length ?? 0}");

        if (upgradeOptions == null || upgradeOptions.Length == 0)
        {
            Debug.LogWarning("ShowUpgradeUI 실패: 업그레이드 옵션이 없습니다");
            return;
        }

        for (int i = 0; i < upgradeOptions.Length; i++)
        {
            var option = upgradeOptions[i];
            Debug.Log($"업그레이드 옵션 {i}: {option?.towerName ?? "null"}");
        }

        UpdateActionObjectTextsForUpgrade(upgradeOptions);

    }

    private void UpdateActionObjectTextsForUpgrade(TowerData[] upgradeOptions)
    {
        for (int i = 0; i < actionObjects.Length; i++)
        {
            GameObject actionObj = actionObjects[i];
            if (actionObj != null)
            {
                if (i < upgradeOptions.Length)
                {
                    // ClickableObject를 사용해서 활성화, 텍스트 및 이미지 설정
                    var clickableObj = actionObj.GetComponent<ClickableObject>();
                    if (clickableObj != null)
                    {
                        clickableObj.SetActive(true);

                        // 업그레이드 옵션 정보 모두 업데이트 (이름, 가격, 설명, 아이콘)
                        clickableObj.UpdateTowerDisplay(
                            upgradeOptions[i].towerName,
                            upgradeOptions[i].cost,
                            upgradeOptions[i].description,
                            upgradeOptions[i].towerIcon
                        );
                    }
                    else
                    {
                        actionObj.SetActive(true);
                        SetActionObjectText(actionObj, upgradeOptions[i].towerName);
                    }
                }
                else
                {
                    // ClickableObject를 사용해서 비활성화
                    var clickableObj = actionObj.GetComponent<ClickableObject>();
                    if (clickableObj != null)
                    {
                        clickableObj.SetActive(false);
                    }
                    else
                    {
                        actionObj.SetActive(false);
                    }
                }
            }
        }
    }

    private void SetActionObjectText(GameObject actionObj, string text)
    {
        // ClickableObject가 있다면 그걸 사용
        var clickableObj = actionObj.GetComponent<ClickableObject>();
        if (clickableObj != null)
        {
            clickableObj.UpdateDisplayText(text);
        }
        else
        {
            // ClickableObject가 없으면 직접 찾기
            TextMeshProUGUI objText = actionObj.GetComponentInChildren<TextMeshProUGUI>();
            if (objText != null)
            {
                objText.text = text;
            }
        }
    }

    /// <summary>
    /// 가까운 Zone들을 찾아서 리스트 업데이트
    /// </summary>
    private void UpdateNearbyZones()
    {
        nearbyZones.Clear();

        // 플레이어가 없으면 중단
        if (playerTransform == null) return;

        // 모든 TowerPlacementZone 찾기
        TowerPlacementZone[] allZones = FindObjectsByType<TowerPlacementZone>(FindObjectsSortMode.None);

        // 각 Zone과 플레이어의 거리 확인
        foreach (TowerPlacementZone zone in allZones)
        {
            float distance = Vector3.Distance(zone.transform.position, playerTransform.position);
            if (distance <= 1.1f)
            {
                nearbyZones.Add(zone);
            }
        }
    }
    
    private void TryUpgradeTower(int upgradeIndex)
    {
        if (selectedTower == null)
        {
            return;
        }

        var upgradeOptions = selectedTower.GetAvailableUpgradeOptions();
        if (upgradeIndex < 0 || upgradeIndex >= upgradeOptions.Length)
        {
            Debug.LogError("유효하지 않은 업그레이드 옵션 인덱스입니다!");
            return;
        }

        var upgradeOption = upgradeOptions[upgradeIndex];
        int upgradeCost = upgradeOption.cost;

        if (GameManager.Instance != null && !GameManager.Instance.SpendGold(upgradeCost))
        {
            Debug.Log($"업그레이드 실패: 골드 부족 (필요: {upgradeCost}G, 보유: {GameManager.Instance.CurrentGold}G)");
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.ShowWarningMessage($"❌ 골드 부족! {upgradeCost}G 필요", 2f);
            }
            return;
        }

        PerformTowerUpgrade(upgradeIndex, upgradeCost);
    }

    private void PerformTowerUpgrade(int upgradeIndex, int upgradeCost)
    {
        if (selectedTower == null)
        {
            Debug.LogError("업그레이드 시도: selectedTower가 null입니다!");
            return;
        }

        var upgradeOptions = selectedTower.GetAvailableUpgradeOptions();
        if (upgradeOptions == null || upgradeIndex < 0 || upgradeIndex >= upgradeOptions.Length)
        {
            Debug.LogError($"업그레이드 시도 실패: 유효하지 않은 옵션 인덱스 {upgradeIndex}");
            return;
        }

        var upgradeOption = upgradeOptions[upgradeIndex];
        string towerNameBeforeUpgrade = selectedTower.name;
        Debug.Log($"타워 업그레이드 시작: {towerNameBeforeUpgrade} -> {upgradeOption?.towerName ?? "Unknown"}");
        
        selectedTower.towerData = upgradeOption;
        selectedTower.name = $"{upgradeOption.towerName} (업그레이드됨)";
        
        selectedTower.Initialize(upgradeOption);
        Debug.Log($"타워 데이터 업그레이드 적용 완료: {upgradeOption.towerName}");

        if (GameManager.Instance != null)
        {
            UpdateGoldDisplay(GameManager.Instance.CurrentGold);
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.ShowWarningMessage($"{towerNameBeforeUpgrade} 업그레이드 성공! (-{upgradeCost}G)", 2f);
        }

        HideMainUI();

        Debug.Log($"타워 업그레이드 완료: {towerNameBeforeUpgrade} -> {upgradeOption?.towerName ?? "Unknown"}");

        if (mainUI != null && mainUI.activeSelf && !isUIAnimating)
        {
            Debug.LogWarning("경고: UI가 여전히 활성화되어 있습니다. 강제로 숨깁니다.");
            mainUI.SetActive(false);
        }

        StartCoroutine(VerifyUIHidden());
    }


    private void TryPlaceTower(int towerIndex)
    {
        if (activeZone == null)
        {
            return;
        }

        if (towerIndex < 0 || towerIndex >= towerPrefabs.Length)
        {
            Debug.LogError("유효하지 않은 타워 인덱스입니다!");
            return;
        }

        var towerData = towerPrefabs[towerIndex].GetComponent<BaseTower>()?.towerData;
        int towerCost = towerData?.cost ?? 100; // fallback

        if (GameManager.Instance != null && !GameManager.Instance.SpendGold(towerCost))
        {
            return;
        }

        if (activeZone.IsOccupied())
        {
            Debug.LogWarning("이 위치에는 이미 타워가 설치되어 있습니다!");
            return;
        }

        PlaceTower(towerIndex, towerCost);
    }
    private void PlaceTower(int towerIndex, int towerCost)
    {
        Vector3 spawnPosition = activeZone.GetTowerPosition();

        GameObject newTower = Instantiate(towerPrefabs[towerIndex], spawnPosition, Quaternion.identity, activeZone.transform);

        var towerData = towerPrefabs[towerIndex].GetComponent<BaseTower>()?.towerData;
        string towerName = towerData?.towerName ?? $"Tower {towerIndex}";
        newTower.name = $"{towerName} (생성됨)";

        activeZone.SetOccupied(true);

        if (GameManager.Instance != null)
        {
            UpdateGoldDisplay(GameManager.Instance.CurrentGold);
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.ShowWarningMessage($"{towerName} 생성 성공!", 2f);
        }

        HideMainUI();

        if (mainUI != null && mainUI.activeSelf)
        {
            Debug.LogWarning("경고: 타워 생성 후 UI가 여전히 활성화되어 있습니다. 강제로 숨깁니다.");
            mainUI.SetActive(false);
        }

        StartCoroutine(VerifyUIHidden());
    }

    private void UpdateGoldDisplay(int newGoldAmount)
    {
        if (goldText != null)
        {
            goldText.text = $"Gold: {newGoldAmount}";
        }
    }


    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
        }

        // 시간 배율 원래대로 복원
        if (Time.timeScale != 1f) SetSlowMotion(false);

        // 싱글톤 정리
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private IEnumerator VerifyUIHidden()
    {
        yield return new WaitForSeconds(0.3f);

        if (mainUI != null && mainUI.activeSelf)
        {
            Debug.LogWarning("UI가 여전히 활성화되어 있어 강제로 숨깁니다.");
            // 즉시 숨김 (애니메이션 없이)
            CanvasGroup canvasGroup = mainUI.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
            mainUI.transform.localScale = Vector3.one;
            mainUI.SetActive(false);
        }
        else
        {
            Debug.Log("UI 숨김 검증 완료: 정상적으로 숨겨져 있습니다.");
        }
    }
    
    public void HideMainUI()
    {
        // UI 상태 초기화
        activeZone = null;
        selectedTower = null;

        // 애니메이션 진행 중 표시
        isUIAnimating = true;

        // === UI 비활성화 시작 - 모든 시스템 안전 복원 ===
        Debug.Log("=== UI 비활성화 시작 - 모든 시스템 안전 복원 ===");
        Debug.Log($"UI 비활성화 전 플레이어 위치: {playerTransform?.position ?? Vector3.zero}");

        // 1. 시간 복원
        Debug.Log("Step 1: Time.timeScale 복원 시작");
        SetSlowMotion(false);
        Debug.Log("Step 1: Time.timeScale 복원 완료");

        // 2. 물리 상태 안정화 생략 (UI 종료 시 불필요)

        // 3. 플레이어와 카메라 위치 복원은 애니메이션 완료 후에 실행
        // (UI가 완전히 사라진 후에 복원하여 위치가 다시 변경되지 않도록)

        Debug.Log($"=== UI 완전 비활성화됨 - 플레이어 위치: {lockedPlayerPosition}, 카메라 위치: {lockedCameraPosition} ===");
        Debug.Log($"UI 비활성화 후 플레이어 위치: {playerTransform?.position ?? Vector3.zero}");
        Debug.Log("=== 모든 시스템 즉시 복원 완료 - UI 안전 모드 해제 ===");

        if (mainUI != null)
        {
            // Dotween을 사용한 부드러운 UI 숨김 애니메이션 (실제 시간 기준으로 동작 - TimeScale 무시)
            CanvasGroup canvasGroup = mainUI.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = mainUI.AddComponent<CanvasGroup>();
            }

            // 애니메이션 시퀀스 생성 (실제 시간 기준으로 동작 - TimeScale 무시)
            Sequence hideSequence = DOTween.Sequence();
            hideSequence.SetUpdate(UpdateType.Normal, true); // 실제 시간 기준으로 업데이트

            // 스케일 다운 + 페이드 아웃 (TimeScale 무시)
            hideSequence.Append(mainUI.transform.DOScale(0.8f, 0.2f).SetEase(Ease.InBack));
            hideSequence.Join(canvasGroup.DOFade(0f, 0.2f).SetEase(Ease.InQuad));

            // 애니메이션 완료 후 비활성화 및 플레이어 복원
            hideSequence.OnComplete(() => {
                mainUI.SetActive(false);
                mainUI.transform.localScale = Vector3.one;
                canvasGroup.alpha = 1f;
                isUIAnimating = false; // 애니메이션 완료

                Debug.Log("=== UI 애니메이션 완전 완료 - 플레이어 위치 복원 시작 ===");
                RestorePlayerAndCamera();
                Debug.Log("=== UI 애니메이션 완전 완료 - 플레이어 이동 제한 해제 ===");
            });

            hideSequence.Play();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Debug.LogError("HideMainUI 실패: mainUI가 null입니다!");
        }
    }
    private Vector3 lockedPlayerPosition;
    private Quaternion lockedPlayerRotation;
    private Vector3 lockedCameraPosition;
    private Quaternion lockedCameraRotation;

    private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;

    private void StabilizePhysics()
    {
        Debug.Log("=== UI 활성화 - 원자적 물리 완전 정지 시작 ===");

        // === 모든 업데이트를 일시적으로 중지하여 충돌 방지 ===
        isUIAnimating = true; // 즉시 애니메이션 플래그 설정

        try
        {
            // 1. 플레이어 물리 즉시 완전 정지 (원자적)
            if (playerTransform != null)
            {
                Debug.Log($"플레이어 원자적 물리 정지 시작 - 위치: {playerTransform.position}");

                // === CharacterController 잔여 움직임 완전 제거 ===
                CharacterController controller = playerTransform.GetComponent<CharacterController>();
                if (controller != null)
                {
                    // 현재 위치 저장
                    Vector3 currentPos = playerTransform.position;
                    Quaternion currentRot = playerTransform.rotation;

                    // 1. CharacterController 완전 비활성화
                    controller.enabled = false;

                    // 2. Transform 위치 완전 고정
                    playerTransform.position = currentPos;
                    playerTransform.rotation = currentRot;

                    // 3. CharacterController 재활성화로 내부 상태 완전 초기화
                    controller.enabled = true;

                    // 4. 잔여 움직임 완전 제거 (여러 번 호출로 확실히 제거)
                    controller.Move(Vector3.zero);
                    controller.Move(Vector3.zero);
                    controller.Move(Vector3.zero);

                    Debug.Log($"CharacterController 잔여 움직임 완전 제거 완료 - 위치: {playerTransform.position}");
                }

                // Rigidbody 즉시 완전 정지
                Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true; // 즉시 완전 정지
                    Debug.Log($"Rigidbody 원자적 정지 완료 - 위치: {playerTransform.position}");
                } else {
                    Debug.Log("플레이어에 Rigidbody가 없습니다.");
                }
            }

            // 2. 모든 테이블 즉시 완전 정지 (원자적)
            TableCore[] allTables = FindObjectsByType<TableCore>(FindObjectsSortMode.None);
            Debug.Log($"총 {allTables.Length}개의 테이블 원자적 정지 시작");

            foreach (var table in allTables)
            {
                if (table != null)
                {
                    Rigidbody tableRb = table.GetComponent<Rigidbody>();
                    if (tableRb != null)
                    {
                        tableRb.linearVelocity = Vector3.zero;
                        tableRb.angularVelocity = Vector3.zero;
                        tableRb.isKinematic = true; // 즉시 완전 정지
                        Debug.Log($"테이블 '{table.name}' 원자적 정지 완료");
                    } else {
                        Debug.Log($"테이블 '{table.name}'에 Rigidbody가 없습니다.");
                    }
                }
            }

            // 3. 모든 Rigidbody 즉시 완전 정지 (원자적)
            Rigidbody[] allRigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            foreach (var rb in allRigidbodies)
            {
                if (rb != null && rb.gameObject != playerTransform?.gameObject)
                {
                    // kinematic으로 설정하기 전에 velocity를 0으로 설정 (안전하게)
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true; // 그 다음 kinematic으로 설정
                    // kinematic 상태에서는 velocity를 다시 설정하지 않음
                }
            }

            Debug.Log($"=== UI 활성화 - 원자적 물리 완전 정지 완료 - 총 Rigidbody: {allRigidbodies.Length} ===");
        }
        finally
        {
            // === 오류 발생 시에도 상태 복원 ===
            isUIAnimating = false;
        }
    }


    private void LockPlayerAndCamera()
    {
        Debug.Log("=== 위치 고정 시작 ===");

        if (playerTransform != null)
        {
            Debug.Log($"LOCK: LockPlayerAndCamera 시작 전 플레이어 위치: {playerTransform.position}");

            // === CRITICAL FIX: UI 활성화 시점의 실제 플레이어 위치 저장 ===
            // 이전에 이동된 위치가 아닌, 실제 게임 플레이 중이던 위치를 저장해야 함
            lockedPlayerPosition = playerTransform.position;
            lockedPlayerRotation = playerTransform.rotation;

            Debug.Log($"LOCK: UI 활성화 시점 플레이어 실제 위치 저장: {lockedPlayerPosition}");

            // === CharacterController와 Transform 동기화 ===
            // PlayerMovement 컴포넌트에서 동기화 처리 (책임 분리)
            PlayerMovement playerMovement = playerTransform.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.ForceSyncCharacterController();
                Debug.Log("PlayerMovement를 통한 CharacterController 동기화 완료");
            }
            else
            {
                Debug.LogWarning("PlayerMovement 컴포넌트를 찾을 수 없어 동기화 건너뜀");
            }

            // Rigidbody 강제 정지
            Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.isKinematic = false;
                Debug.Log($"Rigidbody 물리 즉시 정지 완료 - 위치: {lockedPlayerPosition}");
            }
        }

        // 메인 카메라 즉시 고정
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            lockedCameraPosition = mainCamera.transform.position;
            lockedCameraRotation = mainCamera.transform.rotation;
            Debug.Log($"카메라 위치 즉시 고정: {lockedCameraPosition}");
        }

        Debug.Log("=== 위치 고정 완료 ===");
    }

    private void RestorePlayerAndCamera()
    {
        Debug.Log("=== 위치 복원 시작 ===");

        if (playerTransform != null)
        {
            // === CRITICAL FIX: UI 활성화 전 원래 게임 플레이 위치로 복원 ===
            // lockedPlayerPosition이 아닌 originalPlayerPosition 사용
            playerTransform.position = originalPlayerPosition;
            playerTransform.rotation = originalPlayerRotation;

            // CharacterController 강제 동기화
            CharacterController controller = playerTransform.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                controller.enabled = true;
                Debug.Log($"CharacterController 위치 즉시 복원: {originalPlayerPosition} (원래 게임 플레이 위치)");
            }

            // Rigidbody 상태 안정화
            Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                Debug.Log($"Rigidbody 위치 즉시 복원: {originalPlayerPosition} (원래 게임 플레이 위치)");
            }
        }

        // 메인 카메라 즉시 복원 (원래 게임 플레이 카메라 위치)
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.transform.position = originalCameraPosition;
            mainCamera.transform.rotation = originalCameraRotation;
            Debug.Log($"카메라 위치 즉시 복원: {originalCameraPosition} (원래 게임 플레이 카메라 위치)");
        }

        Debug.Log("=== 위치 복원 완료 ===");
    }
}

