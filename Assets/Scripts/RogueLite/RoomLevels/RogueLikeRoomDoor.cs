using UnityEngine;
using Managers;

public class RogueLikeRoomDoor : RogueLiteDoor
{
    [Header("Target Room")]
    public RogueLiteRoomParent targetRoom;
    public Transform targetSpawnPoint;

    private RogueLiteRoom parentRoom;

    protected override void Start()
    {
        base.Start();
        
        RogueLiteManager.Instance.OnEnemySetupStateChanged += OnEnemySetupStateChanged;
        
        // Check current state immediately in case it already changed before this door was ready
        EnemySetupState currentState = RogueLiteManager.Instance.GetEnemySetupState();
        OnEnemySetupStateChanged(currentState);
    }

    public void Initialize(RogueLiteRoom room)
    {
        parentRoom = room;
    }

    private void OnEnemySetupStateChanged(EnemySetupState state)
    {
        if(state == EnemySetupState.ALL_WAVES_CLEARED)
        {
            ShowDoorEffects();
        }
        else
        {
            HideDoorEffects();
        }
    }

    public override void OnDoorEntered()
    {
        if (isLocked) 
        {
            return;
        }

        if (doorType == DoorStatus.ENTRANCE)
        {
            RogueLiteManager.Instance.EnterRoomWithTransition(this);
        }
        // EXIT doors are locked - no return to previous room functionality
    }

    public override bool CanInteract()
    {
        EnemySetupState currentState = RogueLiteManager.Instance.GetEnemySetupState();
        bool wavesCleared = currentState == EnemySetupState.ALL_WAVES_CLEARED;
        bool baseCanInteract = base.CanInteract();
        bool notExitDoor = doorType != DoorStatus.EXIT;
        
        return wavesCleared && baseCanInteract && notExitDoor;
    }

    public override string GetInteractionText()
    {
        switch (doorType)
        {
            case DoorStatus.LOCKED:
                return "Door Locked";
            case DoorStatus.ENTRANCE:
                return "Enter Room";
            case DoorStatus.EXIT:
                return "Exit Locked";
            default:
                return "INVALID";
        }
    }

    void OnDestroy()
    {
        if (RogueLiteManager.Instance != null)
        {
            RogueLiteManager.Instance.OnEnemySetupStateChanged -= OnEnemySetupStateChanged;
        }
    }
} 