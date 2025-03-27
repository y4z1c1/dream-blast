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
            canvasGroup.interactable = false; // start as non-interactable until animation completes
            canvasGroup.blocksRaycasts = false; // don't block raycasts initially
        }

        Vector3 originalScale = Vector3.one;
        popupContainer.transform.localScale = Vector3.zero;

        // hide all child ui elements initially
        SetChildrenVisibility(popupContainer, false);

        // create a sequence for the popup animation
        Sequence popupSequence = DOTween.Sequence();

        // scale animation with custom bounce
        popupSequence.Append(
            popupContainer.transform.DOScale(originalScale, adjustedDuration)
            .SetEase(Ease.OutBack, 1.1f) // outback ease approximates the bounce effect
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
                    canvasGroup.interactable = true; // now make it interactable
                    canvasGroup.blocksRaycasts = true; // enable blocking raycasts
                }

                // now show all child elements
                SetChildrenVisibility(popupContainer, true);
            }
        });
    }

    // helper to fade the overlay - dotween version no longer needs coroutines
    private static void FadeOverlay(Image overlay, float startAlpha, float endAlpha, float duration)
    {
        if (overlay == null) return;

        // set initial alpha
        Color color = overlay.color;
        color.a = startAlpha;
        overlay.color = color;

        // animate to end alpha
        overlay.DOFade(endAlpha, duration);
    }

    // hide/show all immediate child ui elements - this function remains the same
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

        // store starting values
        Vector3 startScale = popupContainer.transform.localScale;
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
            // store valid original scale - do not modify this value
            originalScale = button.transform.localScale;

            // safety check - if scale is zero somehow, force it to one
            if (originalScale.x == 0 && originalScale.y == 0 && originalScale.z == 0)
            {
                if (IsDebugEnabled()) Debug.LogWarning($"[PopupAnimations] Button {button.name} had zero scale - resetting to (1,1,1)");
                originalScale = Vector3.one;
            }

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
                // ensure proper final scale is set according to element type
                if (button.name == "MainButton")
                {
                    button.transform.localScale = Vector3.one; // scale 1,1,1
                }
                else if (button.name == "CloseButton")
                {
                    button.transform.localScale = Vector3.one; // scale 1,1,1
                }
                else
                {
                    // default - use the original scale
                    button.transform.localScale = originalScale;
                }

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

        // safety check for scale
        if (button.transform.localScale == Vector3.zero)
        {
            if (IsDebugEnabled()) Debug.LogWarning($"[PopupAnimations] Button {button.name} had zero scale in click animation - fixing");
            button.transform.localScale = Vector3.one;
        }

        if (IsDebugEnabled()) Debug.Log($"[PopupAnimations] Starting button click animation for: {button.name}");

        // store original scale
        Vector3 originalScale = button.transform.localScale;
        Vector3 pressedScale = originalScale * 0.9f;
        Vector3 releasedScale = originalScale * 1.05f;

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

    // animate celebratory effect for win popup using dotween (simple bounce animation)
    public static void PlayWinCelebrationAnimation(GameObject popupContainer)
    {
        if (!InitializeReferences() || popupContainer == null)
        {
            if (IsDebugEnabled()) Debug.LogError("[PopupAnimations] Popup container is null in PlayWinCelebrationAnimation!");
            return;
        }

        if (IsDebugEnabled()) Debug.Log("[PopupAnimations] Starting win celebration animation");

        // store original scale for restoration and animation
        Vector3 originalScale = popupContainer.transform.localScale;

        // ensure scale is not too small
        if (originalScale.x < 0.5f || originalScale == Vector3.zero)
        {
            if (IsDebugEnabled()) Debug.LogWarning("[PopupAnimations] Popup container scale was too small for celebration - resetting to (1,1,1)");
            originalScale = Vector3.one;
            popupContainer.transform.localScale = originalScale;
        }

        // create a sequence for the celebration animation
        Sequence celebrationSequence = DOTween.Sequence();

        // use a custom animation path for the bounce effect with more pronounced bounce
        float duration = 0.5f;
        float bounceAmount = 0.15f; // make bounce more visible - same as in popupcontroller

        // use a tweening value to control the bounce - just one clean bounce
        float bounceValue = 0f;
        celebrationSequence.Append(
            DOTween.To(() => bounceValue, x =>
            {
                bounceValue = x;
                // enhanced bounce wave
                float bounce = Mathf.Sin(x * Mathf.PI * 3) * (1 - x) * bounceAmount + 1f;
                // ensure scale never goes too small
                bounce = Mathf.Max(bounce, 0.9f);
                popupContainer.transform.localScale = originalScale * bounce;
            }, 1f, duration)
            .SetEase(Ease.Linear)
        );

        // set the final scale explicitly based on ui element
        celebrationSequence.OnComplete(() =>
        {
            if (IsDebugEnabled()) Debug.Log("[PopupAnimations] Celebration animation complete");

            // make sure we restore to a proper scale
            if (popupContainer.name == "PopupContainer")
            {
                // base popupcontainer scale should be vector3.one
                popupContainer.transform.localScale = Vector3.one;
                if (IsDebugEnabled()) Debug.Log("[PopupAnimations] Set final scale to Vector3.one for PopupContainer");
            }
            else if (popupContainer.transform.Find("PopupBase") != null)
            {
                // from the screenshots, popupbase may need a specific scale
                Transform popupBase = popupContainer.transform.Find("PopupBase");
                if (popupBase != null)
                {
                    // check if we need the larger scale
                    if (popupBase.localScale.x < 5f)
                    {
                        popupBase.localScale = new Vector3(10f, 10f, 10f);
                        if (IsDebugEnabled()) Debug.Log("[PopupAnimations] Set PopupBase scale to (10,10,10)");
                    }
                }

                // set reasonable scales for other elements if needed
                Transform popupRibbon = popupContainer.transform.Find("PopupRibbon");
                if (popupRibbon != null && popupRibbon.localScale.x < 3f)
                {
                    popupRibbon.localScale = new Vector3(6.97f, 6.97f, 6.97f);
                    if (IsDebugEnabled()) Debug.Log("[PopupAnimations] Set PopupRibbon scale to (6.97,6.97,6.97)");
                }

                // set reasonable scales for title text
                Transform titleText = popupContainer.transform.Find("TitleText");
                if (titleText != null && titleText.localScale.x < 1f)
                {
                    titleText.localScale = new Vector3(2f, 2f, 2f);
                    if (IsDebugEnabled()) Debug.Log("[PopupAnimations] Set TitleText scale to (2,2,2)");
                }

                // ensure parent container has proper scale
                popupContainer.transform.localScale = Vector3.one;
            }
            else
            {
                // default - restore original scale
                popupContainer.transform.localScale = originalScale;
                if (IsDebugEnabled()) Debug.Log("[PopupAnimations] Restored original scale: " + originalScale);
            }
        });
    }

    // animate title text appearing with a fade in and slide up using dotween
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

            titleText.rectTransform.anchoredPosition = originalPos;
            titleText.color = originalColor;
        });
    }

    // animate a subtle shake effect for lose popup using dotween
    public static void PlayShakeAnimation(Transform target, float intensity = 5f, float duration = 0.5f)
    {
        if (!InitializeReferences() || target == null)
            return;

        if (IsDebugEnabled()) Debug.Log($"[PopupAnimations] Starting shake animation with intensity: {intensity}, duration: {duration}");

        // store original position
        Vector3 originalPosition = target.localPosition;

        // use dotween's built-in shake animation
        target.DOShakePosition(duration, new Vector3(intensity, intensity, 0), 10, 90, false)
            .OnComplete(() =>
            {
                if (IsDebugEnabled()) Debug.Log("[PopupAnimations] Shake animation completed");

                // restore original position
                target.localPosition = originalPosition;
            });
    }
}