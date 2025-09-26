using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WaveManager))]
public class WaveManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("📁 Resources에 WaveData 생성"))
        {
            CreateWaveDataInResources();
        }

        if (GUILayout.Button("🔄 AssetDatabase 리프레시"))
        {
            AssetDatabase.Refresh();
        }
    }

    private void CreateWaveDataInResources()
    {
        string folderPath = "Assets/Resources/WaveData";

        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder("Assets/Resources", "WaveData");

        for (int i = 1; i <= 20; i++)
        {
            string assetPath = $"{folderPath}/Wave_{i}.asset";
            if (AssetDatabase.LoadAssetAtPath<WaveData>(assetPath) != null) continue;

            WaveData waveData = ScriptableObject.CreateInstance<WaveData>();
            waveData.waveName = $"Wave {i}";
            waveData.description = $"{i}번째 웨이브 - 난이도: {(i <= 3 ? "쉬움" : i <= 6 ? "보통" : "어려움")}";
            waveData.enemyCount = Mathf.RoundToInt(5 * Mathf.Pow(1.5f, i - 1));
            waveData.spawnInterval = Mathf.Max(0.5f, 1.5f - (i - 1) * 0.1f);
            waveData.speedMultiplier = 1f + (i - 1) * 0.1f;
            waveData.healthMultiplier = Mathf.Pow(1.5f, i - 1);
            waveData.goldRewardMultiplier = 1f + (i - 1) * 0.2f;

            AssetDatabase.CreateAsset(waveData, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료", "20개의 WaveData 생성됨", "확인");
    }
}