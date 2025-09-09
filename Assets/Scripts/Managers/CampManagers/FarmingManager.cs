using System.Collections.Generic;
using UnityEngine;

public class FarmingManager : MonoBehaviour
{
    public void Initialize(){
        
    }

    public List<ResourceScriptableObj> GetAllCrops(){
        //This should get all player inventory items that are crop seeds
        return PlayerInventory.Instance.GetAllItemsOfCategory(ItemCategory.CROP_SEED);
    }
}
