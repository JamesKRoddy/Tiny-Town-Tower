using UnityEngine;
using System.Collections;

/// <summary>
/// Component for animating damage area visual effects
/// </summary>
public class DamageAreaVisual : MonoBehaviour
{
    private Vector3 startScale;
    private float duration;

    public void Setup(float visualDuration)
    {
        duration = visualDuration;
        startScale = transform.localScale;
        
        // Start with small scale
        transform.localScale = new Vector3(0f, startScale.y, 0f);
        
        // Animate to full scale
        StartCoroutine(ScaleAnimation());
    }

    private IEnumerator ScaleAnimation()
    {
        float elapsedTime = 0f;
        Vector3 targetScale = startScale;

        while (elapsedTime < duration * 0.3f) // Scale up over first 30% of duration
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (duration * 0.3f);
            
            // Use smooth step for more natural scaling
            t = t * t * (3f - 2f * t);
            
            transform.localScale = Vector3.Lerp(new Vector3(0f, startScale.y, 0f), targetScale, t);
            yield return null;
        }

        // Ensure final scale is exact
        transform.localScale = targetScale;
    }
}
