using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public abstract class MenuBase : MonoBehaviour, IControllerInput
{
    [Header("First Selected Button")]
    [SerializeField] private Button _firstSelected;

    public virtual void SetScreenActive(bool active, float delay = 0.0f, Action onDone = null){
        PlayerUIManager.Instance.SetScreenActive(this, active, delay, () => {
            if (active)
            {
                PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);
                PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;
                
                // Try to find a button to select for controller navigation
                Button buttonToSelect = GetFirstSelectableButton();
                if (buttonToSelect != null)
                {
                    PlayerUIManager.Instance.SetSelectedGameObject(buttonToSelect.gameObject);
                }
            }
            else
            {
                PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
            }
            onDone?.Invoke();
        });
    }

    /// <summary>
    /// Gets the first selectable button for controller navigation.
    /// First tries the explicitly set _firstSelected, then searches children.
    /// </summary>
    protected virtual Button GetFirstSelectableButton()
    {
        // First, try the explicitly set button
        if (_firstSelected != null && _firstSelected.interactable && _firstSelected.gameObject.activeInHierarchy)
        {
            return _firstSelected;
        }

        // Fallback: search for the first active and interactable button in children
        Button[] buttons = GetComponentsInChildren<Button>(false); // false = only active objects
        foreach (Button button in buttons)
        {
            if (button.interactable)
            {
                Debug.Log($"[MenuBase] Auto-selected first button: {button.gameObject.name} for menu: {gameObject.name}");
                return button;
            }
        }

        return null;
    }

    public virtual void DisplayErrorMessage(string message)
    {
        PlayerUIManager.Instance.DisplayUIErrorMessage(message);
    }

    public virtual void LoadScene(SceneNames targetScene, GameMode nextSceneGameMode, bool keepPlayerControls = false, bool keepPossessedNPC = false)
    {
        SceneTransitionManager.Instance.LoadScene(targetScene, nextSceneGameMode, keepPossessedNPC);
    }
    
    public void SetPlayerControlType(PlayerControlType controlType)
    {
        if (PlayerUIManager.Instance.currentMenu != this)
            return;

        SetPlayerControls(controlType);
    }    

    public virtual void SetPlayerControls(PlayerControlType controlType){
        switch (controlType)
        {
            case PlayerControlType.IN_MENU:
                PlayerInput.Instance.OnBPressed += () => BackPressed();
                break;
            default:
                break;
        }
    }

    protected virtual void BackPressed(){
        PlayerUIManager.Instance.BackPressed();
    }

    public virtual void OnDestroy()
    {
        if (PlayerInput.Instance != null)
            PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }
}
