using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class MenuBase : MonoBehaviour, IControllerInput
{
    public virtual void SetScreenActive(bool active, float delay = 0.0f, Action onDone = null){
        PlayerUIManager.Instance.SetScreenActive(this, active, delay, onDone);
    }

    public virtual void OnEnable(){
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);
    }

    public virtual void Setup(){
        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;
    }

    public virtual void DisplayErrorMessage(string message)
    {
        PlayerUIManager.Instance.DisplayUIErrorMessage(message);
    }

    public virtual void LoadScene(string targetScene, GameMode nextSceneGameMode, bool keepPlayerControls = false, bool keepPossessedNPC = false)
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
                PlayerInput.Instance.OnBPressed += () => PlayerUIManager.Instance.utilityMenu.EnableUtilityMenu();
                break;
            default:
                break;
        }
    }
    public virtual void OnDisable(){
        PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }

    public virtual void OnDestroy()
    {
        if (PlayerInput.Instance != null)
            PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }
}
