using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ResearchScriptableObj", menuName = "Scriptable Objects/Camp/ResearchScriptableObj")]
public class ResearchScriptableObj : WorldItemBase
{
    [Header("Research Requirements")]
    public ResourceScriptableObj[] requiredResources;
    public int researchPointsCost;
    public float researchTime;
    public bool isUnlocked = false;

    [Header("Research Prerequisites")]
    public ResearchScriptableObj[] requiredResearch; // Research that must be completed before this can be unlocked
    public int requiredCampLevel; // Minimum camp level required to start this research

    [Header("Research Benefits")]
    public ResourceScriptableObj[] outputResources; // Resources produced when research is completed
    public float[] outputAmounts; // Amount of each output resource produced
    public bool unlocksNewBuilding; // Whether this research unlocks a new building type
    public bool unlocksNewResource; // Whether this research unlocks a new resource type
    public bool unlocksNewTechnology; // Whether this research unlocks a new technology
}
