using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class PreviewButtonBase<T> : MonoBehaviour
{
    [SerializeField] protected Button button;
    [SerializeField] protected Image previewImage;
    [SerializeField] protected TMP_Text nameText;

    protected T data;
    protected Action<T> customClickHandler = null;

    void OnDestroy()
    {
        button.onClick.RemoveAllListeners();
    }

    public virtual void SetupButton(T dataObject, Sprite image = null, string displayName = "Unknown")
    {
        data = dataObject;

        if (image != null)
        {
            previewImage.sprite = image;
        }

        nameText.text = displayName;

        button.onClick.AddListener(OnButtonClicked);
    }

    public virtual void SetupButton(T dataObject, Action<T> onClickHandler, Sprite image = null, string displayName = "Unknown")
    {
        customClickHandler = onClickHandler;
        SetupButton(dataObject, image, displayName);
    }

    protected virtual void OnButtonClicked()
    {
        if (customClickHandler != null)
        {
            Debug.Log($"[PreviewButtonBase] Using custom click handler for {data}");
            customClickHandler(data);
        }
        else
        {
            Debug.Log($"[PreviewButtonBase] Using default click handler for {data}");
            OnDefaultButtonClicked();
        }
    }

    protected abstract void OnDefaultButtonClicked();
}
