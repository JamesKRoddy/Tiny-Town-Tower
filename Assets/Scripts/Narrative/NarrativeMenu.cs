using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System;
using Managers;
using System.Linq;

/// <summary>
/// UI presentation layer for narrative conversations.
/// Handles only the visual display of dialogue, with management logic in NarrativeManager.
/// </summary>
public class NarrativeMenu : MenuBase
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject optionsContainer;
    [SerializeField] private GameObject optionPrefab;

    /// <summary>
    /// Display a dialogue line with options (called by NarrativeManager)
    /// </summary>
    public void DisplayDialogue(DialogueLine line)
    {        
        if (npcNameText != null)
        {
            // Use the actual NPC's name instead of the speaker from the dialogue file
            string actualNPCName = NarrativeManager.Instance?.GetCurrentConversationTargetName() ?? "Unknown NPC";
            npcNameText.text = actualNPCName;
        }
        else
            Debug.LogError("[NarrativeMenu] npcNameText is null!");
            
        if (dialogueText != null)
            dialogueText.text = line.text;
        else
            Debug.LogError("[NarrativeMenu] dialogueText is null!");

        // Clear previous options
        ClearOptions();

        // If the line has options, show them (filtered by flags)
        if (line.options != null && line.options.Count > 0)
        {
            Debug.Log($"[NarrativeMenu] DisplayDialogue: Line has {line.options.Count} options before filtering");
            foreach (var option in line.options)
            {
                Debug.Log($"[NarrativeMenu] Option found: '{option.text}' -> '{option.nextLine}'");
                if (option.requiredInventoryItems != null && option.requiredInventoryItems.Count > 0)
                {
                    Debug.Log($"[NarrativeMenu] Option has {option.requiredInventoryItems.Count} inventory requirements:");
                    foreach (var req in option.requiredInventoryItems)
                    {
                        Debug.Log($"[NarrativeMenu]   - {req.itemName} x{req.requiredQuantity} (consume: {req.consumeOnUse})");
                    }
                }
            }
            
            var optionsWithState = FilterOptionsByFlags(line.options);
            Debug.Log($"[NarrativeMenu] After filtering: {optionsWithState.Count} options total, {optionsWithState.Count(o => o.IsEnabled)} enabled");
            
            if (optionsWithState.Count > 0)
            {
                ShowOptions(optionsWithState);
                return;
            }
            else
            {
                Debug.LogWarning("[NarrativeMenu] No options available!");
            }
        }

        // If the line is terminal, show a "Close" button
        if (line.isTerminal)
        {
            ShowCloseButton();
            return;
        }

        // If there's a next line but no options, add a "Continue" button to proceed
        if (!string.IsNullOrEmpty(line.nextLine))
        {
            ShowContinueButton(line.nextLine);
        }
        else
        {
            // End conversation if no next line and no options
            NarrativeManager.Instance.EndConversation();
        }
    }

    /// <summary>
    /// Filter dialogue options based on component's progression flags.
    /// Returns all options but marks some as disabled based on requirements.
    /// </summary>
    private List<DialogueOptionWithState> FilterOptionsByFlags(List<DialogueOption> options)
    {
        if (NarrativeManager.Instance == null)
            return options.Select(opt => new DialogueOptionWithState(opt, true)).ToList();

        Debug.Log($"[NarrativeMenu] FilterOptionsByFlags: Processing {options.Count} options");

        var optionsWithState = options.Select(option => 
        {
            bool isEnabled = true;
            string disableReason = "";

            // Check required flags
            if (option.requiredFlags != null && option.requiredFlags.Count > 0)
            {
                foreach (string flag in option.requiredFlags)
                {
                    if (!NarrativeManager.Instance.HasFlag(flag))
                    {
                        isEnabled = false;
                        disableReason = $"Missing required flag: {flag}";
                        break;
                    }
                }
            }

            // Check blocked flags (only if not already disabled)
            if (isEnabled && option.blockedByFlags != null && option.blockedByFlags.Count > 0)
            {
                foreach (string flag in option.blockedByFlags)
                {
                    if (NarrativeManager.Instance.HasFlag(flag))
                    {
                        isEnabled = false;
                        disableReason = $"Blocked by flag: {flag}";
                        break;
                    }
                }
            }

            // Check inventory requirements (only if not already disabled)
            if (isEnabled)
            {
                bool passesInventoryCheck = NarrativeManager.Instance.CheckInventoryRequirements(option);
                if (!passesInventoryCheck)
                {
                    isEnabled = false;
                    disableReason = "Insufficient resources";
                }
            }

            if (!isEnabled)
            {
                Debug.Log($"[NarrativeMenu] Option DISABLED: '{option.text}' - {disableReason}");
            }

            return new DialogueOptionWithState(option, isEnabled, disableReason);
        }).ToList();

        Debug.Log($"[NarrativeMenu] FilterOptionsByFlags: {optionsWithState.Count(o => o.IsEnabled)} out of {options.Count} options are enabled");
        return optionsWithState;
    }

    /// <summary>
    /// Clear all option buttons from the UI
    /// </summary>
    private void ClearOptions()
    {
        foreach (Transform child in optionsContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Show dialogue options to the player
    /// </summary>
    private void ShowOptions(List<DialogueOptionWithState> optionsWithState)
    {
        GameObject firstEnabledOption = null;
        
        foreach (var optionState in optionsWithState)
        {
            var option = optionState.Option;
            bool isEnabled = optionState.IsEnabled;
            
            // Create the option text with potential inventory requirements
            string displayText = option.text;
            string inventoryRequirement = NarrativeManager.Instance.GetInventoryRequirementDisplayText(option);
            if (!string.IsNullOrEmpty(inventoryRequirement))
            {
                displayText += $"\n<color=#888888><size=80%>{inventoryRequirement}</size></color>";
            }

            // Add visual indication if disabled
            if (!isEnabled)
            {
                displayText = $"<color=#666666>{displayText}</color>";
                if (!string.IsNullOrEmpty(optionState.DisableReason))
                {
                    displayText += $"\n<color=#AA4444><size=70%>({optionState.DisableReason})</size></color>";
                }
            }

            GameObject optionObj = CreateOptionButton(displayText, () => HandleOptionSelected(option), isEnabled);

            // Set first enabled option for selection
            if (firstEnabledOption == null && isEnabled)
            {
                firstEnabledOption = optionObj;
            }
        }

        SetSelectedButton(firstEnabledOption);
    }

    /// <summary>
    /// Show a close button to end the conversation
    /// </summary>
    private void ShowCloseButton()
    {
        GameObject closeButton = CreateOptionButton("Close", () => NarrativeManager.Instance.EndConversation());
        SetSelectedButton(closeButton);
    }

    /// <summary>
    /// Show a continue button to proceed to the next line
    /// </summary>
    private void ShowContinueButton(string nextLine)
    {
        GameObject continueButton = CreateOptionButton("Continue", () => HandleContinueSelected(nextLine));
        SetSelectedButton(continueButton);
    }

    /// <summary>
    /// Create an option button with the specified text and click action
    /// </summary>
    private GameObject CreateOptionButton(string text, System.Action onClickAction, bool isEnabled = true)
    {
        GameObject optionObj = Instantiate(optionPrefab, optionsContainer.transform);
        TextMeshProUGUI optionText = optionObj.GetComponentInChildren<TextMeshProUGUI>();
        optionText.text = text;

        // Add click event and set enabled state
        Button optionButton = optionObj.GetComponent<Button>();
        optionButton.interactable = isEnabled;
        
        if (isEnabled)
        {
            optionButton.onClick.AddListener(() => onClickAction?.Invoke());
        }

        return optionObj;
    }

    /// <summary>
    /// Set the selected button for controller/keyboard navigation
    /// </summary>
    private void SetSelectedButton(GameObject button)
    {
        if (button != null)
        {
            PlayerUIManager.Instance.SetSelectedGameObject(button);
        }
    }

    /// <summary>
    /// Handle when a dialogue option is selected
    /// </summary>
    private void HandleOptionSelected(DialogueOption option)
    {
        if (NarrativeManager.Instance != null)
        {
            NarrativeManager.Instance.HandleOptionSelected(option);
        }
    }

    /// <summary>
    /// Handle when continue is selected
    /// </summary>
    private void HandleContinueSelected(string nextLine)
    {
        if (NarrativeManager.Instance != null)
        {
            NarrativeManager.Instance.HandleOptionSelected(nextLine);
        }
    }
}

/// <summary>
/// Wrapper class to track dialogue options with their enabled state
/// </summary>
public class DialogueOptionWithState
{
    public DialogueOption Option { get; private set; }
    public bool IsEnabled { get; private set; }
    public string DisableReason { get; private set; }

    public DialogueOptionWithState(DialogueOption option, bool isEnabled, string disableReason = "")
    {
        Option = option;
        IsEnabled = isEnabled;
        DisableReason = disableReason;
    }
}
