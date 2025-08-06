using System.Collections.Generic;

[System.Serializable]
public class DialogueData
{
    public string npcName; // NPC's name
    public List<DialogueLine> lines; // List of dialogue lines
    
    // Interaction Tracking
    public string startingLineId = "Start"; // Default starting line ID
    public List<ConditionalStart> conditionalStarts; // Alternative starting points based on flags
}

[System.Serializable]
public class DialogueLine
{
    public string speaker; // Name of the speaker
    public string text; // Text of the dialogue
    public string id; //Used when narrative just moves to the next line WITH an option
    public string nextLine; //Used when narrative just moves to the next line WITHOUT an option
    public List<DialogueOption> options; // Available player options
    public bool isTerminal; //Ends the conversation
    
    // Interaction Tracking
    public List<string> requiredFlags; // Flags that must be set for this line to be accessible
    public List<string> blockedByFlags; // Flags that prevent this line from being accessible
    public List<string> setFlags; // Flags to set when this line is displayed
    public List<string> removeFlags; // Flags to remove when this line is displayed
}

[System.Serializable]
public class DialogueOption
{
    public string text; // Text for the player option
    public string nextLine; // The ID of the next line to jump to
    public string requiredItem; // (Optional) Item required to enable this option (legacy - use requiredInventoryItems instead)
    public string recruitNPC; // (Optional) Name of NPC to recruit when this option is selected
    
    // Enhanced Inventory Requirements
    public List<InventoryRequirement> requiredInventoryItems; // List of items and quantities required
    
    // Interaction Tracking
    public List<string> requiredFlags; // Flags required for this option to appear
    public List<string> blockedByFlags; // Flags that hide this option
    public List<string> setFlags; // Flags to set when this option is selected
    public List<string> removeFlags; // Flags to remove when this option is selected
}

/// <summary>
/// Represents an inventory requirement for dialogue options
/// </summary>
[System.Serializable]
public class InventoryRequirement
{
    public string itemName; // Name of the required item (matches ResourceScriptableObj.objectName)
    public int requiredQuantity = 1; // Minimum quantity required (default: 1)
    public bool consumeOnUse = false; // Whether to consume the items when this option is selected
    public string displayText; // Optional custom text to show in tooltips (e.g., "Requires 5x Wood")
}

/// <summary>
/// Defines alternative starting points for dialogue based on interaction flags
/// </summary>
[System.Serializable]
public class ConditionalStart
{
    public string lineId; // The line ID to start from
    public List<string> requiredFlags; // Flags that must be set
    public List<string> blockedByFlags; // Flags that must NOT be set
    public int priority = 0; // Higher priority overrides lower priority (in case multiple conditions match)
    public string description; // For editor documentation
}
