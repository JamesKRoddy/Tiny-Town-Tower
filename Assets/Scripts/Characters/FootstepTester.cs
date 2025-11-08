using UnityEngine;
using Managers;

/// <summary>
/// Test/Debug component to manually trigger footstep effects
/// Add this to any character to test footstep sounds and VFX without animation events
/// Useful for testing different surface types and character configurations
/// </summary>
[RequireComponent(typeof(Animator))]
public class FootstepTester : MonoBehaviour
{
    [Header("Character Configuration")]
    [Tooltip("Character type to use for footstep effects")]
    public CharacterType characterType = CharacterType.ZOMBIE_MELEE;
    
    [Header("Surface Testing")]
    [Tooltip("Surface type to test (overrides auto-detection when Manual Mode enabled)")]
    public SurfaceType testSurface = SurfaceType.DEFAULT;
    
    [Tooltip("Enable to use testSurface instead of auto-detecting")]
    public bool manualSurfaceMode = false;
    
    [Header("Test Controls")]
    [Tooltip("Test interval in seconds (0 = manual only)")]
    [Range(0f, 5f)]
    public float autoTestInterval = 0f;
    
    [Tooltip("Show debug information in console")]
    public bool debugMode = true;
    
    [Tooltip("Show visual debug gizmos in scene")]
    public bool showGizmos = true;
    
    [Header("Keyboard Shortcuts")]
    [Tooltip("Key to trigger left footstep")]
    public KeyCode leftFootKey = KeyCode.LeftArrow;
    
    [Tooltip("Key to trigger right footstep")]
    public KeyCode rightFootKey = KeyCode.RightArrow;
    
    [Tooltip("Key to trigger generic footstep")]
    public KeyCode genericFootstepKey = KeyCode.Space;
    
    private Animator animator;
    private float nextAutoTestTime;
    private bool useLeftFoot = true; // Alternate between feet for auto test
    private Vector3 lastFootstepPosition;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    
    private void Update()
    {
        HandleKeyboardInput();
        HandleAutoTest();
    }
    
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(leftFootKey))
        {
            TestLeftFootstep();
        }
        
        if (Input.GetKeyDown(rightFootKey))
        {
            TestRightFootstep();
        }
        
        if (Input.GetKeyDown(genericFootstepKey))
        {
            TestGenericFootstep();
        }
    }
    
    private void HandleAutoTest()
    {
        if (autoTestInterval <= 0f) return;
        
        if (Time.time >= nextAutoTestTime)
        {
            if (useLeftFoot)
            {
                TestLeftFootstep();
            }
            else
            {
                TestRightFootstep();
            }
            
            useLeftFoot = !useLeftFoot;
            nextAutoTestTime = Time.time + autoTestInterval;
        }
    }
    
    /// <summary>
    /// Test left footstep (uses IK if available)
    /// </summary>
    [ContextMenu("Test Left Footstep")]
    public void TestLeftFootstep()
    {
        PlayFootstepAtFoot(AvatarIKGoal.LeftFoot, HumanBodyBones.LeftFoot, "LEFT");
    }
    
    /// <summary>
    /// Test right footstep (uses IK if available)
    /// </summary>
    [ContextMenu("Test Right Footstep")]
    public void TestRightFootstep()
    {
        PlayFootstepAtFoot(AvatarIKGoal.RightFoot, HumanBodyBones.RightFoot, "RIGHT");
    }
    
    /// <summary>
    /// Test generic footstep (uses character position)
    /// </summary>
    [ContextMenu("Test Generic Footstep")]
    public void TestGenericFootstep()
    {
        if (EffectManager.Instance == null)
        {
            Debug.LogWarning("[FootstepTester] EffectManager not found in scene!");
            return;
        }
        
        SurfaceType surfaceType = GetSurfaceType();
        Vector3 position = transform.position;
        lastFootstepPosition = position;
        
        if (debugMode)
        {
            Debug.Log($"[FootstepTester] Generic footstep - Character: {characterType}, Surface: {surfaceType}, Position: {position}");
        }
        
        EffectManager.Instance.PlayFootstepEffect(position, Vector3.up, characterType, surfaceType);
    }
    
    /// <summary>
    /// Cycle through all surface types to test each one
    /// </summary>
    [ContextMenu("Test All Surfaces")]
    public void TestAllSurfaces()
    {
        StartCoroutine(TestAllSurfacesCoroutine());
    }
    
    private System.Collections.IEnumerator TestAllSurfacesCoroutine()
    {
        bool originalManualMode = manualSurfaceMode;
        manualSurfaceMode = true;
        
        System.Array surfaceTypes = System.Enum.GetValues(typeof(SurfaceType));
        
        foreach (SurfaceType surface in surfaceTypes)
        {
            testSurface = surface;
            Debug.Log($"[FootstepTester] Testing surface: {surface}");
            
            TestLeftFootstep();
            yield return new WaitForSeconds(0.5f);
            
            TestRightFootstep();
            yield return new WaitForSeconds(1.5f);
        }
        
        manualSurfaceMode = originalManualMode;
        Debug.Log("[FootstepTester] Surface test complete!");
    }
    
    private void PlayFootstepAtFoot(AvatarIKGoal ikGoal, HumanBodyBones footBone, string footName)
    {
        if (EffectManager.Instance == null)
        {
            Debug.LogWarning("[FootstepTester] EffectManager not found in scene!");
            return;
        }
        
        Vector3 footPosition = transform.position;
        Vector3 footNormal = Vector3.up;
        
        // Try to get accurate foot position from animator IK
        if (animator != null && animator.isHuman)
        {
            Transform footTransform = animator.GetBoneTransform(footBone);
            if (footTransform != null)
            {
                footPosition = footTransform.position;
                
                // Raycast down from foot to find exact ground contact point
                RaycastHit hit;
                if (Physics.Raycast(footPosition + Vector3.up * 0.2f, Vector3.down, out hit, 1.0f))
                {
                    footPosition = hit.point;
                    footNormal = hit.normal;
                }
            }
        }
        
        SurfaceType surfaceType = GetSurfaceType(footPosition);
        lastFootstepPosition = footPosition;
        
        if (debugMode)
        {
            Debug.Log($"[FootstepTester] {footName} footstep - Character: {characterType}, Surface: {surfaceType}, Position: {footPosition}");
        }
        
        EffectManager.Instance.PlayFootstepEffect(footPosition, footNormal, characterType, surfaceType);
    }
    
    private SurfaceType GetSurfaceType(Vector3? position = null)
    {
        if (manualSurfaceMode)
        {
            return testSurface;
        }
        
        Vector3 hitPos;
        if (position.HasValue)
        {
            return SurfaceDetector.DetectSurface(position.Value + Vector3.up * 0.1f, out hitPos, 1.0f);
        }
        
        return SurfaceDetector.DetectSurfaceAtCharacter(transform, out hitPos);
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Draw foot positions if available
        if (animator != null && animator.isHuman)
        {
            DrawFootGizmo(HumanBodyBones.LeftFoot, Color.red);
            DrawFootGizmo(HumanBodyBones.RightFoot, Color.blue);
        }
        
        // Draw last footstep position
        if (lastFootstepPosition != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lastFootstepPosition, 0.1f);
            Gizmos.DrawLine(lastFootstepPosition, lastFootstepPosition + Vector3.up * 0.5f);
        }
        
        // Draw surface detection ray
        Gizmos.color = Color.green;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * 1.0f);
    }
    
    private void DrawFootGizmo(HumanBodyBones footBone, Color color)
    {
        Transform footTransform = animator.GetBoneTransform(footBone);
        if (footTransform != null)
        {
            Gizmos.color = color;
            Gizmos.DrawWireSphere(footTransform.position, 0.05f);
            
            // Raycast to show ground detection
            RaycastHit hit;
            if (Physics.Raycast(footTransform.position + Vector3.up * 0.2f, Vector3.down, out hit, 1.0f))
            {
                Gizmos.DrawLine(footTransform.position, hit.point);
                Gizmos.DrawWireCube(hit.point, Vector3.one * 0.05f);
            }
        }
    }
    
    private void OnGUI()
    {
        if (!debugMode) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("FOOTSTEP TESTER", GUI.skin.box);
        GUILayout.Label($"Character: {characterType}");
        GUILayout.Label($"Surface: {(manualSurfaceMode ? testSurface.ToString() + " (Manual)" : "Auto-Detect")}");
        GUILayout.Space(5);
        GUILayout.Label("Keyboard Controls:");
        GUILayout.Label($"  {leftFootKey} = Left Foot");
        GUILayout.Label($"  {rightFootKey} = Right Foot");
        GUILayout.Label($"  {genericFootstepKey} = Generic");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}

