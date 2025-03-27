using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public static class PopupAnimations
{
    private static AnimationManager animManager;
    private static ParticleManager particleManager;

    // initialize references
    private static bool InitializeReferences()
    {
        // always try to find animationmanager if not set
        if (animManager == null)
        {
            animManager = AnimationManager.Instance;
        }

        // get particlemanager from animationmanager, not directly
        if (animManager != null && particleManager == null)
        {
            particleManager = animManager.GetParticleManager();
        }

        return animManager != null;
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

        if (IsDebugEnabled()) Debug.Log($"[PopupAnimations] Killed tweens on {target.name}");
    }

    // helper method to kill all tweens on a popup container and its children
    public static void KillAllTweens(GameObject popupContainer)
    {
        if (popupContainer == null) return;

        // kill tweens on container itself
        DOTween.Kill(popupContainer.transform);

        // kill tweens on all children recursively
        foreach (Transform child in popupContainer.transform)
        {
            KillAllTweens(child.gameObject);
        }

        // kill tweens on components
        foreach (Component component in popupContainer.GetComponents<Component>())
        {
            if (component != null)
            {
                DOTween.Kill(component);
            }
        }

        if (IsDebugEnabled()) Debug.Log($"[PopupAnimations] Killed all tweens on {popupContainer.name} and children");
    }

    // safely get original scale and ensure it's valid
    private static Vector3 GetSafeOriginalScale(Transform transform)
    {
        if (transform == null) return Vector3.one;

        Vector3 originalScale = transform.localScale;

        // if scale is invalid (zero or very small), use Vector3.one as fallback
        if (originalScale.x <= 0.1f || originalScale.y <= 0.1f || originalScale.z <= 0.1f)
        {
            if (IsDebugEnabled()) Debug.Log($"[PopupAnimations] Scale on {transform.name} was too small ({originalScale}), using Vector3.one");
            return Vector3.one;
        }

        return originalScale;
    }

    // animate a popup appearing with a bounce effect using dotween
    public static void PlayPopupShowAnimation(GameObject popupContainer, Image backgroundOverlay)
    {
        if (!InitializeReferences() || popupContainer == null)
            return;

        // kill any existing animations first
        KillAllTweens(popupContainer);
        if (backgroundOverlay != null)
            DOTween.Kill(backgroundOverlay);

        float duration = animManager.GetPopupShowDuration();

        // apply animation speed multiplier from animationmanager
        float speedMultiplier = animManager.GetAnimationSpeed();
        float adjustedDuration = duration / speedMultiplier;

        if (IsDebugEnabled()) Debug.Log($"[PopupAnimations] Starting popup show animation with duration: {duration}, speed: {speedMultiplier}");

        // set full overlay immediately to block interaction, but transparent
        if (backgroundOverlay != null)
        {
            // activate with 0 alpha initially
            backgroundOverlay.gameObject.SetActive(true);

            // set blocking color immediately (to block interactions) but fully transparent
            Color overlayColor = backgroundOverlay.color;
            overlayColor.a = 0f;
            backgroundOverlay.color = overlayColor;

            // fade in the overlay
            backgroundOverlay.DOFade(0.8f, adjustedDuration * 0.5f)
                .OnKill(() =>
                {
                    if (backgroundOverlay != null)
                    {
                        // ensure proper state if killed
                        backgroundOverlay.gameObject.SetActive(true);
                    }
                });
        }

        // setup initial invisible state for popup
        CanvasGroup canvasGroup = popupContainer.GetComponent<CanvasGroup>();
        bool usingCanvasGroup = canvasGroup != null;

        if (usingCanvasGroup)
        {
            // set initial alpha to zero but keep interactable
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // store original scale before we modify anything
        Vector3 originalScale = GetSafeOriginalScale(popupContainer.transform);

        // temporarily set to zero for animation
        popupContainer.transform.localScale = Vector3.zero;

        // hide all child ui elements initially
        SetChildrenVisibility(popupContainer, false);

        // create a sequence for the popup animation
        Sequence popupSequence = DOTween.Sequence();

        // scale animation with custom bounce
        popupSequence.Append(
            popupContainer.transform.DOScale(originalScale, adjustedDuration)
            .SetEase(Ease.OutBack, 1.1f)
            .OnKill(() =>
            {
                if (popupContainer != null)
                {
                    // ensure proper state if killed
                    popupContainer.transform.localScale = originalScale;
                }
            })
        );

        // fade in canvasgroup if we have one
        if (usingCanvasGroup)
        {
            popupSequence.Join(
                canvasGroup.DOFade(1f, adjustedDuration)
                .SetEase(Ease.OutQuad)
                .OnKill(() =>
                {
                    if (canvasGroup != null)
                    {
                        // ensure proper state if killed
                        canvasGroup.alpha = 1f;
                        canvasGroup.interactable = true;
                        canvasGroup.blocksRaycasts = true;
                    }
                })
            );
        }

        // on complete, ensure everything is set properly and show children
        popupSequence.OnComplete(() =>
        {
            if (IsDebugEnabled()) Debug.Log("[PopupAnimations] Popup show animation completed");

            // ensure final values are set if objects still exist
            if (popupContainer != null)
            {
                popupContainer.transform.localScale = originalScale;

                if (usingCanvasGroup && canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }

                // now show all child elements
                SetChildrenVisibility(popupContainer, true);
            }
        });
    }

    // hide/show all immediate child ui elements
    private static void SetChildrenVisibility(GameObject parent, bool visible)
    {
        if (parent == null) return;

        try
        {
            // process all child objects
            foreach (Transform child in parent.transform)
            {
                // skip the parent container itself
                if (child.gameObject == parent) continue;

                // for ui elements like buttons, text, etc.
                Graphic[] graphics = child.GetComponentsInChildren<Graphic>();
                foreach (Graphic graphic in graphics)
                {
                    if (graphic != null)
                    {
                        Color color = graphic.color;
                        color.a = visible ? 1f : 0f;
                        graphic.color = color;
                    }
                }

                // for specific ui elements that might need special handling
                Button button = child.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = visible;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in SetChildrenVisibility: {e.Message}");

            // if there's an error, make sure everything is visible as a fallback
            if (visible)
            {
                try
                {
                    // set all children active
                    foreach (Transform child in parent.transform)
                    {
                        child.gameObject.SetActive(true);
                    }
                }
                catch
                {
                    // ignore any errors in the fallback
                }
            }
        }
    }

    // animate a popup hiding with a scale effect using dotween
    public static void PlayPopupHideAnimation(GameObject popupContainer, Image backgroundOverlay)
    {
        if (!InitializeReferences() || popupContainer == null)
            return;

        // kill any existing animations first
        KillAllTweens(popupContainer);
        if (backgroundOverlay != null)
            DOTween.Kill(backgroundOverlay);

        float duration = animManager.GetPopupHideDuration();

        // apply animation speed multiplier from animationmanager
        float speedMultiplier = animManager.GetAnimationSpeed();
        float adjustedDuration = duration / speedMultiplier;

        if (IsDebugEnabled()) Debug.Log($"[PopupAnimations] Starting popup hide animation with duration: {duration}, speed: {speedMultiplier}");

        // store starting scale - ensure it's valid
        Vector3 currentScale = GetSafeOriginalScale(popupContainer.transform);
        Vector3 endScale = Vector3.zero;

        // check for canvasgroup
        CanvasGroup canvasGroup = popupContainer.GetComponent<CanvasGroup>();
        bool usingCanvasGroup = canvasGroup != null;

        // immediately disable interaction
        if (usingCanvasGroup)
        {
            canvasGroup.interactable = false;
        }

        // fade out overlay more quickly
        if (backgroundOverlay != null)
        {
            backgroundOverlay.DOFade(0f, adjustedDuration * 0.5f)
                .OnKill(() =>
                {
                    if (backgroundOverlay != null)
                    {
                        backgroundOverlay.color = new Color(
                            backgroundOverlay.color.r,
                            backgroundOverlay.color.g,
                            backgroundOverlay.color.b,
                            0f
                        );
                    }
                });
        }

        // create a sequence for the hide animation
        Sequence hideSequence = DOTween.Sequence();

        // scale down popup
        hideSequence.Append(
            popupContainer.transform.DOScale(endScale, adjustedDuration)
            .SetEase(Ease.InQuad)
            .OnKill(() =>
            {
                if (popupContainer != null)
                {
                    // ensure proper final state if killed
                    popupContainer.transform.localScale = endScale;
                }
            })
        );

        // fade out canvasgroup if we have one
        if (usingCanvasGroup)
        {
            hideSequence.Join(
                canvasGroup.DOFade(0f, adjustedDuration)
                .SetEase(Ease.InQuad)
                .OnKill(() =>
                {
                    if (canvasGroup != null)
                    {
                        // ensure proper state if killed
                        canvasGroup.alpha = 0f;
                        canvasGroup.blocksRaycasts = false;
                    }
                })
            );
        }

        // on complete, ensure everything is properly set
        hideSequence.OnComplete(() =>
        {
            if (IsDebugEnabled()) Debug.Log("[PopupAnimations] Popup hide animation completed");

            // ensure final values are set if objects still exist
            if (popupContainer != null)
            {
                popupContainer.transform.localScale = endScale;

                if (usingCanvasGroup && canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.blocksRaycasts = false;
                }
            }
        });
    }

    // animate a ui button appearing with a pop effect using dotween
    public static void PlayButtonShowAnimation(Button button, float delay = 0f)
    {
        if (!InitializeReferences() || button == null)
            return;

        // variables to store animation state
        Vector3 originalScale;
        Vector3 startScale;
        Vector3 bounceScale;
        Dictionary<Graphic, Color> buttonColors = new Dictionary<Graphic, Color>();
        CanvasGroup buttonCanvasGroup = null;

        try
        {
            // store valid original scale - use safe method
            originalScale = GetSafeOriginalScale(button.transform);

            // important: make button interactable immediately
            button.interactable = true;

            // set up all the animation data
            startScale = Vector3.zero;
            bounceScale = originalScale * 1.2f; // overshoot

            // store original colors for button elements
            Graphic[] graphics = button.GetComponentsInChildren<Graphic>();

            foreach (Graphic graphic in graphics)
            {
                buttonColors[graphic] = graphic.color;

                // start fully transparent
                Color startColor = graphic.color;
                startColor.a = 0f;
                graphic.color = startColor;
            }

            // check for canvasgroup on button
            buttonCanvasGroup = button.GetComponent<CanvasGroup>();
            if (buttonCanvasGroup != null)
            {
                buttonCanvasGroup.alpha = 0f;
            }

            // set initial scale for animation
            button.transform.localScale = startScale;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during button animation setup: {e.Message}");
            return;
        }

        if (IsDebugEnabled()) Debug.Log($"[PopupAnimations] Starting button show animation for: {button.name}");

        // get animation settings from animationmanager
        float duration = animManager.GetPopupShowDuration() * 0.5f;

        // apply animation speed multiplier
        float speedMultiplier = animManager.GetAnimationSpeed();
        float adjustedDuration = duration / speedMultiplier;

        // create a sequence for the button animation
        Sequence buttonSequence = DOTween.Sequence();

        // add delay if specified
        if (delay > 0)
        {
            buttonSequence.AppendInterval(delay);
        }

        // first part: grow to overshoot
        buttonSequence.Append(
            button.transform.DOScale(bounceScale, adjustedDuration * 0.6f)
            .SetEase(Ease.OutQuad)
        );

        // second part: settle back to normal
        buttonSequence.Append(
            button.transform.DOScale(originalScale, adjustedDuration * 0.4f)
            .SetEase(Ease.InOutQuad)
        );

        // fade in button elements
        foreach (var entry in buttonColors)
        {
            Graphic graphic = entry.Key;
            Color originalColor = entry.Value;

            buttonSequence.Join(
                graphic.DOFade(originalColor.a, adjustedDuration * 0.6f)
                .SetEase(Ease.OutQuad)
            );
        }

        // handle canvasgroup if present
        if (buttonCanvasGroup != null)
        {
            buttonSequence.Join(
                buttonCanvasGroup.DOFade(1f, adjustedDuration * 0.6f)
                .SetEase(Ease.OutQuad)
            );
        }

        // final cleanup - ensure everything is set correctly
        buttonSequence.OnComplete(() =>
        {
            if (IsDebugEnabled()) Debug.Log($"[PopupAnimations] Button show animation completed for: {button.name}");

            try
            {
                // restore original scale - don't set to arbitrary values
                button.transform.localScale = originalScale;

                // restore original colors
                foreach (var entry in buttonColors)
                {
                    entry.Key.color = entry.Value;
                }

                if (buttonCanvasGroup != null)
                {
                    buttonCanvasGroup.alpha = 1f;
                }

                // double-check button is interactable
                button.interactable = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during button animation cleanup: {e.Message}");
            }
        });
    }

    // animate a button press with a quick scale effect using dotween
    public static void PlayButtonClickAnimation(Button button)
    {
        if (!InitializeReferences() || button == null)
            return;

        // get original scale - use safe method
        Vector3 originalScale = GetSafeOriginalScale(button.transform);
        Vector3 pressedScale = originalScale * 0.8f;
        Vector3 releasedScale = originalScale * 1.05f;

        if (IsDebugEnabled()) Debug.Log($"[PopupAnimations] Starting button click animation for: {button.name}");

        // get animation settings from animationmanager
        float duration = animManager.GetButtonPressAnimationDuration();

        // apply animation speed multiplier
        float speedMultiplier = animManager.GetAnimationSpeed();
        float adjustedDuration = duration / speedMultiplier;

        // create a sequence for the button click animation
        Sequence clickSequence = DOTween.Sequence();

        // press down animation
        clickSequence.Append(
            button.transform.DOScale(pressedScale, adjustedDuration * 0.3f)
            .SetEase(Ease.OutQuad)
        );

        // release animation - first spring past original
        clickSequence.Append(
            button.transform.DOScale(releasedScale, adjustedDuration * 0.35f)
            .SetEase(Ease.OutQuad)
        );

        // finally, return to original size
        clickSequence.Append(
            button.transform.DOScale(originalScale, adjustedDuration * 0.35f)
            .SetEase(Ease.InOutQuad)
        );

        // ensure final scale is set
        clickSequence.OnComplete(() =>
        {
            if (IsDebugEnabled()) Debug.Log($"[PopupAnimations] Button click animation completed for: {button.name}");

            try
            {
                button.transform.localScale = originalScale;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error setting final button scale: {e.Message}");
            }
        });
    }

    // animate celebratory effect for win popup using dotween
    public static void PlayWinCelebrationAnimation(GameObject popupContainer, Transform particleContainer = null)
    {
        if (!InitializeReferences() || popupContainer == null)
        {
            if (IsDebugEnabled()) Debug.LogError("[PopupAnimations] Popup container is null in PlayWinCelebrationAnimation!");
            return;
        }

        if (IsDebugEnabled()) Debug.Log("[PopupAnimations] Starting win celebration animation");

        // Use the provided particle container or default to popup container if not provided
        Transform particleTarget = particleContainer != null ? particleContainer : popupContainer.transform;

        // create particle effects
        if (particleManager != null)
        {
            // play both particle types on the particle container instead of UI container
            particleManager.PlayEffect("StarConfetti", Vector3.zero, 1000.0f, particleTarget);
            particleManager.PlayEffect("AddStarConfetti", Vector3.zero, 1000.0f, particleTarget);

            if (IsDebugEnabled()) Debug.Log($"[PopupAnimations] Added celebration particle effects to {particleTarget.name}");
        }

        // get original scale using safe method - only animate the UI container
        Vector3 originalScale = GetSafeOriginalScale(popupContainer.transform);

        // simple bounce celebration sequence
        Sequence celebrationSequence = DOTween.Sequence();

        // use a custom animation path for the bounce effect
        float duration = 0.5f;
        float bounceAmount = 0.15f;

        // tweening value for bounce control
        float bounceValue = 0f;
        celebrationSequence.Append(
            DOTween.To(() => bounceValue, x =>
            {
                bounceValue = x;
                // enhanced bounce wave
                float bounce = Mathf.Sin(x * Mathf.PI * 3) * (1 - x) * bounceAmount + 1f;
                // ensure scale never goes too small
                bounce = Mathf.Max(bounce, 0.9f);

                // animate based on original scale
                if (popupContainer != null)
                {
                    popupContainer.transform.localScale = originalScale * bounce;
                }
            }, 1f, duration)
            .SetEase(Ease.Linear)
        );

        // ensure original scale is restored
        celebrationSequence.OnComplete(() =>
        {
            if (IsDebugEnabled()) Debug.Log("[PopupAnimations] Celebration animation complete");

            if (popupContainer != null)
            {
                // restore original scale
                popupContainer.transform.localScale = originalScale;
                if (IsDebugEnabled()) Debug.Log($"[PopupAnimations] Restored original scale: {originalScale}");
            }
        });
    }

    // animate title text appearing with a fade in and slide up
    public static void PlayTitleAnimation(TextMeshProUGUI titleText)
    {
        if (!InitializeReferences() || titleText == null)
            return;

        if (IsDebugEnabled()) Debug.Log("[PopupAnimations] Starting title animation");

        // get animation settings
        float duration = animManager.GetPopupShowDuration() * 0.8f;

        // apply animation speed multiplier
        float speedMultiplier = animManager.GetAnimationSpeed();
        float adjustedDuration = duration / speedMultiplier;

        // store original position and color
        Vector2 originalPos = titleText.rectTransform.anchoredPosition;
        Color originalColor = titleText.color;

        // set initial values - start below and transparent
        Vector2 startPos = originalPos + new Vector2(0, -20f);
        titleText.rectTransform.anchoredPosition = startPos;

        Color startColor = originalColor;
        startColor.a = 0;
        titleText.color = startColor;

        // create a sequence for the title animation
        Sequence titleSequence = DOTween.Sequence();

        // move up animation
        titleSequence.Append(
            titleText.rectTransform.DOAnchorPos(originalPos, adjustedDuration)
            .SetEase(Ease.OutQuad)
        );

        // fade in animation
        titleSequence.Join(
            titleText.DOFade(originalColor.a, adjustedDuration)
            .SetEase(Ease.OutQuad)
        );

        // ensure final values are set
        titleSequence.OnComplete(() =>
        {
            if (IsDebugEnabled()) Debug.Log("[PopupAnimations] Title animation completed");

            if (titleText != null)
            {
                titleText.rectTransform.anchoredPosition = originalPos;
                titleText.color = originalColor;
            }
        });
    }

    // animate a subtle shake effect for lose popup
    public static void PlayShakeAnimation(Transform target, float intensity = 5f, float duration = 0.5f)
    {
        if (!InitializeReferences() || target == null)
            return;

        if (IsDebugEnabled()) Debug.Log($"[PopupAnimations] Starting shake animation with intensity: {intensity}");

        // store original position and scale
        Vector3 originalPosition = target.localPosition;
        Vector3 originalScale = GetSafeOriginalScale(target);

        // use dotween's built-in shake animation
        target.DOShakePosition(duration, new Vector3(intensity, intensity, 0), 10, 90, false)
            .OnComplete(() =>
            {
                if (IsDebugEnabled()) Debug.Log("[PopupAnimations] Shake animation completed");

                if (target != null)
                {
                    // restore original position and scale
                    target.localPosition = originalPosition;
                    target.localScale = originalScale;
                }
            });
    }
}