using UnityEngine;
using Managers;
using System.Collections;

public class DirtPileTask : WorkTask, IInteractive<DirtPileTask>
{
    private const float CLEANING_DISTANCE = 1f;
    private const float PLAYER_CLEAN_SPEED = 2f; // Player cleans faster than NPCs

    protected override void Start()
    {
        base.Start();
        baseWorkTime = 5f;
    }

    public void StartCleaning()
    {
        workProgress = 0f;
    }

    protected override IEnumerator WorkCoroutine()
    {
        if (currentWorkers.Count == 0) yield break;

        // Wait until the NPC is close enough to the dirt pile
        while (workProgress == 0f)
        {
            float distanceToDirtPile = Vector3.Distance(currentWorkers[0].transform.position, transform.position);
            if (distanceToDirtPile <= CLEANING_DISTANCE)
            {
                break;
            }
            yield return null;
        }

        // Now start the actual cleaning process
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        CompleteCleaning();
    }

    private void CompleteCleaning()
    {
        // Notify the CleanlinessManager first
        CampManager.Instance.CleanlinessManager.HandleDirtPileCleaned(this);
        
        // Call base CompleteWork to trigger OnTaskCompleted event
        base.CompleteWork();
        
        // Wait one frame to ensure all completion handlers have run
        StartCoroutine(DestroyAfterCompletion());
    }

    private IEnumerator DestroyAfterCompletion()
    {
        yield return null;  // Wait one frame
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Safety check: ensure grid slot is freed if this object is destroyed for any reason
        if (CampManager.Instance != null)
        {
            Vector2Int dirtPileSize = new Vector2Int(1, 1);
            CampManager.Instance.MarkSharedGridSlotsUnoccupied(transform.position, dirtPileSize);
        }
    }

    public override string GetTooltipText()
    {
        if (!showTooltip) return string.Empty;

        string tooltip = "Dirt Pile\n";
        if (IsOccupied)
        {
            tooltip += $"Cleaning Progress: {(GetProgress() * 100):F1}%\n";
            if (workProgress == 0f)
            {
                tooltip += "Waiting for cleaner to arrive...\n";
            }
        }
        else
        {
            tooltip += "Needs cleaning\n";
        }
        return tooltip;
    }

    // IInteractive implementation
    public DirtPileTask Interact()
    {
        if (!CanInteract()) return null;

        // Start cleaning process for player
        StartCleaning();
        StartCoroutine(PlayerCleanCoroutine());
        return this;
    }

    private IEnumerator PlayerCleanCoroutine()
    {
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime * PLAYER_CLEAN_SPEED;
            yield return null;
        }

        CompleteCleaning();
    }

    public bool CanInteract()
    {
        // Can interact if not already being cleaned
        return !IsOccupied;
    }

    public string GetInteractionText()
    {
        if (!CanInteract()) return "Cannot clean - already being cleaned";
        return "Clean dirt pile";
    }

    object IInteractiveBase.Interact()
    {
        return Interact();
    }
} 