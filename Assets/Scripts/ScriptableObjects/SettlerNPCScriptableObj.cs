using UnityEngine;

[CreateAssetMenu(fileName = "NPCScriptableObj", menuName = "Scriptable Objects/Camp/NPCScriptableObj")]
public class NPCScriptableObj : ScriptableObject
{
    public string nPCName;
    public int nPCAge;
    public string nPCDescription;
    public GameObject prefab;
}
