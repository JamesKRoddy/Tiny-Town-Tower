using UnityEngine;
using Managers;

namespace Enemies
{
    public class ZombieVomitProjectile : MonoBehaviour
    {
        private Vector3 initialPosition;
        private Vector3 targetPosition;
        private float damage;
        [SerializeField] private float speed = 10f;
        [SerializeField] private float maxHeight = 5f;
        [SerializeField] private float maxLifetime = 10f;
        [SerializeField] private EffectDefinition vomitPoolEffect;

        private float timeAlive = 0f;
        private bool hasHit = false;
        private float jumpDuration;

        public void Initialize(Vector3 targetPos, float dmg, EffectDefinition poolEffect)
        {
            initialPosition = transform.position;
            targetPosition = targetPos;
            damage = dmg;
            vomitPoolEffect = poolEffect;
            timeAlive = 0f;
            hasHit = false;

            // Calculate the duration based on distance and speed
            float distance = Vector3.Distance(
                new Vector3(initialPosition.x, 0, initialPosition.z),
                new Vector3(targetPosition.x, 0, targetPosition.z)
            );
            jumpDuration = distance / speed;
        }

        void Update()
        {
            if (hasHit) return;

            timeAlive += Time.deltaTime;

            // Destroy if exceeded max lifetime
            if (timeAlive >= maxLifetime)
            {
                CreateVomitPool();
                Destroy(gameObject);
                return;
            }

            float progress = timeAlive / jumpDuration;

            // Calculate the current position in the jump arc
            Vector3 currentPosition = Vector3.Lerp(initialPosition, targetPosition, progress);
            
            // Add vertical movement using a sine wave
            currentPosition.y += Mathf.Sin(progress * Mathf.PI) * maxHeight;

            // Update position
            transform.position = currentPosition;

            // Check if the projectile has hit the ground
            if (transform.position.y <= 0.1f)
            {
                CreateVomitPool();
                Destroy(gameObject);
            }
        }

        void CreateVomitPool()
        {
            if (hasHit) return;
            hasHit = true;

            Debug.Log("Creating vomit pool");
            if (vomitPoolEffect == null)
            {
                Debug.LogError("Vomit pool effect is not assigned to ZombieVomitProjectile on " + gameObject.name);
                return;
            }

            // Play the vomit pool effect and get the spawned GameObject
            GameObject poolObj = EffectManager.Instance.PlayEffect(
                transform.position,
                Vector3.up,
                Quaternion.identity,
                null,
                vomitPoolEffect,
                5.0f
            );

            // Initialize the vomit pool
            if (poolObj != null)
            {
                ZombieVomitPool pool = poolObj.GetComponent<ZombieVomitPool>();
                if (pool == null)
                {
                    pool = poolObj.AddComponent<ZombieVomitPool>();
                }
                pool.Setup(damage, 0.5f, new Vector3(0.7f, 0.4f, 0.7f));
            }
        }

        // Optional: Visualize the projectile path in editor
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && !hasHit)
            {
                // Draw projectile path
                Gizmos.color = Color.yellow;
                int segments = 20;
                for (int i = 0; i < segments; i++)
                {
                    float progress = i / (float)segments;
                    Vector3 point = Vector3.Lerp(initialPosition, targetPosition, progress);
                    point.y += Mathf.Sin(progress * Mathf.PI) * maxHeight;
                    Gizmos.DrawSphere(point, 0.2f);
                }

                // Draw target position
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(targetPosition, 0.5f);
            }
        }
    }
}
