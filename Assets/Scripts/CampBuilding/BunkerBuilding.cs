using UnityEngine;
using System.Collections.Generic;
using Managers;
using System;

/// <summary>
/// A bunker building that allows NPCs to shelter inside during zombie attacks.
/// Provides protection and can be upgraded for increased capacity.
/// </summary>
public class BunkerBuilding : Building
{
    [Header("Bunker Settings")]
    [SerializeField] private int maxCapacity = 5;
    [SerializeField] private bool isOccupied = false;
    [SerializeField] private List<HumanCharacterController> shelteredNPCs = new List<HumanCharacterController>();

    // Events
    public event Action<BunkerBuilding> OnBunkerOccupied;
    public event Action<BunkerBuilding> OnBunkerVacated;

    public int MaxCapacity => maxCapacity;
    public int CurrentOccupancy => shelteredNPCs.Count;
    public bool IsOccupied => isOccupied;
    public bool HasSpace => shelteredNPCs.Count < maxCapacity;

    protected override void Start()
    {
        base.Start();
    }

    public override void SetupBuilding(BuildingScriptableObj buildingScriptableObj)
    {
        base.SetupBuilding(buildingScriptableObj);
    }

    /// <summary>
    /// Attempts to shelter an NPC in the bunker
    /// </summary>
    /// <param name="npc">The NPC to shelter</param>
    /// <returns>True if successfully sheltered, false if no space</returns>
    public bool ShelterNPC(HumanCharacterController npc)
    {
        Debug.Log($"Sheltering NPC {npc.name} in bunker. HasSpace: {HasSpace}, npc: {npc}");
        if (!HasSpace || npc == null)
        {
            return false;
        }

        // Add NPC to sheltered list
        shelteredNPCs.Add(npc);
        
        // Disable the NPC GameObject to make them invisible and untargetable
        npc.gameObject.SetActive(false);
        
        // Notify that bunker is occupied
        if (!isOccupied)
        {
            isOccupied = true;
            OnBunkerOccupied?.Invoke(this);
        }

        Debug.Log($"NPC {npc.name} sheltered in bunker. Occupancy: {shelteredNPCs.Count}/{maxCapacity}");
        return true;
    }

    /// <summary>
    /// Removes an NPC from the bunker
    /// </summary>
    /// <param name="npc">The NPC to remove</param>
    public void RemoveNPC(HumanCharacterController npc)
    {
        if (shelteredNPCs.Remove(npc))
        {
            // Re-enable the NPC GameObject when they leave the bunker
            npc.gameObject.SetActive(true);
            
            Debug.Log($"NPC {npc.name} removed from bunker. Occupancy: {shelteredNPCs.Count}/{maxCapacity}");
            
            // Check if bunker is now empty
            if (shelteredNPCs.Count == 0)
            {
                isOccupied = false;
                OnBunkerVacated?.Invoke(this);
            }
        }
    }

    /// <summary>
    /// Removes all NPCs from the bunker
    /// </summary>
    public void EvacuateAll()
    {
        // Re-enable all NPC GameObjects before clearing the list
        foreach (var npc in shelteredNPCs)
        {
            if (npc != null)
            {
                npc.gameObject.SetActive(true);
            }
        }
        
        shelteredNPCs.Clear();
        isOccupied = false;
        OnBunkerVacated?.Invoke(this);
        Debug.Log("All NPCs evacuated from bunker");
    }

    /// <summary>
    /// Override Die to evacuate NPCs when bunker is destroyed
    /// </summary>
    public override void Die()
    {
        // Evacuate all NPCs before destroying the bunker
        EvacuateAll();
        Debug.LogWarning("Bunker destroyed! All NPCs evacuated.");
        
        // Call base Die method to handle building destruction
        base.Die();
    }

    public override string GetInteractionText()
    {
        string baseText = base.GetInteractionText();
        string bunkerText = $"\nBunker Status:\nCapacity: {shelteredNPCs.Count}/{maxCapacity}\nHealth: {Health:F0}/{MaxHealth:F0}";
        
        if (HasSpace)
        {
            bunkerText += "\n- Shelter NPC";
        }
        
        if (shelteredNPCs.Count > 0)
        {
            bunkerText += "\n- Evacuate All";
            
            // Check if it's safe to evacuate
            if (IsSafeToEvacuate())
            {
                bunkerText += " (Safe)";
            }
            else
            {
                bunkerText += " (Dangerous)";
            }
        }
        
        return baseText + bunkerText;
    }

    /// <summary>
    /// Checks if it's safe to evacuate NPCs (no enemies nearby)
    /// </summary>
    /// <returns>True if safe to evacuate, false if enemies are nearby</returns>
    public bool IsSafeToEvacuate()
    {
        // Check for nearby enemies
        var enemies = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        float safeDistance = 15f; // Distance within which enemies are considered a threat
        
        foreach (var obj in enemies)
        {
            if (obj is Enemies.EnemyBase enemy && enemy != null)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance <= safeDistance)
                {
                    return false; // Enemy nearby, not safe to evacuate
                }
            }
        }
        
        return true; // No enemies nearby, safe to evacuate
    }

    protected override void OnDestroy()
    {
        // Evacuate all NPCs when bunker is destroyed
        EvacuateAll();
        base.OnDestroy();
    }

    /// <summary>
    /// Override TakeDamage to add debug logging for bunkers
    /// </summary>
    /// <param name="amount">Amount of damage to take</param>
    /// <param name="damageSource">Source of the damage</param>
    public override void TakeDamage(float amount, Transform damageSource = null)
    {
        // Call base TakeDamage to handle the damage
        base.TakeDamage(amount, damageSource);
    }
} 