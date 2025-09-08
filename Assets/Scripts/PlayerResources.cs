using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어 리소스 관리 시스템
/// </summary>
public class PlayerResources : MonoBehaviour
{
    [Header("초기 리소스 설정")]
    [SerializeField] private int initialGold = 1000;

    [Header("현재 리소스")]
    [SerializeField] private int currentGold;

    public int CurrentGold => currentGold;

    private void Awake()
    {
        currentGold = initialGold;
        UpdateUI();
    }

    /// <summary>
    /// 골드 소비 가능 여부 확인
    /// </summary>
    public bool CanAfford(int cost)
    {
        return currentGold >= cost;
    }

    /// <summary>
    /// 골드 소비
    /// </summary>
    public bool SpendGold(int cost)
    {
        if (!CanAfford(cost)) return false;

        currentGold -= cost;
        UpdateUI();
        return true;
    }

    /// <summary>
    /// 골드 획득
    /// </summary>
    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateUI();
    }

    /// <summary>
    /// UI 업데이트 (필요시 구현)
    /// </summary>
    private void UpdateUI()
    {
        // UI 시스템이 연결되면 구현
        Debug.Log($"현재 골드: {currentGold}");
    }
}

