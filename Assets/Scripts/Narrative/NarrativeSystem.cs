using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System;
using Managers;

public class NarrativeSystem : MenuBase
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject optionsContainer;
    [SerializeField] private GameObject optionPrefab;

    private DialogueData currentDialogue;
    private Dictionary<string, DialogueLine> dialogueLinesMap;
    private INarrativeTarget currentConversationTarget;

    public void StartConversation(NarrativeAsset narrativeAsset)
    {
        // Find the NPC we're talking to by getting the current interactive target
        FindConversationTarget();
        
        gameObject.SetActive(true);
        
        if (currentDialogue == null)
        {
            LoadDialogue(narrativeAsset.dialogueFile);
        }

        if (currentDialogue != null && currentDialogue.lines.Count > 0)
        {
            // Pause the conversation target if it implements IConversationTarget
            if (currentConversationTarget != null)
            {
                currentConversationTarget.PauseForConversation();
            }
            
            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_CONVERSATION);
            gameObject.SetActive(true);
            ShowDialogue(currentDialogue.lines[0]);
        }
        else
        {
            Debug.LogWarning("[NarrativeSystem] Failed to load dialogue or dialogue has no lines!");
        }
    }

    public void LoadDialogue(TextAsset dialogueFile)
    {
        if (dialogueFile != null)
        {
            string json = dialogueFile.text;
            
            currentDialogue = JsonUtility.FromJson<DialogueData>(json);

            if (currentDialogue != null)
            {
                dialogueLinesMap = new Dictionary<string, DialogueLine>();
                if (currentDialogue.lines != null)
                {
                    foreach (var line in currentDialogue.lines)
                    {
                        dialogueLinesMap[line.id] = line; // Use 'id' as the key
                    }
                }
            }
            else
            {
                Debug.LogError("[NarrativeSystem] Failed to parse JSON dialogue!");
            }
        }
        else
        {
            Debug.LogError("Dialogue TextAsset is null!");
        }
    }

    public void ShowDialogue(DialogueLine line)
    {        
        if (npcNameText != null)
            npcNameText.text = line.speaker;
        else
            Debug.LogError("[NarrativeSystem] npcNameText is null!");
            
        if (dialogueText != null)
            dialogueText.text = line.text;
        else
            Debug.LogError("[NarrativeSystem] dialogueText is null!");

        // Clear previous options
        foreach (Transform child in optionsContainer.transform)
        {
            Destroy(child.gameObject);
        }

        // If the line has options, show them
        if (line.options != null && line.options.Count > 0)
        {
            GameObject firstOption = null;
            foreach (var option in line.options)
            {
                GameObject optionObj = Instantiate(optionPrefab, optionsContainer.transform);
                TextMeshProUGUI optionText = optionObj.GetComponentInChildren<TextMeshProUGUI>();
                optionText.text = option.text;

                // Add click event
                Button optionButton = optionObj.GetComponent<Button>();
                optionButton.onClick.AddListener(() => HandleOptionSelected(option));

                if (firstOption == null)
                {
                    firstOption = optionObj;
                }
            }

            SetSelectedButton(firstOption);
            return;
        }

        // If the line is terminal, show a "Close" button
        if (line.isTerminal)
        {
            GameObject closeButton = Instantiate(optionPrefab, optionsContainer.transform);
            TextMeshProUGUI closeButtonText = closeButton.GetComponentInChildren<TextMeshProUGUI>();
            closeButtonText.text = "Close";
            Button closeButtonComponent = closeButton.GetComponent<Button>();
            closeButtonComponent.onClick.AddListener(() => EndConversation());

            SetSelectedButton(closeButton);
            return;
        }

        // If there's a next line but no options, add a "Continue" button to proceed
        if (!string.IsNullOrEmpty(line.nextLine))
        {
            GameObject continueButton = Instantiate(optionPrefab, optionsContainer.transform);
            TextMeshProUGUI continueButtonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            continueButtonText.text = "Continue";
            Button continueButtonComponent = continueButton.GetComponent<Button>();
            continueButtonComponent.onClick.AddListener(() => HandleOptionSelected(line.nextLine));

            SetSelectedButton(continueButton);
        }
        else
        {
            // End conversation if no next line and no options
            EndConversation();
        }
    }

    private void SetSelectedButton(GameObject button)
    {
        if (button != null)
        {
            PlayerUIManager.Instance.SetSelectedGameObject(button);
        }
    }

    public void HandleOptionSelected(DialogueOption option)
    {
        // Handle NPC recruitment if specified
        if (!string.IsNullOrEmpty(option.recruitNPC))
        {
            RecruitNPC(option.recruitNPC);
        }

        // Continue to next dialogue line
        HandleOptionSelected(option.nextLine);
    }

    public void HandleOptionSelected(string nextLine)
    {
        if (!string.IsNullOrEmpty(nextLine) && dialogueLinesMap.TryGetValue(nextLine, out DialogueLine nextDialogueLine))
        {
            ShowDialogue(nextDialogueLine);
        }
        else
        {
            EndConversation();
        }
    }

    public void EndConversation()
    {
        // Resume the conversation target if it implements IConversationTarget
        if (currentConversationTarget != null)
        {
            currentConversationTarget.ResumeAfterConversation();
            currentConversationTarget = null;
        }
        
        ReturnToGame();
        gameObject.SetActive(false);
    }

    public void ReturnToGame(PlayerControlType playerControlType = PlayerControlType.NONE)
    {
        if (playerControlType != PlayerControlType.NONE)
        {
            PlayerInput.Instance.UpdatePlayerControls(playerControlType);
        }
        else
        {
            PlayerInput.Instance.UpdatePlayerControls(GameManager.Instance.PlayerGameControlType());
        }
    }

    /// <summary>
    /// Recruit an NPC by name through the dialogue system
    /// </summary>
    private void RecruitNPC(string npcName)
    {
        if (string.IsNullOrEmpty(npcName))
        {
            Debug.LogWarning("[NarrativeSystem] Cannot recruit NPC with empty name!");
            return;
        }

        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("[NarrativeSystem] PlayerInventory not found!");
            return;
        }

        // Find NPCScriptableObj with matching name
        NPCScriptableObj[] allNPCs = Resources.FindObjectsOfTypeAll<NPCScriptableObj>();
        
        foreach (var npc in allNPCs)
        {
            if (npc.nPCName.Equals(npcName, System.StringComparison.OrdinalIgnoreCase))
            {
                PlayerInventory.Instance.RecruitNPC(npc);
                Debug.Log($"[NarrativeSystem] Successfully recruited {npcName}!");
                return;
            }
        }

        Debug.LogWarning($"[NarrativeSystem] Could not find NPC with name '{npcName}'!");
    }

    /// <summary>
    /// Finds the NPC we're currently talking to by detecting what the player is currently interacting with
    /// </summary>
    private void FindConversationTarget()
    {
        currentConversationTarget = null;
        
        if (PlayerInventory.Instance == null || PlayerController.Instance?._possessedNPC == null)
        {
            return;
        }

        // Use the same detection logic as PlayerInventory to find what we're looking at
        RaycastHit hit;
        Vector3 startPos = PlayerController.Instance._possessedNPC.GetTransform().position + Vector3.up;
        Vector3 direction = PlayerController.Instance._possessedNPC.GetTransform().forward;
        Vector3 boxCastSize = new Vector3(0.5f, 0.5f, 0.5f); // Same as PlayerInventory
        float interactionRange = 3f; // Same as PlayerInventory
        
        if (Physics.BoxCast(startPos, boxCastSize * 0.5f, direction, out hit, PlayerController.Instance._possessedNPC.GetTransform().rotation, interactionRange))
        {
            // Check if the hit object implements IConversationTarget
            INarrativeTarget conversationTarget = hit.collider.GetComponent<INarrativeTarget>();
            if (conversationTarget != null)
            {
                currentConversationTarget = conversationTarget;
            }
        }
    }
}
