using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI menu displayed during game start/restart sequence
/// Shows the computer boot sequence with a brief description
/// </summary>
public class GameStartMenu : MenuBase
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button continueButton;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Text Content")]
    [SerializeField] private string newGameTitle = "SYSTEM INITIALIZATION";
    [SerializeField] private string newGameDescription = "Booting up... Survivors detected.\nPowering on camp systems...";
    [SerializeField] private string restartTitle = "SYSTEM REBOOT";
    [SerializeField] private string restartDescription = "Critical failure detected.\nRebooting... New survivors inbound.";
    
    [Header("Loading Animation")]
    [SerializeField] private float dotAnimationSpeed = 0.5f; // Time between dot changes
    
    // Button press tracking
    private bool buttonPressed = false;

    // Events
    public event System.Action OnContinuePressed;

    // Animation state
    private Coroutine dotAnimationCoroutine;
    private string baseDescription; // Description without animated dots

    private void Awake()
    {
        // Ensure canvas group exists
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Hook up button listener
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonPressed);
            continueButton.gameObject.SetActive(false); // Start hidden
        }

        // Start with menu invisible
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        
        // Stop animation if running
        StopDotAnimation();
        
        // Clean up button listener
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueButtonPressed);
        }
    }

    public override void SetScreenActive(bool active, float delay = 0f, System.Action onDone = null)
    {
        if (active)
        {
            gameObject.SetActive(true);
            
            // Set text when showing
            bool hasExistingSave = SaveLoadManager.Instance?.HasSaveFile() ?? false;
            if (hasExistingSave)
            {
                SetRestartText();
            }
            else
            {
                SetNewGameText();
            }

            // Update player controls to IN_MENU
            if (PlayerInput.Instance != null)
            {
                PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);
            }

            // Fade in
            StartCoroutine(FadeIn(onDone));
        }
        else
        {
            // Stop dot animation when closing
            StopDotAnimation();
            
            // Fade out then deactivate
            StartCoroutine(FadeOut(() => {
                gameObject.SetActive(false);
                onDone?.Invoke();
            }));
        }
    }

    private System.Collections.IEnumerator FadeIn(System.Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            }
            yield return null;
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
        onComplete?.Invoke();
    }

    private System.Collections.IEnumerator FadeOut(System.Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            }
            yield return null;
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        onComplete?.Invoke();
    }

    private void SetNewGameText()
    {
        if (titleText != null)
            titleText.text = newGameTitle;
        if (descriptionText != null)
        {
            baseDescription = newGameDescription;
            descriptionText.text = newGameDescription;
            StartDotAnimation();
        }
    }

    private void SetRestartText()
    {
        if (titleText != null)
            titleText.text = restartTitle;
        if (descriptionText != null)
        {
            baseDescription = restartDescription;
            descriptionText.text = restartDescription;
            StartDotAnimation();
        }
    }

    /// <summary>
    /// Update the description text during the sequence
    /// </summary>
    public void UpdateDescription(string newDescription, bool animateDots = false)
    {
        if (descriptionText != null)
        {
            baseDescription = newDescription;
            descriptionText.text = newDescription;
            
            if (animateDots)
            {
                StartDotAnimation();
            }
            else
            {
                StopDotAnimation();
            }
        }
    }

    /// <summary>
    /// Update the title text
    /// </summary>
    public void UpdateTitle(string newTitle)
    {
        if (titleText != null)
        {
            titleText.text = newTitle;
        }
    }

    public override void SetPlayerControls(PlayerControlType controlType)
    {
        // No controls needed - this is a passive display menu
    }

    protected override void BackPressed()
    {
        // Cannot back out of game start sequence
    }

    /// <summary>
    /// Show the continue button
    /// </summary>
    public void ShowContinueButton()
    {
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Hide the continue button
    /// </summary>
    public void HideContinueButton()
    {
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Wait for the player to press the continue button
    /// </summary>
    public System.Collections.IEnumerator WaitForButtonPress()
    {
        buttonPressed = false;
        ShowContinueButton();
        
        // Wait until button is pressed
        while (!buttonPressed)
        {
            yield return null;
        }
        
        HideContinueButton();
    }

    /// <summary>
    /// Called when the continue button is pressed
    /// </summary>
    private void OnContinueButtonPressed()
    {
        buttonPressed = true;
        
        // Fire event to notify systems that game can start
        OnContinuePressed?.Invoke();
        
        // Play button sound if available
        // AudioManager.Instance?.PlayButtonClick();
    }

    #region Dot Animation

    /// <summary>
    /// Start the animated dot effect (. .. ...)
    /// </summary>
    private void StartDotAnimation()
    {
        StopDotAnimation(); // Stop any existing animation
        dotAnimationCoroutine = StartCoroutine(AnimateDotsCoroutine());
    }

    /// <summary>
    /// Stop the animated dot effect
    /// </summary>
    private void StopDotAnimation()
    {
        if (dotAnimationCoroutine != null)
        {
            StopCoroutine(dotAnimationCoroutine);
            dotAnimationCoroutine = null;
        }
    }

    /// <summary>
    /// Coroutine that animates dots at the end of the description
    /// Cycles through ".", "..", "..."
    /// </summary>
    private System.Collections.IEnumerator AnimateDotsCoroutine()
    {
        if (string.IsNullOrEmpty(baseDescription)) yield break;

        // Find the last occurrence of "..." in the base description
        string textWithoutDots = baseDescription;
        int lastDotIndex = baseDescription.LastIndexOf("...");
        
        if (lastDotIndex >= 0)
        {
            // Remove the trailing dots to get the base text
            textWithoutDots = baseDescription.Substring(0, lastDotIndex);
        }
        else
        {
            // Try to find single dots or double dots
            lastDotIndex = baseDescription.LastIndexOf("..");
            if (lastDotIndex >= 0)
            {
                textWithoutDots = baseDescription.Substring(0, lastDotIndex);
            }
            else
            {
                lastDotIndex = baseDescription.LastIndexOf(".");
                if (lastDotIndex >= 0)
                {
                    textWithoutDots = baseDescription.Substring(0, lastDotIndex);
                }
            }
        }

        int dotCount = 1;
        WaitForSeconds wait = new WaitForSeconds(dotAnimationSpeed);

        while (true)
        {
            // Build the animated text with the current number of dots
            string dots = new string('.', dotCount);
            if (descriptionText != null)
            {
                descriptionText.text = textWithoutDots + dots;
            }

            // Cycle through 1, 2, 3 dots
            dotCount++;
            if (dotCount > 3)
            {
                dotCount = 1;
            }

            yield return wait;
        }
    }

    #endregion
}

