using UnityEngine;

/// <summary>
/// Static utility class for applying immediate IK-based hit reactions to humanoid characters.
/// Uses stateless calculations based on time since hit for simple, performant reactions.
/// </summary>
public static class IKReactionUtils
{
    // Configuration constants
    private const float DEFAULT_REACTION_DURATION = 0.6f; // Increased from 0.3s - longer, more visible
    private const float DEFAULT_REACTION_INTENSITY = 1.0f; // Max intensity for visible reactions
    private const float DEFAULT_MAX_IK_OFFSET = 0.4f; // Increased from 0.25 - much more visible movement
    private const float HEAD_IK_WEIGHT = 1.0f; // Full head reactions
    private const float HAND_IK_WEIGHT = 1.0f; // Full hand reactions
    private const float HIT_DETECTION_RADIUS = 2.0f; // Wide detection
    
    // Debug flag
    private static bool enableDebugLogs = true;
    private static bool enableDebugGizmos = true;

    /// <summary>
    /// Applies immediate hit reaction IK to a character based on recent damage.
    /// Call this from OnAnimatorIK to create procedural hit reactions.
    /// </summary>
    /// <param name="animator">Character's animator (must be humanoid)</param>
    /// <param name="characterTransform">Character's transform</param>
    /// <param name="lastHitOrigin">World position where last hit came from</param>
    /// <param name="lastHitTime">Time when last hit occurred</param>
    /// <param name="reactionDuration">How long reactions last (default: 0.3s)</param>
    /// <param name="reactionIntensity">Strength of reactions 0-1 (default: 0.5)</param>
    public static void ApplyHitReactionIK(
        Animator animator,
        Transform characterTransform,
        Vector3 lastHitOrigin,
        float lastHitTime,
        float reactionDuration = DEFAULT_REACTION_DURATION,
        float reactionIntensity = DEFAULT_REACTION_INTENSITY)
    {
        if (animator == null)
        {
            if (enableDebugLogs) Debug.LogWarning("[IKReactionUtils] Animator is null!");
            return;
        }
        
        if (!animator.isHuman)
        {
            if (enableDebugLogs && Time.frameCount % 300 == 0) // Log once every 5 seconds at 60fps
                Debug.LogWarning($"[IKReactionUtils] {characterTransform.name} animator is not humanoid! IK reactions require humanoid rig.");
            return;
        }
        
        if (lastHitOrigin == Vector3.zero) return;
        
        float timeSinceHit = Time.time - lastHitTime;
        if (timeSinceHit > reactionDuration) return;
        
        // Calculate reaction weight using a curve that holds at peak longer
        float reactionProgress = Mathf.Clamp01(timeSinceHit / reactionDuration);
        
        // Custom curve: Quick ramp up, hold at peak, slow fall off
        float weightCurve;
        if (reactionProgress < 0.2f)
        {
            // Quick ramp up (0 to 1 in first 20% of duration)
            weightCurve = reactionProgress / 0.2f;
        }
        else if (reactionProgress < 0.7f)
        {
            // Hold at peak (stay at 1.0 for middle 50% of duration)
            weightCurve = 1.0f;
        }
        else
        {
            // Ease out (1 to 0 in last 30% of duration)
            float falloffProgress = (reactionProgress - 0.7f) / 0.3f;
            weightCurve = 1.0f - falloffProgress;
        }
        
        if (enableDebugLogs && timeSinceHit < 0.05f) // Log once per hit (within first few frames)
        {
            Debug.Log($"[IKReactionUtils] Applying hit reaction to {characterTransform.name} - " +
                     $"timeSinceHit: {timeSinceHit:F3}s, intensity: {reactionIntensity:F2}, weightCurve: {weightCurve:F2}");
        }
        
        // Get body part positions
        Vector3 headPos = GetBodyPartPosition(animator, HumanBodyBones.Head, characterTransform);
        Vector3 leftHandPos = GetBodyPartPosition(animator, HumanBodyBones.LeftHand, characterTransform);
        Vector3 rightHandPos = GetBodyPartPosition(animator, HumanBodyBones.RightHand, characterTransform);
        
        // Calculate hit direction
        Vector3 hitDirection = (characterTransform.position - lastHitOrigin).normalized;
        float hitHeight = lastHitOrigin.y - characterTransform.position.y;
        
        // Determine if hit is from front or back
        float dotProduct = Vector3.Dot(characterTransform.forward, -hitDirection);
        bool hitFromFront = dotProduct > 0;
        
        // Determine hit side (left/right)
        bool hitFromRight = Vector3.Dot(characterTransform.right, -hitDirection) > 0;
        
        // Apply head reaction for upper body hits
        bool isHeadLevel = hitHeight > 1.4f;
        bool isChestLevel = hitHeight > 0.8f && hitHeight <= 1.4f;
        bool isLowHit = hitHeight <= 0.8f;
        
        // Always apply head reaction (recoil away from any hit)
        ApplyHeadReaction(animator, headPos, lastHitOrigin, weightCurve * reactionIntensity);
        
        // Apply hand reactions - more aggressive for all hit heights
        ApplyHandReaction(animator, AvatarIKGoal.LeftHand, leftHandPos, lastHitOrigin, 
            characterTransform, hitFromRight, !hitFromRight, weightCurve * reactionIntensity);
        
        ApplyHandReaction(animator, AvatarIKGoal.RightHand, rightHandPos, lastHitOrigin, 
            characterTransform, hitFromRight, hitFromRight, weightCurve * reactionIntensity);
        
        if (enableDebugLogs && timeSinceHit < 0.05f)
        {
            Debug.Log($"[IKReactionUtils] {characterTransform.name} IK Details - " +
                     $"Head weight: {weightCurve * reactionIntensity * HEAD_IK_WEIGHT:F2}, " +
                     $"Hand weight: {weightCurve * reactionIntensity * HAND_IK_WEIGHT:F2}, " +
                     $"Hit height: {hitHeight:F2}m");
        }
    }

    /// <summary>
    /// Simple overload that applies hit reactions with default settings.
    /// </summary>
    public static void ApplyHitReactionIK(
        Animator animator,
        Transform characterTransform,
        Vector3 lastHitOrigin,
        float lastHitTime)
    {
        ApplyHitReactionIK(animator, characterTransform, lastHitOrigin, lastHitTime, 
            DEFAULT_REACTION_DURATION, DEFAULT_REACTION_INTENSITY);
    }

    private static void ApplyHeadReaction(Animator animator, Vector3 headPos, Vector3 hitOrigin, float intensity)
    {
        // Head recoils away from hit with dramatic upward snap
        Vector3 recoilDirection = (headPos - hitOrigin).normalized;
        recoilDirection += Vector3.up * 0.6f; // Stronger upward component for visible reaction
        recoilDirection.Normalize();
        
        Vector3 targetPos = headPos + recoilDirection * DEFAULT_MAX_IK_OFFSET * 1.5f * intensity; // Increased range
        
        float weight = intensity * HEAD_IK_WEIGHT;
        animator.SetLookAtWeight(weight, weight * 0.9f, weight * 0.7f, weight * 0.5f, weight * 0.5f); // More body involvement
        animator.SetLookAtPosition(targetPos);
    }

    private static void ApplyHandReaction(
        Animator animator,
        AvatarIKGoal handGoal,
        Vector3 handPos,
        Vector3 hitOrigin,
        Transform characterTransform,
        bool hitFromRight,
        bool isHitSideHand,
        float intensity)
    {
        bool isLeftHand = handGoal == AvatarIKGoal.LeftHand;
        float distanceToHit = Vector3.Distance(handPos, hitOrigin);
        
        // React more aggressively - both hands react to any hit
        // Don't skip based on distance, let the intensity scale naturally
        // if (distanceToHit > HIT_DETECTION_RADIUS && !isHitSideHand) return;
        
        // Calculate defensive hand position (move AWAY from hit - recoil)
        Vector3 reactionDirection = (handPos - hitOrigin).normalized;
        Vector3 recoilOffset = reactionDirection * DEFAULT_MAX_IK_OFFSET * 0.8f; // Hands move away from hit
        
        // Add upward and outward spread for dramatic effect
        Vector3 outwardDir = characterTransform.right * (isLeftHand ? -1f : 1f);
        recoilOffset += outwardDir * DEFAULT_MAX_IK_OFFSET * 0.5f;
        recoilOffset += Vector3.up * DEFAULT_MAX_IK_OFFSET * 0.3f; // Slight upward lift
        
        Vector3 targetPos = handPos + recoilOffset * intensity;
        
        // Stronger reaction on hit side
        float handIntensity = isHitSideHand ? intensity : intensity * 0.6f;
        float weight = handIntensity * HAND_IK_WEIGHT;
        
        animator.SetIKPositionWeight(handGoal, weight);
        animator.SetIKRotationWeight(handGoal, weight * 0.5f);
        animator.SetIKPosition(handGoal, targetPos);
        
        // Rotate hand toward hit origin
        Vector3 lookDirection = (hitOrigin - targetPos).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        animator.SetIKRotation(handGoal, targetRotation);
    }

    private static Vector3 GetBodyPartPosition(Animator animator, HumanBodyBones bone, Transform fallback)
    {
        if (animator == null) return fallback.position;
        
        Transform boneTransform = animator.GetBoneTransform(bone);
        if (boneTransform != null)
        {
            return boneTransform.position;
        }
        
        return fallback.position;
    }
    
    /// <summary>
    /// Calculates procedural knockback offset based on hit origin, time, and character poise.
    /// Returns a Vector3 offset that can be applied to character position for smooth knockback.
    /// This integrates with the IK reaction system for unified hit response.
    /// </summary>
    /// <param name="characterTransform">Character's transform</param>
    /// <param name="lastHitOrigin">Where the hit came from</param>
    /// <param name="lastHitTime">When the hit occurred</param>
    /// <param name="maxKnockbackDistance">Maximum knockback distance (default: 1.0)</param>
    /// <param name="knockbackDuration">How long knockback lasts (default: 0.3s)</param>
    /// <param name="characterMaxPoise">Character's max poise (higher poise = more resistance to knockback)</param>
    /// <param name="characterCurrentPoise">Character's current poise (lower poise = less resistance)</param>
    /// <returns>Knockback offset to apply to character position</returns>
    public static Vector3 CalculateKnockbackOffset(
        Transform characterTransform,
        Vector3 lastHitOrigin,
        float lastHitTime,
        float maxKnockbackDistance = 1.0f,
        float knockbackDuration = 0.3f,
        float characterMaxPoise = 50f,
        float characterCurrentPoise = 50f)
    {
        if (lastHitOrigin == Vector3.zero) return Vector3.zero;
        
        float timeSinceHit = Time.time - lastHitTime;
        if (timeSinceHit > knockbackDuration) return Vector3.zero;
        
        // Calculate knockback direction (away from hit)
        Vector3 knockbackDirection = (characterTransform.position - lastHitOrigin).normalized;
        knockbackDirection.y = 0; // Keep horizontal
        
        // Scale knockback by distance from source (closer = stronger knockback)
        float distanceFromSource = Vector3.Distance(characterTransform.position, lastHitOrigin);
        float distanceScale = Mathf.Lerp(1.0f, 0.3f, Mathf.Clamp01(distanceFromSource / 5f));
        
        // Poise resistance: Higher max poise = more stable
        // Characters with low current poise are more susceptible to knockback
        float poiseRatio = Mathf.Clamp01(characterCurrentPoise / Mathf.Max(characterMaxPoise, 1f));
        float poiseResistance = Mathf.Lerp(0.3f, 1.0f, 1f - (characterMaxPoise / 200f)); // Normalize: 200 maxPoise = 0.3x knockback, 0 = 1.0x
        float poiseMultiplier = poiseResistance * Mathf.Lerp(0.5f, 1.0f, poiseRatio); // Reduced poise = more knockback
        
        // Use a punch curve for knockback (quick push, then ease out)
        float progress = timeSinceHit / knockbackDuration;
        float knockbackCurve = 1f - Mathf.Pow(progress, 2f); // Quadratic ease-out
        
        Vector3 knockbackOffset = knockbackDirection * maxKnockbackDistance * distanceScale * knockbackCurve * poiseMultiplier;
        
        if (enableDebugLogs && timeSinceHit < 0.05f)
        {
            Debug.Log($"[IKReactionUtils] Knockback - " +
                     $"maxDist: {maxKnockbackDistance:F2}m, " +
                     $"distScale: {distanceScale:F2}, " +
                     $"curve: {knockbackCurve:F2}, " +
                     $"poiseResist: {poiseMultiplier:F2} (maxPoise: {characterMaxPoise}, currentPoise: {characterCurrentPoise:F1}), " +
                     $"final: {maxKnockbackDistance * distanceScale * knockbackCurve * poiseMultiplier:F3}m");
        }
        
        return knockbackOffset;
    }
    
    /// <summary>
    /// Enables or disables debug logging for IK reactions.
    /// </summary>
    public static void SetDebugLogging(bool enabled)
    {
        enableDebugLogs = enabled;
    }
}

