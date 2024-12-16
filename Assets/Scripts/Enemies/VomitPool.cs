using UnityEngine;

public class VomitPool : MonoBehaviour
{
    private float damage;

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Damage the player if they are in the vomit pool
            other.gameObject.GetComponent<PlayerHealth>().TakeDamage(damage);
        }
    }

    public void SetDamage(float dmg)
    {
        damage = dmg;
    }
}
