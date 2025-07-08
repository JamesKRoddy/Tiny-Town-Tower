using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;
using TMPro;
using System.Collections;
using Managers;

public class SelectionPopup : PreviewPopupBase<object, string>
{
    [System.Serializable]
    public class SelectionOption
    {
        public string optionName;
        public Action onSelected;
        public Func<bool> canSelect;
        public WorkTask workTask; // Reference to the work task for tooltips
        public string customTooltip; // Custom tooltip text (overrides workTask tooltip if set)
    }

    [Header("Selection Options")]
    [SerializeField] private Button[] optionButtons;

    private List<SelectionOption> currentOptions = new List<SelectionOption>();
    private Action onClose;
    private int currentSelectedIndex = -1;

    protected override void Start()
    {
        base.Start();
        closeButton.onClick.AddListener(ReturnToGame);
    }

    public void Setup(List<SelectionOption> options, GameObject element, Action onClose = null)
    {
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);

        currentOptions = options;
        selectedElement = element;
        this.onClose = onClose;
        
        // Disable all buttons in the parent UI except popup buttons
        SetParentUIButtonsInteractable(false);

        // Show popup
        gameObject.SetActive(true);

        // Update buttons and set initial selection
        UpdateOptionButtons();
        SetupInitialSelection();
    }

    private void UpdateOptionButtons()
    {
        if (currentOptions.Count > optionButtons.Length)
        {
            Debug.LogError($"SelectionPopup: Not enough buttons for all options! Need {currentOptions.Count} but only have {optionButtons.Length}");
            return;
        }

        // Update each button based on available options
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < currentOptions.Count)
            {
                SelectionOption option = currentOptions[i];
                Button button = optionButtons[i];
                button.gameObject.SetActive(true);
                
                // Get the text component from the button's child
                TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    buttonText.text = option.optionName;
                }
                
                // Set button interactable based on canSelect function
                button.interactable = option.canSelect?.Invoke() ?? true;
                
                // Add listener for this specific option
                int optionIndex = i; // Capture the index for the closure
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnOptionSelected(optionIndex));

                // Add event triggers for tooltip
                EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = button.gameObject.AddComponent<EventTrigger>();
                }
                trigger.triggers.Clear();

                // Add pointer enter event
                EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enterEntry.callback.AddListener((data) => ShowTooltip(optionIndex));
                trigger.triggers.Add(enterEntry);

                // Add pointer exit event
                EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exitEntry.callback.AddListener((data) => HideTooltip());
                trigger.triggers.Add(exitEntry);

                // Add select event for controller
                EventTrigger.Entry selectEntry = new EventTrigger.Entry { eventID = EventTriggerType.Select };
                selectEntry.callback.AddListener((data) => ShowTooltip(optionIndex));
                trigger.triggers.Add(selectEntry);

                // Add deselect event for controller
                EventTrigger.Entry deselectEntry = new EventTrigger.Entry { eventID = EventTriggerType.Deselect };
                deselectEntry.callback.AddListener((data) => HideTooltip());
                trigger.triggers.Add(deselectEntry);
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    protected override void ShowTooltip(int optionIndex)
    {
        if (optionIndex >= 0 && optionIndex < currentOptions.Count)
        {
            var option = currentOptions[optionIndex];
            string tooltipText = null;
            
            // Use custom tooltip if set, otherwise use workTask tooltip
            if (!string.IsNullOrEmpty(option.customTooltip))
            {
                tooltipText = option.customTooltip;
            }
            else if (option.workTask != null)
            {
                tooltipText = option.workTask.GetTooltipText();
            }
            
            if (!string.IsNullOrEmpty(tooltipText))
            {
                this.tooltipText.text = tooltipText;
                tooltip.SetActive(true);
                currentSelectedIndex = optionIndex;
            }
        }
    }

    protected override void HideTooltip()
    {
        base.HideTooltip();
        currentSelectedIndex = -1;
    }

    private void OnOptionSelected(int optionIndex)
    {
        if (optionIndex >= 0 && optionIndex < currentOptions.Count)
        {
            currentOptions[optionIndex].onSelected?.Invoke();
        }
        OnCloseClicked();
    }

    protected override void SetupInitialSelection()
    {
        // Find the first interactable button and select it
        foreach (var button in optionButtons)
        {
            if (button.gameObject.activeSelf && button.interactable)
            {
                StartCoroutine(SetSelectedAfterDelay(button.gameObject));
                break;
            }
        }
    }

    private IEnumerator SetSelectedAfterDelay(GameObject button)
    {
        yield return new WaitForSeconds(0.5f); // Wait for 0.5 seconds before selecting the button
        PlayerUIManager.Instance.SetSelectedGameObject(button);
    }

    void ReturnToGame()
    {
        PlayerInput.Instance.UpdatePlayerControls(GameManager.Instance.PlayerGameControlType());
    }

    public override void OnCloseClicked()
    {
        base.OnCloseClicked();
        onClose?.Invoke();
    }
} 