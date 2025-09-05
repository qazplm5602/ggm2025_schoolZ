#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class HealthBarCreator : MonoBehaviour
{
    [MenuItem("Tools/Create HealthBar Prefab")]
    private static void CreateHealthBarPrefab()
    {
        // 프리팹 폴더 확인 및 생성
        string prefabFolder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // HealthBar 루트 오브젝트 생성
        GameObject healthBarRoot = new GameObject("HealthBar");

        // Canvas 추가 및 설정
        Canvas canvas = healthBarRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        // Canvas Scaler 설정
        CanvasScaler scaler = healthBarRoot.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;

        // RectTransform 설정
        RectTransform rectTransform = healthBarRoot.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 50);
        rectTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // 배경 이미지 생성
        GameObject background = new GameObject("Background");
        background.transform.SetParent(healthBarRoot.transform);

        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f); // 반투명 검정

        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(200, 50);
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // 체력바 이미지 생성
        GameObject healthFill = new GameObject("HealthFill");
        healthFill.transform.SetParent(background.transform);

        Image fillImage = healthFill.AddComponent<Image>();
        fillImage.color = Color.green;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;

        RectTransform fillRect = healthFill.GetComponent<RectTransform>();
        fillRect.sizeDelta = new Vector2(180, 30);
        fillRect.anchorMin = new Vector2(0.1f, 0.3f);
        fillRect.anchorMax = new Vector2(0.9f, 0.7f);

        // 체력 텍스트 생성 (TextMeshPro 사용)
        GameObject healthText = new GameObject("HealthText");
        healthText.transform.SetParent(background.transform);

        TextMeshProUGUI textComponent = healthText.AddComponent<TextMeshProUGUI>();
        textComponent.text = "100/100";
        textComponent.fontSize = 20;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;

        RectTransform textRect = healthText.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(180, 30);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // HealthBar 스크립트 추가
        HealthBar healthBarScript = healthBarRoot.AddComponent<HealthBar>();
        healthBarScript.healthFillImage = fillImage;
        healthBarScript.healthText = textComponent;

        // 프리팹 생성
        string prefabPath = prefabFolder + "/HealthBar.prefab";
        PrefabUtility.SaveAsPrefabAsset(healthBarRoot, prefabPath);

        // 임시 오브젝트 삭제
        DestroyImmediate(healthBarRoot);

        Debug.Log("HealthBar 프리팹이 생성되었습니다: " + prefabPath);
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Create Simple HealthBar Prefab")]
    private static void CreateSimpleHealthBarPrefab()
    {
        // 프리팹 폴더 확인 및 생성
        string prefabFolder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // 머티리얼 폴더 확인 및 생성
        string materialFolder = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(materialFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        // SimpleHealthBar 루트 오브젝트 생성
        GameObject healthBarRoot = new GameObject("SimpleHealthBar");

        // SimpleHealthBar 스크립트 추가
        SimpleHealthBar simpleHealthBar = healthBarRoot.AddComponent<SimpleHealthBar>();

        // 기본 머티리얼 생성
        Material bgMaterial = new Material(Shader.Find("Standard"));
        bgMaterial.color = Color.black;
        string bgMaterialPath = materialFolder + "/HealthBar_Background.mat";
        AssetDatabase.CreateAsset(bgMaterial, bgMaterialPath);

        Material fillMaterial = new Material(Shader.Find("Standard"));
        fillMaterial.color = Color.green;
        string fillMaterialPath = materialFolder + "/HealthBar_Fill.mat";
        AssetDatabase.CreateAsset(fillMaterial, fillMaterialPath);

        // 머티리얼 설정
        simpleHealthBar.backgroundMaterial = AssetDatabase.LoadAssetAtPath<Material>(bgMaterialPath);
        simpleHealthBar.fillMaterial = AssetDatabase.LoadAssetAtPath<Material>(fillMaterialPath);

        // 프리팹 생성
        string prefabPath = prefabFolder + "/SimpleHealthBar.prefab";
        PrefabUtility.SaveAsPrefabAsset(healthBarRoot, prefabPath);

        // 임시 오브젝트 삭제
        DestroyImmediate(healthBarRoot);

        Debug.Log("SimpleHealthBar 프리팹이 생성되었습니다: " + prefabPath);
        AssetDatabase.Refresh();
    }
}
#endif
