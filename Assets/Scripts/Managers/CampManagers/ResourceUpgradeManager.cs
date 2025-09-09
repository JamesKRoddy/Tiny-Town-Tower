using UnityEngine;
using System.Collections.Generic;

namespace Managers
{
    public class ResourceUpgradeManager : MonoBehaviour
    {
        [SerializeField] private List<ResourceUpgradeScriptableObj> allUpgrades = new List<ResourceUpgradeScriptableObj>();
        private List<ResourceUpgradeScriptableObj> availableUpgrades = new List<ResourceUpgradeScriptableObj>();
        private List<ResourceUpgradeScriptableObj> unlockedUpgrades = new List<ResourceUpgradeScriptableObj>();

        public void Initialize()
        {
            availableUpgrades = new List<ResourceUpgradeScriptableObj>(allUpgrades);
            unlockedUpgrades = new List<ResourceUpgradeScriptableObj>();
        }

        public List<ResourceUpgradeScriptableObj> GetAllUpgrades()
        {
            return allUpgrades;
        }

        public List<ResourceUpgradeScriptableObj> GetAvailableUpgrades()
        {
            return availableUpgrades;
        }

        public List<ResourceUpgradeScriptableObj> GetUnlockedUpgrades()
        {
            return unlockedUpgrades;
        }

        public bool IsUpgradeUnlocked(ResourceUpgradeScriptableObj upgrade)
        {
            return unlockedUpgrades.Contains(upgrade);
        }

        public bool IsUpgradeAvailable(ResourceUpgradeScriptableObj upgrade)
        {
            return availableUpgrades.Contains(upgrade);
        }

        public bool UnlockUpgrade(ResourceUpgradeScriptableObj upgrade)
        {
            if (!availableUpgrades.Contains(upgrade) || upgrade.isUnlocked)
                return false;

            upgrade.isUnlocked = true;
            availableUpgrades.Remove(upgrade);
            unlockedUpgrades.Add(upgrade);
            return true;
        }

        public bool CanPerformUpgrade(ResourceUpgradeScriptableObj upgrade, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!availableUpgrades.Contains(upgrade))
            {
                errorMessage = "This upgrade is not available!";
                return false;
            }

            if (upgrade.isUnlocked)
            {
                errorMessage = "This upgrade has already been unlocked!";
                return false;
            }

            // Check if player has required resources
            if (upgrade.requiredResources != null)
            {
                foreach (var resource in upgrade.requiredResources)
                {
                    if (PlayerInventory.Instance.GetItemCount(resource.resourceScriptableObj) < resource.count)
                    {
                        errorMessage = "Not enough resources!";
                        return false;
                    }
                }
            }

            return true;
        }
    }
} 