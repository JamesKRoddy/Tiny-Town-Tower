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
 * TODO 
 * set out tasks for settler NPCs
 * Flesh out construction sites for buildings
 * Building functionality
 * start looking at levels to spawn in enemies
 * set up screen for merchants
 * Create a screen to assign a settler to a work task, in this screen show all settlers and what their current task is
 * Theres a bug where it will place the building soon as the sreen opens!!!!!!!!
 * 
 * 
 */