using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System;
using Managers;

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

        // If the line has options, show them
        if (line.options != null && line.options.Count > 0)
        {
            ShowOptions(line.options);
            return;
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
