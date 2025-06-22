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
        OnEnemySetupStateChanged(RogueLiteManager.Instance.GetEnemySetupState());
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
        if (isLocked) return;

        if (doorType == DoorStatus.ENTRANCE)
        {
            RogueLiteManager.Instance.EnterRoomWithTransition(this);
        }
        else if (doorType == DoorStatus.EXIT)
        {
            RogueLiteManager.Instance.ReturnToPreviousRoom(this);
        }
    }

    public override bool CanInteract()
    {
        if (RogueLiteManager.Instance.GetEnemySetupState() != EnemySetupState.ALL_WAVES_CLEARED)
            return false;

        return base.CanInteract();
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
                return "Can't Go Back";
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