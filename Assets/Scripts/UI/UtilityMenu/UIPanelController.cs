using UnityEngine;
using TMPro;

public class UIPanelController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject panel; // The UI panel to show/hide
    [SerializeField] private TextMeshProUGUI textElement; // The TextMeshPro element to update

    /// <summary>
    /// Displays the panel and updates its text.
    /// </summary>
    /// <param name="message">The message to display on the panel.</param>
    public void ShowPanel(string message)
    {
        if (panel != null)
        {
            panel.SetActive(true); // Activate the panel
        }
        else
        {
            Debug.LogWarning("Panel GameObject is not assigned.");
        }

        if (textElement != null)
        {
            textElement.text = message; // Set the text
        }
        else
        {
            Debug.LogWarning("TextMeshProUGUI component is not assigned.");
        }
    }

    /// <summary>
    /// Hides the panel.
    /// </summary>
    public void HidePanel()
    {
        if (panel != null)
        {
            panel.SetActive(false); // Deactivate the panel
        }
        else
        {
            Debug.LogWarning("Panel GameObject is not assigned.");
        }
    }
}
