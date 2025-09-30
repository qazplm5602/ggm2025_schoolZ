using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ClickableObject : MonoBehaviour, IClickable, IPointerClickHandler
{
    public event System.Action OnClicked;
    public event System.Action<int> OnClickedWithIndex;

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private Image displayImage;


    public int ObjectIndex { get; private set; }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke();
        OnClickedWithIndex?.Invoke(ObjectIndex);
    }

    public void SimulateClick()
    {
        OnClicked?.Invoke();
        OnClickedWithIndex?.Invoke(ObjectIndex);
    }

    public void SetObjectIndex(int index)
    {
        ObjectIndex = index;
    }
    public void UpdateDisplayText(string text)
    {
        if (displayText == null)
        {
            displayText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (displayText != null)
        {
            displayText.text = text;
        }
    }
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
    public void UpdateDisplay(string text, Sprite sprite = null)
    {
        UpdateDisplayText(text);

        if (sprite != null)
        {
            UpdateDisplayImage(sprite);
        }
    }
    public void UpdateTowerDisplay(string name, int cost, string description, Sprite icon = null)
    {
        string displayText = $"{name}\n{cost}G";
        if (!string.IsNullOrEmpty(description))
        {
            displayText += $"\n{description}";
        }

        UpdateDisplay(displayText, icon);
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }
    
    private void OnDestroy()
    {
        OnClicked = null;
        OnClickedWithIndex = null;
    }
}