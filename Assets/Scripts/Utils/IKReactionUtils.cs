using UnityEngine;

/// <summary>
/// Static utility class for applying immediate IK-based hit reactions to humanoid characters.
/// Uses stateless calculations based on time since hit for simple, performant reactions.
/// </summary>
public static class IKReactionUtils
{
    // Configuration constants
    private const float DEFAULT_REACTION_DURATION = 0.3f;
    private const float DEFAULT_REACTION_INTENSITY = 0.8f; // Increased from 0.5 for more visible reactions
    private const float DEFAULT_MAX_IK_OFFSET = 0.25f; // Increased from 0.15 for more visible movement
    private const float HEAD_IK_WEIGHT = 1.0f; // Increased from 0.7 for stronger head reactions
    private const float HAND_IK_WEIGHT = 0.8f; // Increased from 0.5 for stronger hand reactions
    private const float HIT_DETECTION_RADIUS = 1.5f; // Increased from 0.8 for wider detection
    
    // Debug flag
    private static bool enableDebugLogs = true;

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
        
        // Calculate reaction weight using smooth sine curve
        float reactionProgress = Mathf.Clamp01(timeSinceHit / reactionDuration);
        float weightCurve = Mathf.Sin(reactionProgress * Mathf.PI); // Smooth in/out
        
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
        // Head recoils away from hit
        Vector3 recoilDirection = (headPos - hitOrigin).normalized;
        recoilDirection += Vector3.up * 0.3f; // Add upward component
        recoilDirection.Normalize();
        
        Vector3 targetPos = headPos + recoilDirection * DEFAULT_MAX_IK_OFFSET * intensity;
        
        float weight = intensity * HEAD_IK_WEIGHT;
        animator.SetLookAtWeight(weight, weight * 0.8f, weight * 0.5f);
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
        
        // Calculate defensive hand position (move toward hit defensively)
        Vector3 reactionDirection = (handPos - hitOrigin).normalized;
        Vector3 defensiveOffset = -reactionDirection * DEFAULT_MAX_IK_OFFSET * 0.5f;
        
        // Add outward spread
        Vector3 outwardDir = characterTransform.right * (isLeftHand ? -1f : 1f);
        defensiveOffset += outwardDir * DEFAULT_MAX_IK_OFFSET * 0.3f;
        
        Vector3 targetPos = handPos + defensiveOffset * intensity;
        
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
    /// Calculates procedural knockback offset based on hit origin and time.
    /// Returns a Vector3 offset that can be applied to character position for smooth knockback.
    /// This integrates with the IK reaction system for unified hit response.
    /// </summary>
    /// <param name="characterTransform">Character's transform</param>
    /// <param name="lastHitOrigin">Where the hit came from</param>
    /// <param name="lastHitTime">When the hit occurred</param>
    /// <param name="maxKnockbackDistance">Maximum knockback distance (default: 1.0)</param>
    /// <param name="knockbackDuration">How long knockback lasts (default: 0.2s)</param>
    /// <returns>Knockback offset to apply to character position</returns>
    public static Vector3 CalculateKnockbackOffset(
        Transform characterTransform,
        Vector3 lastHitOrigin,
        float lastHitTime,
        float maxKnockbackDistance = 1.0f,
        float knockbackDuration = 0.2f)
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
        
        // Use a punch curve for knockback (quick push, then ease out)
        float progress = timeSinceHit / knockbackDuration;
        float knockbackCurve = 1f - Mathf.Pow(progress, 2f); // Quadratic ease-out
        
        Vector3 knockbackOffset = knockbackDirection * maxKnockbackDistance * distanceScale * knockbackCurve;
        
        if (enableDebugLogs && timeSinceHit < 0.05f)
        {
            Debug.Log($"[IKReactionUtils] Knockback - " +
                     $"maxDist: {maxKnockbackDistance:F2}m, " +
                     $"distScale: {distanceScale:F2}, " +
                     $"curve: {knockbackCurve:F2}, " +
                     $"final: {maxKnockbackDistance * distanceScale * knockbackCurve:F3}m, " +
                     $"dir: {knockbackDirection}");
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

