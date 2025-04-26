using UnityEngine;
using System.Collections.Generic;

namespace Managers
{
    public class ResearchManager : MonoBehaviour
    {
        private List<ResearchScriptableObj> availableResearch = new List<ResearchScriptableObj>();
        private List<ResearchScriptableObj> completedResearch = new List<ResearchScriptableObj>();
        private List<WorldItemBase> unlockedItems = new List<WorldItemBase>();

        public void Initialize(List<ResearchScriptableObj> allResearch)
        {
            availableResearch = new List<ResearchScriptableObj>(allResearch);
            completedResearch = new List<ResearchScriptableObj>();
            unlockedItems = new List<WorldItemBase>();
        }

        public List<ResearchScriptableObj> GetAvailableResearch()
        {
            return availableResearch;
        }

        public List<ResearchScriptableObj> GetCompletedResearch()
        {
            return completedResearch;
        }

        public List<WorldItemBase> GetUnlockedItems()
        {
            return unlockedItems;
        }

        public bool IsItemUnlocked(WorldItemBase item)
        {
            return unlockedItems.Contains(item);
        }

        public bool CompleteResearch(ResearchScriptableObj research)
        {
            if (!availableResearch.Contains(research) || research.isUnlocked)
                return false;

            research.isUnlocked = true;
            availableResearch.Remove(research);
            completedResearch.Add(research);

            // Unlock the associated items
            if (research.unlocksNewBuilding)
            {
                // Add building to unlocked items
                // This would need to be implemented based on your building system
            }
            if (research.unlocksNewResource)
            {
                // Add resource to unlocked items
                // This would need to be implemented based on your resource system
            }
            if (research.unlocksNewTechnology)
            {
                // Add technology to unlocked items
                // This would need to be implemented based on your technology system
            }

            return true;
        }

        public bool CanStartResearch(ResearchScriptableObj research)
        {
            if (!availableResearch.Contains(research) || research.isUnlocked)
                return false;

            // Check prerequisites
            if (research.requiredResearch != null)
            {
                foreach (var prereq in research.requiredResearch)
                {
                    if (!prereq.isUnlocked)
                        return false;
                }
            }

            return true;
        }
    } 
}