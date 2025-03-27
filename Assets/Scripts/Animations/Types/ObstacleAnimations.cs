using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public static class ObstacleAnimations
{
    private static AnimationManager animManager;
    private static ParticleManager particleManager;

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
                if (IsDebugEnabled()) Debug.Log("[ObstacleAnimations] Successfully initialized references");
                return true;
            }

            Debug.LogError("[ObstacleAnimations] Failed to find AnimationManager instance");
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ObstacleAnimations] Error during initialization: {e}");
            return false;
        }
    }

    // helper to check debug mode from animation manager
    private static bool IsDebugEnabled()
    {
        return animManager != null && animManager.IsDebugEnabled();
    }

    // play obstacle destruction animation using dotween - similar to cube destruction but with obstacle-specific particles
    public static void PlayObstacleDestroyAnimation(Obstacle obstacle, bool playParticles = true)
    {
        if (!InitializeReferences())
        {
            if (IsDebugEnabled()) Debug.LogWarning("[ObstacleAnimations] Failed to initialize references for destroy animation");
            return;
        }

        if (obstacle == null)
        {
            if (IsDebugEnabled()) Debug.LogWarning("[ObstacleAnimations] Attempted to animate null obstacle");
            return;
        }

        Transform obstacleTransform = obstacle.transform;
        SpriteRenderer spriteRenderer = obstacle.GetComponent<SpriteRenderer>();

        if (obstacleTransform == null || spriteRenderer == null)
        {
            if (IsDebugEnabled()) Debug.LogError("[ObstacleAnimations] Obstacle is missing required components");
            return;
        }

        // store original values
        Vector3 originalScale = obstacleTransform.localScale;
        Color originalColor = spriteRenderer.color;

        // handle multiple particle effects
        List<ParticleSystem> spawnedParticles = new List<ParticleSystem>();
        if (playParticles && particleManager != null)
        {
            // get the right particle names based on obstacle type
            List<string> particleNames = GetParticleNamesForObstacle(obstacle);

            foreach (string particleName in particleNames)
            {
                if (!string.IsNullOrEmpty(particleName))
                {
                    if (IsDebugEnabled()) Debug.Log($"[ObstacleAnimations] Playing particle effect: {particleName} at {obstacleTransform.position}");
                    ParticleSystem effect = particleManager.PlayEffect(particleName, obstacleTransform.position, 1.0f);
                    if (effect != null)
                    {
                        spawnedParticles.Add(effect);
                        if (IsDebugEnabled()) Debug.Log($"[ObstacleAnimations] Particle system created: {particleName}");
                    }
                }
            }

            if (spawnedParticles.Count == 0)
            {
                if (IsDebugEnabled()) Debug.Log($"[ObstacleAnimations] No particles spawned: playParticles={playParticles}, particleManager={(particleManager != null)}");
            }
        }
        else
        {
            if (IsDebugEnabled()) Debug.Log($"[ObstacleAnimations] Skipping particles: playParticles={playParticles}, particleManager={(particleManager != null)}");
        }

        // get animation settings
        float duration = animManager.GetObstacleDestroyDuration();
        float speedMultiplier = animManager.GetAnimationSpeed();
        Ease scaleEase = Ease.OutBounce; // approximation of bounce curve

        if (IsDebugEnabled()) Debug.Log($"[ObstacleAnimations] Starting destroy animation with duration: {duration}, speed: {speedMultiplier}");

        // create a sequence for the animations
        Sequence destroySequence = DOTween.Sequence();

        // phase 1: quick scale up for "pop" effect (20% of total duration)
        float initialPopDuration = duration * 0.2f / speedMultiplier;
        destroySequence.Append(
            obstacleTransform.DOScale(originalScale * 1.1f, initialPopDuration)
            .SetEase(Ease.OutQuad)
        );

        // phase 2: scale down and fade out (80% of total duration)
        float destructionDuration = duration * 0.8f / speedMultiplier;
        destroySequence.Append(
            obstacleTransform.DOScale(Vector3.zero, destructionDuration)
            .SetEase(scaleEase)
        );

        // fade out during phase 2
        destroySequence.Join(
            spriteRenderer.DOFade(0, destructionDuration)
            .SetEase(Ease.OutQuad)
        );

        // add completion callback
        destroySequence.OnComplete(() =>
        {
            if (IsDebugEnabled()) Debug.Log("[ObstacleAnimations] Destroy animation sequence completed");

            // cleanup all particle effects
            foreach (ParticleSystem particle in spawnedParticles)
            {
                if (particle != null)
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }

            // ensure obstacle is invisible
            if (spriteRenderer != null)
            {
                Color finalColor = originalColor;
                finalColor.a = 0;
                spriteRenderer.color = finalColor;
            }

            if (obstacleTransform != null)
            {
                obstacleTransform.localScale = Vector3.zero;
            }
        });
    }

    // play obstacle damage animation using dotween when an obstacle takes damage but isn't destroyed
    public static void PlayObstacleDamageAnimation(Obstacle obstacle, bool playParticles = true)
    {
        if (!InitializeReferences())
        {
            if (IsDebugEnabled()) Debug.LogWarning("[ObstacleAnimations] Failed to initialize references for damage animation");
            return;
        }

        if (obstacle == null)
        {
            if (IsDebugEnabled()) Debug.LogWarning("[ObstacleAnimations] Attempted to animate null obstacle");
            return;
        }

        // only play damage animation for vase obstacles
        if (!(obstacle is VaseObstacle))
        {
            if (IsDebugEnabled()) Debug.Log("[ObstacleAnimations] Skipping damage animation for non-vase obstacle");
            return;
        }

        SpriteRenderer spriteRenderer = obstacle.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            if (IsDebugEnabled()) Debug.LogError("[ObstacleAnimations] Obstacle is missing SpriteRenderer component");
            return;
        }

        float duration = animManager.GetObstacleDamageDuration();
        Color damageColor = animManager.GetDamageFlashColor();

        // get animation settings
        float speedMultiplier = animManager.GetAnimationSpeed();

        if (IsDebugEnabled()) Debug.Log($"[ObstacleAnimations] Starting damage animation with duration: {duration}, speed: {speedMultiplier}");

        // store original values
        Color originalColor = spriteRenderer.color;
        Vector3 originalScale = obstacle.transform.localScale;
        Vector3 originalPosition = obstacle.transform.position;

        // play damage particles - but only one particle effect instead of all three
        if (playParticles && particleManager != null)
        {
            // just use the main particle effect instead of all three variants
            string particleName = "Vase_1"; // use only the main vase damage particle

            if (!string.IsNullOrEmpty(particleName))
            {
                if (IsDebugEnabled()) Debug.Log($"[ObstacleAnimations] Playing damage particle effect: {particleName}");
                particleManager.PlayEffect(particleName, obstacle.transform.position, 0.5f);
            }
        }

        // create a sequence for damage animations
        Sequence damageSequence = DOTween.Sequence();

        // color flash animation
        // we need to manually handle the flash effect to get the in-out effect
        float flashValue = 0f;

        // use an anonymous tweening value for the flash control
        damageSequence.Append(
            DOTween.To(() => flashValue, x =>
            {
                flashValue = x;
                // flash color (full flash at t=0.5, back to normal at t=1)
                float flashIntensity = 1 - Mathf.Abs(x * 2 - 1);
                spriteRenderer.color = Color.Lerp(originalColor, damageColor, flashIntensity);
            }, 1f, duration / speedMultiplier)
            .SetEase(Ease.Linear)
        );

        // Add shake effect instead of scale pulsing
        float shakeIntensity = 0.05f;
        damageSequence.Join(
            DOTween.Sequence()
                .AppendCallback(() =>
                {
                    // Apply shake using DOTween's built-in shake
                    obstacle.transform.DOShakePosition(duration / speedMultiplier, shakeIntensity, 20, 90, false, true);
                })
        );

        // restore original appearance when complete
        damageSequence.OnComplete(() =>
        {
            if (IsDebugEnabled()) Debug.Log("[ObstacleAnimations] Damage animation sequence completed");

            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;

            if (obstacle != null && obstacle.transform != null)
            {
                obstacle.transform.localScale = originalScale;
                obstacle.transform.position = originalPosition; // ensure position is reset after shake
            }
        });
    }

    // get appropriate particle effect name based on obstacle type
    private static string GetParticleNameForObstacle(Obstacle obstacle)
    {
        if (obstacle == null) return string.Empty;

        if (obstacle is BoxObstacle)
            return "Box";
        else if (obstacle is StoneObstacle)
            return "Stone";
        else if (obstacle is VaseObstacle)
            return "Vase";

        return string.Empty; // default fallback
    }

    // get appropriate particle effect names based on obstacle type
    private static List<string> GetParticleNamesForObstacle(Obstacle obstacle)
    {
        List<string> particleNames = new List<string>();
        string baseName = "";

        if (obstacle is BoxObstacle)
            baseName = "Box";
        else if (obstacle is StoneObstacle)
            baseName = "Stone";
        else if (obstacle is VaseObstacle)
            baseName = "Vase";
        else
            return particleNames; // empty list for unknown obstacles

        // add all three variants
        particleNames.Add(baseName + "_1");
        particleNames.Add(baseName + "_2");
        particleNames.Add(baseName + "_3");

        return particleNames;
    }
}