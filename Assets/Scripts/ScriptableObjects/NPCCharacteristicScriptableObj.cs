using UnityEngine;

namespace Characters.NPC.Characteristic
{
    [CreateAssetMenu(fileName = "NewNPCCharacteristic", menuName = "Scriptable Objects/Camp/NPC/Characteristics")]
    public class NPCCharacteristicScriptableObj : ResourceScriptableObj
    {
        public string CharicteristicDescription(){
            if(prefab != null){
                return prefab.GetComponent<BaseNPCCharacteristicEffect>().GetStatsDescription();
            }
            return description;
        }
    }
} 