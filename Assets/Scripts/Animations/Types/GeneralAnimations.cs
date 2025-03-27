using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// general animations for common game elements not specific to a particular type
public static class GeneralAnimations
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
                if (IsDebugEnabled()) Debug.Log("[GeneralAnimations] Successfully initialized references");
                return true;
            }

            Debug.LogError("[GeneralAnimations] Failed to find AnimationManager instance");
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GeneralAnimations] Error during initialization: {e}");
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

        if (IsDebugEnabled()) Debug.Log($"[GeneralAnimations] Killed tweens on {target.name}");
    }

    // helper method to kill all tweens on an object and its children
    public static void KillAllTweens(GameObject gameObject)
    {
        if (gameObject == null) return;

        // kill tweens on the object itself
        DOTween.Kill(gameObject.transform);

        // kill tweens on all children recursively
        foreach (Transform child in gameObject.transform)
        {
            DOTween.Kill(child);
        }

        // kill tweens on components
        foreach (Component component in gameObject.GetComponents<Component>())
        {
            if (component != null)
            {
                DOTween.Kill(component);
            }
        }

        if (IsDebugEnabled()) Debug.Log($"[GeneralAnimations] Killed all tweens on {gameObject.name} and children");
    }

    // animate button appearing from the bottom with a bounce effect
    public static void PlayButtonAppearFromBottom(GameObject buttonObj, float duration, float delay = 0.2f, System.Action onComplete = null)
    {
        if (!InitializeReferences() || buttonObj == null)
        {
            onComplete?.Invoke();
            return;
        }

        // apply animation speed multiplier
        float speedMultiplier = animManager.GetAnimationSpeed();
        float adjustedDuration = duration / speedMultiplier;
        float adjustedDelay = delay / speedMultiplier;

        if (IsDebugEnabled()) Debug.Log($"[GeneralAnimations] Starting button appear animation for {buttonObj.name}");

        // store original position and move button below screen
        Vector3 originalPosition = buttonObj.transform.position;
        Vector3 startPosition = originalPosition;
        startPosition.y -= Screen.height * 0.3f; // move below screen

        // set initial position
        buttonObj.transform.position = startPosition;

        // cancel any previous animations on this transform
        DOTween.Kill(buttonObj.transform);

        // create animation sequence
        Sequence buttonSequence = DOTween.Sequence();

        // add delay if specified
        if (adjustedDelay > 0)
            buttonSequence.AppendInterval(adjustedDelay);

        // animate movement from bottom with bounce
        buttonSequence.Append(
            // check if button transform exists before tweening
            buttonObj != null && buttonObj.transform != null ?
            buttonObj.transform.DOMove(originalPosition, adjustedDuration)
            .SetEase(Ease.OutBack, 1.2f) // bounce effect
            .OnUpdate(() =>
            {
                // check if target was destroyed during tween
                if (buttonObj == null || buttonObj.transform == null)
                {
                    DOTween.Kill(buttonObj.transform);
                }
            }) : DOTween.Sequence() // empty sequence if button is null
        );

        // handle completion
        buttonSequence.OnComplete(() =>
        {
            if (IsDebugEnabled()) Debug.Log($"[GeneralAnimations] Button appear animation completed for {buttonObj.name}");

            // ensure button is at the final position
            buttonObj.transform.position = originalPosition;

            // make button interactable
            UnityEngine.UI.Button button = buttonObj.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
                button.interactable = true;

            // invoke completion callback
            onComplete?.Invoke();
        });
    }

    // animate item falling with physics-like movement
    public static void PlayFallingAnimation(GridItem item, Vector3 sourcePosition, Vector3 targetPosition, System.Action onComplete = null)
    {
        if (!InitializeReferences() || item == null)
        {
            onComplete?.Invoke();
            return;
        }

        // Kill any existing animations on this item
        if (item.gameObject != null)
            KillAllTweens(item.gameObject);

        // get physics simulation parameters from animation manager
        float gravity = animManager.GetFallingGravity();
        float bounceIntensity = animManager.GetFallingBounceIntensity();
        float distance = sourcePosition.y - targetPosition.y;
        float duration = Mathf.Sqrt(2 * distance / gravity); // t = sqrt(2h/g)

        // apply animation speed modifier
        float speedMultiplier = animManager.GetAnimationSpeed();
        duration /= speedMultiplier;

        // get row index to add staggered delay (higher rows start falling later)
        int row = Mathf.RoundToInt(sourcePosition.y);
        float staggerDelay = 0.035f * row;

        if (IsDebugEnabled()) Debug.Log($"[GeneralAnimations] Starting falling animation for {item.name} from {sourcePosition} to {targetPosition} with duration {duration}, gravity {gravity}, row delay {staggerDelay}");

        // reset item to start position to avoid visual jump
        item.transform.position = sourcePosition;

        // Store reference to transform and gameObject for safety in callbacks
        Transform itemTransform = item.transform;
        GameObject itemObject = item.gameObject;

        // create a sequence for the animation
        Sequence moveSequence = DOTween.Sequence();

        // add staggered delay based on row
        if (staggerDelay > 0)
        {
            moveSequence.AppendInterval(staggerDelay / speedMultiplier);
        }

        // custom ease function for gravity - use callbacks instead
        Vector3 initialPos = sourcePosition;
        moveSequence.Append(
            DOTween.To(() => 0f, t =>
            {
                // Skip update if item was destroyed
                if (itemTransform == null) return;

                // use t^2 to simulate gravity acceleration
                float verticalProgress = t * t;
                // calculate current position based on gravity progress
                float newY = initialPos.y - (distance * verticalProgress);
                itemTransform.position = new Vector3(initialPos.x, newY, initialPos.z);
            }, 1f, duration)
            .SetEase(Ease.Linear)
            .OnUpdate(() =>
            {
                // Extra check if target was destroyed
                if (itemTransform == null)
                {
                    DOTween.Kill(itemTransform);
                }
            })
            .OnKill(() =>
            {
                // Set final position if killed
                if (itemTransform != null)
                {
                    itemTransform.position = targetPosition;
                }
            })
        );

        // add bounce effect after reaching target with configurable intensity
        Vector3 bounceScale = new Vector3(bounceIntensity, bounceIntensity, bounceIntensity);
        moveSequence.Append(
            // first check if the transform is still valid before creating the tween
            (itemTransform != null) ?
            itemTransform.DOPunchScale(bounceScale, 0.15f / speedMultiplier, 1, 0)
            .OnUpdate(() =>
            {
                // check if target was destroyed during tween
                if (itemTransform == null)
                {
                    DOTween.Kill(itemTransform);
                }
            })
            .OnKill(() =>
            {
                // Reset scale if killed
                if (itemTransform != null)
                {
                    itemTransform.localScale = Vector3.one;
                }
            })
            // if transform was destroyed, create an empty tween to keep sequence flowing
            : DOTween.Sequence().SetAutoKill(true)
        );

        // ensure final position when complete
        moveSequence.OnComplete(() =>
        {
            if (itemTransform != null)
            {
                itemTransform.position = targetPosition;
            }

            if (IsDebugEnabled()) Debug.Log($"[GeneralAnimations] Completed falling animation for {(itemObject != null ? itemObject.name : "destroyed item")}");

            // execute callback if provided
            onComplete?.Invoke();
        });

        // Add safety for if sequence is killed
        moveSequence.OnKill(() =>
        {
            if (IsDebugEnabled()) Debug.Log($"[GeneralAnimations] Falling animation killed, invoking completion callback");

            // Set final position if killed
            if (itemTransform != null)
            {
                itemTransform.position = targetPosition;
            }

            // Always invoke completion callback
            onComplete?.Invoke();
        });
    }

    // animate multiple items falling as a batch with row-based synchronization
    public static void PlayFallingBatchAnimation(List<GridItem> items, List<Vector3> sourcePositions, List<Vector3> targetPositions, System.Action onComplete = null)
    {
        if (!InitializeReferences())
        {
            onComplete?.Invoke();
            return;
        }

        // validate parameters
        if (items == null || items.Count == 0 ||
            sourcePositions == null || sourcePositions.Count != items.Count ||
            targetPositions == null || targetPositions.Count != items.Count)
        {
            if (IsDebugEnabled()) Debug.LogWarning("[GeneralAnimations] Invalid parameters for batch falling animation");
            onComplete?.Invoke();
            return;
        }

        // get physics simulation parameters from animation manager
        float gravity = animManager.GetFallingGravity();
        float bounceIntensity = animManager.GetFallingBounceIntensity();
        float speedMultiplier = animManager.GetAnimationSpeed();

        if (IsDebugEnabled()) Debug.Log($"[GeneralAnimations] Starting synchronized batch falling animation for {items.Count} items");

        // track animation completion
        int activeCount = items.Count;
        bool callbackInvoked = false;

        // group items by their target row (y position)
        Dictionary<int, List<int>> rowGroups = new Dictionary<int, List<int>>();

        // organize items by row
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
            {
                activeCount--;
                continue;
            }

            // get row index from target position
            int row = Mathf.RoundToInt(targetPositions[i].y);

            if (!rowGroups.ContainsKey(row))
                rowGroups[row] = new List<int>();

            rowGroups[row].Add(i);
        }

        // sort rows from bottom to top
        List<int> sortedRows = new List<int>(rowGroups.Keys);
        sortedRows.Sort();

        // create master sequence
        Sequence masterSequence = DOTween.Sequence();

        // process each row as a group (from bottom to top)
        foreach (int row in sortedRows)
        {
            List<int> itemIndices = rowGroups[row];

            // create a sequence for this row
            Sequence rowSequence = DOTween.Sequence();

            // for each item in this row
            foreach (int itemIndex in itemIndices)
            {
                GridItem item = items[itemIndex];
                if (item == null) continue;

                Vector3 sourcePos = sourcePositions[itemIndex];
                Vector3 targetPos = targetPositions[itemIndex];
                float distance = sourcePos.y - targetPos.y;

                // calculate physics-based duration - same for all items in row
                float duration = Mathf.Sqrt(2 * distance / gravity);
                duration /= speedMultiplier;

                // kill any existing animations
                if (item.gameObject != null)
                    KillAllTweens(item.gameObject);

                // set starting position
                item.transform.position = sourcePos;

                // store references for safety
                Transform itemTransform = item.transform;
                GameObject itemObject = item.gameObject;

                // create item sequence and add to row sequence
                Sequence itemSequence = DOTween.Sequence();

                // gravity falling animation
                Vector3 initialPos = sourcePos;
                itemSequence.Append(
                    DOTween.To(() => 0f, t =>
                    {
                        if (itemTransform == null) return;

                        // t^2 for gravity acceleration
                        float verticalProgress = t * t;
                        float newY = initialPos.y - (distance * verticalProgress);
                        itemTransform.position = new Vector3(initialPos.x, newY, initialPos.z);
                    }, 1f, duration)
                    .SetEase(Ease.Linear)
                    .OnKill(() =>
                    {
                        if (itemTransform != null)
                            itemTransform.position = targetPos;
                    })
                );

                // bounce effect
                Vector3 bounceScale = new Vector3(bounceIntensity, bounceIntensity, bounceIntensity);
                itemSequence.Append(
                    (itemTransform != null) ?
                    itemTransform.DOPunchScale(bounceScale, 0.15f / speedMultiplier, 1, 0)
                    .OnKill(() =>
                    {
                        if (itemTransform != null)
                            itemTransform.localScale = Vector3.one;
                    })
                    : DOTween.Sequence().SetAutoKill(true)
                );

                // handle completion for this item
                itemSequence.OnComplete(() =>
                {
                    if (itemTransform != null)
                        itemTransform.position = targetPos;

                    // mark cube as ready when animation completes
                    if (item is Cube cube)
                        cube.SetHasReachedTarget(true);

                    // decrement active count and check if all done
                    activeCount--;

                    if (activeCount <= 0 && !callbackInvoked)
                    {
                        callbackInvoked = true;
                        if (IsDebugEnabled()) Debug.Log("[GeneralAnimations] Batch falling animation completed");
                        onComplete?.Invoke();
                    }
                });

                // handle killed sequence
                itemSequence.OnKill(() =>
                {
                    if (itemTransform != null)
                    {
                        itemTransform.position = targetPos;
                        itemTransform.localScale = Vector3.one;
                    }

                    // mark cube as ready if animation is killed
                    if (item is Cube cube)
                        cube.SetHasReachedTarget(true);

                    // decrement active count and check if all done
                    activeCount--;

                    if (activeCount <= 0 && !callbackInvoked)
                    {
                        callbackInvoked = true;
                        if (IsDebugEnabled()) Debug.Log("[GeneralAnimations] Batch falling animation completed (after kill)");
                        onComplete?.Invoke();
                    }
                });

                // join item sequence to row sequence (all start at same time)
                rowSequence.Join(itemSequence);
            }

            // append row sequence to master sequence (rows fall one after another)
            masterSequence.Append(rowSequence);
        }

        // if no items were valid, just invoke callback
        if (activeCount <= 0 && !callbackInvoked)
        {
            callbackInvoked = true;
            if (IsDebugEnabled()) Debug.Log("[GeneralAnimations] No valid items in batch, completing immediately");
            onComplete?.Invoke();
        }
    }

    // animate cube spawning and falling from above
    public static void PlayCubeSpawnAnimation(Cube cube, Vector3 startPosition, Vector3 targetPosition, System.Action onComplete = null)
    {
        if (!InitializeReferences() || cube == null)
        {
            onComplete?.Invoke();
            return;
        }

        // Kill any existing animations on this cube
        if (cube.gameObject != null)
            KillAllTweens(cube.gameObject);

        // get physics simulation parameters from animation manager
        float gravity = animManager.GetSpawnGravity();
        float bounceIntensity = animManager.GetSpawnBounceIntensity();
        float fadeDuration = animManager.GetSpawnFadeDuration();
        float distance = startPosition.y - targetPosition.y;
        float duration = Mathf.Max(0.1f, Mathf.Sqrt(2 * distance / gravity)); // ensure minimum duration

        // apply animation speed modifier
        float speedMultiplier = animManager.GetAnimationSpeed();
        duration /= speedMultiplier;
        fadeDuration /= speedMultiplier;

        if (IsDebugEnabled()) Debug.Log($"[GeneralAnimations] Starting cube spawn animation for {cube.name} from {startPosition} to {targetPosition} with duration {duration}");

        // set initial position
        cube.transform.position = startPosition;

        // Store references for safety in callbacks
        Transform cubeTransform = cube.transform;
        GameObject cubeObject = cube.gameObject;

        // get renderers for fade-in effect
        Renderer[] renderers = cube.GetComponentsInChildren<Renderer>();

        // set initial alpha to zero
        SetRenderersAlpha(renderers, 0f);

        // create a sequence for the animation
        Sequence spawnSequence = DOTween.Sequence();

        // set the auto-kill to true and set a faster update mode
        spawnSequence.SetAutoKill(true)
            .SetUpdate(UpdateType.Normal);

        // optional: add an ID for better tracking
        spawnSequence.SetId("cubeSpawn_" + cube.GetInstanceID());

        // fade in as cube falls
        spawnSequence.Join(
            DOTween.To(() => 0f, x =>
            {
                // Skip if renderers or cube were destroyed
                if (renderers == null || cubeObject == null) return;

                SetRenderersAlpha(renderers, x);
            }, 1f, fadeDuration)
            .OnUpdate(() =>
            {
                // Extra check if target was destroyed
                if (renderers == null || cubeObject == null)
                {
                    // If object is gone, kill the tween
                    DOTween.Kill(cubeObject);
                }
            })
            .OnKill(() =>
            {
                // ensure full opacity if killed during fade
                if (renderers != null)
                    SetRenderersAlpha(renderers, 1f);
            })
        );

        // custom ease function for gravity - use callbacks instead
        Vector3 initialPos = startPosition;
        spawnSequence.Append(
            DOTween.To(() => 0f, t =>
            {
                // Skip update if cube was destroyed
                if (cubeTransform == null) return;

                // use t^2 to simulate gravity acceleration
                float verticalProgress = t * t;
                // calculate current position based on gravity progress
                float newY = initialPos.y - (distance * verticalProgress);
                cubeTransform.position = new Vector3(initialPos.x, newY, initialPos.z);
            }, 1f, duration)
            .SetEase(Ease.Linear)
            .OnUpdate(() =>
            {
                // Extra check if target was destroyed
                if (cubeTransform == null)
                {
                    DOTween.Kill(cubeTransform);
                }
            })
            .OnKill(() =>
            {
                // set final position if killed
                if (cubeTransform != null)
                {
                    cubeTransform.position = targetPosition;
                }
            })
        );

        // add bounce effect after reaching target
        Vector3 bounceScale = new Vector3(bounceIntensity, bounceIntensity, bounceIntensity);
        spawnSequence.Append(
            // first check if the transform is still valid before creating the tween
            (cubeTransform != null) ?
            cubeTransform.DOPunchScale(bounceScale, 0.15f / speedMultiplier, 1, 0)
            .OnUpdate(() =>
            {
                // check if target was destroyed during tween
                if (cubeTransform == null)
                {
                    DOTween.Kill(cubeTransform);
                }
            })
            .OnKill(() =>
            {
                // reset scale if killed
                if (cubeTransform != null)
                {
                    cubeTransform.localScale = Vector3.one;
                }
            })
            // if transform was destroyed, create an empty tween to keep sequence flowing
            : DOTween.Sequence().SetAutoKill(true)
        );

        // ensure final position when complete
        spawnSequence.OnComplete(() =>
        {
            if (cubeTransform != null)
            {
                cubeTransform.position = targetPosition; // ensure final position
            }

            if (IsDebugEnabled()) Debug.Log($"[GeneralAnimations] Completed cube spawn animation for {(cubeObject != null ? cubeObject.name : "destroyed cube")}");

            // execute callback if provided
            onComplete?.Invoke();

            // ensure the sequence is killed to free up resources
            spawnSequence.Kill();
        });

        // Add safety for if sequence is killed
        spawnSequence.OnKill(() =>
        {
            if (IsDebugEnabled()) Debug.Log($"[GeneralAnimations] Cube spawn animation killed, invoking completion callback");

            // Set final state if killed
            if (cubeTransform != null)
            {
                cubeTransform.position = targetPosition;
                SetRenderersAlpha(renderers, 1f);
            }

            // Always invoke completion callback
            onComplete?.Invoke();
        });
    }

    // helper method to set alpha for all renderers
    private static void SetRenderersAlpha(Renderer[] renderers, float alpha)
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = alpha;
                renderer.material.color = color;
            }
        }
    }
}