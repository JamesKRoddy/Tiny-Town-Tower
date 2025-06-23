using UnityEngine;
using System.Collections.Generic;
using Managers;
using System;

/// <summary>
/// A bunker building that allows NPCs to shelter inside during zombie attacks.
/// Provides protection and can be upgraded for increased durability and capacity.
/// </summary>
public class BunkerBuilding : Building
{
    [Header("Bunker Settings")]
    [SerializeField] private int maxCapacity = 5;
    [SerializeField] private float durability = 100f;
    [SerializeField] private float maxDurability = 100f;
    [SerializeField] private Transform[] shelterPoints; // Points where NPCs can shelter inside
    [SerializeField] private bool isOccupied = false;
    [SerializeField] private List<HumanCharacterController> shelteredNPCs = new List<HumanCharacterController>();

    [Header("Upgrade Parameters")]
    [SerializeField] private int capacityIncreasePerUpgrade = 2;
    [SerializeField] private float durabilityIncreasePerUpgrade = 50f;

    // Events
    public event Action<BunkerBuilding> OnBunkerOccupied;
    public event Action<BunkerBuilding> OnBunkerVacated;
    public event Action<float> OnDurabilityChanged;

    public int MaxCapacity => maxCapacity;
    public int CurrentOccupancy => shelteredNPCs.Count;
    public float Durability => durability;
    public float MaxDurability => maxDurability;
    public bool IsOccupied => isOccupied;
    public bool HasSpace => shelteredNPCs.Count < maxCapacity;

    protected override void Start()
    {
        base.Start();
        
        // Setup shelter points if not assigned
        if (shelterPoints == null || shelterPoints.Length == 0)
        {
            SetupDefaultShelterPoints();
        }
    }

    public override void SetupBuilding(BuildingScriptableObj buildingScriptableObj)
    {
        base.SetupBuilding(buildingScriptableObj);
        
        // Initialize bunker-specific properties
        durability = maxDurability;
    }

    private void SetupDefaultShelterPoints()
    {
        // Create default shelter points inside the bunker
        int defaultPoints = Mathf.Min(maxCapacity, 8); // Max 8 default points
        shelterPoints = new Transform[defaultPoints];
        
        for (int i = 0; i < defaultPoints; i++)
        {
            GameObject shelterPoint = new GameObject($"ShelterPoint_{i}");
            shelterPoint.transform.SetParent(transform);
            
            // Arrange points in a grid pattern inside the bunker
            float spacing = 1.5f;
            int row = i / 4;
            int col = i % 4;
            
            Vector3 localPosition = new Vector3(
                (col - 1.5f) * spacing,
                0,
                (row - 0.5f) * spacing
            );
            
            shelterPoint.transform.localPosition = localPosition;
            shelterPoints[i] = shelterPoint.transform;
        }
    }

    /// <summary>
    /// Attempts to shelter an NPC in the bunker
    /// </summary>
    /// <param name="npc">The NPC to shelter</param>
    /// <returns>True if successfully sheltered, false if no space</returns>
    public bool ShelterNPC(HumanCharacterController npc)
    {
        if (!HasSpace || npc == null)
        {
            return false;
        }

        // Find an available shelter point
        Transform shelterPoint = GetAvailableShelterPoint();
        if (shelterPoint == null)
        {
            return false;
        }

        // Add NPC to sheltered list
        shelteredNPCs.Add(npc);
        
        // Move NPC to shelter point
        npc.transform.position = shelterPoint.position;
        
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
        shelteredNPCs.Clear();
        isOccupied = false;
        OnBunkerVacated?.Invoke(this);
        Debug.Log("All NPCs evacuated from bunker");
    }

    /// <summary>
    /// Takes damage to the bunker's durability
    /// </summary>
    /// <param name="damage">Amount of damage to take</param>
    public void TakeDurabilityDamage(float damage)
    {
        float previousDurability = durability;
        durability = Mathf.Max(0, durability - damage);
        
        OnDurabilityChanged?.Invoke(durability / maxDurability);
        
        if (durability <= 0)
        {
            // Bunker is destroyed, evacuate all NPCs
            EvacuateAll();
            Debug.LogWarning("Bunker destroyed! All NPCs evacuated.");
        }
    }

    /// <summary>
    /// Repairs the bunker's durability
    /// </summary>
    /// <param name="repairAmount">Amount of durability to restore</param>
    public void RepairDurability(float repairAmount)
    {
        float previousDurability = durability;
        durability = Mathf.Min(maxDurability, durability + repairAmount);
        
        OnDurabilityChanged?.Invoke(durability / maxDurability);
        
        Debug.Log($"Bunker durability repaired: {previousDurability} -> {durability}");
    }

    /// <summary>
    /// Upgrades the bunker's capacity and durability
    /// </summary>
    public void UpgradeBunker()
    {
        maxCapacity += capacityIncreasePerUpgrade;
        maxDurability += durabilityIncreasePerUpgrade;
        durability = maxDurability; // Restore full durability on upgrade
        
        // Add new shelter points if needed
        if (shelterPoints.Length < maxCapacity)
        {
            AddNewShelterPoints();
        }
        
        Debug.Log($"Bunker upgraded! New capacity: {maxCapacity}, New durability: {maxDurability}");
    }

    private void AddNewShelterPoints()
    {
        int newPointsNeeded = maxCapacity - shelterPoints.Length;
        Transform[] newShelterPoints = new Transform[shelterPoints.Length + newPointsNeeded];
        
        // Copy existing points
        for (int i = 0; i < shelterPoints.Length; i++)
        {
            newShelterPoints[i] = shelterPoints[i];
        }
        
        // Add new points
        for (int i = shelterPoints.Length; i < newShelterPoints.Length; i++)
        {
            GameObject shelterPoint = new GameObject($"ShelterPoint_{i}");
            shelterPoint.transform.SetParent(transform);
            
            // Arrange new points in a grid pattern
            float spacing = 1.5f;
            int row = i / 4;
            int col = i % 4;
            
            Vector3 localPosition = new Vector3(
                (col - 1.5f) * spacing,
                0,
                (row - 0.5f) * spacing
            );
            
            shelterPoint.transform.localPosition = localPosition;
            newShelterPoints[i] = shelterPoint.transform;
        }
        
        shelterPoints = newShelterPoints;
    }

    private Transform GetAvailableShelterPoint()
    {
        for (int i = 0; i < shelterPoints.Length && i < maxCapacity; i++)
        {
            if (shelterPoints[i] != null)
            {
                // Check if this point is already occupied
                bool isOccupied = false;
                foreach (var npc in shelteredNPCs)
                {
                    if (npc != null && Vector3.Distance(npc.transform.position, shelterPoints[i].position) < 0.5f)
                    {
                        isOccupied = true;
                        break;
                    }
                }
                
                if (!isOccupied)
                {
                    return shelterPoints[i];
                }
            }
        }
        
        return null;
    }

    public override string GetInteractionText()
    {
        string baseText = base.GetInteractionText();
        string bunkerText = $"\nBunker Status:\nCapacity: {shelteredNPCs.Count}/{maxCapacity}\nDurability: {durability:F0}/{maxDurability:F0}";
        
        if (HasSpace)
        {
            bunkerText += "\n- Shelter NPC";
        }
        
        if (shelteredNPCs.Count > 0)
        {
            bunkerText += "\n- Evacuate All";
        }
        
        return baseText + bunkerText;
    }

    protected override void OnDestroy()
    {
        // Evacuate all NPCs when bunker is destroyed
        EvacuateAll();
        base.OnDestroy();
    }
} 