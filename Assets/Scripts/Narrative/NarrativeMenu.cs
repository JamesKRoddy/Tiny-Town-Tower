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
            npcNameText.text = line.speaker;
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
            var filteredOptions = FilterOptionsByFlags(line.options);
            if (filteredOptions.Count > 0)
            {
                ShowOptions(filteredOptions);
                return;
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
    /// Filter dialogue options based on component's progression flags
    /// </summary>
    private List<DialogueOption> FilterOptionsByFlags(List<DialogueOption> options)
    {
        if (NarrativeManager.Instance == null)
            return options;

        return options.Where(option => 
        {
            // Check required flags
            if (option.requiredFlags != null && option.requiredFlags.Count > 0)
            {
                foreach (string flag in option.requiredFlags)
                {
                    if (!NarrativeManager.Instance.HasFlag(flag))
                    {
                        return false;
                    }
                }
            }

            // Check blocked flags
            if (option.blockedByFlags != null && option.blockedByFlags.Count > 0)
            {
                foreach (string flag in option.blockedByFlags)
                {
                    if (NarrativeManager.Instance.HasFlag(flag))
                    {
                        return false;
                    }
                }
            }

            return true;
        }).ToList();
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
    private void ShowOptions(List<DialogueOption> options)
    {
        GameObject firstOption = null;
        
        foreach (var option in options)
        {
            GameObject optionObj = CreateOptionButton(option.text, () => HandleOptionSelected(option));

            if (firstOption == null)
            {
                firstOption = optionObj;
            }
        }

        SetSelectedButton(firstOption);
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
    private GameObject CreateOptionButton(string text, System.Action onClickAction)
    {
        GameObject optionObj = Instantiate(optionPrefab, optionsContainer.transform);
        TextMeshProUGUI optionText = optionObj.GetComponentInChildren<TextMeshProUGUI>();
        optionText.text = text;

        // Add click event
        Button optionButton = optionObj.GetComponent<Button>();
        optionButton.onClick.AddListener(() => onClickAction?.Invoke());

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
