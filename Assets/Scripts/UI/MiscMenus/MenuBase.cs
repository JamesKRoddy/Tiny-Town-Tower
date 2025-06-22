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
                PlayerUIManager.Instance.SetSelectedGameObject(_firstSelected.gameObject);
            }
            else
            {
                PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
            }
            onDone?.Invoke();
        });
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
