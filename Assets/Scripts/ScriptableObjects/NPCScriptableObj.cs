using UnityEngine;

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
