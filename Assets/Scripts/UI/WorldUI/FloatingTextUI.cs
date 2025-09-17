using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Simple floating text UI component for world space UI elements
/// Works with existing PlayerUIManager canvas system
/// </summary>
public class FloatingTextUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI textComponent;
    [SerializeField] private Image backgroundImage;
    
    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color errorColor = Color.red;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private Vector3 offsetFromTarget = Vector3.up * 2f;
    [SerializeField] private bool followTarget = true;
    [SerializeField] private bool fadeOutAfterDuration = true;
    
    [Header("Animation Settings")]
    [SerializeField] private bool animateUpward = true;
    [SerializeField] private float upwardSpeed = 1f;
    [SerializeField] private bool scaleAnimation = true;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1f);
    
    private Transform targetTransform;
    private Camera mainCamera;
    private bool isVisible = false;
    private Coroutine fadeCoroutine;
    private Coroutine displayCoroutine;
    private Vector3 startPosition;
    private float startTime;
    private CanvasGroup canvasGroup;
    
    private void Start()
    {
        mainCamera = Camera.main;
        
        // Create CanvasGroup if it doesn't exist
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
    }
    
    public void Initialize(Transform target, string text, FloatingTextType textType = FloatingTextType.Normal)
    {
        targetTransform = target;
        startPosition = target.position + offsetFromTarget;
        
        if (textComponent != null)
        {
            textComponent.text = text;
            textComponent.color = GetColorForType(textType);
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.color = GetBackgroundColorForType(textType);
        }
        
        // Reset scale for animation
        if (scaleAnimation)
        {
            transform.localScale = Vector3.zero;
        }
        
        Show();
    }
    
    public void Show()
    {
        if (isVisible) return;
        
        isVisible = true;
        gameObject.SetActive(true);
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeIn());
        
        if (fadeOutAfterDuration)
        {
            if (displayCoroutine != null)
            {
                StopCoroutine(displayCoroutine);
            }
            displayCoroutine = StartCoroutine(AutoHideAfterDuration());
        }
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
    
    private Color GetColorForType(FloatingTextType textType)
    {
        return textType switch
        {
            FloatingTextType.Warning => warningColor,
            FloatingTextType.Error => errorColor,
            FloatingTextType.Success => successColor,
            _ => normalColor
        };
    }
    
    private Color GetBackgroundColorForType(FloatingTextType textType)
    {
        Color baseColor = GetColorForType(textType);
        return new Color(baseColor.r, baseColor.g, baseColor.b, 0.3f);
    }
    
    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;
        
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        startTime = Time.time;
        
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeInDuration;
            
            // Fade alpha
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, progress);
            
            // Scale animation
            if (scaleAnimation)
            {
                float scaleValue = scaleCurve.Evaluate(progress);
                transform.localScale = Vector3.one * scaleValue;
            }
            
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        if (scaleAnimation)
        {
            transform.localScale = Vector3.one;
        }
        fadeCoroutine = null;
    }
    
    private IEnumerator FadeOut()
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
    
    private IEnumerator AutoHideAfterDuration()
    {
        yield return new WaitForSeconds(displayDuration);
        Hide();
    }
    
    private void Update()
    {
        if (targetTransform != null && mainCamera != null && followTarget)
        {
            // Position the floating text above the target
            Vector3 worldPos = targetTransform.position + offsetFromTarget;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            
            // Only show if the position is in front of the camera
            if (screenPos.z > 0)
            {
                transform.position = screenPos;
                
                // Optional: Hide if too far from camera or outside screen bounds
                float distanceToCamera = Vector3.Distance(mainCamera.transform.position, targetTransform.position);
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
                // Target is behind the camera, hide the floating text
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
            }
        }
        
        // Animate upward movement
        if (animateUpward && isVisible)
        {
            float elapsedTime = Time.time - startTime;
            Vector3 currentOffset = offsetFromTarget + Vector3.up * (elapsedTime * upwardSpeed);
            
            if (targetTransform != null && mainCamera != null)
            {
                Vector3 worldPos = targetTransform.position + currentOffset;
                Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
                
                if (screenPos.z > 0)
                {
                    transform.position = screenPos;
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
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }
    }
}

