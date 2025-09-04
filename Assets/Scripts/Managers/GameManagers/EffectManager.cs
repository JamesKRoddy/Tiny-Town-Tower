using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Managers
{  
    public class EffectManager : MonoBehaviour
    {
        public static EffectManager Instance { get; private set; }

        [Header("Character Effects")]
        [Tooltip("Array of effect sets for different character types")]
        public CharacterEffects[] characterEffects;

        [Header("Building Effects")]
        [Tooltip("Array of effect sets for different building types")]
        public BuildingEffects[] buildingEffects;

        [Tooltip("Number of instances of each effect to keep in the object pool")]
        public int poolSize = 20;

        private Dictionary<EffectDefinition, Queue<GameObject>> effectPools = new Dictionary<EffectDefinition, Queue<GameObject>>();
        private Dictionary<EffectDefinition, List<GameObject>> activeEffects = new Dictionary<EffectDefinition, List<GameObject>>();
        
        // Status effect tracking
        private Dictionary<IStatusEffectTarget, Dictionary<StatusEffectType, ActiveStatusEffect>> activeStatusEffects = 
            new Dictionary<IStatusEffectTarget, Dictionary<StatusEffectType, ActiveStatusEffect>>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializePools();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializePools()
        {
            InitializeCharacterEffectPools();
            InitializeBuildingEffectPools();
        }

        private void InitializeCharacterEffectPools()
        {
            if (characterEffects == null) return;

            foreach (var charEffect in characterEffects)
            {
                if (charEffect == null) continue;

                InitializeEffectPool(charEffect.bloodEffects);
                InitializeEffectPool(charEffect.impactEffects);
                InitializeEffectPool(charEffect.deathEffects);
                InitializeEffectPool(charEffect.destructionEffects);
                InitializeEffectPool(charEffect.footstepEffects);
                InitializeEffectPool(charEffect.spawnEffects);
                InitializeEffectPool(charEffect.idleEffects);
            }
        }

        private void InitializeBuildingEffectPools()
        {
            if (buildingEffects == null) return;

            foreach (var buildingEffect in buildingEffects)
            {
                if (buildingEffect == null) continue;

                InitializeEffectPool(buildingEffect.impactEffects);
                InitializeEffectPool(buildingEffect.destructionEffects);
                InitializeEffectPool(buildingEffect.constructionEffects);
                InitializeEffectPool(buildingEffect.constructionCompleteEffects);
                InitializeEffectPool(buildingEffect.repairEffects);
                InitializeEffectPool(buildingEffect.upgradeEffects);
                InitializeEffectPool(buildingEffect.ambientEffects);
            }
        }

        private void InitializeEffectPool(EffectDefinition[] effects)
        {
            if (effects == null) return;

            foreach (var effect in effects)
            {
                if (effect != null && !effectPools.ContainsKey(effect) && effect.prefabs != null && effect.prefabs.Length > 0)
                {
                    Queue<GameObject> pool = new Queue<GameObject>();
                    List<GameObject> active = new List<GameObject>();

                    for (int i = 0; i < poolSize; i++)
                    {
                        GameObject prefab = effect.prefabs[Random.Range(0, effect.prefabs.Length)];
                        if (prefab != null)
                        {
                            GameObject obj = Instantiate(prefab, transform);
                            obj.SetActive(false);
                            pool.Enqueue(obj);
                        }
                    }

                    effectPools[effect] = pool;
                    activeEffects[effect] = active;
                }
            }
        }

        public void PlayHitEffect(Vector3 position, Vector3 normal, IDamageable damageable)
        {
            if (damageable == null) return;
            
            var characterEffects = GetCharacterEffects(damageable.CharacterType);
            if (characterEffects != null)
            {
                if (characterEffects.bloodEffects != null && characterEffects.bloodEffects.Length > 0)
                {
                    PlayEffect(position, normal, Quaternion.LookRotation(normal), null, characterEffects.bloodEffects[Random.Range(0, characterEffects.bloodEffects.Length)]);
                }

                if (characterEffects.impactEffects != null && characterEffects.impactEffects.Length > 0)
                {
                    PlayEffect(position, normal, Quaternion.LookRotation(normal), null, characterEffects.impactEffects[Random.Range(0, characterEffects.impactEffects.Length)]);
                }
            }
        }

        public void PlayHitEffect(Vector3 position, Vector3 normal, CampBuildingCategory buildingCategory)
        {
            var buildingEffects = GetBuildingEffects(buildingCategory);
            if (buildingEffects == null || buildingEffects.impactEffects == null || buildingEffects.impactEffects.Length == 0) 
            {
                Debug.LogWarning($"No hit effects found for building category: {buildingCategory}");
                return;
            }

            PlayEffect(position, normal, Quaternion.LookRotation(normal), null, buildingEffects.impactEffects[Random.Range(0, buildingEffects.impactEffects.Length)]);
        }

        public void PlayDeathEffect(Vector3 position, Vector3 normal, IDamageable damageable)
        {
            if (damageable == null) return;
            var effects = GetCharacterEffects(damageable.CharacterType);
            if (effects == null || effects.deathEffects == null || effects.deathEffects.Length == 0)
            {
                Debug.LogWarning($"No death effects found for character type: {damageable.CharacterType}");
                return;
            }

            PlayEffect(position, normal, Quaternion.LookRotation(normal), null, effects.deathEffects[Random.Range(0, effects.deathEffects.Length)]);
        }

        public void PlayDestructionEffect(Vector3 position, Vector3 normal, CampBuildingCategory buildingCategory)
        {
            var buildingEffects = GetBuildingEffects(buildingCategory);
            if (buildingEffects == null || buildingEffects.destructionEffects == null || buildingEffects.destructionEffects.Length == 0)
            {
                Debug.LogWarning($"No destruction effects found for building category: {buildingCategory}");
                return;
            }

            PlayEffect(position, normal, Quaternion.LookRotation(normal), null, buildingEffects.destructionEffects[Random.Range(0, buildingEffects.destructionEffects.Length)]);
        }

        public void PlayConstructionEffect(Vector3 position, Vector3 normal, CampBuildingCategory buildingCategory)
        {
            var buildingEffects = GetBuildingEffects(buildingCategory);
            if (buildingEffects == null || buildingEffects.constructionEffects == null || buildingEffects.constructionEffects.Length == 0) return;

            PlayEffect(position, normal, Quaternion.LookRotation(normal), null, buildingEffects.constructionEffects[Random.Range(0, buildingEffects.constructionEffects.Length)]);
        }

        public void PlayConstructionCompleteEffect(Vector3 position, Vector3 normal, CampBuildingCategory buildingCategory)
        {
            var buildingEffects = GetBuildingEffects(buildingCategory);
            if (buildingEffects == null || buildingEffects.constructionCompleteEffects == null || buildingEffects.constructionCompleteEffects.Length == 0) return;

            PlayEffect(position, normal, Quaternion.LookRotation(normal), null, buildingEffects.constructionCompleteEffects[Random.Range(0, buildingEffects.constructionCompleteEffects.Length)]);
        }

        public void PlayRepairEffect(Vector3 position, Vector3 normal, CampBuildingCategory buildingCategory)
        {
            var buildingEffects = GetBuildingEffects(buildingCategory);
            if (buildingEffects == null || buildingEffects.repairEffects == null || buildingEffects.repairEffects.Length == 0) return;

            PlayEffect(position, normal, Quaternion.LookRotation(normal), null, buildingEffects.repairEffects[Random.Range(0, buildingEffects.repairEffects.Length)]);
        }

        public void PlayUpgradeEffect(Vector3 position, Vector3 normal, CampBuildingCategory buildingCategory)
        {
            var buildingEffects = GetBuildingEffects(buildingCategory);
            if (buildingEffects == null || buildingEffects.upgradeEffects == null || buildingEffects.upgradeEffects.Length == 0) return;

            PlayEffect(position, normal, Quaternion.LookRotation(normal), null, buildingEffects.upgradeEffects[Random.Range(0, buildingEffects.upgradeEffects.Length)]);
        }

        public void PlayFootstepEffect(Vector3 position, Vector3 normal, CharacterType characterType)
        {
            var effects = GetCharacterEffects(characterType);
            if (effects == null || effects.footstepEffects == null || effects.footstepEffects.Length == 0) return;

            PlayEffect(position, normal, Quaternion.LookRotation(normal), null, effects.footstepEffects[Random.Range(0, effects.footstepEffects.Length)]);
        }

        public void PlaySpawnEffect(Vector3 position, Vector3 normal, CharacterType characterType)
        {
            Debug.Log($"[EffectManager] PlaySpawnEffect called for character type: {characterType} at position: {position}");
            
            var effects = GetCharacterEffects(characterType);
            if (effects == null || effects.spawnEffects == null || effects.spawnEffects.Length == 0) 
            {
                Debug.LogWarning($"[EffectManager] No spawn effects found for character type: {characterType}");
                return;
            }

            var selectedEffect = effects.spawnEffects[Random.Range(0, effects.spawnEffects.Length)];
            Debug.Log($"[EffectManager] Playing spawn effect: {selectedEffect.name} for character type: {characterType}");
            PlayEffect(position, normal, Quaternion.LookRotation(normal), null, selectedEffect);
        }

        public void PlaySpawnEffect(Vector3 position, Vector3 normal, EffectDefinition spawnEffect)
        {
            Debug.Log($"[EffectManager] PlaySpawnEffect called with specific effect: {(spawnEffect != null ? spawnEffect.name : "null")} at position: {position}");
            
            if (spawnEffect == null) 
            {
                Debug.LogWarning("[EffectManager] Spawn effect is null");
                return;
            }
            
            PlayEffect(position, normal, Quaternion.LookRotation(normal), null, spawnEffect);
        }

        private CharacterEffects GetCharacterEffects(CharacterType characterType)
        {
            if (characterEffects == null) return null;

            foreach (var effect in characterEffects)
            {
                if (effect != null && effect.characterType == characterType)
                {
                    return effect;
                }
            }
            Debug.LogWarning($"No effects found for character type: {characterType}");
            return null;
        }

        private BuildingEffects GetBuildingEffects(CampBuildingCategory buildingCategory)
        {
            if (buildingEffects == null) return null;

            foreach (var effect in buildingEffects)
            {
                if (effect != null && effect.buildingCategory == buildingCategory)
                {
                    return effect;
                }
            }
            Debug.LogWarning($"No effects found for building category: {buildingCategory}");
            return null;
        }

        public GameObject PlayEffect(Vector3 position, Vector3 normal, Quaternion rotation, Transform parent, EffectDefinition effect, float duration = 0f)
        {
            if (effect == null)
            {
                Debug.LogWarning("[EffectManager] Attempted to play null effect");
                return null;
            }

            // If the effect isn't in our pools yet, initialize it
            if (!effectPools.ContainsKey(effect))
            {
                InitializeEffectPool(new[] { effect });
            }

            GameObject vfx = GetPooledObject(effect);
            if (vfx == null)
            {
                // If we couldn't get a pooled object, create a new one
                if (effect.prefabs != null && effect.prefabs.Length > 0)
                {
                    GameObject prefab = effect.playMode == EffectDefinition.PlayMode.Random
                        ? effect.prefabs[Random.Range(0, effect.prefabs.Length)]
                        : effect.prefabs[0]; // For All mode, we'll create additional instances below

                    if (prefab != null)
                    {
                        vfx = Instantiate(prefab, parent ?? transform);
                        activeEffects[effect].Add(vfx);
                    }
                }
            }

            if (vfx == null)
            {
                Debug.LogError($"[EffectManager] Failed to create effect instance for: {effect.name}");
                return null;
            }

            // Set parent first to ensure proper local space calculations
            vfx.transform.SetParent(parent ?? transform, false);
            
            // Set position and rotation in world space
            vfx.transform.position = position;
            vfx.transform.rotation = rotation;

            float particleDuration = 0f;
            float audioDuration = 0f;

            // Find all particle systems in the effect (including children) and get the longest duration
            particleDuration = PlayAllParticleSystemsAndGetMaxDuration(vfx);

            if (effect.sounds != null && effect.sounds.Length > 0)
            {
                AudioSource audioSource = vfx.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = vfx.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.loop = false;
                }

                AudioClip[] soundsToPlay = effect.playMode == EffectDefinition.PlayMode.Random
                    ? new[] { effect.sounds[Random.Range(0, effect.sounds.Length)] }
                    : effect.sounds;

                foreach (var sound in soundsToPlay)
                {
                    audioSource.clip = sound;
                    audioSource.pitch = Random.Range(effect.minPitch, effect.maxPitch);
                    audioSource.volume = effect.volume;
                    audioSource.spatialBlend = effect.spatialBlend;
                    audioSource.Play();
                    audioDuration = Mathf.Max(audioDuration, sound.length);
                }
            }

            // If in All mode, create additional instances for remaining prefabs
            if (effect.playMode == EffectDefinition.PlayMode.All && effect.prefabs != null && effect.prefabs.Length > 1)
            {
                for (int i = 1; i < effect.prefabs.Length; i++)
                {
                    GameObject additionalVfx = Instantiate(effect.prefabs[i], parent ?? transform);
                    additionalVfx.transform.position = position;
                    additionalVfx.transform.rotation = rotation;
                    activeEffects[effect].Add(additionalVfx);

                    // Play all particle systems in this additional effect and get max duration
                    float additionalDuration = PlayAllParticleSystemsAndGetMaxDuration(additionalVfx);
                    particleDuration = Mathf.Max(particleDuration, additionalDuration);
                }
            }

            if (duration <= 0)
                duration = effect.duration > 0 ? effect.duration : Mathf.Max(particleDuration, audioDuration);

            StartCoroutine(ReturnToPoolAfterDuration(vfx, effect, duration));

            return vfx;
        }

        /// <summary>
        /// Play all particle systems in the GameObject and its children, returning the maximum duration
        /// </summary>
        private float PlayAllParticleSystemsAndGetMaxDuration(GameObject effectObject)
        {
            float maxDuration = 0f;
            
            // Get all particle systems including children
            ParticleSystem[] particleSystems = effectObject.GetComponentsInChildren<ParticleSystem>();
            
            foreach (ParticleSystem ps in particleSystems)
            {
                if (ps != null)
                {
                    ps.Play();
                    float duration = ps.main.duration;
                    
                    // Consider start lifetime if duration is very small (some particle systems use lifetime instead)
                    if (duration < 0.1f)
                    {
                        duration = ps.main.startLifetime.constantMax;
                    }
                    
                    maxDuration = Mathf.Max(maxDuration, duration);
                    Debug.Log($"[EffectManager] ParticleSystem '{ps.name}' duration: {duration}s");
                }
            }
            
            Debug.Log($"[EffectManager] Maximum particle duration found: {maxDuration}s across {particleSystems.Length} particle systems");
            return maxDuration;
        }

        private GameObject GetPooledObject(EffectDefinition effect)
        {
            if (effect == null || !effectPools.ContainsKey(effect)) return null;

            Queue<GameObject> pool = effectPools[effect];
            List<GameObject> active = activeEffects[effect];

            GameObject obj;
            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
                obj.SetActive(true);
                active.Add(obj);
            }
            else if (active.Count > 0)
            {
                obj = active[0];
                active.RemoveAt(0);
                active.Add(obj);
            }
            else
            {
                return null;
            }

            // Ensure AudioSource component exists if the effect has sounds
            if (effect.sounds != null && effect.sounds.Length > 0)
            {
                AudioSource audioSource = obj.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = obj.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.loop = false;
                }
            }

            return obj;
        }

        private IEnumerator ReturnToPoolAfterDuration(GameObject obj, EffectDefinition effect, float duration)
        {
            yield return new WaitForSeconds(duration);
            
            if (obj != null && effect != null && activeEffects.ContainsKey(effect))
            {
                // Reset parent back to EffectManager before disabling
                obj.transform.SetParent(transform, false);
                obj.SetActive(false);
                activeEffects[effect].Remove(obj);
                if (effectPools.ContainsKey(effect))
                {
                    effectPools[effect].Enqueue(obj);
                }
            }
        }
        
        #region Status Effect Management
        
        /// <summary>
        /// Apply a status effect to a character
        /// </summary>
        public void ApplyStatusEffect(IStatusEffectTarget target, StatusEffectType statusType, float duration = 0f)
        {
            if (target == null)
            {
                Debug.LogWarning("[EffectManager] Cannot apply status effect to null target");
                return;
            }
            
            var statusDefinition = GetStatusEffectDefinition(target.GetCharacterType(), statusType);
            if (statusDefinition == null)
            {
                Debug.LogWarning($"[EffectManager] No status effect definition found for {statusType} on {target.GetCharacterType()}");
                return;
            }
            
            // Initialize status effects dictionary for this target if needed
            if (!activeStatusEffects.ContainsKey(target))
            {
                activeStatusEffects[target] = new Dictionary<StatusEffectType, ActiveStatusEffect>();
            }
            
            var targetEffects = activeStatusEffects[target];
            
            // Handle existing effect based on behavior
            if (targetEffects.ContainsKey(statusType))
            {
                var existingEffect = targetEffects[statusType];
                
                switch (statusDefinition.behavior)
                {
                    case StatusEffectBehavior.IGNORE_IF_EXISTS:
                        return;
                        
                    case StatusEffectBehavior.REFRESH_DURATION:
                        if (statusDefinition.canRefreshDuration)
                        {
                            existingEffect.duration = duration > 0 ? duration : statusDefinition.defaultDuration;
                            existingEffect.startTime = Time.time;
                            return;
                        }
                        break;
                        
                    case StatusEffectBehavior.REPLACE_EXISTING:
                    case StatusEffectBehavior.STACK:
                    default:
                        RemoveStatusEffect(target, statusType);
                        break;
                }
            }
            
            // Create new status effect
            var activeEffect = new ActiveStatusEffect
            {
                statusType = statusType,
                definition = statusDefinition,
                startTime = Time.time,
                duration = duration > 0 ? duration : statusDefinition.defaultDuration,
                isActive = true
            };
            
            // Apply visual effect
            ApplyStatusVisualEffect(target, activeEffect);
            
            // Apply gameplay effects
            ApplyStatusGameplayEffects(target, activeEffect);
            
            // Set up duration handling
            if (activeEffect.duration > 0)
            {
                activeEffect.durationCoroutine = StartCoroutine(RemoveStatusEffectAfterDuration(target, statusType, activeEffect.duration));
            }
            
            // Set up damage/healing over time
            if (statusDefinition.damagePerSecond != 0f || statusDefinition.healingPerSecond != 0f)
            {
                activeEffect.damageCoroutine = StartCoroutine(HandleStatusDamageOverTime(target, activeEffect));
            }
            
            targetEffects[statusType] = activeEffect;
            
            // Notify target
            target.OnStatusEffectApplied(statusType, activeEffect.duration);
            
            Debug.Log($"[EffectManager] Applied status effect {statusType} to {target.GetCharacterType()} for {activeEffect.duration}s");
        }
        
        /// <summary>
        /// Remove a status effect from a character
        /// </summary>
        public void RemoveStatusEffect(IStatusEffectTarget target, StatusEffectType statusType)
        {
            if (target == null || !activeStatusEffects.ContainsKey(target)) return;
            
            var targetEffects = activeStatusEffects[target];
            if (!targetEffects.ContainsKey(statusType)) return;
            
            var activeEffect = targetEffects[statusType];
            
            // Remove visual effects
            RemoveStatusVisualEffect(target, activeEffect);
            
            // Remove gameplay effects
            RemoveStatusGameplayEffects(target, activeEffect);
            
            // Cleanup
            activeEffect.Cleanup();
            targetEffects.Remove(statusType);
            
            // Notify target
            target.OnStatusEffectRemoved(statusType);
            
            Debug.Log($"[EffectManager] Removed status effect {statusType} from {target.GetCharacterType()}");
        }
        
        /// <summary>
        /// Remove all status effects from a character
        /// </summary>
        public void RemoveAllStatusEffects(IStatusEffectTarget target)
        {
            if (target == null || !activeStatusEffects.ContainsKey(target)) return;
            
            var targetEffects = activeStatusEffects[target];
            var effectTypes = new StatusEffectType[targetEffects.Count];
            targetEffects.Keys.CopyTo(effectTypes, 0);
            
            foreach (var effectType in effectTypes)
            {
                RemoveStatusEffect(target, effectType);
            }
            
            activeStatusEffects.Remove(target);
        }
        
        /// <summary>
        /// Check if a character has a specific status effect
        /// </summary>
        public bool HasStatusEffect(IStatusEffectTarget target, StatusEffectType statusType)
        {
            return activeStatusEffects.ContainsKey(target) && 
                   activeStatusEffects[target].ContainsKey(statusType);
        }
        
        /// <summary>
        /// Get the status effect definition for a character type and status type
        /// </summary>
        private StatusEffectDefinition GetStatusEffectDefinition(CharacterType characterType, StatusEffectType statusType)
        {
            var characterEffects = GetCharacterEffects(characterType);
            if (characterEffects?.statusEffects == null) return null;
            
            foreach (var statusEffect in characterEffects.statusEffects)
            {
                if (statusEffect.statusType == statusType)
                {
                    return statusEffect;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Apply visual effects for a status effect
        /// </summary>
        private void ApplyStatusVisualEffect(IStatusEffectTarget target, ActiveStatusEffect activeEffect)
        {
            var definition = activeEffect.definition;
            var targetTransform = (target as Component)?.transform;
            if (targetTransform == null) return;
            
            // Play visual effect
            if (definition.HasVisualEffect())
            {
                Vector3 position = targetTransform.position + Vector3.up * definition.heightOffset;
                activeEffect.visualEffectInstance = PlayEffect(
                    position, 
                    Vector3.up, 
                    Quaternion.identity, 
                    targetTransform, 
                    definition.GetVisualEffect(), 
                    activeEffect.duration
                );
            }
            
            // Show floating text
            if (definition.HasFloatingText())
            {
                // TODO: Implement floating text system or integrate with UI system
                Debug.Log($"[EffectManager] Status text: {definition.floatingText}");
            }
            
            // Apply material modifications
            if (definition.ModifiesAppearance())
            {
                ApplyMaterialEffects(targetTransform, definition);
            }
        }
        
        /// <summary>
        /// Remove visual effects for a status effect
        /// </summary>
        private void RemoveStatusVisualEffect(IStatusEffectTarget target, ActiveStatusEffect activeEffect)
        {
            var targetTransform = (target as Component)?.transform;
            if (targetTransform == null) return;
            
            // Restore materials if modified
            if (activeEffect.definition.ModifiesAppearance())
            {
                RestoreMaterialEffects(targetTransform, activeEffect);
            }
        }
        
        /// <summary>
        /// Apply gameplay effects for a status effect
        /// </summary>
        private void ApplyStatusGameplayEffects(IStatusEffectTarget target, ActiveStatusEffect activeEffect)
        {
            var definition = activeEffect.definition;
            var targetTransform = (target as Component)?.transform;
            if (targetTransform == null) return;
            
            // Apply movement speed modifier
            if (definition.movementSpeedMultiplier != 1f)
            {
                var navAgent = targetTransform.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (navAgent != null)
                {
                    activeEffect.originalMovementSpeed = navAgent.speed;
                    navAgent.speed *= definition.movementSpeedMultiplier;
                }
            }
            
            // Apply animation speed modifier
            if (definition.modifyAnimations && definition.animationSpeedMultiplier != 1f)
            {
                var animator = targetTransform.GetComponent<Animator>();
                if (animator != null)
                {
                    activeEffect.originalAnimationSpeed = animator.speed;
                    animator.speed *= definition.animationSpeedMultiplier;
                }
            }
            
            // Set animation trigger
            if (definition.modifyAnimations && !string.IsNullOrEmpty(definition.animationTrigger))
            {
                var animator = targetTransform.GetComponent<Animator>();
                animator?.SetTrigger(definition.animationTrigger);
            }
            
            // Handle action prevention
            if (definition.preventActions)
            {
                var navAgent = targetTransform.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (navAgent != null)
                {
                    navAgent.isStopped = true;
                }
            }
        }
        
        /// <summary>
        /// Remove gameplay effects for a status effect
        /// </summary>
        private void RemoveStatusGameplayEffects(IStatusEffectTarget target, ActiveStatusEffect activeEffect)
        {
            var definition = activeEffect.definition;
            var targetTransform = (target as Component)?.transform;
            if (targetTransform == null) return;
            
            // Restore movement speed
            if (definition.movementSpeedMultiplier != 1f && activeEffect.originalMovementSpeed > 0)
            {
                var navAgent = targetTransform.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (navAgent != null)
                {
                    navAgent.speed = activeEffect.originalMovementSpeed;
                }
            }
            
            // Restore animation speed
            if (definition.modifyAnimations && definition.animationSpeedMultiplier != 1f && activeEffect.originalAnimationSpeed > 0)
            {
                var animator = targetTransform.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.speed = activeEffect.originalAnimationSpeed;
                }
            }
            
            // Remove action prevention
            if (definition.preventActions)
            {
                var navAgent = targetTransform.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (navAgent != null)
                {
                    navAgent.isStopped = false;
                }
            }
        }
        
        /// <summary>
        /// Apply material effects for status effects
        /// </summary>
        private void ApplyMaterialEffects(Transform target, StatusEffectDefinition definition)
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (definition.characterTint != Color.white)
                    {
                        materials[i].color = Color.Lerp(materials[i].color, definition.characterTint, 0.5f);
                    }
                    
                    if (definition.alphaValue != 1f)
                    {
                        var color = materials[i].color;
                        color.a = definition.alphaValue;
                        materials[i].color = color;
                    }
                    
                    if (definition.emissionIntensity > 0f)
                    {
                        materials[i].SetColor("_EmissionColor", definition.emissionColor * definition.emissionIntensity);
                        materials[i].EnableKeyword("_EMISSION");
                    }
                }
                renderer.materials = materials;
            }
        }
        
        /// <summary>
        /// Restore material effects for status effects
        /// </summary>
        private void RestoreMaterialEffects(Transform target, ActiveStatusEffect activeEffect)
        {
            // This would require storing original materials - simplified for now
            var renderers = target.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    // Reset to default values - in a real implementation, we'd restore original values
                    if (activeEffect.definition.emissionIntensity > 0f)
                    {
                        materials[i].SetColor("_EmissionColor", Color.black);
                        materials[i].DisableKeyword("_EMISSION");
                    }
                }
                renderer.materials = materials;
            }
        }
        
        /// <summary>
        /// Handle damage or healing over time for status effects
        /// </summary>
        private IEnumerator HandleStatusDamageOverTime(IStatusEffectTarget target, ActiveStatusEffect activeEffect)
        {
            var definition = activeEffect.definition;
            var targetComponent = target as Component;
            
            while (activeEffect.isActive && targetComponent != null)
            {
                if (definition.damagePerSecond > 0f)
                {
                    var damageable = targetComponent.GetComponent<IDamageable>();
                    damageable?.TakeDamage(definition.damagePerSecond);
                }
                
                if (definition.healingPerSecond > 0f)
                {
                    // TODO: Implement healing interface or system
                    Debug.Log($"[EffectManager] Healing {target.GetCharacterType()} for {definition.healingPerSecond}");
                }
                
                yield return new WaitForSeconds(1f);
            }
        }
        
        /// <summary>
        /// Remove status effect after duration expires
        /// </summary>
        private IEnumerator RemoveStatusEffectAfterDuration(IStatusEffectTarget target, StatusEffectType statusType, float duration)
        {
            yield return new WaitForSeconds(duration);
            RemoveStatusEffect(target, statusType);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Helper class for playing effects with delays and automatic positioning.
    /// Used primarily by boss attacks to manage effect timing and positioning.
    /// </summary>
    public class EffectPlayer
    {
        private readonly MonoBehaviour owner;
        private readonly EffectDefinition effect;
        private readonly float delay;
        private Coroutine activeCoroutine;

        /// <summary>
        /// Creates a new EffectPlayer instance.
        /// </summary>
        /// <param name="owner">The MonoBehaviour that owns this effect player (used for coroutines)</param>
        /// <param name="effect">The effect definition to play</param>
        /// <param name="delay">Delay in seconds before playing the effect</param>
        public EffectPlayer(MonoBehaviour owner, EffectDefinition effect, float delay)
        {
            this.owner = owner;
            this.effect = effect;
            this.delay = delay;
        }

        /// <summary>
        /// Plays the effect with optional position, normal, rotation and parent.
        /// If not specified, uses the owner's position and forward direction.
        /// </summary>
        public void Play(Vector3? position = null, Vector3? normal = null, Quaternion? rotation = null, Transform parent = null)
        {
            if (effect == null) return;
            
            Vector3 effectPosition = position ?? owner.transform.position;
            Vector3 effectNormal = normal ?? owner.transform.forward;
            Quaternion effectRotation = rotation ?? Quaternion.LookRotation(effectNormal);
            
            activeCoroutine = owner.StartCoroutine(PlayWithDelay(effectPosition, effectNormal, effectRotation, parent));
        }

        /// <summary>
        /// Stops any currently playing delayed effect.
        /// </summary>
        public void Stop()
        {
            if (activeCoroutine != null)
            {
                owner.StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }
        }

        private IEnumerator PlayWithDelay(Vector3 position, Vector3 normal, Quaternion rotation, Transform parent)
        {
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
            EffectManager.Instance.PlayEffect(position, normal, rotation, parent, effect);
        }
    }
    
    /// <summary>
    /// Internal class to track active status effects on characters
    /// </summary>
    public class ActiveStatusEffect
    {
        public StatusEffectType statusType;
        public StatusEffectDefinition definition;
        public GameObject visualEffectInstance;
        public float startTime;
        public float duration;
        public Coroutine durationCoroutine;
        public Coroutine damageCoroutine;
        public bool isActive;
        
        // Store original values for restoration
        public Color[] originalColors;
        public float originalMovementSpeed;
        public float originalAnimationSpeed;
        
        public void Cleanup()
        {
            if (visualEffectInstance != null)
            {
                Object.Destroy(visualEffectInstance);
                visualEffectInstance = null;
            }
            
            if (durationCoroutine != null)
            {
                EffectManager.Instance.StopCoroutine(durationCoroutine);
                durationCoroutine = null;
            }
            
            if (damageCoroutine != null)
            {
                EffectManager.Instance.StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
        }
    }
}
