using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class PreviewButtonBase<T> : MonoBehaviour
{
    [SerializeField] protected Button button;
    [SerializeField] protected Image previewImage;
    [SerializeField] protected TMP_Text nameText;

    protected T data;

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

    protected abstract void OnButtonClicked();
}
