using System;
using System.Collections.Generic;
using UnityEngine;

public class SettlerNPCMenu : PreviewListMenuBase<string, SettlerNPC>, IControllerInput
{
    public override void Setup()
    {
        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;
    }

    public override void OnDisable()
    {
        base.OnDisable();

        PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }

    public void SetPlayerControlType(PlayerControlType controlType)
    {
        if (PlayerUIManager.Instance.currentMenu != this)
            return;

        switch (controlType)
        {
            case PlayerControlType.IN_MENU:
                PlayerInput.Instance.OnRBPressed += rightScreenBtn.onClick.Invoke;
                PlayerInput.Instance.OnLBPressed += leftScreenBtn.onClick.Invoke;
                PlayerInput.Instance.OnBPressed += () => PlayerUIManager.Instance.utilityMenu.EnableUtilityMenu();
                break;
            default:
                break;
        }
    }

    public override void SetScreenActive(bool active, float delay = 0.0f, Action onDone = null)
    {
        PlayerUIManager.Instance.SetScreenActive(this, active, delay);
    }

    // Retrieve all NPCs in the scene that inherit from SettlerNPC
    public override IEnumerable<SettlerNPC> GetItems()
    {
        var npcs = FindObjectsByType<SettlerNPC>(FindObjectsSortMode.None);

        foreach (var npc in npcs)
        {
            if (npc.nPCDataObj != null)
            {
                yield return npc;
            }
            else
            {
                Debug.LogWarning($"Settler {npc.gameObject.name} has no NPCData!");
            }
        }
    }

    public override string GetItemCategory(SettlerNPC item)
    {
        return "Default"; // Grouping not relevant; returning default category
    }

    public override void SetupItemButton(SettlerNPC item, GameObject button)
    {
        var buttonComponent = button.GetComponent<SettlerPreviewBtn>();
        buttonComponent.SetupButton(item);
    }

    public override string GetPreviewName(SettlerNPC item)
    {
        return item.nPCDataObj.nPCName;
    }

    public override Sprite GetPreviewSprite(SettlerNPC item)
    {
        return null; // Assuming no sprite for NPCs; modify if sprites exist
    }

    public override string GetPreviewDescription(SettlerNPC item)
    {
        return item.nPCDataObj.nPCDescription;
    }

    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(SettlerNPC item)
    {
        yield break; // No resource costs for NPCs
    }

    public override void UpdatePreviewSpecifics(SettlerNPC item)
    {
        //Debug.Log($"Displaying details for NPC: {item.nPCDataObj.nPCName}");
    }

    public override void DestroyPreviewSpecifics()
    {
        Debug.Log("Clearing NPC preview details");
    }
}
