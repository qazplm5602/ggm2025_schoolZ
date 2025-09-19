using UnityEngine;
using UnityEditor;

/// <summary>
/// WaveManager를 위한 심플 에디터
/// </summary>
[CustomEditor(typeof(WaveManager))]
public class WaveManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 기본 인스펙터 표시
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("빠른 설정", EditorStyles.boldLabel);

        if (GUILayout.Button("웨이브 추가"))
        {
            AddSimpleWave();
        }

        if (GUILayout.Button("10라운드 웨이브 생성"))
        {
            Create10WaveDatas();
        }

        if (GUILayout.Button("20웨이브 세팅"))
        {
            LoadExistingWaves();
        }

        if (GUILayout.Button("20웨이브 밸런스 업데이트"))
        {
            UpdateAllWaveBalances();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void AddSimpleWave()
    {
        SerializedProperty wavesProperty = serializedObject.FindProperty("waves");

        // 현재 웨이브 수를 확인해서 다음 웨이브 번호 계산
        int nextWaveIndex = wavesProperty.arraySize + 1;
        string waveName = $"Wave_{nextWaveIndex}";

        // SO/Waves 폴더 확인 및 생성
        string folderPath = "Assets/SO/Waves";
        if (!UnityEditor.AssetDatabase.IsValidFolder(folderPath))
        {
            UnityEditor.AssetDatabase.CreateFolder("Assets/SO", "Waves");
        }

        // 새로운 WaveData 생성 및 난이도 자동 설정
        WaveData newWaveData = UnityEngine.ScriptableObject.CreateInstance<WaveData>();
        newWaveData.waveName = $"Wave {nextWaveIndex}";
        newWaveData.description = $"{nextWaveIndex}번째 웨이브 - 난이도: {GetDifficultyLevel(nextWaveIndex - 1)}";

        // 난이도에 따른 자동 설정
        ConfigureWaveByDifficulty(newWaveData, nextWaveIndex);

        // 파일로 저장
        string assetPath = $"{folderPath}/{waveName}.asset";
        UnityEditor.AssetDatabase.CreateAsset(newWaveData, assetPath);

        // 웨이브 배열 크기 증가 및 새로운 웨이브 등록
        wavesProperty.arraySize++;
        wavesProperty.GetArrayElementAtIndex(wavesProperty.arraySize - 1).objectReferenceValue = newWaveData;

        // 에셋 데이터베이스 갱신
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        UnityEditor.EditorUtility.DisplayDialog("웨이브 추가 완료",
            $"{waveName}.asset 파일이 생성되고 등록되었습니다!\n" +
            $"난이도: {GetDifficultyLevel(nextWaveIndex - 1)}\n" +
            $"적 수: {newWaveData.enemyCount}마리\n" +
            $"Assets/SO/Waves 폴더를 확인하세요.", "확인");
    }

    private void ConfigureWaveByDifficulty(WaveData waveData, int waveIndex)
    {
        // 난이도에 따른 자동 설정 (기하급수적 증가)
        if (waveIndex <= 3) // Wave 1-3: 쉬움
        {
            int relativeIndex = waveIndex - 1; // 0, 1, 2
            waveData.enemyCount = Mathf.RoundToInt(5 * Mathf.Pow(1.5f, relativeIndex)); // 5, 8, 11 (약 1.5배씩 증가)
            waveData.spawnInterval = Mathf.Max(0.5f, 1.0f - relativeIndex * 0.15f); // 1.0, 0.85, 0.7
            // maxEnemiesAtOnce 제거됨 - 제한 없음
            waveData.speedMultiplier = 1.0f + relativeIndex * 0.15f; // 1.0, 1.15, 1.30 (약 15%씩 증가)
            waveData.healthMultiplier = 1.0f * Mathf.Pow(1.5f, relativeIndex); // 1.0, 1.5, 2.25 (1.5배씩 증가)
            waveData.goldRewardMultiplier = 1.0f + relativeIndex * 0.35f; // 1.0, 1.35, 1.7 (약 35%씩 증가)
        }
        else if (waveIndex <= 6) // Wave 4-6: 보통
        {
            int relativeIndex = waveIndex - 4; // 0, 1, 2 (Wave 4, 5, 6)
            waveData.enemyCount = Mathf.RoundToInt(20 * Mathf.Pow(1.2f, relativeIndex)); // 15, 20, 26 (약 1.3배씩 증가)
            waveData.spawnInterval = Mathf.Max(0.4f, 0.85f - relativeIndex * 0.15f); // 0.85, 0.7, 0.55
            // maxEnemiesAtOnce 제거됨 - 제한 없음
            waveData.speedMultiplier = 1.35f + relativeIndex * 0.12f; // 1.15, 1.27, 1.39 (약 12%씩 증가)
            waveData.healthMultiplier = 4f * Mathf.Pow(1.3f, relativeIndex); // 2.25, 3.375, 5.0625 (1.5배씩 증가)
            waveData.goldRewardMultiplier = 1.6f + relativeIndex * 0.35f; // 1.3, 1.65, 2.0 (약 35%씩 증가)
        }
        else if (waveIndex <= 10) // Wave 7-10: 어려움
        {
            int relativeIndex = waveIndex - 7; // 0, 1, 2, 3 (Wave 7, 8, 9, 10)
            waveData.enemyCount = Mathf.RoundToInt(26 * Mathf.Pow(1.1f, relativeIndex)); // 26, 36, 51, 71 (약 1.4배씩 증가)
            waveData.spawnInterval = Mathf.Max(0.3f, 0.7f - relativeIndex * 0.1f); // 0.7, 0.6, 0.5, 0.4
            // maxEnemiesAtOnce 제거됨 - 제한 없음
            waveData.speedMultiplier = 1.5f + relativeIndex * 0.19f; // 1.3, 1.45, 1.6, 1.75 (약 15%씩 증가)
            waveData.healthMultiplier = 7f * Mathf.Pow(1.2f, relativeIndex); // 5.0625, 7.59375, 11.390625, 17.0859375 (1.5배씩 증가)
            waveData.goldRewardMultiplier = 1.8f + relativeIndex * 0.4f; // 1.6, 2.0, 2.4, 2.8 (약 40%씩 증가)
        }
        else // Wave 11+: 최고 난이도
        {
            int relativeIndex = waveIndex - 11; // 0, 1, 2, ... (Wave 11, 12, 13, ...)
            waveData.enemyCount = Mathf.RoundToInt(40 * Mathf.Pow(1.1f, relativeIndex)); // 40, 60, 90, 135, ... (약 1.5배씩 증가)
            waveData.spawnInterval = Mathf.Max(0.01f, 0.55f - relativeIndex * 0.08f); // 0.55, 0.47, 0.39, ...
            // maxEnemiesAtOnce 제거됨 - 제한 없음
            waveData.speedMultiplier = 1.95f + relativeIndex * 0.38f; // 1.45, 1.63, 1.81, ... (약 18%씩 증가)
            waveData.healthMultiplier = 10.0f * Mathf.Pow(1.1f, relativeIndex); // 17., 25.62890625, 38.443359375, ... (1.5배씩 증가)
            waveData.goldRewardMultiplier = 2.1f + relativeIndex * 0.5f; // 1.9, 2.4, 2.9, ... (약 50%씩 증가)
        }

        // 공통 설정
        // useRandomSpawn 제거됨 - 기본적으로 랜덤 스폰 사용
        waveData.enemyPrefab = null; // WaveManager의 기본 프리팹 사용
    }

    private void Create10WaveDatas()
    {
        // SO/Waves 폴더 확인 및 생성
        string folderPath = "Assets/SO/Waves";
        if (!UnityEditor.AssetDatabase.IsValidFolder(folderPath))
        {
            UnityEditor.AssetDatabase.CreateFolder("Assets/SO", "Waves");
        }

        SerializedProperty wavesProperty = serializedObject.FindProperty("waves");
        wavesProperty.arraySize = 10; // 10개의 웨이브로 설정

        // 10라운드 웨이브 데이터 생성
        int[] enemyCounts = { 5, 8, 12, 15, 18, 22, 26, 30, 35, 40 };
        float[] healthMultipliers = { 1.0f, 1.2f, 1.44f, 1.44f, 1.73f, 2.08f, 2.08f, 2.5f, 3.0f, 3.6f };
        float[] speedMultipliers = { 1.0f, 1.05f, 1.1f, 1.15f, 1.2f, 1.25f, 1.3f, 1.35f, 1.4f, 1.45f };
        float[] goldMultipliers = { 1.0f, 1.1f, 1.2f, 1.3f, 1.4f, 1.5f, 1.6f, 1.7f, 1.8f, 1.9f };
        float[] spawnIntervals = { 1.0f, 0.95f, 0.9f, 0.85f, 0.8f, 0.75f, 0.7f, 0.65f, 0.6f, 0.55f };
        // maxEnemiesAtOnce 제거됨 - 제한 없음

        for (int i = 0; i < 10; i++)
        {
            // 새로운 WaveData 생성
            WaveData waveData = UnityEngine.ScriptableObject.CreateInstance<WaveData>();
            waveData.waveName = $"Wave {i + 1}";
            waveData.description = $"{i + 1}번째 웨이브 - 난이도: {GetDifficultyLevel(i)}";

            // 난이도 설정
            waveData.enemyCount = enemyCounts[i];
            waveData.healthMultiplier = healthMultipliers[i];
            waveData.speedMultiplier = speedMultipliers[i];
            waveData.goldRewardMultiplier = goldMultipliers[i];
            waveData.spawnInterval = spawnIntervals[i];
            // maxEnemiesAtOnce와 useRandomSpawn 제거됨 - 기본값 사용

            // 파일로 저장
            string assetPath = $"{folderPath}/Wave_{i + 1}.asset";
            UnityEditor.AssetDatabase.CreateAsset(waveData, assetPath);

            // WaveManager의 배열에 연결
            wavesProperty.GetArrayElementAtIndex(i).objectReferenceValue = waveData;
        }

        // 에셋 데이터베이스 갱신
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        UnityEditor.EditorUtility.DisplayDialog("완료", "10개의 웨이브 데이터를 생성했습니다!\nAssets/SO/Waves 폴더를 확인하세요.", "확인");
    }

    private void LoadExistingWaves()
    {
        string folderPath = "Assets/SO/Waves";
        SerializedProperty wavesProperty = serializedObject.FindProperty("waves");

        // 20개의 웨이브로 배열 크기 설정
        wavesProperty.arraySize = 20;

        int loadedCount = 0;

        // Wave_1.asset부터 Wave_20.asset까지 로드해서 세팅
        for (int i = 1; i <= 20; i++)
        {
            string assetPath = $"{folderPath}/Wave_{i}.asset";

            // 에셋 로드
            WaveData waveData = UnityEditor.AssetDatabase.LoadAssetAtPath<WaveData>(assetPath);

            if (waveData != null)
            {
                // WaveManager의 waves 배열에 연결 (세팅)
                wavesProperty.GetArrayElementAtIndex(i - 1).objectReferenceValue = waveData;
                loadedCount++;
                Debug.Log($"WaveManager waves[{i - 1}]에 Wave_{i}.asset 세팅 완료 - {waveData.waveName}");
            }
            else
            {
                Debug.LogError($"Wave_{i}.asset 파일을 찾을 수 없습니다: {assetPath}");
            }
        }

        // 에셋 데이터베이스 갱신
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        UnityEditor.EditorUtility.DisplayDialog("20웨이브 세팅 완료",
            $"{loadedCount}개의 웨이브 데이터를 WaveManager에 세팅했습니다!\n\n" +
            "세팅된 웨이브들:\n" +
            "waves[0] ← Wave_1.asset\n" +
            "waves[1] ← Wave_2.asset\n" +
            "...\n" +
            "waves[19] ← Wave_20.asset\n\n" +
            "이제 게임에서 20웨이브를 사용할 수 있습니다.",
            "확인");
    }

    private void UpdateAllWaveBalances()
    {
        string folderPath = "Assets/SO/Waves";
        int updatedCount = 0;

        for (int i = 1; i <= 20; i++)
        {
            string assetPath = $"{folderPath}/Wave_{i}.asset";

            // 에셋 로드
            WaveData waveData = UnityEditor.AssetDatabase.LoadAssetAtPath<WaveData>(assetPath);

            if (waveData != null)
            {
                // ConfigureWaveByDifficulty 로직으로 밸런스 업데이트
                ConfigureWaveByDifficulty(waveData, i);

                // 변경사항 저장
                UnityEditor.EditorUtility.SetDirty(waveData);
                updatedCount++;

                Debug.Log($"Wave_{i}.asset 밸런스 업데이트 완료:");
                Debug.Log($"  - 적 수: {waveData.enemyCount}");
                Debug.Log($"  - 스폰 간격: {waveData.spawnInterval:F2}초");
                Debug.Log($"  - 체력 배율: {waveData.healthMultiplier:F2}x");
                Debug.Log($"  - 속도 배율: {waveData.speedMultiplier:F2}x");
                Debug.Log($"  - 골드 배율: {waveData.goldRewardMultiplier:F2}x");
            }
            else
            {
                Debug.LogError($"Wave_{i}.asset 파일을 찾을 수 없습니다: {assetPath}");
            }
        }

        // 에셋 데이터베이스 갱신
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        UnityEditor.EditorUtility.DisplayDialog("밸런스 업데이트 완료",
            $"{updatedCount}개의 웨이브 파일이 업데이트되었습니다!\n\n" +
            "업데이트된 값들:\n" +
            "• 적 체력: 1.5배씩 증가 (Wave 1: 1.0x → Wave 20: 엄청난 값)\n" +
            "• 적 수: 점진적 증가 (Wave 1: 5마리 → Wave 20: 135마리)\n" +
            "• 스폰 속도: 점진적 증가 (Wave 1: 1초 → Wave 20: 0.2초)\n" +
            "• 골드 보상: 35-50%씩 증가 (Wave 1: 1.0x → Wave 20: 6.4x)\n" +
            "• 이동속도: 12-18%씩 증가 (Wave 1: 1.0x → Wave 20: 4.5x+)\n\n" +
            "Assets/SO/Waves 폴더의 모든 파일이 업데이트되었습니다.",
            "확인");
    }

    private string GetDifficultyLevel(int waveIndex)
    {
        if (waveIndex < 3) return "쉬움";
        if (waveIndex < 6) return "보통";
        return "어려움";
    }
}