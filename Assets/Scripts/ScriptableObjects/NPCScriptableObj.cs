using UnityEngine;

/// <summary>
/// NPCScriptableObj is a scriptable object that contains all the information about unique NPCs.
/// It is used to create NPCs in the game such as merchants, guards, etc.
/// Not to be confused with SettlerNPC.
/// </summary>
[CreateAssetMenu(fileName = "NPCScriptableObj", menuName = "Scriptable Objects/NPCScriptableObj")]
public class NPCScriptableObj : ScriptableObject
{
    [Header("NPC Information")]
    public string nPCName;
    public int nPCAge;
    public string nPCDescription;
    
    [Header("NPC Assets")]
    public Sprite sprite; // Portrait/icon for UI display
    public GameObject prefab; // NPC prefab for spawning
}
