using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class RocketAnimations : MonoBehaviour
{
    private static AnimationManager animManager;
    private static ParticleManager particleManager;

    // prefab paths for directional projectiles
    private static readonly string UP_PROJECTILE_PATH = "Prefabs/UpProjectile";
    private static readonly string DOWN_PROJECTILE_PATH = "Prefabs/DownProjectile";
    private static readonly string LEFT_PROJECTILE_PATH = "Prefabs/LeftProjectile";
    private static readonly string RIGHT_PROJECTILE_PATH = "Prefabs/RightProjectile";

    // initialize references
    private static bool InitializeReferences()
    {
        try
        {
            if (animManager == null)
            {
                animManager = AnimationManager.Instance;
            }

            if (animManager != null)
            {
                particleManager = animManager.GetParticleManager();
                if (IsDebugEnabled()) Debug.Log("[RocketAnimations] Successfully initialized references");
                return true;
            }

            Debug.LogError("[RocketAnimations] Failed to find AnimationManager instance");
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RocketAnimations] Error during initialization: {e}");
            return false;
        }
    }

    // check if debug mode is enabled
    private static bool IsDebugEnabled()
    {
        return animManager != null && animManager.IsDebugEnabled();
    }

    // helper method to kill tweens on a specific object
    public static void KillTweensOnObject(Object target)
    {
        if (target == null) return;

        DOTween.Kill(target);

        if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Killed tweens on {target.name}");
    }

    // helper method to kill all tweens on a rocket and its children
    public static void KillAllTweens(GameObject rocketObject)
    {
        if (rocketObject == null) return;

        // kill tweens on the rocket itself
        DOTween.Kill(rocketObject.transform);

        // kill tweens on all children recursively
        foreach (Transform child in rocketObject.transform)
        {
            DOTween.Kill(child);
        }

        // kill tweens on components
        foreach (Component component in rocketObject.GetComponents<Component>())
        {
            if (component != null)
            {
                DOTween.Kill(component);
            }
        }

        if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Killed all tweens on {rocketObject.name} and children");
    }

    // configure both trail and smoke particle systems for rocket projectiles
    private static void ConfigureDualEffects(ParticleSystem trailEffect, ParticleSystem smokeEffect)
    {
        // configure trail effect (rocketstar)
        if (trailEffect != null)
        {
            ConfigureTrailEffect(trailEffect);
            if (IsDebugEnabled()) Debug.Log("[RocketAnimations] Configured trail effect");
        }

        // configure smoke effect
        if (smokeEffect != null)
        {
            ConfigureSmokeEffect(smokeEffect);
            if (IsDebugEnabled()) Debug.Log("[RocketAnimations] Configured smoke effect");
        }
    }

    // configure smoke particle system for rocket projectiles
    private static void ConfigureSmokeEffect(ParticleSystem smoke)
    {
        if (smoke == null) return;

        // clear existing particles before configuration
        smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Get multipliers from animation manager
        float emissionMultiplier = animManager.GetRocketSmokeEmissionMultiplier();
        float sizeMultiplier = animManager.GetRocketSmokeSizeMultiplier();
        float burstMultiplier = animManager.GetParticleBurstMultiplier();

        // main module - core particle properties
        // experimental values, these seem to work well
        var main = smoke.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f); // balanced lifetime
        main.startSize = new ParticleSystem.MinMaxCurve(
            0.25f * sizeMultiplier,
            0.4f * sizeMultiplier); // balanced particle size with multiplier
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.95f, 0.7f, 0.25f),  // softer yellow, balanced opacity
            new Color(1f, 0.9f, 0.5f, 0.18f)    // golden yellow, balanced opacity
        );
        main.startSpeed = 0.12f;                // balanced speed
        main.gravityModifier = -0.02f;         // very slight upward drift

        var emission = smoke.emission;
        emission.rateOverTime = 25f * emissionMultiplier;           // balanced emission rate with multiplier
        emission.rateOverDistance = 18f * emissionMultiplier;       // balanced trail density with multiplier

        var shape = smoke.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.09f;                  // balanced radius

        // gentle velocity for soft movement
        var velocityOverLifetime = smoke.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        // Use ParticleSystemCurveMode.TwoConstants for all curves to ensure consistency
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.055f, 0.055f) { mode = ParticleSystemCurveMode.TwoConstants };
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.055f, 0.055f) { mode = ParticleSystemCurveMode.TwoConstants };
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.055f, 0.055f) { mode = ParticleSystemCurveMode.TwoConstants };

        // color over lifetime - how particles change color
        var colorOverLifetime = smoke.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
            new GradientColorKey(new Color(1f, 0.95f, 0.7f), 0.0f),
            new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0.6f),
            new GradientColorKey(new Color(0.9f, 0.8f, 0.3f), 1.0f)
            },
            new GradientAlphaKey[] {
            new GradientAlphaKey(0.3f, 0.0f),  // start with balanced opacity
            new GradientAlphaKey(0.2f, 0.5f),  // balanced fade
            new GradientAlphaKey(0.0f, 1.0f)    // fade to transparent
            }
        );
        colorOverLifetime.color = gradient;

        // size over lifetime - balanced expansion
        var sizeOverLifetime = smoke.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.75f);    // start at 75% size
        sizeCurve.AddKey(0.3f, 0.85f);  // Balanced growth
        sizeCurve.AddKey(0.7f, 1.05f);  // balanced expansion
        sizeCurve.AddKey(1f, 1.15f);    // end slightly larger
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // very gentle rotation for subtle movement
        var rotationOverLifetime = smoke.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-6f, 6f); // balanced rotation

        // renderer settings
        var renderer = smoke.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.YoungestInFront;
            renderer.minParticleSize = 0.12f * sizeMultiplier;  // balanced minimum size with multiplier
            renderer.maxParticleSize = 1.6f * sizeMultiplier;   // balanced maximum size with multiplier
        }

        // play the effect
        smoke.Play();
    }

    // animate rocket projectile moving along an explosion path 
    public static void AnimateRocketProjectile(Rocket rocket, List<Vector2Int> path, System.Action<Vector2Int> onHitPosition = null, System.Action onComplete = null)
    {
        if (!InitializeReferences() || rocket == null || path == null || path.Count == 0)
        {
            if (IsDebugEnabled()) Debug.LogWarning("[RocketAnimations] Invalid parameters for rocket projectile animation");
            onComplete?.Invoke();
            return;
        }

        // Kill any existing animations on this rocket
        if (rocket.gameObject != null)
            KillAllTweens(rocket.gameObject);

        if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Starting rocket projectile animation for path with {path.Count} points");

        // get rocket direction and position
        Rocket.RocketDirection direction = rocket.GetDirection();
        Vector2Int rocketPos = rocket.GetGridPosition();

        // determine which projectile prefab to use based on direction and path
        string prefabPath;

        // we need at least 2 points to determine direction (rocket position and one target)
        if (path.Count < 2)
        {
            // default to a direction based on rocket type if path is too short
            if (direction == Rocket.RocketDirection.Horizontal)
                prefabPath = RIGHT_PROJECTILE_PATH; // default for horizontal
            else
                prefabPath = UP_PROJECTILE_PATH; // default for vertical

            if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Using default projectile direction due to short path");
        }
        else
        {
            // get the second point in the path (first after rocket position)
            Vector2Int secondPoint = path[1];

            // determine direction based on the position difference
            if (secondPoint.x < rocketPos.x)
            {
                prefabPath = LEFT_PROJECTILE_PATH;
                if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Using LEFT projectile for path");
            }
            else if (secondPoint.x > rocketPos.x)
            {
                prefabPath = RIGHT_PROJECTILE_PATH;
                if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Using RIGHT projectile for path");
            }
            else if (secondPoint.y < rocketPos.y)
            {
                prefabPath = DOWN_PROJECTILE_PATH;
                if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Using DOWN projectile for path");
            }
            else
            {
                prefabPath = UP_PROJECTILE_PATH;
                if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Using UP projectile for path");
            }
        }

        // load the projectile prefab
        GameObject projectilePrefab = Resources.Load<GameObject>(prefabPath);
        if (projectilePrefab == null)
        {
            Debug.LogError($"[RocketAnimations] Failed to load projectile prefab: {prefabPath}");

            // process all positions immediately if prefab can't be loaded
            float delay = 0f;
            foreach (Vector2Int pos in path)
            {
                DOVirtual.DelayedCall(delay, () => onHitPosition?.Invoke(pos));
                delay += 0.05f;
            }

            // complete the animation immediately
            onComplete?.Invoke();
            return;
        }

        // instantiate the projectile
        Vector3 rocketPosition = rocket.transform.position;
        GameObject projectileObj = GameObject.Instantiate(projectilePrefab, rocketPosition, Quaternion.identity);

        // get animation settings
        float projectileSpeed = animManager.GetRocketProjectileSpeed();

        // create both trail and smoke effects
        ParticleSystem trailEffect = null;
        ParticleSystem smokeEffect = null;

        if (particleManager != null)
        {
            // create RocketStar effect
            trailEffect = particleManager.PlayEffect("RocketStar", projectileObj.transform.position, 2.0f, projectileObj.transform);

            // create RocketSmoke effect
            smokeEffect = particleManager.PlayEffect("RocketSmoke", projectileObj.transform.position, 2.0f, projectileObj.transform);

            // configure both effects
            ConfigureDualEffects(trailEffect, smokeEffect);
        }

        // check if there's a grid manager to get cell positions
        GridManager gridManager = rocket.GetGridManager();
        if (gridManager == null)
        {
            CleanupProjectile(projectileObj, trailEffect, smokeEffect);
            onComplete?.Invoke();
            return;
        }

        // create sequence for path animation
        Sequence pathSequence = DOTween.Sequence();

        // add animations for each path point
        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int targetPosition = path[i];
            Vector3 worldPos = gridManager.GridToWorldPosition(targetPosition.x, targetPosition.y);

            // calculate distance and duration
            float distance = Vector3.Distance(
                i == 0 ? projectileObj.transform.position : gridManager.GridToWorldPosition(path[i - 1].x, path[i - 1].y),
                worldPos
            );
            float duration = distance / projectileSpeed;

            // skip if already at position
            if (duration < 0.01f)
            {
                int index = i; // capture for callback
                pathSequence.AppendCallback(() => onHitPosition?.Invoke(path[index]));
                continue;
            }

            // move projectile to the target position (captured in scope)
            int currentIndex = i;
            pathSequence.Append(
                // check if projectile transform exists before tweening
                projectileObj != null && projectileObj.transform != null ?
                projectileObj.transform.DOMove(worldPos, duration)
                .SetEase(Ease.Linear)
                .OnUpdate(() =>
                {
                    // check if target was destroyed during tween
                    if (projectileObj == null || projectileObj.transform == null)
                    {
                        DOTween.Kill(projectileObj.transform);
                    }
                })
                .OnKill(() =>
                {
                    // If killed, still try to process the hit
                    if (currentIndex < path.Count)
                    {
                        onHitPosition?.Invoke(path[currentIndex]);
                    }
                })
                .OnComplete(() =>
                {
                    // call the hit callback for this position
                    onHitPosition?.Invoke(targetPosition);

                    // show impact effect
                    if (particleManager != null)
                    {
                        particleManager.PlayEffect("RocketStar", worldPos, 0.5f);
                    }
                }) : DOTween.Sequence() // empty sequence if projectile is null
            );
            // small pause
            if (i < path.Count - 1)
            {
                pathSequence.AppendInterval(0.03f);
            }

        }

        // Clean up projectile when sequence completes
        pathSequence.OnComplete(() =>
        {
            // wait for 0.1 seconds before cleaning up
            CleanupProjectile(projectileObj, trailEffect, smokeEffect);

            // invoke completion callback
            onComplete?.Invoke();

            if (IsDebugEnabled()) Debug.Log("[RocketAnimations] Rocket projectile animation completed");
        });

        // Add safety handler for if sequence is killed
        pathSequence.OnKill(() =>
        {
            if (IsDebugEnabled()) Debug.Log("[RocketAnimations] Rocket projectile animation killed, cleaning up");

            CleanupProjectile(projectileObj, trailEffect, smokeEffect);

            // Ensure callback is still invoked
            onComplete?.Invoke();
        });
    }

    // Helper to safely clean up projectile and particle effects
    private static void CleanupProjectile(GameObject projectileObj, ParticleSystem trailEffect, ParticleSystem smokeEffect)
    {
        if (projectileObj == null) return;

        // Kill any tweens on the projectile
        KillAllTweens(projectileObj);

        // Clean up particle effects
        if (trailEffect != null)
        {
            trailEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            trailEffect.transform.parent = null;
            GameObject.Destroy(trailEffect.gameObject, trailEffect.main.startLifetimeMultiplier);
        }

        if (smokeEffect != null)
        {
            smokeEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            smokeEffect.transform.parent = null;
            GameObject.Destroy(smokeEffect.gameObject, smokeEffect.main.startLifetimeMultiplier);
        }

        // Destroy the projectile
        GameObject.Destroy(projectileObj);
    }

    // helper method for camera shake effect
    private static void CreateCameraShake(float intensity, float duration)
    {
        // get main camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        // store original position
        Vector3 originalPos = mainCamera.transform.position;

        // create shake sequence
        Sequence shakeSequence = DOTween.Sequence();

        // number of shakes proportional to duration
        int shakeCount = Mathf.Max(3, Mathf.FloorToInt(duration * 20));

        // add shake effect
        for (int i = 0; i < shakeCount; i++)
        {
            // check if camera transform exists before tweening
            if (mainCamera != null && mainCamera.transform != null)
            {
                shakeSequence.Append(mainCamera.transform.DOMove(
                    originalPos + new Vector3(Random.Range(-intensity, intensity),
                                            Random.Range(-intensity, intensity), 0),
                    duration / shakeCount
                ).OnUpdate(() =>
                {
                    // check if target was destroyed during tween
                    if (mainCamera == null || mainCamera.transform == null)
                    {
                        DOTween.Kill(mainCamera.transform);
                    }
                }));
            }
        }

        // return to original position
        if (mainCamera != null && mainCamera.transform != null)
        {
            shakeSequence.Append(mainCamera.transform.DOMove(originalPos, 0.1f)
            .OnUpdate(() =>
            {
                // check if target was destroyed during tween
                if (mainCamera == null || mainCamera.transform == null)
                {
                    DOTween.Kill(mainCamera.transform);
                }
            }));
        }

        if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Camera shake with intensity {intensity} and duration {duration}");
    }

    // animate a rocket combination explosion with multiple projectiles using DOTween
    public static void AnimateRocketCombination(Vector2Int rocketPosition, GridManager gridManager, List<List<Vector2Int>> paths, System.Action<Vector2Int> onHitPosition = null, System.Action onComplete = null)
    {
        if (!InitializeReferences() || gridManager == null || paths.Count == 0)
        {
            if (IsDebugEnabled()) Debug.LogWarning("[RocketAnimations] Invalid parameters for rocket combination animation");
            return;
        }

        if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Starting rocket combination animation with {paths.Count} paths");



        // get camera shake parameters for combo explosion
        float shakeIntensity = animManager.GetComboExplosionShakeIntensity();
        float shakeDuration = animManager.GetComboExplosionShakeDuration();

        // apply camera shake for combo explosion
        CreateCameraShake(shakeIntensity, shakeDuration);


        // get the world position of the rocket
        Vector3 rocketWorldPos = gridManager.GridToWorldPosition(rocketPosition.x, rocketPosition.y);

        // create master sequence for all animations
        Sequence masterSequence = DOTween.Sequence();

        // track all path sequences
        List<Sequence> pathSequences = new List<Sequence>();

        // group paths by direction for consistent projectile types
        List<List<Vector2Int>> upPaths = new List<List<Vector2Int>>();
        List<List<Vector2Int>> downPaths = new List<List<Vector2Int>>();
        List<List<Vector2Int>> leftPaths = new List<List<Vector2Int>>();
        List<List<Vector2Int>> rightPaths = new List<List<Vector2Int>>();

        // categorize paths based on their direction
        foreach (var path in paths)
        {
            if (path.Count < 2) continue;

            Vector2Int startPos = path[0];
            Vector2Int secondPos = path[1];

            // determine direction based on first two points
            if (secondPos.y > startPos.y)
            {
                if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Adding UP path starting from ({startPos.x}, {startPos.y})");
                upPaths.Add(path);
            }
            else if (secondPos.y < startPos.y)
            {
                if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Adding DOWN path starting from ({startPos.x}, {startPos.y})");
                downPaths.Add(path);
            }
            else if (secondPos.x < startPos.x)
            {
                if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Adding LEFT path starting from ({startPos.x}, {startPos.y})");
                leftPaths.Add(path);
            }
            else if (secondPos.x > startPos.x)
            {
                if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Adding RIGHT path starting from ({startPos.x}, {startPos.y})");
                rightPaths.Add(path);
            }
        }

        if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Path counts - Up: {upPaths.Count}, Down: {downPaths.Count}, Left: {leftPaths.Count}, Right: {rightPaths.Count}");

        // process up paths
        foreach (var path in upPaths)
        {
            Sequence pathSequence = DOTween.Sequence();
            pathSequences.Add(pathSequence);

            if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Creating UP projectile for path starting at ({path[0].x}, {path[0].y})");
            AnimateComboPath(
                rocketPosition,
                gridManager,
                path,
                UP_PROJECTILE_PATH,
                onHitPosition,
                pathSequence
            );
        }

        // process down paths
        foreach (var path in downPaths)
        {
            Sequence pathSequence = DOTween.Sequence();
            pathSequences.Add(pathSequence);

            if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Creating DOWN projectile for path starting at ({path[0].x}, {path[0].y})");
            AnimateComboPath(
                rocketPosition,
                gridManager,
                path,
                DOWN_PROJECTILE_PATH,
                onHitPosition,
                pathSequence
            );
        }

        // process left paths
        foreach (var path in leftPaths)
        {
            Sequence pathSequence = DOTween.Sequence();
            pathSequences.Add(pathSequence);

            if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Creating LEFT projectile for path starting at ({path[0].x}, {path[0].y})");
            AnimateComboPath(
                rocketPosition,
                gridManager,
                path,
                LEFT_PROJECTILE_PATH,
                onHitPosition,
                pathSequence
            );
        }

        // process right paths
        foreach (var path in rightPaths)
        {
            Sequence pathSequence = DOTween.Sequence();
            pathSequences.Add(pathSequence);

            if (IsDebugEnabled()) Debug.Log($"[RocketAnimations] Creating RIGHT projectile for path starting at ({path[0].x}, {path[0].y})");
            AnimateComboPath(
                rocketPosition,
                gridManager,
                path,
                RIGHT_PROJECTILE_PATH,
                onHitPosition,
                pathSequence
            );
        }

        // join all path sequences to the master sequence
        foreach (var sequence in pathSequences)
        {
            masterSequence.Join(sequence);
        }

        // small buffer to ensure all effects are finished
        masterSequence.AppendInterval(0f);

        // invoke the completion callback if provided
        masterSequence.OnComplete(() =>
        {
            if (IsDebugEnabled()) Debug.Log("[RocketAnimations] All combination paths complete");


            onComplete?.Invoke();
        });
    }

    // animate a single combo path with a projectile using dotween
    private static void AnimateComboPath(
        Vector2Int rocketPosition,
        GridManager gridManager,
        List<Vector2Int> path,
        string prefabPath,
        System.Action<Vector2Int> onHitPosition,
        Sequence pathSequence)
    {
        // validate the path
        if (path == null || path.Count == 0)
        {
            if (IsDebugEnabled()) Debug.LogWarning("[RocketAnimations] Invalid path for combo animation");
            return;
        }

        // load projectile prefab
        GameObject projectilePrefab = Resources.Load<GameObject>(prefabPath);
        if (projectilePrefab == null)
        {
            if (IsDebugEnabled()) Debug.LogError($"[RocketAnimations] Failed to load projectile prefab: {prefabPath}");
            // process path without animation if prefab loading fails
            float delay = 0f;
            foreach (Vector2Int pos in path)
            {
                int index = path.IndexOf(pos);
                pathSequence.InsertCallback(delay, () => onHitPosition?.Invoke(pos));
                delay += 0.05f;
            }
            return;
        }

        // create projectile at rocket position
        Vector3 rocketWorldPos = gridManager.GridToWorldPosition(rocketPosition.x, rocketPosition.y);
        GameObject projectile = GameObject.Instantiate(projectilePrefab, rocketWorldPos, Quaternion.identity);

        // add both trail and smoke effects
        ParticleSystem trailEffect = null;
        ParticleSystem smokeEffect = null;

        if (particleManager != null)
        {
            // create RocketStar effect
            trailEffect = particleManager.PlayEffect("RocketStar", projectile.transform.position, 2.0f, projectile.transform);

            // create RocketSmoke effect
            smokeEffect = particleManager.PlayEffect("RocketSmoke", projectile.transform.position, 2.0f, projectile.transform);

            // configure both effects
            ConfigureDualEffects(trailEffect, smokeEffect);
        }

        // get animation settings
        float projectileSpeed = animManager.GetRocketProjectileSpeed();

        // skip index 0 which is the rocket position itself
        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int targetPos = path[i];
            Vector3 targetWorldPos = gridManager.GridToWorldPosition(targetPos.x, targetPos.y);

            // calculate distance and duration
            Vector3 fromPos = i == 1 ? rocketWorldPos : gridManager.GridToWorldPosition(path[i - 1].x, path[i - 1].y);
            float distance = Vector3.Distance(fromPos, targetWorldPos);
            float duration = distance / projectileSpeed;

            // skip if already at position
            if (duration < 0.01f)
            {
                int index = i; // capture for callback
                pathSequence.AppendCallback(() => onHitPosition?.Invoke(path[index]));
                continue;
            }

            int currentIndex = i; // capture for closure

            // move projectile
            pathSequence.Append(
                // check if projectile transform exists before tweening
                projectile != null && projectile.transform != null ?
                projectile.transform.DOMove(targetWorldPos, duration)
                .SetEase(Ease.Linear)
                .OnUpdate(() =>
                {
                    // check if target was destroyed during tween
                    if (projectile == null || projectile.transform == null)
                    {
                        DOTween.Kill(projectile.transform);
                        return;
                    }

                    // ensure both effects are emitting as projectile moves
                    if (trailEffect != null && !trailEffect.isPlaying)
                    {
                        trailEffect.Play();
                    }

                    if (smokeEffect != null && !smokeEffect.isPlaying)
                    {
                        smokeEffect.Play();
                    }
                })
                .OnComplete(() =>
                {
                    // trigger hit
                    onHitPosition?.Invoke(targetPos);

                    // show impact
                    if (particleManager != null)
                    {
                        particleManager.PlayEffect("RocketStar", targetWorldPos, 0.5f);
                    }
                }) : DOTween.Sequence() // empty sequence if projectile is null
            );

            // small pause
            if (i < path.Count - 1)
            {
                pathSequence.AppendInterval(0.03f);
            }
        }

        // clean up on completion
        pathSequence.OnComplete(() =>
        {
            if (trailEffect != null)
            {
                trailEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                trailEffect.transform.parent = null;
                GameObject.Destroy(trailEffect.gameObject, trailEffect.main.startLifetimeMultiplier);
            }

            if (smokeEffect != null)
            {
                smokeEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                smokeEffect.transform.parent = null;
                GameObject.Destroy(smokeEffect.gameObject, smokeEffect.main.startLifetimeMultiplier);
            }

            Destroy(projectile);
        });
    }


    /// configure trail particle system for rocket projectiles
    private static void ConfigureTrailEffect(ParticleSystem trail)
    {
        if (trail == null) return;

        // clear any existing particles and stop to configure before playing
        trail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Get multipliers from animation manager
        float emissionMultiplier = animManager.GetRocketStarEmissionMultiplier();
        float sizeMultiplier = animManager.GetRocketStarSizeMultiplier();
        float burstMultiplier = animManager.GetParticleBurstMultiplier();

        // main module settings
        // experimental vales, these seem to work well
        var main = trail.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startSize = new ParticleSystem.MinMaxCurve(
            0.4f * sizeMultiplier,
            0.7f * sizeMultiplier); // apply size multiplier
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.9f, 0.2f, 1f),
            new Color(1f, 0.7f, 0.0f, 1f)
        );
        main.startSpeed = 0f;
        main.gravityModifier = 0f;

        var emission = trail.emission;
        emission.rateOverTime = 50f * emissionMultiplier; // apply emission multiplier
        emission.rateOverDistance = 20f * emissionMultiplier; // apply emission multiplier

        // shape module for trail spread
        var shape = trail.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var velocityOverLifetime = trail.velocityOverLifetime;
        velocityOverLifetime.enabled = false;
        // Fix: explicitly set all curves to use TwoConstants mode for consistency
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0f, 0f) { mode = ParticleSystemCurveMode.TwoConstants };
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0f, 0f) { mode = ParticleSystemCurveMode.TwoConstants };
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f) { mode = ParticleSystemCurveMode.TwoConstants };

        // color over lifetime for fade out
        var colorOverLifetime = trail.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0.0f),
                new GradientColorKey(new Color(1f, 0.7f, 0.0f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.8f, 0.7f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorOverLifetime.color = gradient;

        // size over lifetime for better visual effect
        var sizeOverLifetime = trail.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.8f);
        sizeCurve.AddKey(0.2f, 1.0f);
        sizeCurve.AddKey(0.7f, 0.9f);
        sizeCurve.AddKey(1f, 0.1f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // renderer settings
        var renderer = trail.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.YoungestInFront;
            renderer.maxParticleSize = 2.0f * sizeMultiplier; // apply size multiplier
        }

        // play the effect
        trail.Play();
    }

    /// animate rocket explosion with particles 
    public static void AnimateRocketExplosion(Vector3 position, System.Action onComplete = null)
    {
        if (!InitializeReferences())
        {
            onComplete?.Invoke();
            return;
        }

        // create a temporary game object for the explosion
        GameObject explosionObj = new GameObject("RocketExplosion");
        explosionObj.transform.position = position;

        if (particleManager != null)
        {
            // play explosion effect
            ParticleSystem starEffect = particleManager.PlayEffect("RocketStar", position, 1.0f);

            // Apply particle density configuration if effect was created
            if (starEffect != null)
            {
                float emissionMultiplier = animManager.GetRocketStarEmissionMultiplier();
                float sizeMultiplier = animManager.GetRocketStarSizeMultiplier();
                float burstMultiplier = animManager.GetParticleBurstMultiplier();

                var main = starEffect.main;
                main.startSize = new ParticleSystem.MinMaxCurve(
                    main.startSize.constantMin * sizeMultiplier,
                    main.startSize.constantMax * sizeMultiplier
                );

                var emission = starEffect.emission;
                if (emission.enabled)
                {
                    emission.rateOverTime = emission.rateOverTime.constant * emissionMultiplier;
                    emission.rateOverDistance = emission.rateOverDistance.constant * emissionMultiplier;

                    // Apply burst multiplier if there are any bursts
                    if (emission.burstCount > 0)
                    {
                        for (int i = 0; i < emission.burstCount; i++)
                        {
                            ParticleSystem.Burst burst = emission.GetBurst(i);
                            burst.count = burst.count.constant * burstMultiplier;
                            emission.SetBurst(i, burst);
                        }
                    }
                }
            }
        }

        // get camera shake parameters
        float shakeIntensity = animManager.GetRocketExplosionShakeIntensity();
        float shakeDuration = animManager.GetRocketExplosionShakeDuration();

        // apply camera shake
        CreateCameraShake(shakeIntensity, shakeDuration);

        // create a sequence to handle the explosion timing
        Sequence explosionSequence = DOTween.Sequence();

        // wait for effects to finish playing
        explosionSequence.AppendInterval(1.0f);

        // clean up temporary object and notify completion
        explosionSequence.OnComplete(() =>
        {
            Destroy(explosionObj);

            // invoke completion callback
            onComplete?.Invoke();
        });
    }

    /// animate multiple rockets combining to create a special explosion
    public static void PlayRocketCombineSpecialAnimation(List<Rocket> rockets, Vector2Int targetPosition, System.Action<bool> onComplete = null)
    {
        if (!InitializeReferences() || rockets == null || rockets.Count < 2)
        {
            if (IsDebugEnabled()) Debug.LogWarning("[RocketAnimations] Failed to initialize references or invalid rocket list for special combine");
            onComplete?.Invoke(false);
            return;
        }

        // directly find the rocket at target position for efficiency
        Rocket targetRocket = rockets.Find(r => r != null && r.GetGridPosition() == targetPosition);

        if (targetRocket == null)
        {
            if (IsDebugEnabled()) Debug.LogError("[RocketAnimations] Could not find rocket at target position");
            onComplete?.Invoke(false);
            return;
        }

        // get target world position and grid manager
        Vector3 targetWorldPos = targetRocket.transform.position;
        GridManager gridManager = targetRocket.GetGridManager();

        if (gridManager == null)
        {
            if (IsDebugEnabled()) Debug.LogError("[RocketAnimations] Could not determine grid manager for special explosion");
            onComplete?.Invoke(false);
            return;
        }

        if (IsDebugEnabled()) Debug.Log("[RocketAnimations] Starting rocket combine special animation");

        // cached animation settings
        float duration = animManager.GetRocketCreateDuration();
        float speedMultiplier = animManager.GetAnimationSpeed();
        Ease moveEase = Ease.InOutQuad;

        // pre-allocate lists with capacity
        int validRocketCount = rockets.Count;
        List<Transform> rocketTransforms = new List<Transform>(validRocketCount);
        List<Vector3> startPositions = new List<Vector3>(validRocketCount);
        List<Vector3> startScales = new List<Vector3>(validRocketCount);
        List<SpriteRenderer> renderers = new List<SpriteRenderer>(validRocketCount);
        List<Color> startColors = new List<Color>(validRocketCount);
        List<Vector3> midPositions = new List<Vector3>(validRocketCount);

        // create master sequence upfront
        Sequence masterSequence = DOTween.Sequence();

        // process and prepare rockets
        foreach (Rocket rocket in rockets)
        {
            if (rocket == null) continue;

            Transform transform = rocket.transform;
            SpriteRenderer renderer = rocket.GetComponent<SpriteRenderer>();

            if (transform != null && renderer != null)
            {
                // collect references
                rocketTransforms.Add(transform);
                Vector3 startPos = transform.position;
                startPositions.Add(startPos);
                startScales.Add(transform.localScale);
                renderers.Add(renderer);
                startColors.Add(renderer.color);

                // calculate mid-position in one pass
                Vector3 directionFromTarget = (startPos - targetWorldPos).normalized;
                midPositions.Add(startPos + (directionFromTarget * 0.4f));

                // clear cell and disable rocket
                Vector2Int rocketPos = rocket.GetGridPosition();
                GridCell cell = gridManager.GetCell(rocketPos.x, rocketPos.y);
                if (cell != null)
                {
                    cell.ClearItem();
                }
                rocket.enabled = false;
            }
        }

        // play one initial particle effect 
        if (particleManager != null)
        {
            particleManager.PlayEffect("RocketStar", targetWorldPos, 1.0f);
        }

        // create animations for each rocket
        for (int i = 0; i < rocketTransforms.Count; i++)
        {
            Transform trans = rocketTransforms[i];
            SpriteRenderer renderer = renderers[i];

            // skip invalid references
            if (trans == null || renderer == null) continue;

            // single sequence per rocket for better performance
            Sequence rocketSequence = DOTween.Sequence();

            // reusable null check for transform
            System.Func<bool> isTransformValid = () => trans != null;

            // reusable null check for renderer
            System.Func<bool> isRendererValid = () => renderer != null;

            // first movement - outward
            rocketSequence.Append(
                trans.DOMove(midPositions[i], duration * 0.3f / speedMultiplier)
                .SetEase(moveEase)
                .SetTarget(trans) // better target handling
                .OnUpdate(() =>
                {
                    if (!isTransformValid()) DOTween.Kill(trans);
                })
            );

            // rotation during first movement
            rocketSequence.Join(
                trans.DORotate(new Vector3(0, 0, Random.Range(-30f, 30f)), duration * 0.3f / speedMultiplier)
                .SetEase(moveEase)
                .SetTarget(trans) // better target handling
            );

            // second movement - to target
            rocketSequence.Append(
                trans.DOMove(targetWorldPos, duration * 0.7f / speedMultiplier)
                .SetEase(moveEase)
                .SetTarget(trans) // better target handling
            );

            // scaling during second movement
            rocketSequence.Join(
                trans.DOScale(startScales[i] * 0.7f, duration * 0.7f / speedMultiplier)
                .SetEase(moveEase)
                .SetTarget(trans) // better target handling
            );

            // rotation during second movement
            rocketSequence.Join(
                trans.DORotate(new Vector3(0, 0, Random.Range(-90f, 90f)), duration * 0.7f / speedMultiplier)
                .SetEase(moveEase)
                .SetTarget(trans) // better target handling
            );

            // fade out with color shift
            Color finalColor = new Color(1f, 1f, 0f, 0f); // yellow and transparent
            rocketSequence.Join(
                renderer.DOColor(finalColor, duration * 0.3f / speedMultiplier)
                .SetDelay(duration * 0.7f / speedMultiplier)
                .SetEase(moveEase)
                .SetTarget(renderer) // better target handling
                .OnUpdate(() =>
                {
                    if (!isRendererValid()) DOTween.Kill(renderer);
                })
            );

            // add to master sequence
            masterSequence.Join(rocketSequence);
        }

        // final effects and cleanup
        masterSequence.OnComplete(() =>
        {
            if (particleManager != null)
            {
                // Get particle multipliers from animation manager
                float starEmissionMultiplier = animManager.GetRocketStarEmissionMultiplier();
                float smokeEmissionMultiplier = animManager.GetRocketSmokeEmissionMultiplier();
                float starSizeMultiplier = animManager.GetRocketStarSizeMultiplier();
                float smokeSizeMultiplier = animManager.GetRocketSmokeSizeMultiplier();
                float burstMultiplier = animManager.GetParticleBurstMultiplier();

                // create enhanced star effect
                ParticleSystem starEffect = particleManager.PlayEffect("RocketStar", targetWorldPos, 2.5f);
                if (starEffect != null)
                {
                    // configure in one block
                    var main = starEffect.main;
                    main.startSize = new ParticleSystem.MinMaxCurve(
                        0.6f * starSizeMultiplier,
                        1.0f * starSizeMultiplier);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);

                    var emission = starEffect.emission;
                    emission.rateOverTime = 70f * starEmissionMultiplier;
                    emission.SetBurst(0, new ParticleSystem.Burst(0.0f, 35 * burstMultiplier));
                }

                // create enhanced smoke effect
                ParticleSystem smokeEffect = particleManager.PlayEffect("RocketSmoke", targetWorldPos, 3.0f);
                if (smokeEffect != null)
                {
                    // configure in one block
                    var main = smokeEffect.main;
                    main.startSize = new ParticleSystem.MinMaxCurve(
                        0.5f * smokeSizeMultiplier,
                        0.9f * smokeSizeMultiplier);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);

                    var emission = smokeEffect.emission;
                    emission.rateOverTime = 60f * smokeEmissionMultiplier;
                    emission.SetBurst(0, new ParticleSystem.Burst(0.0f, 30 * burstMultiplier));
                }
            }

            // notify that combine is complete and special explosion can be created
            onComplete?.Invoke(true);
        });
    }


}
