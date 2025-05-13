using UnityEngine;
using Managers;

public class DirtPile : MonoBehaviour
{
    [SerializeField] private float cleanTime = 5f;
    [SerializeField] private float currentCleanProgress = 0f;
    private bool isBeingCleaned = false;

    public void StartCleaning()
    {
        isBeingCleaned = true;
    }

    public void StopCleaning()
    {
        isBeingCleaned = false;
    }

    public void AddCleanProgress(float progress)
    {
        if (!isBeingCleaned) return;

        currentCleanProgress += progress;
        if (currentCleanProgress >= cleanTime)
        {
            CompleteCleaning();
        }
    }

    private void CompleteCleaning()
    {
        CampManager.Instance.CleanlinessManager.HandleDirtPileCleaned(this);
        Destroy(gameObject);
    }

    public float GetCleanProgress()
    {
        return currentCleanProgress / cleanTime;
    }

    public bool IsBeingCleaned()
    {
        return isBeingCleaned;
    }
} 