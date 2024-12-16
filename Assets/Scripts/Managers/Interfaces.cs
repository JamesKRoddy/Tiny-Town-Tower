using UnityEngine;

public interface IControllerInput
{
    public void SetPlayerControlType(PlayerControlType controlType);
}

public interface IPickupableItem
{
    public string GetItemName();
    public string GetItemDescription();
    public Sprite GetItemImage();
}

public interface IInteractiveBase
{
    bool CanInteract();
    string GetInteractionText();
    object Interact();
}

public interface IInteractive<out T> : IInteractiveBase
{
    new T Interact(); // Hides the base Interact method with a strongly-typed version
}

/*
 * 
 * 
 * 
 * TODO set out tasks for settler NPCs
 * TODO Construction sites for buildings
 * TODO Building functionality
 * TODO start looking at levels to spawn in enemies
 * TODO set up screen for merchants
 * 
 * 
 * 
 * 
 */