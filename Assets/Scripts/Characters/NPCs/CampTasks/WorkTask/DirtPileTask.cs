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

    public override bool DoWork(HumanCharacterController worker, float deltaTime)
    {
        // Check if worker is close enough to the dirt pile
        float distanceToDirtPile = Vector3.Distance(worker.transform.position, transform.position);
        if (distanceToDirtPile > CLEANING_DISTANCE)
        {
            // Worker needs to get closer, don't advance work yet but continue trying
            return true;
        }

        // Call base DoWork to handle electricity and progress
        bool canContinue = base.DoWork(worker, deltaTime);
        
        // Check if cleaning is complete (base DoWork will handle completion)
        if (!canContinue && workProgress >= baseWorkTime)
        {
            CompleteCleaning();
        }
        
        return canContinue;
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

    protected override void OnDestroy()
    {
        base.OnDestroy(); // Call base class cleanup
        
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