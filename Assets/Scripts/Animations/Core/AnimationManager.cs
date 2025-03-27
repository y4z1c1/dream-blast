using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class AnimationManager : MonoBehaviour
{
    // singleton instance
    public static AnimationManager Instance { get; private set; }

    // animation duration settings
    [Header("Debug Settings")]
    [SerializeField] private bool debugMode = false;

    [Header("Global Animation Settings")]
    [SerializeField] private float defaultAnimationSpeed = 1.0f;
    [SerializeField] private bool animationsEnabled = true;

    // specific animation type toggles
    [Header("Animation Type Toggles")]
    [SerializeField] private bool cubeAnimationsEnabled = true;
    [SerializeField] private bool rocketAnimationsEnabled = true;
    [SerializeField] private bool obstacleAnimationsEnabled = true;
    [SerializeField] private bool uiAnimationsEnabled = true;
    [SerializeField] private bool fallingAnimationsEnabled = true;
    [SerializeField] private bool spawnAnimationsEnabled = true;

    // cube animation settings
    [Header("Cube Animations")]
    [Tooltip("Duration for cube destruction animation")]
    [SerializeField] private float cubeDestroyDuration = 0.3f;

    // rocket animation settings
    [Header("Rocket Animations")]
    [Tooltip("Duration for rocket creation animation")]
    [SerializeField] private float rocketCreateDuration = 0.5f;

    [Tooltip("Duration for rocket explosion animation")]
    [SerializeField] private float rocketExplosionDuration = 0.6f;

    [Tooltip("Speed for rocket projectiles")]
    [SerializeField] private float rocketProjectileSpeed = 15f;

    [Tooltip("Intensity of camera shake for normal rocket explosions")]
    [SerializeField] private float rocketExplosionShakeIntensity = 0.05f;

    [Tooltip("Duration of camera shake for normal rocket explosions")]
    [SerializeField] private float rocketExplosionShakeDuration = 0.2f;

    [Tooltip("Intensity of camera shake for combo rocket explosions")]
    [SerializeField] private float comboExplosionShakeIntensity = 0.08f;

    [Tooltip("Duration of camera shake for combo rocket explosions")]
    [SerializeField] private float comboExplosionShakeDuration = 0.3f;

    // rocket particle settings
    [Header("Rocket Particle Settings")]
    [Tooltip("Emission rate multiplier for RocketStar particle effect")]
    [Range(0.1f, 3.0f)]
    [SerializeField] private float rocketStarEmissionMultiplier = 1.0f;

    [Tooltip("Emission rate multiplier for RocketSmoke particle effect")]
    [Range(0.1f, 3.0f)]
    [SerializeField] private float rocketSmokeEmissionMultiplier = 1.0f;

    [Tooltip("Size multiplier for RocketStar particles")]
    [Range(0.5f, 2.0f)]
    [SerializeField] private float rocketStarSizeMultiplier = 1.0f;

    [Tooltip("Size multiplier for RocketSmoke particles")]
    [Range(0.5f, 2.0f)]
    [SerializeField] private float rocketSmokeSizeMultiplier = 1.0f;

    [Tooltip("Burst count multiplier for particle effects")]
    [Range(0.5f, 2.0f)]
    [SerializeField] private float particleBurstMultiplier = 1.0f;

    // obstacle animation settings
    [Header("Obstacle Animations")]
    [Tooltip("Duration for obstacle damage animation")]
    [SerializeField] private float obstacleDamageDuration = 0.2f;

    [Tooltip("Duration for obstacle destruction animation")]
    [SerializeField] private float obstacleDestroyDuration = 0.4f;


    // ui animation settings
    [Header("UI Animations")]
    [Tooltip("Duration for popup show animation")]
    [SerializeField] private float popupShowDuration = 0.3f;

    [Tooltip("Duration for popup hide animation")]
    [SerializeField] private float popupHideDuration = 0.2f;

    [Tooltip("Duration for button press animation")]
    [SerializeField] private float buttonPressAnimationDuration = 0.15f;

    [Tooltip("Duration for button appear/disappear animation")]
    [SerializeField] private float buttonTransitionDuration = 0.5f;

    [Tooltip("Delay before button appears")]
    [SerializeField] private float buttonAppearDelay = 0.2f;

    // falling animation settings
    [Header("Falling Animations")]
    [Tooltip("Gravity multiplier for falling animations")]
    [SerializeField] private float fallingGravity = 80f;

    [Tooltip("Bounce intensity for falling items")]
    [SerializeField] private float fallingBounceIntensity = 0.1f;

    // spawn animation settings
    [Header("Spawn Animations")]
    [Tooltip("Gravity multiplier for spawn animations")]
    [SerializeField] private float spawnGravity = 100f;

    [Tooltip("Bounce intensity for spawned cubes")]
    [SerializeField] private float spawnBounceIntensity = 0.2f;

    [Tooltip("Duration for fade-in effect during spawn")]
    [SerializeField] private float spawnFadeDuration = 0.15f;

    // color settings
    [Header("Color Settings")]
    [SerializeField] private Color damageFlashColor = new Color(1, 0.3f, 0.3f, 1);

    // References to other managers
    [Header("References")]
    [SerializeField] private ParticleManager particleManager;

    [SerializeField] private float destructionAnimHalfwayDelay = 0.15f; // estimated time to reach halfway point in destruction animation
    [SerializeField] private bool animateCubeDestroy = true;

    private bool isDestroying = false;
    private int activeRocketAnimationCount = 0; // track active rocket animations

    private void Awake()
    {
        // singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // make the animationmanager persist across scenes
            if (debugMode) Debug.Log("[AnimationManager] Instance created and set to persist");

            // set dotween capacity to a higher value to handle many concurrent animations
            // max regular tweens: 500, max sequences: 200
            DOTween.SetTweensCapacity(500, 200);
            if (debugMode) Debug.Log("[AnimationManager] DOTween capacity set to 500/200");
        }
        else
        {
            Destroy(gameObject);
            if (debugMode) Debug.Log("[AnimationManager] Duplicate instance destroyed");
            return;
        }

        // try to find particlemanager if not set
        if (particleManager == null)
        {
            particleManager = FindFirstObjectByType<ParticleManager>();
            if (debugMode) Debug.Log("[AnimationManager] ParticleManager reference found: " + (particleManager != null));
        }
    }

    // set global animation speed
    public void SetAnimationSpeed(float speed)
    {
        defaultAnimationSpeed = Mathf.Clamp(speed, 0.1f, 2.0f);
        if (debugMode) Debug.Log($"[AnimationManager] Animation speed set to: {defaultAnimationSpeed}");
    }

    // get the current animation speed multiplier
    public float GetAnimationSpeed()
    {
        return defaultAnimationSpeed;
    }

    // check if debug mode is enabled
    public bool IsDebugEnabled()
    {
        return debugMode;
    }

    // toggle animations on/off
    public void SetAnimationsEnabled(bool enabled)
    {
        animationsEnabled = enabled;
        if (debugMode) Debug.Log($"[AnimationManager] Animations {(enabled ? "enabled" : "disabled")}");

        // If disabling all animations, no need to check individual types
        // If enabling animations, individual toggles retain their previous state
    }

    // check if animations are enabled
    public bool AreAnimationsEnabled()
    {
        return animationsEnabled;
    }

    // toggle cube animations on/off
    public void SetCubeAnimationsEnabled(bool enabled)
    {
        cubeAnimationsEnabled = enabled;
        if (debugMode) Debug.Log($"[AnimationManager] Cube animations {(enabled ? "enabled" : "disabled")}");
    }

    // check if cube animations are enabled
    public bool AreCubeAnimationsEnabled()
    {
        return animationsEnabled && cubeAnimationsEnabled;
    }

    // toggle rocket animations on/off
    public void SetRocketAnimationsEnabled(bool enabled)
    {
        rocketAnimationsEnabled = enabled;
        if (debugMode) Debug.Log($"[AnimationManager] Rocket animations {(enabled ? "enabled" : "disabled")}");
    }

    // check if rocket animations are enabled
    public bool AreRocketAnimationsEnabled()
    {
        return animationsEnabled && rocketAnimationsEnabled;
    }

    // toggle obstacle animations on/off
    public void SetObstacleAnimationsEnabled(bool enabled)
    {
        obstacleAnimationsEnabled = enabled;
        if (debugMode) Debug.Log($"[AnimationManager] Obstacle animations {(enabled ? "enabled" : "disabled")}");
    }

    // check if obstacle animations are enabled
    public bool AreObstacleAnimationsEnabled()
    {
        return animationsEnabled && obstacleAnimationsEnabled;
    }

    // toggle UI animations on/off
    public void SetUIAnimationsEnabled(bool enabled)
    {
        uiAnimationsEnabled = enabled;
        if (debugMode) Debug.Log($"[AnimationManager] UI animations {(enabled ? "enabled" : "disabled")}");
    }

    // check if UI animations are enabled
    public bool AreUIAnimationsEnabled()
    {
        return animationsEnabled && uiAnimationsEnabled;
    }

    // toggle falling animations on/off
    public void SetFallingAnimationsEnabled(bool enabled)
    {
        fallingAnimationsEnabled = enabled;
        if (debugMode) Debug.Log($"[AnimationManager] Falling animations {(enabled ? "enabled" : "disabled")}");
    }

    // check if falling animations are enabled
    public bool AreFallingAnimationsEnabled()
    {
        return animationsEnabled && fallingAnimationsEnabled;
    }

    // toggle spawn animations on/off
    public void SetSpawnAnimationsEnabled(bool enabled)
    {
        spawnAnimationsEnabled = enabled;
        if (debugMode) Debug.Log($"[AnimationManager] Spawn animations {(enabled ? "enabled" : "disabled")}");
    }

    // check if spawn animations are enabled
    public bool AreSpawnAnimationsEnabled()
    {
        return animationsEnabled && spawnAnimationsEnabled;
    }

    // get particle manager reference
    public ParticleManager GetParticleManager()
    {
        return particleManager;
    }

    // check if there are active rocket animations
    public bool HasActiveRocketAnimations()
    {
        return activeRocketAnimationCount > 0;
    }

    // increment active rocket animation count
    public void IncrementRocketAnimationCount()
    {
        activeRocketAnimationCount++;
        if (debugMode) Debug.Log($"[AnimationManager] Rocket animation started. Active count: {activeRocketAnimationCount}");

        StartCoroutine(AutoDecrementRocketAnimationCount());
    }

    // automatically decrement rocket animation count after a delay
    private IEnumerator AutoDecrementRocketAnimationCount()
    {
        yield return new WaitForSeconds(0.5f);
        if (debugMode) Debug.Log($"[AnimationManager] Rocket animation completed. Active count: {activeRocketAnimationCount}");
        DecrementRocketAnimationCount();
    }

    // decrement active rocket animation count
    public void DecrementRocketAnimationCount()
    {
        if (activeRocketAnimationCount > 0)
        {
            activeRocketAnimationCount--;
        }

        if (debugMode) Debug.Log($"[AnimationManager] Rocket animation completed. Active count: {activeRocketAnimationCount}");
    }

    // get animation config reference
    public float GetCubeDestroyDuration() => cubeDestroyDuration;
    public float GetRocketCreateDuration() => rocketCreateDuration;
    public float GetRocketExplosionDuration() => rocketExplosionDuration;
    public float GetRocketProjectileSpeed() => rocketProjectileSpeed;
    public float GetObstacleDamageDuration() => obstacleDamageDuration;
    public float GetObstacleDestroyDuration() => obstacleDestroyDuration;
    public float GetPopupShowDuration() => popupShowDuration;
    public float GetPopupHideDuration() => popupHideDuration;
    public float GetButtonPressAnimationDuration() => buttonPressAnimationDuration;
    public float GetButtonTransitionDuration() => buttonTransitionDuration;
    public float GetButtonAppearDelay() => buttonAppearDelay;
    public float GetFallingGravity() => fallingGravity;
    public float GetFallingBounceIntensity() => fallingBounceIntensity;
    public float GetSpawnGravity() => spawnGravity;
    public float GetSpawnBounceIntensity() => spawnBounceIntensity;
    public float GetSpawnFadeDuration() => spawnFadeDuration;
    public Color GetDamageFlashColor() => damageFlashColor;
    public float GetRocketExplosionShakeIntensity() => rocketExplosionShakeIntensity;
    public float GetRocketExplosionShakeDuration() => rocketExplosionShakeDuration;
    public float GetComboExplosionShakeIntensity() => comboExplosionShakeIntensity;
    public float GetComboExplosionShakeDuration() => comboExplosionShakeDuration;

    // particle configuration getters
    public float GetRocketStarEmissionMultiplier() => rocketStarEmissionMultiplier;
    public float GetRocketSmokeEmissionMultiplier() => rocketSmokeEmissionMultiplier;
    public float GetRocketStarSizeMultiplier() => rocketStarSizeMultiplier;
    public float GetRocketSmokeSizeMultiplier() => rocketSmokeSizeMultiplier;
    public float GetParticleBurstMultiplier() => particleBurstMultiplier;

    // particle configuration setters
    public void SetRocketStarEmissionMultiplier(float value)
    {
        rocketStarEmissionMultiplier = Mathf.Clamp(value, 0.1f, 3.0f);
        if (debugMode) Debug.Log($"[AnimationManager] RocketStar emission multiplier set to: {rocketStarEmissionMultiplier}");
    }

    public void SetRocketSmokeEmissionMultiplier(float value)
    {
        rocketSmokeEmissionMultiplier = Mathf.Clamp(value, 0.1f, 3.0f);
        if (debugMode) Debug.Log($"[AnimationManager] RocketSmoke emission multiplier set to: {rocketSmokeEmissionMultiplier}");
    }

    public void SetRocketStarSizeMultiplier(float value)
    {
        rocketStarSizeMultiplier = Mathf.Clamp(value, 0.5f, 2.0f);
        if (debugMode) Debug.Log($"[AnimationManager] RocketStar size multiplier set to: {rocketStarSizeMultiplier}");
    }

    public void SetRocketSmokeSizeMultiplier(float value)
    {
        rocketSmokeSizeMultiplier = Mathf.Clamp(value, 0.5f, 2.0f);
        if (debugMode) Debug.Log($"[AnimationManager] RocketSmoke size multiplier set to: {rocketSmokeSizeMultiplier}");
    }

    public void SetParticleBurstMultiplier(float value)
    {
        particleBurstMultiplier = Mathf.Clamp(value, 0.5f, 2.0f);
        if (debugMode) Debug.Log($"[AnimationManager] Particle burst multiplier set to: {particleBurstMultiplier}");
    }

    // animate cube destruction and then destroy the gameobject
    public void AnimateCubeDestruction(Cube cube)
    {
        // early validation
        if (cube == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to destroy null cube");
            return;
        }

        if (!AreCubeAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] Cube animations disabled, destroying cube immediately");
            Destroy(cube.gameObject);
            return;
        }

        // mark cube as being destroyed to prevent multiple destroy calls
        if (cube.gameObject.GetComponent<CubeDestructionMarker>() != null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Cube is already being destroyed");
            return;
        }
        cube.gameObject.AddComponent<CubeDestructionMarker>();
        if (debugMode) Debug.Log("[AnimationManager] Starting cube destruction animation");

        StartCoroutine(AnimateCubeDestructionRoutine(cube));
    }

    private IEnumerator AnimateCubeDestructionRoutine(Cube cube)
    {
        if (cube == null) yield break;

        isDestroying = true;

        // disable cube interactions while animating
        cube.enabled = false;

        IEnumerator animationCoroutine = null;
        try
        {
            // get the animation coroutine
            animationCoroutine = CubeAnimations.PlayCubeDestroyAnimation(cube);
        }
        catch (System.Exception e)
        {
            if (debugMode) Debug.LogError($"[AnimationManager] Error during cube destruction animation setup: {e}");
        }

        // execute the animation if we got it successfully
        if (animationCoroutine != null)
        {
            yield return animationCoroutine;
        }
        else
        {
            // fallback if animation failed to initialize
            yield return new WaitForSeconds(cubeDestroyDuration);
        }

        // ensure cube is destroyed even if animation fails
        if (cube != null && cube.gameObject != null)
        {
            Destroy(cube.gameObject);
            if (debugMode) Debug.Log("[AnimationManager] Cube destroyed successfully");
        }

        isDestroying = false;
    }

    // animate falling item
    public void AnimateItemFalling(GridItem item, Vector3 sourcePosition, Vector3 targetPosition, System.Action onComplete = null)
    {
        // early validation
        if (item == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to animate falling of null item");
            onComplete?.Invoke();
            return;
        }

        if (!AreFallingAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] Falling animations disabled, teleporting item immediately");
            item.transform.position = targetPosition;
            onComplete?.Invoke();
            return;
        }

        if (debugMode) Debug.Log($"[AnimationManager] Starting item falling animation for {item.name}");

        // delegate to GeneralAnimations
        GeneralAnimations.PlayFallingAnimation(item, sourcePosition, targetPosition, onComplete);
    }

    // animate multiple items falling in a batch
    public void AnimateItemsBatchFalling(List<GridItem> items, List<Vector3> sourcePositions, List<Vector3> targetPositions, System.Action onComplete = null)
    {
        // early validation
        if (items == null || items.Count == 0 || sourcePositions == null || targetPositions == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to animate batch falling with invalid parameters");
            onComplete?.Invoke();
            return;
        }

        // validate all lists have the same count
        if (items.Count != sourcePositions.Count || items.Count != targetPositions.Count)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Batch falling animation lists have mismatched counts");
            onComplete?.Invoke();
            return;
        }

        // check if falling animations are enabled
        if (!AreFallingAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] Falling animations disabled, teleporting items immediately");

            // immediately place items at target positions
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    items[i].transform.position = targetPositions[i];
                }
            }

            onComplete?.Invoke();
            return;
        }

        if (debugMode) Debug.Log($"[AnimationManager] Starting batch falling animation for {items.Count} items");

        // delegate to GeneralAnimations
        GeneralAnimations.PlayFallingBatchAnimation(items, sourcePositions, targetPositions, onComplete);
    }

    // animate cube spawning from top with fade-in
    public void AnimateCubeSpawn(Cube cube, Vector3 startPosition, Vector3 targetPosition, System.Action onComplete = null)
    {
        // early validation
        if (cube == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to animate spawn of null cube");
            onComplete?.Invoke();
            return;
        }

        if (!AreSpawnAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] Spawn animations disabled, teleporting cube immediately");
            cube.transform.position = targetPosition;

            // Make sure cube is visible even without animation
            Renderer[] renderers = cube.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    Color color = renderer.material.color;
                    color.a = 1f;
                    renderer.material.color = color;
                }
            }

            onComplete?.Invoke();
            return;
        }

        if (debugMode) Debug.Log($"[AnimationManager] Starting cube spawn animation for {cube.name}");

        // delegate to GeneralAnimations
        GeneralAnimations.PlayCubeSpawnAnimation(cube, startPosition, targetPosition, onComplete);
    }

    // signal that other operations can start after destruction has progressed somewhat
    public IEnumerator WaitForDestructionHalfway()
    {
        if (isDestroying)
        {
            // wait for estimated halfway point of destruction animation
            yield return new WaitForSeconds(destructionAnimHalfwayDelay);
        }
    }

    // animate cubes combining into a rocket
    public void AnimateRocketCombine(List<Cube> cubes, Vector2Int targetPosition)
    {
        if (cubes == null || cubes.Count < 4)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Invalid cube list for rocket combine animation");
            return;
        }

        if (!AreRocketAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] Rocket animations disabled, creating rocket directly");
            // Create the rocket directly without animation
            if (cubes.Count > 0)
            {
                GridManager gridManager = cubes[0].GetGridManager();
                if (gridManager != null)
                {
                    // Destroy the cubes immediately
                    foreach (Cube cube in cubes)
                    {
                        if (cube != null && cube.gameObject != null)
                        {
                            Destroy(cube.gameObject);
                        }
                    }

                    RocketCreator.CreateRocket(targetPosition, gridManager);
                    if (debugMode) Debug.Log("[AnimationManager] Rocket created directly at target position");
                }
            }
            return;
        }

        if (debugMode) Debug.Log("[AnimationManager] Starting rocket combine animation");
        StartCoroutine(AnimateRocketCombineRoutine(cubes, targetPosition));
    }

    // animate obstacle destruction
    public void AnimateObstacleDestruction(Obstacle obstacle, bool playParticles = true)
    {
        // early validation
        if (obstacle == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to animate destruction of null obstacle");
            return;
        }

        if (!AreObstacleAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] Obstacle animations disabled, skipping obstacle destruction animation");
            return;
        }

        if (debugMode) Debug.Log("[AnimationManager] Starting obstacle destruction animation");
        ObstacleAnimations.PlayObstacleDestroyAnimation(obstacle, playParticles);
    }

    // animate obstacle damage
    public void AnimateObstacleDamage(Obstacle obstacle, bool playParticles = true)
    {
        // early validation
        if (obstacle == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to animate damage of null obstacle");
            return;
        }

        if (!AreObstacleAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] Obstacle animations disabled, skipping obstacle damage animation");
            return;
        }

        if (debugMode) Debug.Log("[AnimationManager] Starting obstacle damage animation");
        ObstacleAnimations.PlayObstacleDamageAnimation(obstacle, playParticles);
    }

    // animate popup show
    public void AnimatePopupShow(GameObject popupContainer, Image backgroundOverlay)
    {
        // early validation
        if (popupContainer == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to animate showing of null popup container");
            return;
        }

        if (!AreUIAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] UI animations disabled, skipping popup show animation");
            return;
        }

        if (debugMode) Debug.Log("[AnimationManager] Starting popup show animation");
        PopupAnimations.PlayPopupShowAnimation(popupContainer, backgroundOverlay);
    }

    // animate popup hide
    public void AnimatePopupHide(GameObject popupContainer, Image backgroundOverlay)
    {
        // early validation
        if (popupContainer == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to animate hiding of null popup container");
            return;
        }

        if (!AreUIAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] UI animations disabled, skipping popup hide animation");
            return;
        }

        if (debugMode) Debug.Log("[AnimationManager] Starting popup hide animation");
        PopupAnimations.PlayPopupHideAnimation(popupContainer, backgroundOverlay);
    }

    // animate button show
    public void AnimateButtonShow(Button button, float delay = 0f)
    {
        // early validation
        if (button == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to animate showing of null button");
            return;
        }

        if (!AreUIAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] UI animations disabled, skipping button show animation");
            return;
        }

        if (debugMode) Debug.Log("[AnimationManager] Starting button show animation");
        PopupAnimations.PlayButtonShowAnimation(button, delay);
    }

    // animate button click
    public void AnimateButtonClick(Button button)
    {
        // early validation
        if (button == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to animate click of null button");
            return;
        }

        if (!AreUIAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] UI animations disabled, skipping button click animation");
            return;
        }

        if (debugMode) Debug.Log("[AnimationManager] Starting button click animation");
        PopupAnimations.PlayButtonClickAnimation(button);
    }

    // animate win celebration
    public void AnimateWinCelebration(GameObject popupContainer)
    {
        // early validation
        if (popupContainer == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to animate win celebration of null popup container");
            return;
        }

        if (!AreUIAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] UI animations disabled, skipping win celebration animation");
            return;
        }

        if (debugMode) Debug.Log("[AnimationManager] Starting win celebration animation");

        // ensure particle manager is properly referenced before passing to animations
        if (particleManager == null)
        {
            particleManager = FindFirstObjectByType<ParticleManager>();
            if (debugMode) Debug.Log("[AnimationManager] Finding ParticleManager for win celebration: " + (particleManager != null));
        }

        // delegate to PopupAnimations
        PopupAnimations.PlayWinCelebrationAnimation(popupContainer);
    }

    // animate title
    public void AnimateTitle(TextMeshProUGUI titleText)
    {
        // early validation
        if (titleText == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to animate null title text");
            return;
        }

        if (!AreUIAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] UI animations disabled, skipping title animation");
            return;
        }

        if (debugMode) Debug.Log("[AnimationManager] Starting title animation");
        PopupAnimations.PlayTitleAnimation(titleText);
    }

    // animate shake
    public void AnimateShake(Transform target, float intensity = 5f, float duration = 0.5f)
    {
        // early validation
        if (target == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to animate shake of null target");
            return;
        }

        if (!AreUIAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] UI animations disabled, skipping shake animation");
            return;
        }

        if (debugMode) Debug.Log("[AnimationManager] Starting shake animation");
        PopupAnimations.PlayShakeAnimation(target, intensity, duration);
    }

    private IEnumerator AnimateRocketCombineRoutine(List<Cube> cubes, Vector2Int targetPosition)
    {
        isDestroying = true;

        // run the combine animation
        bool animationSucceeded = false;

        CubeAnimations.PlayRocketCombineAnimation(cubes, targetPosition,
            (success) =>
            {
                animationSucceeded = success;
                if (debugMode) Debug.Log($"[AnimationManager] Rocket combine animation {(success ? "succeeded" : "failed")}");
            });

        // wait for animation to complete
        yield return new WaitForSeconds(0.3f);

        // create the rocket at the target position
        if (cubes.Count > 0)
        {
            GridManager gridManager = cubes[0].GetGridManager();
            if (gridManager != null)
            {
                RocketCreator.CreateRocket(targetPosition, gridManager);
                if (debugMode) Debug.Log("[AnimationManager] Rocket created at target position");
            }
        }

        // add a small additional delay to ensure animations complete properly
        yield return new WaitForSeconds(0.3f);

        isDestroying = false;
    }

    // animate rocket explosion at a specific position
    public void AnimateRocketExplosion(Vector3 position)
    {
        // early validation
        if (!AreRocketAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] Rocket animations disabled, skipping rocket explosion animation");
            return;
        }

        // increment active rocket animation count
        IncrementRocketAnimationCount();

        if (debugMode) Debug.Log("[AnimationManager] Starting rocket explosion animation");

        // delegate to RocketAnimations without decrementing counter in callback
        RocketAnimations.AnimateRocketExplosion(position, null);
    }

    // animate rocket projectile along a path
    public void AnimateRocketProjectile(Rocket rocket, List<Vector2Int> path, System.Action<Vector2Int> onHitPosition = null)
    {
        // early validation
        if (rocket == null || path == null || path.Count == 0)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Invalid parameters for rocket projectile animation");
            return;
        }

        if (!AreRocketAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] Rocket animations disabled, processing path without animation");
            // process path immediately if animations disabled
            float delay = 0f;
            foreach (Vector2Int pos in path)
            {
                if (pos != rocket.GetGridPosition()) // skip rocket position
                {
                    DOVirtual.DelayedCall(delay, () => onHitPosition?.Invoke(pos));
                    delay += 0.05f;
                }
            }
            return;
        }

        // increment active rocket animation count
        IncrementRocketAnimationCount();

        if (debugMode) Debug.Log("[AnimationManager] Starting rocket projectile animation");

        // delegate to RocketAnimations without decrementing counter in callback
        RocketAnimations.AnimateRocketProjectile(rocket, path, onHitPosition, null);
    }

    // animate rockets combining to create a special explosion
    public void AnimateRocketCombineSpecial(List<Rocket> rockets, Vector2Int targetPosition, System.Action<bool> onComplete = null)
    {
        // early validation
        if (rockets == null || rockets.Count < 2)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Invalid rocket list for special combine animation");
            onComplete?.Invoke(false);
            return;
        }

        if (!AreRocketAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] Rocket animations disabled, skipping rocket combine special animation");
            onComplete?.Invoke(true); // still report success so gameplay can continue
            return;
        }

        // increment active rocket animation count
        IncrementRocketAnimationCount();

        if (debugMode) Debug.Log("[AnimationManager] Starting rocket combine special animation");

        // Use the original onComplete callback without decrementing
        RocketAnimations.PlayRocketCombineSpecialAnimation(rockets, targetPosition, onComplete);
    }

    // animate rocket combination with multiple projectiles
    public void AnimateRocketCombination(Vector2Int rocketPosition, GridManager gridManager, List<List<Vector2Int>> paths, System.Action<Vector2Int> onHitPosition = null, System.Action onComplete = null)
    {
        // early validation
        if (gridManager == null || paths == null || paths.Count == 0)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Invalid parameters for rocket combination animation");
            onComplete?.Invoke();
            return;
        }

        if (!AreRocketAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] Rocket animations disabled, processing paths without animation");
            // process all paths immediately if animations disabled
            foreach (var path in paths)
            {
                foreach (Vector2Int pos in path)
                {
                    if (pos != rocketPosition) // skip rocket position
                    {
                        onHitPosition?.Invoke(pos);
                    }
                }
            }
            onComplete?.Invoke();
            return;
        }

        // increment active rocket animation count
        IncrementRocketAnimationCount();

        if (debugMode) Debug.Log("[AnimationManager] Starting rocket combination animation");

        // Use the original onComplete callback without decrementing
        RocketAnimations.AnimateRocketCombination(rocketPosition, gridManager, paths, onHitPosition, onComplete);
    }

    // animate cube rocket indicator
    public void AnimateCubeRocketIndicator(Cube cube, bool show, Sprite defaultSprite, Sprite rocketSprite, float glowDuration = 0.9f, float glowIntensity = 1.2f)
    {
        // early validation
        if (cube == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to animate rocket indicator of null cube");
            return;
        }

        if (!AreCubeAnimationsEnabled())
        {
            // if animations disabled, just switch sprite directly
            SpriteRenderer spriteRenderer = cube.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = show ? rocketSprite : defaultSprite;
            }
            if (debugMode) Debug.Log("[AnimationManager] Cube animations disabled, changing sprite directly");
            return;
        }

        if (debugMode) Debug.Log($"[AnimationManager] Starting cube rocket indicator animation, show={show}");

        // delegate to CubeAnimations
        CubeAnimations.PlayRocketIndicatorAnimation(cube, show, defaultSprite, rocketSprite, glowDuration, glowIntensity);
    }

    // animate grid appearing from bottom
    public void AnimateGridAppearance(Transform gridCellsContainer, Transform gridBackground, float duration = 0.8f, float offset = 20f, System.Action onComplete = null)
    {
        // early validation
        if (gridCellsContainer == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to animate grid appearance with null container");
            onComplete?.Invoke();
            return;
        }

        if (!AreUIAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] UI animations disabled, showing grid immediately");

            // make elements visible without animation
            gridCellsContainer.gameObject.SetActive(true);
            if (gridBackground != null)
                gridBackground.gameObject.SetActive(true);

            onComplete?.Invoke();
            return;
        }

        if (debugMode) Debug.Log("[AnimationManager] Starting grid appearance animation");

        // delegate to GeneralAnimations
        GeneralAnimations.PlayGridAppearAnimation(gridCellsContainer, gridBackground, duration, offset, onComplete);
    }

    // animate header appearing from top
    public void AnimateHeaderAppearance(Transform headerTransform, float duration = 0.5f, float offset = 5f, System.Action onComplete = null)
    {
        // early validation
        if (headerTransform == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to animate header appearance with null transform");
            onComplete?.Invoke();
            return;
        }

        if (!AreUIAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] UI animations disabled, showing header immediately");
            onComplete?.Invoke();
            return;
        }

        if (debugMode) Debug.Log("[AnimationManager] Starting header appearance animation");

        // delegate to GeneralAnimations
        GeneralAnimations.PlayHeaderAppearAnimation(headerTransform, duration, offset, onComplete);
    }

    // animate level button appearing from bottom
    public void AnimateLevelButtonAppearance(Transform buttonTransform, float duration = 0.6f, float offset = 8f, System.Action onComplete = null)
    {
        // early validation
        if (buttonTransform == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to animate level button appearance with null transform");
            onComplete?.Invoke();
            return;
        }

        if (!AreUIAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] UI animations disabled, showing level button immediately");

            // ensure button is visible even without animation
            buttonTransform.gameObject.SetActive(true);

            // invoke callback
            onComplete?.Invoke();
            return;
        }

        if (debugMode) Debug.Log("[AnimationManager] Starting level button appearance animation");

        // delegate to GeneralAnimations
        GeneralAnimations.PlayLevelButtonAppearAnimation(buttonTransform, duration, offset, onComplete);
    }

    // animate cube shake for invalid move
    public void AnimateInvalidMove(Cube cube, float duration = 0.15f, float strength = 0.1f)
    {
        // early validation
        if (cube == null)
        {
            if (debugMode) Debug.LogWarning("[AnimationManager] Attempted to animate invalid move of null cube");
            return;
        }

        if (!AreCubeAnimationsEnabled())
        {
            if (debugMode) Debug.Log("[AnimationManager] Cube animations disabled, skipping invalid move animation");
            return;
        }

        if (debugMode) Debug.Log("[AnimationManager] Starting invalid move shake animation");

        // delegate to CubeAnimations
        CubeAnimations.PlayInvalidMoveShake(cube, duration, strength);
    }

    // toggle all animation types on/off
    public void SetAllAnimationTypesEnabled(bool enabled)
    {
        cubeAnimationsEnabled = enabled;
        rocketAnimationsEnabled = enabled;
        obstacleAnimationsEnabled = enabled;
        uiAnimationsEnabled = enabled;
        fallingAnimationsEnabled = enabled;
        spawnAnimationsEnabled = enabled;

        if (debugMode) Debug.Log($"[AnimationManager] All animation types {(enabled ? "enabled" : "disabled")}");
    }

    // marker component to prevent multiple destroy animations
    private class CubeDestructionMarker : MonoBehaviour { }
}