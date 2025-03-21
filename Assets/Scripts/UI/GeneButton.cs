using UnityEngine;
using UnityEngine.UI;

public class GeneButton : MonoBehaviour
{
    [SerializeField] private Button geneButton;
    [SerializeField] private GeneticMutationUI geneticMutationMenu;

    private void Awake()
    {
        if (geneButton == null)
        {
            geneButton = GetComponent<Button>();
            if (geneButton == null)
            {
                Debug.LogError("GeneButton: No Button component found!");
                return;
            }
        }

        if (geneticMutationMenu == null)
        {
            geneticMutationMenu = FindFirstObjectByType<GeneticMutationUI>();
            if (geneticMutationMenu == null)
            {
                Debug.LogError("GeneButton: No GeneticMutationUI found in scene!");
                return;
            }
        }

        // Add click listener
        geneButton.onClick.AddListener(OnGeneButtonClick);
    }

    private void OnGeneButtonClick()
    {
        // Enable the genetic mutation menu
        if (geneticMutationMenu != null)
        {
            geneticMutationMenu.SetScreenActive(true);
            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);
        }
    }

    private void OnDestroy()
    {
        if (geneButton != null)
        {
            geneButton.onClick.RemoveListener(OnGeneButtonClick);
        }
    }
} 