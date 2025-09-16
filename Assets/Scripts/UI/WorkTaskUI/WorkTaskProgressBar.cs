using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component to display work task progress as a world space progress bar
/// </summary>
public class WorkTaskProgressBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI taskNameText;
    [SerializeField] private TextMeshProUGUI progressText;
    
    // CanvasGroup will be created at runtime since this is instantiated dynamically
    private CanvasGroup canvasGroup;

    [Header("Visual Settings")]
    [SerializeField] private Color normalProgressColor = Color.green;
    [SerializeField] private Color pausedProgressColor = Color.yellow;
    [SerializeField] private Color errorProgressColor = Color.red;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private Vector3 offsetFromTask = Vector3.up * 2f;

    private WorkTask associatedTask;
    private Camera mainCamera;
    private bool isVisible = false;
    private Coroutine fadeCoroutine;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    public void Initialize(WorkTask task)
    {
        associatedTask = task;
        
        // Create CanvasGroup component if it doesn't exist
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // Initialize as invisible
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        
        if (taskNameText != null)
        {
            taskNameText.text = GetFriendlyTaskName(task);
        }
        
        if (progressSlider != null)
        {
            progressSlider.maxValue = 1f;
            progressSlider.value = 0f;
        }
        
        UpdateProgressVisual(0f, WorkTaskProgressState.Normal);
    }

    private string GetFriendlyTaskName(WorkTask task)
    {
        string taskType = task.GetType().Name;
        
        // Convert technical names to user-friendly names
        switch (taskType)
        {
            case "ResourceUpgradeTask":
                if (task is ResourceUpgradeTask upgradeTask && upgradeTask.currentUpgrade != null)
                {
                    var outputResources = upgradeTask.currentUpgrade.outputResources;
                    if (outputResources != null && outputResources.Length > 0 && outputResources[0].resourceScriptableObj != null)
                        return $"Upgrading {outputResources[0].resourceScriptableObj.objectName}";
                }
                return "Upgrading Resources";
            
            case "CookingTask":
                if (task is CookingTask cookingTask && cookingTask.currentRecipe != null)
                {
                    var outputResources = cookingTask.currentRecipe.outputResources;
                    if (outputResources != null && outputResources.Length > 0 && outputResources[0].resourceScriptableObj != null)
                        return $"Cooking {outputResources[0].resourceScriptableObj.objectName}";
                }
                return "Cooking";
            
            case "ResearchTask":
                if (task is ResearchTask researchTask && researchTask.CurrentResearch != null)
                    return $"Researching {researchTask.CurrentResearch.objectName}";
                return "Researching";
            
            default:
                // Remove "Task" suffix and add spaces before capital letters
                return System.Text.RegularExpressions.Regex.Replace(
                    taskType.Replace("Task", ""), 
                    "([a-z])([A-Z])", 
                    "$1 $2"
                );
        }
    }

    public void UpdateProgress(float progress, WorkTaskProgressState state = WorkTaskProgressState.Normal)
    {
        if (progressSlider != null)
        {
            progressSlider.value = Mathf.Clamp01(progress);
        }
        
        if (progressText != null)
        {
            progressText.text = $"{(progress * 100f):F0}%";
        }
        
        UpdateProgressVisual(progress, state);
    }

    private void UpdateProgressVisual(float progress, WorkTaskProgressState state)
    {
        if (progressFill != null)
        {
            Color targetColor = state switch
            {
                WorkTaskProgressState.Paused => pausedProgressColor,
                WorkTaskProgressState.Error => errorProgressColor,
                _ => normalProgressColor
            };
            
            progressFill.color = targetColor;
        }
    }

    public void Show()
    {
        if (isVisible) return;
        
        Debug.Log($"[WorkTaskProgressBar] Showing progress bar for {associatedTask?.name}");
        
        isVisible = true;
        gameObject.SetActive(true);
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeIn());
    }

    public void Hide()
    {
        if (!isVisible) return;
        
        isVisible = false;
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeOut());
    }

    private System.Collections.IEnumerator FadeIn()
    {
        if (canvasGroup == null) 
        {
            Debug.LogWarning("[WorkTaskProgressBar] CanvasGroup is null, cannot fade in");
            yield break;
        }
        
        Debug.Log($"[WorkTaskProgressBar] Starting fade in for {associatedTask?.name}");
        
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsedTime / fadeInDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        fadeCoroutine = null;
        
        Debug.Log($"[WorkTaskProgressBar] Fade in complete for {associatedTask?.name}");
    }

    private System.Collections.IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;
        
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        fadeCoroutine = null;
    }

    private void Update()
    {
        if (associatedTask != null && mainCamera != null)
        {
            // Position the progress bar above the task
            Vector3 worldPos = associatedTask.transform.position + offsetFromTask;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            
            // Only show if the position is in front of the camera
            if (screenPos.z > 0)
            {
                transform.position = screenPos;
                
                // Optional: Hide if too far from camera or outside screen bounds
                float distanceToCamera = Vector3.Distance(mainCamera.transform.position, associatedTask.transform.position);
                bool shouldBeVisible = distanceToCamera < 50f && 
                                     screenPos.x > -100f && screenPos.x < Screen.width + 100f &&
                                     screenPos.y > -100f && screenPos.y < Screen.height + 100f;
                
                if (canvasGroup != null)
                {
                    // Only override alpha if we're not currently fading out
                    if (fadeCoroutine == null)
                    {
                        canvasGroup.alpha = shouldBeVisible ? (isVisible ? 1f : 0f) : 0f;
                    }
                    else if (!shouldBeVisible)
                    {
                        // If we're fading but shouldn't be visible, hide immediately
                        canvasGroup.alpha = 0f;
                    }
                }
            }
            else
            {
                // Task is behind the camera, hide the progress bar
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
    }
}

