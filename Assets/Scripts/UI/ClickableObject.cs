using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 클릭 가능한 게임 오브젝트
/// 자식 UI 업데이트와 클릭 이벤트 처리를 담당합니다
/// </summary>
public class ClickableObject : MonoBehaviour, IClickable, IPointerClickHandler
{
    /// <summary>
    /// 클릭 이벤트 (외부에서 구독해서 사용)
    /// </summary>
    public event System.Action OnClicked;

    /// <summary>
    /// 클릭 이벤트 with 인덱스 (TowerPlacementSystem용)
    /// </summary>
    public event System.Action<int> OnClickedWithIndex;

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private Image displayImage;


    /// <summary>
    /// 이 오브젝트의 인덱스 (어떤 버튼인지 식별용)
    /// </summary>
    public int ObjectIndex { get; private set; }

    /// <summary>
    /// Unity의 PointerClick 이벤트 핸들러
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 클릭 이벤트 발생 (기본 이벤트)
        OnClicked?.Invoke();

        // 인덱스와 함께 클릭 이벤트 발생 (TowerPlacementSystem용)
        OnClickedWithIndex?.Invoke(ObjectIndex);
    }

    /// <summary>
    /// 프로그래밍 방식으로 클릭 이벤트 호출
    /// </summary>
    public void SimulateClick()
    {
        OnClicked?.Invoke();
        OnClickedWithIndex?.Invoke(ObjectIndex);
    }

    /// <summary>
    /// 오브젝트 인덱스 설정
    /// </summary>
    public void SetObjectIndex(int index)
    {
        ObjectIndex = index;
    }



    /// <summary>
    /// 표시 텍스트 업데이트
    /// </summary>
    public void UpdateDisplayText(string text)
    {
        if (displayText == null)
        {
            // 자동으로 자식에서 TextMeshProUGUI 찾기
            displayText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (displayText != null)
        {
            displayText.text = text;
        }
    }

    /// <summary>
    /// 표시 이미지 업데이트
    /// </summary>
    public void UpdateDisplayImage(Sprite sprite)
    {
        if (displayImage == null)
        {
            // 자동으로 자식에서 Image 찾기
            displayImage = GetComponentInChildren<Image>();
        }

        if (displayImage != null)
        {
            displayImage.sprite = sprite;
        }
    }

    /// <summary>
    /// 표시 이미지 색상 변경
    /// </summary>
    public void SetDisplayImageColor(Color color)
    {
        if (displayImage == null)
        {
            displayImage = GetComponentInChildren<Image>();
        }

        if (displayImage != null)
        {
            displayImage.color = color;
        }
    }

    /// <summary>
    /// 표시 텍스트 색상 변경
    /// </summary>
    public void SetDisplayTextColor(Color color)
    {
        if (displayText != null)
        {
            displayText.color = color;
        }
    }

    /// <summary>
    /// 텍스트와 이미지 모두 업데이트
    /// </summary>
    public void UpdateDisplay(string text, Sprite sprite = null)
    {
        UpdateDisplayText(text);

        if (sprite != null)
        {
            UpdateDisplayImage(sprite);
        }
    }

    /// <summary>
    /// 타워 정보 표시 (이름, 가격, 설명, 아이콘)
    /// </summary>
    public void UpdateTowerDisplay(string name, int cost, string description, Sprite icon = null)
    {
        string displayText = $"{name}\n{cost}G";
        if (!string.IsNullOrEmpty(description))
        {
            displayText += $"\n{description}";
        }

        UpdateDisplay(displayText, icon);
    }

    /// <summary>
    /// 오브젝트 활성화/비활성화
    /// </summary>
    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }




    /// <summary>
    /// 클릭 이벤트 구독 해제
    /// </summary>
    private void OnDestroy()
    {
        OnClicked = null;
        OnClickedWithIndex = null;
    }
}