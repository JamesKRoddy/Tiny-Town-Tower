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
 * Have to refactor the weapon inventory system because its stupid, get rid of the weapon class on the prefab and just use scrptable obj, have it spawn in on the players hand correctly
 * Swap player Controlls over to another NPC
 * Create prefab of dash stuff and weapon point
 * On PlayerSwitchMenu use image as a render texture that follows the currently selected player
 * 
 * 
 * AFTER BREAK!!!!!!!!
 * Issue after swapping npcs, old npc wont go back to wander
 * Have to create an event for the swap its too messy with references at the moment
 * Closing out of settler menu how to properly reset the player controls to in game
 * After swapping a player and when equipping a weapon the asset is enabled on the original player
 *  
 */