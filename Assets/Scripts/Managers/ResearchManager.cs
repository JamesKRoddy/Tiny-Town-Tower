using UnityEngine;
using System.Collections.Generic;

namespace Managers
{
    public class ResearchManager : MonoBehaviour
    {
        [SerializeField] private List<ResearchScriptableObj> allResearch = new List<ResearchScriptableObj>();
        private List<ResearchScriptableObj> availableResearch = new List<ResearchScriptableObj>();
        private List<ResearchScriptableObj> completedResearch = new List<ResearchScriptableObj>();
        private List<WorldItemBase> unlockedItems = new List<WorldItemBase>();

        public void Initialize()
        {
            availableResearch = new List<ResearchScriptableObj>(allResearch);
            completedResearch = new List<ResearchScriptableObj>();
            unlockedItems = new List<WorldItemBase>();
        }

        public List<ResearchScriptableObj> GetAllResearch()
        {
            return allResearch;
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

        public bool IsResearchCompleted(ResearchScriptableObj research)
        {
            return completedResearch.Contains(research);
        }

        public bool IsResearchAvailable(ResearchScriptableObj research)
        {
            return availableResearch.Contains(research);
        }

        public bool CompleteResearch(ResearchScriptableObj research)
        {
            if (!availableResearch.Contains(research) || research.isUnlocked)
                return false;

            research.isUnlocked = true;
            availableResearch.Remove(research);
            completedResearch.Add(research);

            // Add any unlocked items to the unlocked items list
            if (research.unlockedItems != null && research.unlockType != ResearchUnlockType.NONE)
            {
                foreach (var item in research.unlockedItems)
                {
                    unlockedItems.Add(item);
                }
            }

            return true;
        }

        public bool CanStartResearch(ResearchScriptableObj research, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!availableResearch.Contains(research))
            {
                errorMessage = "This research is not available!";
                return false;
            }

            if (research.isUnlocked)
            {
                errorMessage = "This research has already been completed!";
                return false;
            }

            // Check prerequisites
            if (research.requiredResearch != null)
            {
                foreach (var prereq in research.requiredResearch)
                {
                    if (!prereq.isUnlocked)
                    {
                        errorMessage = "Research prerequisites not met!";
                        return false;
                    }
                }
            }

            return true;
        }
    } 
}