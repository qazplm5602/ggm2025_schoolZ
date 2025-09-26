using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Tower Defense/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("기본 설정")]
    public string waveName = "Wave";
    public string description = "웨이브 설명";
    public GameObject enemyPrefab;
    public int enemyCount = 10;
    public float spawnInterval = 1f;

    [Header("적 배치 설정")]
    public bool useRandomSpawn = true;
    public int maxEnemiesAtOnce = 10;

    [Header("특별 효과")]
    public float speedMultiplier = 1f;
    public float healthMultiplier = 1f;
    public float goldRewardMultiplier = 1f;

    public float GetTotalDuration() => enemyCount * spawnInterval;
    public string GetWaveSummary() => $"{waveName}: {enemyCount}마리, {spawnInterval}초 간격, 총 {GetTotalDuration():F1}초";
}