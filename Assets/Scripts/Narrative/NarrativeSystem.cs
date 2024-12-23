using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class NarrativeSystem : MenuBase
{
    private static NarrativeSystem _instance;

    public static NarrativeSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<NarrativeSystem>();
                if (_instance == null)
                {
                    Debug.LogWarning("NarrativeSystem instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject optionsContainer;
    [SerializeField] private GameObject optionPrefab;

    private GameObject dialoguePanel;
    private DialogueData currentDialogue;
    private Dictionary<string, DialogueLine> dialogueLinesMap;

    private PlayerControlType returnToControls; //Used for when the menu is closed which controlls are gonna be used

    private void Awake()
    {
        Setup();
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

        dialoguePanel = gameObject; //TODO can i get rid of dialoguePanel?
        dialoguePanel.SetActive(false);
    }

    public void StartConversation(NarrativeAsset narrativeAsset)
    {
        if (currentDialogue == null)
        {
            LoadDialogue(narrativeAsset.dialogueFile);
        }

        if (currentDialogue != null && currentDialogue.lines.Count > 0)
        {
            dialoguePanel.SetActive(true);
            ShowDialogue(currentDialogue.lines[0]);
        }
    }

    public void LoadDialogue(TextAsset dialogueFile)
    {
        if (dialogueFile != null)
        {
            string json = dialogueFile.text;
            currentDialogue = JsonUtility.FromJson<DialogueData>(json);

            dialogueLinesMap = new Dictionary<string, DialogueLine>();
            foreach (var line in currentDialogue.lines)
            {
                dialogueLinesMap[line.id] = line; // Use 'id' as the key
            }
        }
        else
        {
            Debug.LogError("Dialogue TextAsset is null!");
        }
    }

    public void ShowDialogue(DialogueLine line)
    {
        npcNameText.text = line.speaker;
        dialogueText.text = line.text;

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
                optionButton.onClick.AddListener(() => HandleOptionSelected(option.nextLine));

                if (firstOption == null)
                {
                    firstOption = optionObj;
                }
            }

            StartCoroutine(SetSelectedButton(firstOption));
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

            StartCoroutine(SetSelectedButton(closeButton));
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

            StartCoroutine(SetSelectedButton(continueButton));
        }
        else
        {
            // End conversation if no next line and no options
            EndConversation();
        }
    }

    private IEnumerator SetSelectedButton(GameObject button)
    {
        yield return new WaitForEndOfFrame();
        if (button != null)
        {
            EventSystem.current.SetSelectedGameObject(button);
        }
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
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.COMBAT_MOVEMENT); //TODO figure out which controls to go back to combat or camp movement, TEST IF this works
        dialoguePanel.SetActive(false);
    }

    private IEnumerator EndAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndConversation();
    }

    public override void SetScreenActive(bool active, float delay = 0.0f)
    {
        if (active)
        {
            PlayerControlType controlType = PlayerInput.Instance.currentControlType;

            if (controlType == PlayerControlType.CAMP_MOVEMENT || controlType == PlayerControlType.COMBAT_MOVEMENT)
                returnToControls = controlType;
        }
        else
        {
            if(dialoguePanel.activeInHierarchy == true)
                PlayerInput.Instance.UpdatePlayerControls(returnToControls);
        }
    }
}
