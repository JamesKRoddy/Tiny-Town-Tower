using UnityEngine;

namespace Enemies{
    public class VomitPool : MonoBehaviour
    {
        private float damage;

        void OnTriggerStay(Collider other)
        {
            if (other.gameObject.GetComponent<IDamageable>() != null)
            {
                // Damage the player if they are in the vomit pool
                other.gameObject.GetComponent<IDamageable>().TakeDamage(damage);
            }
        }

        public void SetDamage(float dmg)
        {
            damage = dmg;
        }
    }
}
