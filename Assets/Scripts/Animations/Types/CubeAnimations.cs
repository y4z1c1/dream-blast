using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public static class CubeAnimations
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
                if (IsDebugEnabled()) Debug.Log("[CubeAnimations] Successfully initialized references");
                return true;
            }

            Debug.LogError("[CubeAnimations] Failed to find AnimationManager instance");
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CubeAnimations] Error during initialization: {e}");
            return false;
        }
    }

    // helper to check debug mode from animation manager
    private static bool IsDebugEnabled()
    {
        return animManager != null && animManager.IsDebugEnabled();
    }

    // helper method to kill tweens on a specific object
    public static void KillTweensOnObject(Object target)
    {
        if (target == null) return;

        DOTween.Kill(target);

        if (IsDebugEnabled()) Debug.Log($"[CubeAnimations] Killed tweens on {target.name}");
    }

    // helper method to kill all tweens on a cube and its children
    public static void KillAllTweens(GameObject cubeObject)
    {
        if (cubeObject == null) return;

        // kill tweens on the cube itself
        DOTween.Kill(cubeObject.transform);

        // kill tweens on all children recursively
        foreach (Transform child in cubeObject.transform)
        {
            DOTween.Kill(child);
        }

        // kill tweens on components
        foreach (Component component in cubeObject.GetComponents<Component>())
        {
            if (component != null)
            {
                DOTween.Kill(component);
            }
        }

        if (IsDebugEnabled()) Debug.Log($"[CubeAnimations] Killed all tweens on {cubeObject.name} and children");
    }

    // play cube destruction animation using dotween
    public static IEnumerator PlayCubeDestroyAnimation(Cube cube)
    {
        if (!InitializeReferences() || cube == null) yield break;

        // kill any existing animations on this cube first
        if (cube.gameObject != null)
            KillAllTweens(cube.gameObject);

        float duration = animManager.GetCubeDestroyDuration();

        Transform cubeTransform = cube.transform;
        SpriteRenderer spriteRenderer = cube.GetComponent<SpriteRenderer>();

        if (cubeTransform == null || spriteRenderer == null)
        {
            if (IsDebugEnabled()) Debug.LogError("[CubeAnimations] Cube is missing required components");
            yield break;
        }

        // store original values
        Vector3 originalScale = cubeTransform.localScale;
        Color originalColor = spriteRenderer.color;

        // handle particle effects
        ParticleSystem particleEffect = null;
        if (particleManager != null)
        {
            string particleEffectName = GetParticleNameForCube(cube);
            if (!string.IsNullOrEmpty(particleEffectName))
            {
                if (IsDebugEnabled()) Debug.Log($"[CubeAnimations] Playing particle effect: {particleEffectName} at {cubeTransform.position}");
                particleEffect = particleManager.PlayEffect(particleEffectName, cubeTransform.position, 1.0f);
                if (IsDebugEnabled()) Debug.Log($"[CubeAnimations] Particle system created: {(particleEffect != null)}");
            }
            else
            {
                if (IsDebugEnabled()) Debug.Log($"[CubeAnimations] Skipping particles: playParticles={true}, particleManager={(particleManager != null)}, effectName=empty");
            }
        }
        else
        {
            if (IsDebugEnabled()) Debug.Log($"[CubeAnimations] Skipping particles: playParticles={true}, particleManager={(particleManager != null)}, effectName=not checked");
        }

        // get animation settings
        float speedMultiplier = animManager.GetAnimationSpeed();

        if (IsDebugEnabled()) Debug.Log($"[CubeAnimations] Starting destroy animation with duration: {duration}, speed: {speedMultiplier}");

        // create a sequence for the animations
        Sequence destroySequence = DOTween.Sequence();

        // scale down with bounce effect
        destroySequence.Append(cubeTransform.DOScale(Vector3.zero, duration / speedMultiplier)
            .SetEase(Ease.InOutQuad)
            .OnKill(() =>
            {
                // safety check for if tween is killed
                if (cubeTransform != null)
                {
                    cubeTransform.localScale = Vector3.zero;
                }
            }));

        // fade out
        destroySequence.Join(spriteRenderer.DOFade(0, duration / speedMultiplier)
            .SetEase(Ease.InOutQuad)
            .OnKill(() =>
            {
                // safety check for if tween is killed
                if (spriteRenderer != null)
                {
                    Color finalColor = spriteRenderer.color;
                    finalColor.a = 0;
                    spriteRenderer.color = finalColor;
                }
            }));

        // add completion callback
        destroySequence.OnComplete(() =>
        {
            if (IsDebugEnabled()) Debug.Log("[CubeAnimations] Destroy animation sequence completed");

            // cleanup particle effect if it's still playing
            if (particleEffect != null)
            {
                particleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            // ensure cube is invisible - add null checks
            if (spriteRenderer != null)
            {
                Color finalColor = originalColor;
                finalColor.a = 0;
                spriteRenderer.color = finalColor;
            }

            if (cubeTransform != null)
            {
                cubeTransform.localScale = Vector3.zero;
            }
        });

        yield return destroySequence.WaitForCompletion();
    }

    // play animation for cubes combining into a rocket using dotween
    public static void PlayRocketCombineAnimation(List<Cube> cubes, Vector2Int targetPosition, System.Action<bool> onComplete = null)
    {
        if (!InitializeReferences() || cubes == null || cubes.Count < 4)
        {
            if (IsDebugEnabled()) Debug.LogWarning("[CubeAnimations] Failed to initialize references or invalid cube list for rocket combine");
            onComplete?.Invoke(false);
            return;
        }

        // get the target world position (where the rocket will appear)
        Vector3 targetWorldPos = Vector3.zero;
        GridManager gridManager = null;

        // find the grid position for the rocket
        foreach (Cube cube in cubes)
        {
            if (cube.GetGridPosition() == targetPosition)
            {
                targetWorldPos = cube.transform.position;
                gridManager = cube.GetGridManager();
                break;
            }
        }

        if (targetWorldPos == Vector3.zero || gridManager == null)
        {
            if (IsDebugEnabled()) Debug.LogError("[CubeAnimations] Could not determine target position for rocket combine");
            onComplete?.Invoke(false);
            return;
        }

        // create lists to track animation states
        List<Transform> cubeTransforms = new List<Transform>();
        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> startScales = new List<Vector3>();
        List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        List<Color> startColors = new List<Color>();

        // Kill any existing animations on these cubes
        foreach (Cube cube in cubes)
        {
            if (cube != null && cube.gameObject != null)
            {
                KillAllTweens(cube.gameObject);
            }
        }

        // initialize lists and get starting positions
        foreach (Cube cube in cubes)
        {
            if (cube == null) continue;

            Transform trans = cube.transform;
            SpriteRenderer renderer = cube.GetComponent<SpriteRenderer>();

            if (trans != null && renderer != null)
            {
                // add to tracking lists
                cubeTransforms.Add(trans);
                startPositions.Add(trans.position);
                startScales.Add(trans.localScale);
                renderers.Add(renderer);
                startColors.Add(renderer.color);

                // clear the cell immediately to prevent it from being used again
                Vector2Int cubePos = cube.GetGridPosition();
                GridCell cell = gridManager.GetCell(cubePos.x, cubePos.y);
                if (cell != null)
                {
                    cell.ClearItem();
                }

                // disable cube component to prevent interactions
                cube.enabled = false;
            }
        }

        if (IsDebugEnabled()) Debug.Log($"[CubeAnimations] Starting rocket combine animation with {cubeTransforms.Count} cubes");

        // play subtle particle effects at target position
        if (particleManager != null)
        {
            particleManager.PlayEffect("RocketStar", targetWorldPos, 0.05f);
            particleManager.PlayEffect("RocketSmoke", targetWorldPos, 0.05f);
        }

        // animation settings
        float duration = animManager.GetRocketCreateDuration();
        float speedMultiplier = animManager.GetAnimationSpeed();

        // calculate movement paths - first move slightly away from target, then to target
        List<Vector3> midPositions = new List<Vector3>();
        for (int i = 0; i < startPositions.Count; i++)
        {
            // calculate direction from target and add a small outward movement
            Vector3 directionFromTarget = (startPositions[i] - targetWorldPos).normalized;
            float extraDistance = 0.2f;
            Vector3 midPosition = startPositions[i] + (directionFromTarget * extraDistance);
            midPositions.Add(midPosition);
        }

        // create the master sequence to track all animations
        Sequence masterSequence = DOTween.Sequence();

        // create animations for each cube
        for (int i = 0; i < cubeTransforms.Count; i++)
        {
            Transform trans = cubeTransforms[i];
            SpriteRenderer renderer = renderers[i];

            // Skip if transform or renderer was destroyed
            if (trans == null || renderer == null) continue;

            // Store original values for OnKill safety
            Vector3 origPosition = trans.position;
            Vector3 origScale = trans.localScale;
            Color origColor = renderer.color;
            Vector3 finalMidPosition = midPositions[i];
            int currentIndex = i; // capture for closures

            // create a sequence for this cube
            Sequence cubeSequence = DOTween.Sequence();

            // first movement - outward
            cubeSequence.Append(trans.DOMove(finalMidPosition, duration * 0.6f / speedMultiplier)
                .SetEase(Ease.InOutQuad)
                .OnKill(() =>
                {
                    if (trans != null)
                    {
                        trans.position = finalMidPosition;
                    }
                }));

            // second movement - to target
            cubeSequence.Append(trans.DOMove(targetWorldPos, duration * 0.6f / speedMultiplier)
                .SetEase(Ease.InOutQuad)
                .OnKill(() =>
                {
                    if (trans != null)
                    {
                        trans.position = targetWorldPos;
                    }
                }));

            // scale down during second movement
            Vector3 scaleTarget = (i < startScales.Count) ? startScales[i] * 0.6f : Vector3.one * 0.6f;
            cubeSequence.Join(trans.DOScale(scaleTarget, duration * 0.7f / speedMultiplier)
                .SetEase(Ease.InOutQuad)
                .OnUpdate(() =>
                {
                    // check if target was destroyed during tween
                    if (trans == null)
                    {
                        DOTween.Kill(trans);
                    }
                })
                .OnKill(() =>
                {
                    if (trans != null)
                    {
                        trans.localScale = scaleTarget;
                    }
                }));

            // fade out at the end
            cubeSequence.Join(renderer.DOFade(0, duration * 0.3f / speedMultiplier)
                .SetDelay(duration * 0.3f / speedMultiplier)
                .SetEase(Ease.InOutQuad)
                .OnKill(() =>
                {
                    if (renderer != null)
                    {
                        Color finalColor = renderer.color;
                        finalColor.a = 0;
                        renderer.color = finalColor;
                    }
                }));

            // add this cube's sequence to the master sequence
            masterSequence.Join(cubeSequence);
        }

        // final burst effect and cleanup
        masterSequence.OnComplete(() =>
        {
            if (IsDebugEnabled()) Debug.Log("[CubeAnimations] Rocket combine animation completed");


            // clean up - destroy original cubes
            foreach (Cube cube in cubes)
            {
                if (cube != null && cube.gameObject != null)
                {
                    // Kill any remaining tweens before destroying
                    KillAllTweens(cube.gameObject);
                    Object.Destroy(cube.gameObject);
                }
            }

            // notify that combine is complete and rocket can be created
            onComplete?.Invoke(true);
        });

        // Add safety OnKill handler for master sequence
        masterSequence.OnKill(() =>
        {
            if (IsDebugEnabled()) Debug.Log("[CubeAnimations] Rocket combine master sequence killed, cleaning up");

            // Set all cubes to proper end states
            for (int i = 0; i < cubeTransforms.Count; i++)
            {
                // Ensure index is valid for all collections
                if (i >= renderers.Count || i >= startScales.Count) continue;

                Transform trans = cubeTransforms[i];
                SpriteRenderer renderer = renderers[i];

                if (trans != null)
                {
                    trans.position = targetWorldPos;
                    // Only access startScales if index is valid
                    if (i < startScales.Count)
                    {
                        trans.localScale = startScales[i] * 0.6f;
                    }
                    else
                    {
                        trans.localScale = Vector3.one * 0.6f; // fallback value
                    }
                }

                if (renderer != null)
                {
                    Color finalColor = renderer.color;
                    finalColor.a = 0;
                    renderer.color = finalColor;
                }
            }

            // Ensure callback is still invoked
            onComplete?.Invoke(true);
        });
    }

    // get appropriate particle effect name based on cube color
    private static string GetParticleNameForCube(Cube cube)
    {
        if (cube == null) return string.Empty;

        switch (cube.GetColor())
        {
            case Cube.CubeColor.Red:
                return "CubeRed";
            case Cube.CubeColor.Blue:
                return "CubeBlue";
            case Cube.CubeColor.Green:
                return "CubeGreen";
            case Cube.CubeColor.Yellow:
                return "CubeYellow";
            default:
                return "CubeBlue";
        }
    }

    // play glow animation for rocket indicator
    public static void PlayRocketIndicatorAnimation(Cube cube, bool show, Sprite defaultSprite, Sprite rocketSprite, float glowDuration = 0.9f, float glowIntensity = 1.2f)
    {
        if (!InitializeReferences() || cube == null)
        {
            if (IsDebugEnabled()) Debug.LogWarning("[CubeAnimations] Failed to initialize references for rocket indicator animation");
            return;
        }

        SpriteRenderer spriteRenderer = cube.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            if (IsDebugEnabled()) Debug.LogError("[CubeAnimations] Cube is missing SpriteRenderer component");
            return;
        }

        // Check if we have both sprites needed
        if (defaultSprite == null || rocketSprite == null)
        {
            // If sprites aren't assigned, just switch directly
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = show ? rocketSprite : defaultSprite;
            }
            if (IsDebugEnabled()) Debug.Log("[CubeAnimations] Direct sprite switch without animation");
            return;
        }

        // Kill any existing tweens on this renderer
        DOTween.Kill(spriteRenderer);

        // Get animation settings
        float speedMultiplier = animManager.GetAnimationSpeed();
        float duration = glowDuration / speedMultiplier;

        if (IsDebugEnabled()) Debug.Log($"[CubeAnimations] Starting rocket indicator animation, show={show}");

        // create a material instance for cross-fade effect
        Material originalMaterial = spriteRenderer.material;
        Material fadeMaterial = new Material(originalMaterial);
        fadeMaterial.EnableKeyword("_ALPHABLEND_ON");

        // setup for cross-fade
        GameObject fadeObject = null;
        SpriteRenderer fadeRenderer = null;

        // create temporary object for the fade effect
        fadeObject = new GameObject("FadeSprite");
        fadeObject.transform.SetParent(cube.transform);
        fadeObject.transform.localPosition = Vector3.zero;
        fadeObject.transform.localRotation = Quaternion.identity;
        fadeObject.transform.localScale = Vector3.one;

        // add sprite renderer for the fade
        fadeRenderer = fadeObject.AddComponent<SpriteRenderer>();
        fadeRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        fadeRenderer.material = fadeMaterial;

        if (show)
        {
            // setup for transition to rocket sprite
            fadeRenderer.sprite = rocketSprite;
            fadeRenderer.color = new Color(1f, 1f, 1f, 0f);

            // sequence for smooth transition
            Sequence showSequence = DOTween.Sequence();

            // phase 1: glow up original sprite
            showSequence.Append(spriteRenderer.DOColor(new Color(glowIntensity, glowIntensity, glowIntensity, 1f), duration * 0.1f)
                .SetEase(Ease.OutQuad));

            // phase 2: fade in rocket sprite
            showSequence.Append(fadeRenderer.DOFade(1f, duration * 0.15f)
                .SetEase(Ease.InOutQuad));

            // phase 3: complete transition and cleanup
            showSequence.OnComplete(() =>
            {
                // apply final state
                spriteRenderer.sprite = rocketSprite;
                spriteRenderer.color = Color.white;

                // destroy temporary fade object
                if (fadeObject != null)
                {
                    Object.Destroy(fadeObject);
                }

                if (IsDebugEnabled()) Debug.Log("[CubeAnimations] Rocket indicator show animation completed");
            });
        }
        else
        {
            // setup for transition back to default sprite
            fadeRenderer.sprite = defaultSprite;
            fadeRenderer.color = new Color(1f, 1f, 1f, 0f);

            // sequence for smooth transition without glow
            Sequence hideSequence = DOTween.Sequence();

            // remove glow phase, just fade in default sprite directly
            hideSequence.Append(fadeRenderer.DOFade(1f, duration * 0.15f)
                .SetEase(Ease.InOutQuad));

            // complete transition and cleanup
            hideSequence.OnComplete(() =>
            {
                // apply final state
                spriteRenderer.sprite = defaultSprite;
                spriteRenderer.color = Color.white;

                // destroy temporary fade object
                if (fadeObject != null)
                {
                    Object.Destroy(fadeObject);
                }

                if (IsDebugEnabled()) Debug.Log("[CubeAnimations] Rocket indicator hide animation completed");
            });
        }
    }

    // play shake animation for invalid move
    public static void PlayInvalidMoveShake(Cube cube, float duration = 0.3f, float strength = 0.1f)
    {
        if (!InitializeReferences() || cube == null)
        {
            if (IsDebugEnabled()) Debug.LogWarning("[CubeAnimations] Failed to initialize references for invalid move shake");
            return;
        }

        if (IsDebugEnabled()) Debug.Log("[CubeAnimations] Playing invalid move shake animation");

        // get animation settings from animation manager
        float speedMultiplier = animManager.GetAnimationSpeed();

        // store original position
        Vector3 originalPosition = cube.transform.position;

        // store cube object for safer onKill reference
        GameObject cubeObj = cube.gameObject;

        // kill any existing DOTween animations on this transform
        DOTween.Kill(cube.transform);

        // apply shake using DOTween
        Tween shakeTween = cube.transform.DOShakePosition(duration / speedMultiplier, strength, 12, 90, false, true)
            .OnComplete(() =>
            {
                // ensure cube returns to original position
                if (cube != null && cube.transform != null)
                {
                    cube.transform.position = originalPosition;
                    if (IsDebugEnabled()) Debug.Log("[CubeAnimations] Invalid move shake animation completed");
                }
            });

        // add extra safety to ensure position is restored if tween is killed
        shakeTween.OnKill(() =>
        {
            // Try to find the transform even if the cube component is gone
            Transform transform = null;

            if (cube != null && cube.transform != null)
            {
                transform = cube.transform;
            }
            else if (cubeObj != null)
            {
                transform = cubeObj.transform;
            }

            if (transform != null)
            {
                transform.position = originalPosition;
                if (IsDebugEnabled()) Debug.Log("[CubeAnimations] Invalid move shake animation killed, position restored");
            }
        });
    }
}