using UnityEngine;
using UnityEditor;

/// <summary>
/// WaveData를 위한 커스텀 에디터
/// 웨이브 데이터를 쉽게 편집할 수 있게 해줍니다
/// </summary>
[CustomEditor(typeof(WaveData))]
public class WaveDataEditor : Editor
{
    private WaveData waveData;
    private bool showPreview = false;

    private void OnEnable()
    {
        waveData = (WaveData)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawWaveDataHeader();
        DrawBasicInfo();
        DrawEnemySettings();
        DrawPlacementSettings();
        DrawSpecialEffects();
        DrawPreview();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawWaveDataHeader()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("웨이브 데이터 편집기", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("웨이브의 적 생성, 배치, 특수 효과 등을 설정합니다.", MessageType.Info);
    }

    private void DrawBasicInfo()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("📝 기본 정보", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("waveName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"웨이브 요약: {waveData.GetWaveSummary()}", EditorStyles.helpBox);
    }

    private void DrawEnemySettings()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("👾 적 설정", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("enemyPrefab"));

        // 프리팹 상태 표시
        if (waveData.enemyPrefab == null)
        {
            EditorGUILayout.HelpBox("적 프리팹이 설정되지 않았습니다. WaveManager의 기본 프리팹을 사용합니다.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField($"프리팹: {waveData.enemyPrefab.name}", EditorStyles.miniLabel);
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("enemyCount"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnInterval"));

        // 경고 표시
        if (waveData.enemyCount > 50)
        {
            EditorGUILayout.HelpBox("적 수가 너무 많습니다. 성능에 영향을 줄 수 있습니다.", MessageType.Warning);
        }

        if (waveData.spawnInterval < 0.1f)
        {
            EditorGUILayout.HelpBox("생성 간격이 너무 짧습니다. 적이 겹칠 수 있습니다.", MessageType.Warning);
        }
    }

    private void DrawPlacementSettings()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("📍 배치 설정", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("적 배치 설정은 기본값을 사용합니다.\n- 스폰: 랜덤\n- 동시 적 수: 제한 없음", MessageType.Info);
    }

    private void DrawSpecialEffects()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("✨ 특수 효과", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("speedMultiplier"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("healthMultiplier"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("goldRewardMultiplier"));

        // 특수 효과 요약
        if (waveData.speedMultiplier != 1f || waveData.healthMultiplier != 1f || waveData.goldRewardMultiplier != 1f)
        {
            string effects = "적용된 효과: ";
            if (waveData.speedMultiplier != 1f) effects += $"속도 x{waveData.speedMultiplier} ";
            if (waveData.healthMultiplier != 1f) effects += $"체력 x{waveData.healthMultiplier} ";
            if (waveData.goldRewardMultiplier != 1f) effects += $"골드 x{waveData.goldRewardMultiplier} ";

            EditorGUILayout.LabelField(effects, EditorStyles.helpBox);
        }
    }

    private void DrawPreview()
    {
        EditorGUILayout.Space();
        showPreview = EditorGUILayout.Foldout(showPreview, "미리보기");

        if (showPreview)
        {
            EditorGUI.indentLevel++;

            // 웨이브 통계
            EditorGUILayout.LabelField("웨이브 통계", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"총 적 수: {waveData.enemyCount}");
            EditorGUILayout.LabelField($"총 소요 시간: {waveData.GetTotalDuration():F1}초");
            EditorGUILayout.LabelField($"초당 생성률: {1f / waveData.spawnInterval:F1}마리");

            // 예상 골드 보상
            GameObject prefabToUse = waveData.enemyPrefab;
            string prefabSource = waveData.enemyPrefab != null ? "WaveData" : "기본 프리팹";

            if (prefabToUse == null)
            {
                // WaveManager에서 기본 프리팹을 가져와야 하지만, 여기서는 추정값만 표시
                EditorGUILayout.LabelField($"프리팹: {prefabSource} 사용 예정");
                EditorGUILayout.LabelField($"예상 골드 보상: {Mathf.RoundToInt(waveData.enemyCount * 10 * waveData.goldRewardMultiplier)}G (기본값)");
            }
            else
            {
                int estimatedGold = EstimateGoldReward(prefabToUse, waveData.enemyCount, waveData.goldRewardMultiplier);
                EditorGUILayout.LabelField($"프리팹: {prefabSource} ({prefabToUse.name})");
                EditorGUILayout.LabelField($"예상 골드 보상: {estimatedGold}G");
            }

            EditorGUI.indentLevel--;
        }
    }

    private int EstimateGoldReward(GameObject enemyPrefab, int count, float multiplier)
    {
        // 프리팹 이름에 따라 골드 보상 추정
        string name = enemyPrefab.name.ToLower();
        int baseGold = 20;

        if (name.Contains("basic")) baseGold = 20;
        else if (name.Contains("attacking")) baseGold = 30;
        else if (name.Contains("zombie")) baseGold = 40;
        else if (name.Contains("boss")) baseGold = 100;

        return Mathf.RoundToInt(baseGold * count * multiplier);
    }
}
