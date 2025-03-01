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

public interface IPossessable
{
    void OnPossess();
    void OnUnpossess();
    void PossessedUpdate();
    void Movement(Vector3 movement);
    void Attack();
    void Dash();
    WeaponScriptableObj GetEquipped();
    void EquipWeapon(WeaponScriptableObj weapon);
    Transform GetTransform();
}

public interface IDamageable
{
    [SerializeField] public float Health { get; set; } // Property for current health
    [SerializeField] public float MaxHealth { get; set; } // Property for max health

    void TakeDamage(float amount); // Method to handle damage
    void Heal(float amount);       // Optional: Method to handle healing
    void Die();
}


/*
 * 
 * 
 * 
 * TODO 
 * set out tasks for settler NPCs
 * Flesh out construction sites for buildings
 * Building functionality
 * set up screen for merchants
 * On PlayerSwitchMenu use image as a render texture that follows the currently selected player
 * Setup zombie idle animations
 * Zombie death animations
 * Look at implementing difficulty system in roguelike controlled by buildings and floors
 * survivour infection level, once filled they turn harming other npcs, drugs found during the day can reduce infection, slow it down or cure them
 * Have to implement a system to move around buildings/turrets
 * Change the possession screen so that it opens up a selection box to talkt to them (quests) assign them work or possess them
 * 
 * 
 * CURRENT  WORK MENU FLOW
 * From game screen have a pause menu when you can get back to the main screen and clean up
 * camp scene to over world
 * roguelike to over world
 * roguelike to camp scene
 * over world to roguelike
 * camp scene to turret scene
 * turret scene to camp
 * 
 * 
 * Curent work refactoring player
 * rework inventory system CharacterInventory.cs needs to be looked at read info at the top
 * 
 * 
 * 
 * Next Large things to move onto
 * Genetic mutations
 * Over world
 *  
 */