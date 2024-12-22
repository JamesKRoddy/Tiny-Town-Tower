using System.Collections.Generic;
using UnityEngine;

public class SettlerNPCMenu : PreviewListMenuBase<string, SettlerNPCScriptableObj>, IControllerInput
{
    private static SettlerNPCMenu _instance;

    public static SettlerNPCMenu Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SettlerNPCMenu>();
                if (_instance == null)
                {
                    Debug.LogError("SettlerNPCMenu instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    public override void Setup()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();

        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;

        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);
    }

    public override void OnDisable()
    {
        base.OnDisable();

        PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }

    public void SetPlayerControlType(PlayerControlType controlType)
    {
        switch (controlType)
        {
            case PlayerControlType.IN_MENU:
                PlayerInput.Instance.OnRBPressed += rightScreenBtn.onClick.Invoke;
                PlayerInput.Instance.OnLBPressed += leftScreenBtn.onClick.Invoke;
                PlayerInput.Instance.OnBPressed += () => UtilityMenu.Instance.EnableUtilityMenu();
                break;
            default:
                break;
        }
    }

    public override void SetScreenActive(bool active, float delay = 0.0f)
    {
        PlayerUIManager.Instance.SetScreenActive(this, active, delay);
    }

    // Retrieve all NPCs in the scene that inherit from SettlerNPC
    public override IEnumerable<SettlerNPCScriptableObj> GetItems()
    {
        var npcs = FindObjectsOfType<SettlerNPC>();
        foreach (var npc in npcs)
        {
            if (npc.NPCData != null)
            {
                yield return npc.NPCData;
            }
        }
    }

    public override string GetItemCategory(SettlerNPCScriptableObj item)
    {
        return "Default"; // Grouping not relevant; returning default category
    }

    public override void SetupItemButton(SettlerNPCScriptableObj item, GameObject button)
    {
        var buttonComponent = button.GetComponent<SettlerPreviewBtn>();
        buttonComponent.SetupButton(item);
    }

    public override string GetPreviewName(SettlerNPCScriptableObj item)
    {
        return item.nPCName;
    }

    public override Sprite GetPreviewSprite(SettlerNPCScriptableObj item)
    {
        return null; // Assuming no sprite for NPCs; modify if sprites exist
    }

    public override string GetPreviewDescription(SettlerNPCScriptableObj item)
    {
        return item.nPCDescription;
    }

    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(SettlerNPCScriptableObj item)
    {
        yield break; // No resource costs for NPCs
    }

    public override void UpdatePreviewSpecifics(SettlerNPCScriptableObj item)
    {
        Debug.Log($"Displaying details for NPC: {item.nPCName}");
    }

    public override void DestroyPreviewSpecifics()
    {
        Debug.Log("Clearing NPC preview details");
    }
}
