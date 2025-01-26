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
 * Swap player Controlls over to another NPC
 * Create prefab of dash stuff and weapon point
 * On PlayerSwitchMenu use image as a render texture that follows the currently selected player
 * When the error message is displayed on the building menu for not enough resources, we can still select different buttons
 * Have to create an event for the swap its too messy with references at the moment
 * Closing out of settler menu how to properly reset the player controls to in game
 * Setup zombie idle animations
 * Zombie death animations
 * Cleanup and moving onto new rooms
 * Look at implementing difficulty system in roguelike controlled by buildings and floors
 * survivour infection level, once filled they turn harming other npcs, drugs found during the day can reduce infection, slow it down or cure them
 * 
 * 
 * CURRENT  WORK TURRETS
 * 
 * 
 * 
 * 
 * Next Large things to move onto
 * Genetic mutations
 * Over world
 *  
 */