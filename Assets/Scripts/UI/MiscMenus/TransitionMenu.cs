using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Managers;

public class TransitionMenu : MonoBehaviour
{
    [SerializeField] private Image blackOverlay;
    [SerializeField] private float transitionDuration = 1f;
    
    private void Start()
    {
        // Ensure the black overlay is fully transparent at start
        if (blackOverlay != null)
        {
            Color color = blackOverlay.color;
            color.a = 0f;
            blackOverlay.color = color;
            blackOverlay.gameObject.SetActive(false);
        }
    }

    public IEnumerator FadeIn()
    {
        Debug.Log("FadeIn");
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.TRANSITION);
        blackOverlay.gameObject.SetActive(true);
        yield return FadeImage(blackOverlay, 0f, 1f, transitionDuration);
    }

    public IEnumerator FadeOut()
    {
        Debug.Log("FadeOut");
        yield return FadeImage(blackOverlay, 1f, 0f, transitionDuration);
        blackOverlay.gameObject.SetActive(false);
        PlayerInput.Instance.UpdatePlayerControls(GameManager.Instance.PlayerGameControlType());
    }

    private IEnumerator FadeImage(Image image, float startAlpha, float targetAlpha, float duration)
    {
        float elapsedTime = 0f;
        Color color = image.color;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
            color.a = alpha;
            image.color = color;
            yield return null;
        }

        // Ensure we end up exactly at the target alpha
        color.a = targetAlpha;
        image.color = color;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
