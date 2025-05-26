using System;
using UnityEngine;
using TMPro;
using Managers;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
public class TextPopup : MonoBehaviour
{
    [SerializeField] TMP_Text text;
    [SerializeField] Button closeButton;

    void Start()
    {
        closeButton.onClick.AddListener(ReturnToGame);
    }

    internal void Setup(string message)
    {
        gameObject.SetActive(true);
        text.text = message;
        SetupInitialSelection();
    }
    protected virtual void SetupInitialSelection()
    {
        // Override in derived classes to set initial button selection
        if (closeButton != null)
        {
            // Use a small delay to prevent immediate button press
            StartCoroutine(SetSelectedAfterDelay(closeButton.gameObject));
        }
    }

    private IEnumerator SetSelectedAfterDelay(GameObject obj)
    {
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(obj);
    }

    void ReturnToGame()
    {
        Debug.Log("Returning to game");
        gameObject.SetActive(false);
        PlayerInput.Instance.UpdatePlayerControls(GameManager.Instance.PlayerGameControlType());
    }

    void OnDestroy()
    {
        closeButton.onClick.RemoveListener(ReturnToGame);
    }
}
