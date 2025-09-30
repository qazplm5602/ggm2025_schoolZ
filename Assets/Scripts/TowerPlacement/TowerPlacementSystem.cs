using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

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

    [SerializeField] private GameObject[] towerPrefabs;

    private TowerPlacementZone activeZone;
    private List<TowerPlacementZone> nearbyZones = new List<TowerPlacementZone>();
    private Transform playerTransform; 
    private BaseTower selectedTower; 
    private bool isUIAnimating;
    private UIMode currentMode; 
    
    private Vector3 originalPlayerPosition, originalCameraPosition;
    private Quaternion originalPlayerRotation, originalCameraRotation;
    private Vector3 lockedPlayerPosition, lockedCameraPosition;
    private Quaternion lockedPlayerRotation, lockedCameraRotation;
    
    private const float SLOW_TIME_SCALE = 0.3f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Start()
    {
        mainUI?.SetActive(false);
        SetupActionObjects();
        if (GameManager.Instance != null)
        {
            UpdateGoldDisplay(GameManager.Instance.CurrentGold);
            GameManager.Instance.OnGoldChanged += UpdateGoldDisplay;
        }
        currentMode = UIMode.Placement;
    }

    public bool IsUIActive => mainUI != null && mainUI.activeSelf;

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.tabKey.wasPressedThisFrame)
            ToggleUI();

        if (Keyboard.current.escapeKey.wasPressedThisFrame && IsUIActive)
            HideMainUI();
    }

    private void ToggleUI()
    {
        if (IsUIActive)
            HideMainUI();
        else
            ForceShowUI();
    }

    private void ForceShowUI()
    {
        ResetUIState();
        UpdateNearbyZones();
        
        var occupiedZones = nearbyZones.Where(zone => zone.IsOccupied()).ToList();
        
        if (occupiedZones.Any())
        {
            currentMode = UIMode.Upgrade;
            ShowUpgradeUIForOccupiedZones(occupiedZones);
        }
        else if (FindClosestEmptyZone() is TowerPlacementZone closestZone)
        {
            activeZone = closestZone;
            currentMode = UIMode.Placement;
            ShowMainUI();
        }
    }

    private void ResetUIState()
    {
        isUIAnimating = false;
        activeZone = null;
        selectedTower = null;
        mainUI?.SetActive(false);
        
        var canvasGroup = mainUI?.GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (mainUI != null) mainUI.transform.localScale = Vector3.one;
    }
    

    private TowerPlacementZone FindClosestEmptyZone()
    {
        return nearbyZones
            .Where(zone => !zone.IsOccupied() && playerTransform != null)
            .OrderBy(zone => Vector3.Distance(zone.transform.position, playerTransform.position))
            .FirstOrDefault();
    }

    private void SetupActionObjects()
    {
        for (int i = 0; i < (actionObjects?.Length ?? 0); i++)
        {
            var actionObj = actionObjects[i];
            if (actionObj?.GetComponent<ClickableObject>() is ClickableObject clickable)
            {
                clickable.SetObjectIndex(i);
                clickable.OnClickedWithIndex += HandleActionObject;
            }
            else if (actionObj != null)
            {
                SetupEventTrigger(actionObj, i);
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
        if (isUIAnimating) return;

        switch (currentMode)
        {
            case UIMode.Placement:
                TryPlaceTower(objectIndex);
                break;
            case UIMode.Upgrade:
                TryUpgradeTower(objectIndex);
                break;
        }
    }



    private void ShowUpgradeUIForOccupiedZones(List<TowerPlacementZone> occupiedZones)
    {
        var upgradeableTowers = occupiedZones
            .Select(zone => zone.GetPlacedTower())
            .Where(tower => tower?.towerData?.canUpgrade == true)
            .ToList();

        if (!upgradeableTowers.Any() || playerTransform == null) return;

        selectedTower = upgradeableTowers
            .OrderBy(tower => Vector3.Distance(tower.transform.position, playerTransform.position))
            .First();

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

        SaveOriginalPositions();
        InitializePhysics();
        SetUIContent();
        PlayShowAnimation();
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void SaveOriginalPositions()
    {
        if (playerTransform != null && !IsUIActive)
        {
            (originalPlayerPosition, originalPlayerRotation) = (playerTransform.position, playerTransform.rotation);
            (originalCameraPosition, originalCameraRotation) = Camera.main != null ? 
                (Camera.main.transform.position, Camera.main.transform.rotation) : (Vector3.zero, Quaternion.identity);
        }
    }

    private void InitializePhysics()
    {
        LockPlayerAndCamera();
        SetSlowMotion(true);
        StabilizePhysics();
        isUIAnimating = true;
    }

    private void PlayShowAnimation()
    {
        var canvasGroup = mainUI.GetComponent<CanvasGroup>() ?? mainUI.AddComponent<CanvasGroup>();
        
        mainUI.SetActive(true);
        (canvasGroup.alpha, mainUI.transform.localScale) = (0f, new Vector3(0.8f, 0.8f, 1f));

        DOTween.Sequence()
            .SetUpdate(UpdateType.Normal, true)
            .Append(mainUI.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack))
            .Join(canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad))
            .OnComplete(() => isUIAnimating = false)
            .Play();
    }


    private void SetUIContent()
    {
        if (currentMode == UIMode.Placement) ShowPlacementUI(); 
        else ShowUpgradeUI();
    }

    private void ShowPlacementUI()
    {
        if (mainUI == null || actionObjects == null || activeZone?.IsOccupied() == true) return;
        UpdateActionObjects(null);
    }

    private void ShowUpgradeUI()
    {
        var upgradeOptions = selectedTower?.GetAvailableUpgradeOptions();
        if (upgradeOptions?.Length > 0) UpdateActionObjects(upgradeOptions);
    }

    private void UpdateActionObjects(TowerData[] upgradeOptions = null)
    {
        var isUpgradeMode = upgradeOptions != null;
        var dataSource = isUpgradeMode ? upgradeOptions : towerPrefabs?.Select(p => p.GetComponent<BaseTower>()?.towerData).ToArray();
        
        for (int i = 0; i < (actionObjects?.Length ?? 0); i++)
        {
            var actionObj = actionObjects[i];
            if (actionObj == null) continue;

            var towerData = i < (dataSource?.Length ?? 0) ? dataSource[i] : null;
            var clickable = actionObj.GetComponent<ClickableObject>();
            
            if (towerData != null)
            {
                SetTowerDisplay(clickable, actionObj, towerData, true);
            }
            else
            {
                SetActiveState(clickable, actionObj, false);
            }
        }
    }

    private void SetTowerDisplay(ClickableObject clickable, GameObject actionObj, TowerData towerData, bool active)
    {
        if (clickable != null)
        {
            clickable.SetActive(active);
            if (active) clickable.UpdateTowerDisplay(towerData.towerName, towerData.cost, towerData.description, towerData.towerIcon);
        }
        else
        {
            actionObj.SetActive(active);
            if (active) SetActionObjectText(actionObj, $"{towerData.towerName}\n{towerData.cost}G");
        }
    }

    private void SetActionObjectText(GameObject actionObj, string text)
    {
        var clickable = actionObj.GetComponent<ClickableObject>();
        if (clickable != null) clickable.UpdateDisplayText(text);
        else actionObj.GetComponentInChildren<TextMeshProUGUI>()?.SetText(text);
    }

    private void SetActiveState(ClickableObject clickable, GameObject actionObj, bool active)
    {
        if (clickable != null) clickable.SetActive(active);
        else actionObj.SetActive(active);
    }

    private void UpdateNearbyZones()
    {
        nearbyZones.Clear();
        if (playerTransform == null) return;
        
        nearbyZones.AddRange(
            FindObjectsByType<TowerPlacementZone>(FindObjectsSortMode.None)
            .Where(zone => Vector3.Distance(zone.transform.position, playerTransform.position) <= 1.1f)
        );
    }
    
    private void TryUpgradeTower(int upgradeIndex)
    {
        var upgradeOptions = selectedTower?.GetAvailableUpgradeOptions();
        if (upgradeOptions == null || upgradeIndex < 0 || upgradeIndex >= upgradeOptions.Length) return;

        var option = upgradeOptions[upgradeIndex];
        if (GameManager.Instance.SpendGold(option.cost))
        {
            PerformTowerUpgrade(option);
        }
        else
        {
            WaveManager.Instance?.ShowWarningMessage($"❌ 골드 부족! {option.cost}G 필요", 2f);
        }
    }

    private void PerformTowerUpgrade(TowerData upgradeOption)
    {
        string oldName = selectedTower.name;
        selectedTower.towerData = upgradeOption;
        selectedTower.name = $"{upgradeOption.towerName} (업그레이드됨)";
        selectedTower.Initialize(upgradeOption);

        UpdateGoldDisplay(GameManager.Instance.CurrentGold);
        WaveManager.Instance?.ShowWarningMessage($"{oldName} 업그레이드 성공!", 2f);
        
        HideMainUI();
        StartCoroutine(VerifyUIHidden());
    }


    private void TryPlaceTower(int towerIndex)
    {
        if (activeZone?.IsOccupied() == true || towerIndex < 0 || towerIndex >= (towerPrefabs?.Length ?? 0)) return;

        var towerData = towerPrefabs[towerIndex].GetComponent<BaseTower>()?.towerData;
        if (GameManager.Instance.SpendGold(towerData?.cost ?? 100))
        {
            PlaceTower(towerIndex, towerData);
        }
    }

    private void PlaceTower(int towerIndex, TowerData towerData)
    {
        var newTower = Instantiate(towerPrefabs[towerIndex], activeZone.GetTowerPosition(), Quaternion.identity, activeZone.transform);
        newTower.name = $"{towerData?.towerName ?? "Tower"} (생성됨)";
        
        activeZone.SetOccupied(true);
        UpdateGoldDisplay(GameManager.Instance.CurrentGold);
        WaveManager.Instance?.ShowWarningMessage($"{towerData?.towerName} 생성 성공!", 2f);
        
        HideMainUI();
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
        }

        if (Time.timeScale != 1f) SetSlowMotion(false);

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
        activeZone = null;
        selectedTower = null;

        isUIAnimating = true;

        Debug.Log("Step 1: Time.timeScale 복원 시작");
        SetSlowMotion(false);
        Debug.Log("Step 1: Time.timeScale 복원 완료");

        if (mainUI != null)
        {
            CanvasGroup canvasGroup = mainUI.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = mainUI.AddComponent<CanvasGroup>();
            }

            Sequence hideSequence = DOTween.Sequence();
            hideSequence.SetUpdate(UpdateType.Normal, true);

            hideSequence.Append(mainUI.transform.DOScale(0.8f, 0.2f).SetEase(Ease.InBack));
            hideSequence.Join(canvasGroup.DOFade(0f, 0.2f).SetEase(Ease.InQuad));

            hideSequence.OnComplete(() => {
                mainUI.SetActive(false);
                mainUI.transform.localScale = Vector3.one;
                canvasGroup.alpha = 1f;
                isUIAnimating = false;
                RestorePlayerAndCamera();
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


    private void StabilizePhysics()
    {
        isUIAnimating = true;

        try
        {
            if (playerTransform != null)
            {
                CharacterController controller = playerTransform.GetComponent<CharacterController>();
                if (controller != null)
                {
                    Vector3 currentPos = playerTransform.position;
                    Quaternion currentRot = playerTransform.rotation;

                    controller.enabled = false;

                    playerTransform.position = currentPos;
                    playerTransform.rotation = currentRot;

                    controller.enabled = true;

                    controller.Move(Vector3.zero);
                    controller.Move(Vector3.zero);
                    controller.Move(Vector3.zero);
                }

                Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
            }

            TableCore[] allTables = FindObjectsByType<TableCore>(FindObjectsSortMode.None);

            foreach (var table in allTables)
            {
                if (table != null)
                {
                    Rigidbody tableRb = table.GetComponent<Rigidbody>();
                    if (tableRb != null)
                    {
                        tableRb.linearVelocity = Vector3.zero;
                        tableRb.angularVelocity = Vector3.zero;
                        tableRb.isKinematic = true;
                    }
                }
            }

            Rigidbody[] allRigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            foreach (var rb in allRigidbodies)
            {
                if (rb != null && rb.gameObject != playerTransform?.gameObject)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
            }
        }
        finally
        {
            isUIAnimating = false;
        }
    }
    private void LockPlayerAndCamera()
    {
        if (playerTransform != null)
        {
            lockedPlayerPosition = playerTransform.position;
            lockedPlayerRotation = playerTransform.rotation;

            PlayerMovement playerMovement = playerTransform.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.ForceSyncCharacterController();
            }

            Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.isKinematic = false;
            }
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            lockedCameraPosition = mainCamera.transform.position;
            lockedCameraRotation = mainCamera.transform.rotation;
        }
    }
    private void RestorePlayerAndCamera()
    {
        if (playerTransform != null)
        {
            playerTransform.position = originalPlayerPosition;
            playerTransform.rotation = originalPlayerRotation;

            CharacterController controller = playerTransform.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                controller.enabled = true;
            }

            Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.transform.position = originalCameraPosition;
            mainCamera.transform.rotation = originalCameraRotation;
        }
    }
}

