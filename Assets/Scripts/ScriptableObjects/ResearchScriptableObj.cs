using UnityEngine;
using System.Collections.Generic;
using Managers;

[CreateAssetMenu(fileName = "ResearchScriptableObj", menuName = "Scriptable Objects/Camp/ResearchScriptableObj")]
public class ResearchScriptableObj : CraftableScriptableObj
{
    [Header("Research Benefits")]
    public ResearchUnlockType unlockType; // What type of item this research unlocks
    public WorldItemBase[] unlockedItems; // The specific items that are unlocked by this research
}
