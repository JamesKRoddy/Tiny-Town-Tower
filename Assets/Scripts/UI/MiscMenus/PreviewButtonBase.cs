using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PreviewButtonBase : MonoBehaviour
{
    [SerializeField] protected Button button;
    [SerializeField] protected Image previewImage;
    [SerializeField] protected TMP_Text nameText;
    
    // Optional UI elements that can be assigned in inspector
    [SerializeField] protected TMP_Text countText;

    protected object data;
    protected Action<object> customClickHandler = null;

    public object Data => data;

    void OnDestroy()
    {
        button.onClick.RemoveAllListeners();
    }

    public virtual void SetupButton(object dataObject, Sprite image = null, string displayName = "Unknown", string countDisplay = "")
    {
        data = dataObject;

        if (image != null && previewImage != null)
        {
            previewImage.sprite = image;
        }

        if (nameText != null)
        {
            nameText.text = displayName;
        }

        // Handle optional count text
        if (countText != null)
        {
            if (!string.IsNullOrEmpty(countDisplay))
            {
                countText.text = countDisplay;
                countText.gameObject.SetActive(true);
            }
            else
            {
                countText.gameObject.SetActive(false);
            }
        }

        // Remove previous listeners before adding new one
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnButtonClicked);
    }

    public virtual void SetupButton(object dataObject, Action<object> onClickHandler, Sprite image = null, string displayName = "Unknown", string countDisplay = "")
    {
        customClickHandler = onClickHandler;
        SetupButton(dataObject, image, displayName, countDisplay);
    }

    protected virtual void OnButtonClicked()
    {
        if (customClickHandler != null)
        {
            customClickHandler(data);
        }
    }

    // Method to update count text without re-setting up the entire button
    public void UpdateCountText(string countDisplay)
    {
        if (countText != null)
        {
            if (!string.IsNullOrEmpty(countDisplay))
            {
                countText.text = countDisplay;
                countText.gameObject.SetActive(true);
            }
            else
            {
                countText.gameObject.SetActive(false);
            }
        }
    }

    // Helper method to get typed data
    public T GetData<T>() where T : class
    {
        return data as T;
    }
}
