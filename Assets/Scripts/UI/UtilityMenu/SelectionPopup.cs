using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;

public class SelectionPopup : PreviewPopupBase<object, string>
{
    [System.Serializable]
    public class SelectionOption
    {
        public string optionName;
        public Action onSelected;
        public Func<bool> canSelect;
    }

    [Header("Selection Options")]
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private Text[] optionButtonTexts;

    private List<SelectionOption> currentOptions = new List<SelectionOption>();

    protected override void Start()
    {
        base.Start();
    }

    public void Setup(List<SelectionOption> options, GameObject element)
    {
        currentOptions = options;
        selectedElement = element;
        
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
        // Update each button based on available options
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < currentOptions.Count)
            {
                SelectionOption option = currentOptions[i];
                optionButtons[i].gameObject.SetActive(true);
                optionButtonTexts[i].text = option.optionName;
                
                // Set button interactable based on canSelect function
                optionButtons[i].interactable = option.canSelect?.Invoke() ?? true;
                
                // Add listener for this specific option
                int optionIndex = i; // Capture the index for the closure
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(optionIndex));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
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
                EventSystem.current.SetSelectedGameObject(button.gameObject);
                break;
            }
        }
    }
} 