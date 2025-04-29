using UnityEngine;
using System.Collections.Generic;
using Managers;

[CreateAssetMenu(fileName = "ResearchScriptableObj", menuName = "Scriptable Objects/Camp/ResearchScriptableObj")]
public class ResearchScriptableObj : WorldItemBase
{
    [Header("Research Requirements")]
    public ResourceItemCount[] requiredResources; // Resources required to start the research
    [Min(5f)]
    public float researchTime; // Time it takes to complete the research
    public bool isUnlocked = false; //Has the research been completed?

    [Header("Research Prerequisites")]
    public ResearchScriptableObj[] requiredResearch; // Research that must be completed before this can be unlocked
    public int requiredCampLevel; // Minimum camp level required to start this research

    [Header("Research Benefits")]
    public ResourceScriptableObj[] outputResources; // Resources produced when research is completed
    public float[] outputAmounts; // Amount of each output resource produced
    public ResearchUnlockType unlockType; // What type of item this research unlocks
    public WorldItemBase[] unlockedItems; // The specific items that are unlocked by this research
}
