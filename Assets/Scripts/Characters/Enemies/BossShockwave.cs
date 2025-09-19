using UnityEngine;

namespace Enemies
{
    public class BossShockwave : MonoBehaviour
    {
        private Vector3 direction;
        private float speed;
        private float range;
        private float damage;
        private float distanceTraveled = 0f;
        private Vector3 startPosition;

        public void Initialize(Vector3 direction, float speed, float range, float damage)
        {
            this.direction = direction;
            this.speed = speed;
            this.range = range;
            this.damage = damage;
            this.startPosition = transform.position;
        }

        private void Update()
        {
            // Move the shockwave
            float distanceThisFrame = speed * Time.deltaTime;
            transform.position += direction * distanceThisFrame;
            distanceTraveled += distanceThisFrame;

            // Check if we've reached max range
            if (distanceTraveled >= range)
            {
                Destroy(gameObject);
                return;
            }

            // Check for collisions
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1f);
            foreach (var hitCollider in hitColliders)
            {
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null && damageable.GetAllegiance() == Allegiance.FRIENDLY)
                {
                    // Shockwave attacks deal moderate poise damage
                    float poiseDamage = damage * 0.6f; // 60% of health damage as poise damage
                    damageable.TakeDamage(damage, poiseDamage, transform);
                    // Optionally destroy the shockwave on hit
                    Destroy(gameObject);
                    return;
                }
            }
        }

        private void OnDrawGizmos()
        {
            // Draw collision sphere
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Green with transparency
            Gizmos.DrawWireSphere(transform.position, 1f);

            // Draw path from start to current position
            if (startPosition != Vector3.zero)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.5f); // Cyan with transparency
                Gizmos.DrawLine(startPosition, transform.position);
            }
        }
    }
} 