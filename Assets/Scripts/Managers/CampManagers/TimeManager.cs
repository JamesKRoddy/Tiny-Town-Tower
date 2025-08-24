using UnityEngine;
using System;
using System.Collections;

namespace Managers
{
    /// <summary>
    /// Manages the day/night cycle for the camp system.
    /// Integrates with existing camp managers to affect NPC behavior, lighting, and operations.
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        #region Time Configuration
        
        [Header("Time Settings")]
        [SerializeField] private float dayDurationInSeconds = 300f; // 5 minutes real time = 1 day
        [SerializeField] private float nightDurationInSeconds = 180f; // 3 minutes real time = 1 night
        [SerializeField] private float transitionDurationInSeconds = 30f; // 30 seconds for dawn/dusk
        [SerializeField] private bool autoStartCycle = true;
        [SerializeField] private TimeOfDay startingTimeOfDay = TimeOfDay.Dawn;
        
        [Header("Visual Settings")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Light moonLight;
        [SerializeField] private Gradient sunLightingGradient; // Sunrise to Midday (color + intensity via alpha)
        [SerializeField] private Gradient moonLightingGradient; // Moon lighting throughout night
        [SerializeField] private Gradient fogGradient; // Fog color transitions
        [SerializeField] private Gradient ambientColorGradient; // Ambient lighting color
        
        [Header("Directional Light Rotation")]
        [SerializeField] private bool rotateLights = true;
        [SerializeField] private Vector3 sunStartRotation = new Vector3(-90f, 0f, 0f); // Sun starts below horizon
        [SerializeField] private Vector3 moonOffset = new Vector3(180f, 0f, 0f); // Moon is opposite the sun
        
        [Header("Camp Effects")]
        [SerializeField] private float nightWorkEfficiencyMultiplier = 0.7f;
        [SerializeField] private float nightMovementSpeedMultiplier = 0.8f;
        [SerializeField] private bool enableNightSleepBehavior = true;
        [SerializeField] private float sleepChance = 0.6f; // 60% chance NPCs will sleep at night
        
        #endregion
        
        #region Events
        
        public static event Action<TimeOfDay> OnTimeOfDayChanged;
        public static event Action<float> OnTimeProgressChanged; // 0-1 progress through current time period
        public static event Action OnDayStarted;
        public static event Action OnNightStarted;
        public static event Action<float> OnNightWorkEfficiencyChanged;
        
        #endregion
        
        #region Private Fields
        
        private TimeOfDay currentTimeOfDay;
        private float currentTimeProgress = 0f; // 0-1 progress through current time period
        private float totalDayProgress = 0f; // 0-1 progress through entire day/night cycle
        private Coroutine timeCoroutine;
        private bool isPaused = false;
        
        // Calculated values
        private float totalCycleDuration; // Total time for complete day/night cycle
        private float rotationSpeed; // Degrees per second for 360° rotation
        
        // Original light settings for restoration
        private Color originalSunColor;
        private Color originalMoonColor;
        private float originalSunIntensity;
        private float originalMoonIntensity;
        
        #endregion
        
        #region Public Properties
        
        public TimeOfDay CurrentTimeOfDay => currentTimeOfDay;
        public float CurrentTimeProgress => currentTimeProgress;
        public float TotalDayProgress => totalDayProgress;
        public bool IsDay => currentTimeOfDay == TimeOfDay.Day || currentTimeOfDay == TimeOfDay.Dawn;
        public bool IsNight => currentTimeOfDay == TimeOfDay.Night || currentTimeOfDay == TimeOfDay.Dusk;
        public bool IsTransition => currentTimeOfDay == TimeOfDay.Dawn || currentTimeOfDay == TimeOfDay.Dusk;
        public float CurrentWorkEfficiencyMultiplier => IsNight ? nightWorkEfficiencyMultiplier : 1f;
        public float CurrentMovementSpeedMultiplier => IsNight ? nightMovementSpeedMultiplier : 1f;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeLightSettings();
        }
        
        private void Start()
        {
            if (autoStartCycle)
            {
                StartTimeCycle();
            }
        }
        
        private void OnDestroy()
        {
            if (timeCoroutine != null)
            {
                StopCoroutine(timeCoroutine);
            }
        }
        
        #endregion
        
        #region Time Management
        
        /// <summary>
        /// Start the time cycle
        /// </summary>
        public void StartTimeCycle()
        {
            if (timeCoroutine != null)
            {
                StopCoroutine(timeCoroutine);
            }
            
            currentTimeOfDay = startingTimeOfDay;
            currentTimeProgress = 0f;
            totalDayProgress = 0f;
            isPaused = false;
            
            // Calculate total cycle duration and rotation speed
            totalCycleDuration = dayDurationInSeconds + nightDurationInSeconds + (transitionDurationInSeconds * 2);
            rotationSpeed = 360f / totalCycleDuration; // Degrees per second for complete rotation
            
            Debug.Log($"[TimeManager] Starting cycle - Total duration: {totalCycleDuration}s, Rotation speed: {rotationSpeed}°/s");
            
            timeCoroutine = StartCoroutine(TimeCycleCoroutine());
            OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
        }
        
        /// <summary>
        /// Stop the time cycle
        /// </summary>
        public void StopTimeCycle()
        {
            if (timeCoroutine != null)
            {
                StopCoroutine(timeCoroutine);
                timeCoroutine = null;
            }
        }
        
        /// <summary>
        /// Pause/unpause the time cycle
        /// </summary>
        public void PauseTimeCycle(bool pause)
        {
            isPaused = pause;
        }
        
        /// <summary>
        /// Force set the time of day
        /// </summary>
        public void SetTimeOfDay(TimeOfDay timeOfDay)
        {
            currentTimeOfDay = timeOfDay;
            currentTimeProgress = 0f;
            OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
            UpdateVisuals();
        }
        
        /// <summary>
        /// Get current time duration for the active time period
        /// </summary>
        private float GetCurrentPeriodDuration()
        {
            return currentTimeOfDay switch
            {
                TimeOfDay.Day => dayDurationInSeconds,
                TimeOfDay.Night => nightDurationInSeconds,
                TimeOfDay.Dawn or TimeOfDay.Dusk => transitionDurationInSeconds,
                _ => dayDurationInSeconds
            };
        }
        
        #endregion
        
        #region Coroutines
        
        /// <summary>
        /// Main time cycle coroutine
        /// </summary>
        private IEnumerator TimeCycleCoroutine()
        {
            while (true)
            {
                if (!isPaused)
                {
                    float deltaTime = Time.deltaTime;
                    float periodDuration = GetCurrentPeriodDuration();
                    
                    // Update current period progress
                    currentTimeProgress += deltaTime / periodDuration;
                    
                    // Update total day progress (0-1 through complete cycle)
                    totalDayProgress = (totalDayProgress + (deltaTime / totalCycleDuration)) % 1f;
                    
                    OnTimeProgressChanged?.Invoke(currentTimeProgress);
                    UpdateVisuals();
                    
                    // Check if current period is complete
                    if (currentTimeProgress >= 1f)
                    {
                        TransitionToNextTimeOfDay();
                    }
                }
                
                yield return null;
            }
        }
        
        #endregion
        
        #region Time Transitions
        
        /// <summary>
        /// Transition to the next time of day
        /// </summary>
        private void TransitionToNextTimeOfDay()
        {
            currentTimeProgress = 0f;
            
            TimeOfDay nextTimeOfDay = currentTimeOfDay switch
            {
                TimeOfDay.Dawn => TimeOfDay.Day,
                TimeOfDay.Day => TimeOfDay.Dusk,
                TimeOfDay.Dusk => TimeOfDay.Night,
                TimeOfDay.Night => TimeOfDay.Dawn,
                _ => TimeOfDay.Day
            };
            
            currentTimeOfDay = nextTimeOfDay;
            OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
            
            // Trigger specific events
            if (currentTimeOfDay == TimeOfDay.Day)
            {
                OnDayStarted?.Invoke();
                Debug.Log("[TimeManager] Day has started!");
            }
            else if (currentTimeOfDay == TimeOfDay.Night)
            {
                OnNightStarted?.Invoke();
                OnNightWorkEfficiencyChanged?.Invoke(nightWorkEfficiencyMultiplier);
                Debug.Log("[TimeManager] Night has started!");
            }
        }
        
        #endregion
        
        #region Visual Updates
        
        /// <summary>
        /// Initialize light settings
        /// </summary>
        private void InitializeLightSettings()
        {
            if (sunLight != null)
            {
                originalSunColor = sunLight.color;
                originalSunIntensity = sunLight.intensity;
            }
            
            if (moonLight != null)
            {
                originalMoonColor = moonLight.color;
                originalMoonIntensity = moonLight.intensity;
                moonLight.gameObject.SetActive(false); // Start with moon light off
            }
        }
        
        /// <summary>
        /// Update visual elements based on current time
        /// </summary>
        private void UpdateVisuals()
        {
            UpdateLighting();
            UpdateFog();
        }
        
        /// <summary>
        /// Update lighting based on time of day
        /// </summary>
        private void UpdateLighting()
        {
            UpdateSunLighting();
            UpdateMoonLighting();
            UpdateAmbientLighting();
            
            // Rotate lights based on total day progress
            if (rotateLights)
            {
                UpdateLightRotation();
            }
        }
        
        /// <summary>
        /// Update sun lighting using gradient based on sun position
        /// </summary>
        private void UpdateSunLighting()
        {
            if (sunLight == null || sunLightingGradient == null) return;
            
            // Calculate sun angle (0° = horizon, 90° = overhead, 180° = opposite horizon)
            float sunAngle = (totalDayProgress * 360f + sunStartRotation.x) % 360f;
            if (sunAngle < 0f) sunAngle += 360f;
            
            // Map sun angle to gradient time
            // When sun is above horizon (0° to 180°), use gradient
            // When sun is below horizon (180° to 360°), use minimum values
            float gradientTime = 0f;
            
            if (sunAngle >= 0f && sunAngle <= 180f)
            {
                // Sun is above horizon - map 0°→180° to gradient 0→1→0
                if (sunAngle <= 90f)
                {
                    gradientTime = sunAngle / 90f; // 0° to 90° = 0.0 to 1.0
                }
                else
                {
                    gradientTime = 1f - ((sunAngle - 90f) / 90f); // 90° to 180° = 1.0 to 0.0
                }
            }
            else
            {
                // Sun is below horizon - use minimum lighting
                gradientTime = 0f;
            }
            
            Color sunColor = sunLightingGradient.Evaluate(gradientTime);
            
            // Use alpha channel for intensity
            sunLight.color = new Color(sunColor.r, sunColor.g, sunColor.b, 1f);
            sunLight.intensity = sunColor.a * originalSunIntensity;
            
            sunLight.gameObject.SetActive(true);
        }
        
        /// <summary>
        /// Update moon lighting
        /// </summary>
        private void UpdateMoonLighting()
        {
            if (moonLight == null) return;
            
            // Calculate moon angle (opposite the sun)
            float moonAngle = (totalDayProgress * 360f + sunStartRotation.x + moonOffset.x) % 360f;
            if (moonAngle < 0f) moonAngle += 360f;
            
            // Moon should be visible when it's above horizon
            bool shouldShowMoon = moonAngle >= 0f && moonAngle <= 180f;
            moonLight.gameObject.SetActive(shouldShowMoon);
            
            if (shouldShowMoon && moonLightingGradient != null)
            {
                // Map moon angle to gradient (similar to sun but for night time)
                float gradientTime = 0f;
                if (moonAngle <= 90f)
                {
                    gradientTime = moonAngle / 90f; // 0° to 90° = 0.0 to 1.0
                }
                else
                {
                    gradientTime = 1f - ((moonAngle - 90f) / 90f); // 90° to 180° = 1.0 to 0.0
                }
                
                Color moonColor = moonLightingGradient.Evaluate(gradientTime);
                
                moonLight.color = new Color(moonColor.r, moonColor.g, moonColor.b, 1f);
                moonLight.intensity = moonColor.a * originalMoonIntensity;
            }
        }
        
        /// <summary>
        /// Update ambient lighting color
        /// </summary>
        private void UpdateAmbientLighting()
        {
            if (ambientColorGradient != null)
            {
                float timeNormalized = GetNormalizedTimeForLighting();
                Color ambientColor = ambientColorGradient.Evaluate(timeNormalized);
                RenderSettings.ambientLight = ambientColor;
            }
        }
        

        
        /// <summary>
        /// Update both sun and moon rotation based on total day progress
        /// </summary>
        private void UpdateLightRotation()
        {
            // Calculate current rotation angle based on total day progress
            float currentAngle = totalDayProgress * 360f + sunStartRotation.x;
            
            // Update sun rotation
            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(currentAngle, sunStartRotation.y, sunStartRotation.z);
            }
            
            // Update moon rotation (opposite the sun)
            if (moonLight != null)
            {
                float moonAngle = currentAngle + moonOffset.x;
                moonLight.transform.rotation = Quaternion.Euler(moonAngle, sunStartRotation.y, sunStartRotation.z);
            }
        }
        
        /// <summary>
        /// Update fog based on time of day
        /// </summary>
        private void UpdateFog()
        {
            if (!RenderSettings.fog || fogGradient == null) return;
            
            float timeNormalized = GetNormalizedTimeForLighting();
            RenderSettings.fogColor = fogGradient.Evaluate(timeNormalized);
        }
        
        /// <summary>
        /// Get normalized time value (0-1) for lighting calculations
        /// 0 = full day lighting, 1 = full night lighting
        /// </summary>
        private float GetNormalizedTimeForLighting()
        {
            return currentTimeOfDay switch
            {
                TimeOfDay.Dawn => 1f - currentTimeProgress, // Start dark, get brighter
                TimeOfDay.Day => 0f, // Full daylight
                TimeOfDay.Dusk => currentTimeProgress, // Start bright, get darker
                TimeOfDay.Night => 1f, // Full night
                _ => 0f
            };
        }
        
        /// <summary>
        /// Get the total progress through the entire day cycle (0-1)
        /// Useful for smooth transitions and effects
        /// </summary>
        public float GetDayCycleProgress()
        {
            float totalCycleDuration = dayDurationInSeconds + nightDurationInSeconds + (transitionDurationInSeconds * 2);
            float currentCycleTime = 0f;
            
            switch (currentTimeOfDay)
            {
                case TimeOfDay.Dawn:
                    currentCycleTime = currentTimeProgress * transitionDurationInSeconds;
                    break;
                case TimeOfDay.Day:
                    currentCycleTime = transitionDurationInSeconds + (currentTimeProgress * dayDurationInSeconds);
                    break;
                case TimeOfDay.Dusk:
                    currentCycleTime = transitionDurationInSeconds + dayDurationInSeconds + (currentTimeProgress * transitionDurationInSeconds);
                    break;
                case TimeOfDay.Night:
                    currentCycleTime = (transitionDurationInSeconds * 2) + dayDurationInSeconds + (currentTimeProgress * nightDurationInSeconds);
                    break;
            }
            
            return currentCycleTime / totalCycleDuration;
        }
        
        #endregion
        
        #region Public Utility Methods
        
        /// <summary>
        /// Check if NPCs should sleep during current time
        /// </summary>
        public bool ShouldNPCSleep()
        {
            return enableNightSleepBehavior && IsNight && UnityEngine.Random.value < sleepChance;
        }
        
        /// <summary>
        /// Get time-modified work efficiency for tasks
        /// </summary>
        public float GetWorkEfficiencyMultiplier(WorkTask workTask = null)
        {
            // Some tasks might not be affected by time of day
            if (workTask != null)
            {
                // Add specific task logic here if needed
                // For example, security tasks might be more efficient at night
            }
            
            return CurrentWorkEfficiencyMultiplier;
        }
        
        /// <summary>
        /// Get time of day as a readable string
        /// </summary>
        public string GetTimeOfDayString()
        {
            return currentTimeOfDay switch
            {
                TimeOfDay.Dawn => "Dawn",
                TimeOfDay.Day => "Day",
                TimeOfDay.Dusk => "Dusk",
                TimeOfDay.Night => "Night",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Get formatted time string with progress
        /// </summary>
        public string GetFormattedTimeString()
        {
            float remainingTime = (1f - currentTimeProgress) * GetCurrentPeriodDuration();
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            
            return $"{GetTimeOfDayString()} - {minutes:00}:{seconds:00}";
        }
        
        #endregion
    }
    
    /// <summary>
    /// Enum representing different times of day
    /// </summary>
    public enum TimeOfDay
    {
        Dawn,    // Transition from night to day
        Day,     // Full daylight
        Dusk,    // Transition from day to night
        Night    // Full night
    }
}
