using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// 타워 생성 시스템 메인 매니저
/// 플레이어 근처 설치 위치에서 UI를 표시하고 타워 생성을 관리합니다
/// </summary>
public class TowerPlacementSystem : MonoBehaviour
{
    public static TowerPlacementSystem Instance { get; private set; }

    [Header("UI 설정")]
    [SerializeField] private GameObject placementUI; // 타워 선택 UI 패널
    [SerializeField] private Button[] towerButtons; // 타워 선택 버튼들
    [SerializeField] private TextMeshProUGUI goldText; // 골드 표시 텍스트

    [Header("타워 설정")]
    [SerializeField] private GameObject[] towerPrefabs; // 타워 프리팹들
    [SerializeField] private int[] towerCosts = { 100, 150, 200, 300 }; // 타워 가격들
    [SerializeField] private string[] towerNames = { "Melee Tower", "Ranged Tower", "Special Tower", "Ultimate Tower" }; // 타워 이름들

    [Header("시스템 설정")]
    [SerializeField] private int initialGold = 1000; // 초기 골드

    // 현재 상태
    private TowerPlacementZone activeZone; // 현재 활성화된 설치 위치
    private int currentGold; // 현재 골드
    private System.Collections.Generic.List<TowerPlacementZone> nearbyZones = new System.Collections.Generic.List<TowerPlacementZone>(); // 가까운 Zone들
    private Transform playerTransform; // 플레이어 Transform

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
            return;
        }

        // 플레이어 Transform 찾기
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }

        // 초기 골드 설정
        currentGold = initialGold;
    }

    private void Start()
    {
        // UI 초기화
        if (placementUI != null)
        {
            placementUI.SetActive(false);
        }

        // 버튼 이벤트 연결
        SetupTowerButtons();

        // 골드 표시 업데이트
        UpdateGoldDisplay();
    }

    private void Update()
    {
        // Tab 키로 UI 토글
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleUI();
        }
    }

    /// <summary>
    /// UI 토글 (Tab 키로 직접 제어)
    /// </summary>
    private void ToggleUI()
    {
        // 현재 UI가 표시되어 있는지 확인
        bool isUIShowing = placementUI != null && placementUI.activeSelf;

        if (isUIShowing)
        {
            // UI 숨김
            HideUI();
        }
        else
        {
            // 가까운 Zone들을 확인하고 UI 표시
            UpdateNearbyZones();

            if (nearbyZones.Count > 0)
            {
                ShowUI();
            }
            else
            {
                Debug.Log("가까운 설치 가능 위치가 없습니다!");
            }
        }
    }

    /// <summary>
    /// 타워 버튼들 설정
    /// </summary>
    private void SetupTowerButtons()
    {
        if (towerButtons == null || towerButtons.Length == 0) return;

        for (int i = 0; i < towerButtons.Length && i < towerPrefabs.Length; i++)
        {
            int towerIndex = i; // 클로저 문제 해결
            Button button = towerButtons[i];

            if (button != null)
            {
                button.onClick.AddListener(() => TryPlaceTower(towerIndex));

                // 버튼 텍스트 설정 (가격 표시)
                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = $"{towerNames[towerIndex]}\n{towerCosts[towerIndex]}G";
                }
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
            if (zone.IsAvailable()) // 점유되지 않은 Zone만
            {
                float distance = Vector3.Distance(zone.transform.position, playerTransform.position);
                if (distance <= 3f) // 기본 거리 범위
                {
                    nearbyZones.Add(zone);
                }
            }
        }
    }

    /// <summary>
    /// UI 표시 (가장 가까운 Zone 선택)
    /// </summary>
    private void ShowUI()
    {
        if (nearbyZones.Count == 0 || playerTransform == null) return;

        // 가장 가까운 Zone 찾기
        TowerPlacementZone closestZone = null;
        float closestDistance = float.MaxValue;

        foreach (TowerPlacementZone zone in nearbyZones)
        {
            float distance = Vector3.Distance(zone.transform.position, playerTransform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestZone = zone;
            }
        }

        if (closestZone == null) return;

        if (activeZone == closestZone) return; // 이미 활성화된 위치면 무시

        activeZone = closestZone;

        if (placementUI != null)
        {
            placementUI.SetActive(true);

            // 마우스 커서 표시 (UI가 표시될 때만)
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            Debug.Log("타워 배치 UI 표시됨");
        }
    }

    /// <summary>
    /// UI 숨김
    /// </summary>
    public void HideUI()
    {
        if (activeZone == null) return;

        activeZone = null;

        if (placementUI != null)
        {
            placementUI.SetActive(false);

            // 마우스 커서 숨김 (UI가 숨겨질 때)
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            Debug.Log("타워 배치 UI 숨겨짐");
        }
    }

    // 배치 모드 관련 메소드들은 제거됨 - UI 토글만 사용

    /// <summary>
    /// 타워 배치 시도
    /// </summary>
    private void TryPlaceTower(int towerIndex)
    {
        if (activeZone == null)
        {
            Debug.Log("유효한 설치 위치가 없습니다!");
            return;
        }

        // 타워 인덱스 검증
        if (towerIndex < 0 || towerIndex >= towerPrefabs.Length)
        {
            Debug.LogError("유효하지 않은 타워 인덱스입니다!");
            return;
        }

        // 골드 확인
        if (currentGold < towerCosts[towerIndex])
        {
            Debug.Log($"골드가 부족합니다! 필요: {towerCosts[towerIndex]}, 현재: {currentGold}");
            return;
        }

        // 타워 생성
        PlaceTower(towerIndex);
    }

    /// <summary>
    /// 실제 타워 생성
    /// </summary>
    private void PlaceTower(int towerIndex)
    {
        // 골드 차감
        currentGold -= towerCosts[towerIndex];

        // 타워 생성 (TowerPlacementZone을 부모로 설정)
        Vector3 spawnPosition = activeZone.GetTowerPosition();
        GameObject newTower = Instantiate(towerPrefabs[towerIndex], spawnPosition, Quaternion.identity, activeZone.transform);

        // 타워 이름 설정
        newTower.name = $"{towerNames[towerIndex]} (생성됨)";

        // 설치 위치 점유 표시
        activeZone.SetOccupied(true);

        // UI 업데이트
        UpdateGoldDisplay();

        Debug.Log($"{towerNames[towerIndex]} 생성 완료! 비용: {towerCosts[towerIndex]}G, 남은 골드: {currentGold}");
    }

    /// <summary>
    /// 골드 표시 업데이트
    /// </summary>
    private void UpdateGoldDisplay()
    {
        if (goldText != null)
        {
            goldText.text = $"Gold: {currentGold}";
        }
    }

    /// <summary>
    /// 골드 추가 (외부에서 호출 가능)
    /// </summary>
    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateGoldDisplay();
        Debug.Log($"골드 획득: +{amount}, 총 골드: {currentGold}");
    }

    /// <summary>
    /// 현재 골드 반환
    /// </summary>
    public int GetCurrentGold()
    {
        return currentGold;
    }

    private void OnDestroy()
    {
        // 싱글톤 정리
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
