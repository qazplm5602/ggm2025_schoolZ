using UnityEngine;

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
