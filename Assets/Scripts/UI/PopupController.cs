using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class PopupController : MonoBehaviour
{
    // singleton instance
    public static PopupController Instance { get; private set; }

    [Header("Popup Elements")]
    [SerializeField] private GameObject popupContainer;
    [SerializeField] private Image backgroundOverlay;
    [SerializeField] private Image popupBase;
    [SerializeField] private Image popupRibbon;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button mainButton;
    [SerializeField] private TextMeshProUGUI mainButtonText;
    [SerializeField] private Transform particleContainer; // separate container for particles

    [Header("Debug & Animation Settings")]
    [SerializeField] private bool debugMode = false; // debug mode to show extra logs
    [SerializeField] private bool useAnimations = true;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float buttonDelay = 0.1f;

    // block raycasts during popup animations
    [SerializeField] private GraphicRaycaster popupCanvasRaycaster;

    private Action onCloseCallback;
    private bool isAnimating = false;
    private bool isPopupActive = false; // track if popup is already active

    private void Awake()
    {
        // setup singleton
        if (Instance != null && Instance != this)
        {
            if (debugMode) Debug.LogWarning("multiple popupcontroller instances found. destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // make sure this gameobject is active
        gameObject.SetActive(true);

        // hide popup by default
        if (popupContainer != null)
            popupContainer.SetActive(false);

        if (backgroundOverlay != null)
            backgroundOverlay.gameObject.SetActive(false);

        // set up close button handler
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePopup);

        // try to find canvas raycaster if not assigned
        if (popupCanvasRaycaster == null)
        {
            popupCanvasRaycaster = GetComponentInParent<GraphicRaycaster>();
        }

        // register with levelcontroller
        RegisterWithLevelController();
    }

    private void Start()
    {
        // double-check references
        CheckReferences();
    }

    // check critical references
    private void CheckReferences()
    {
        if (popupContainer == null)
        {
            Debug.LogError("popupcontainer reference is missing! please assign in inspector.");
        }

        if (titleText == null)
        {
            Debug.LogError("titletext reference is missing! please assign in inspector.");
        }

        if (mainButton == null)
        {
            Debug.LogError("mainbutton reference is missing! please assign in inspector.");
        }

        // create canvasgroup if doesn't exist on popupcontainer
        if (popupContainer != null && popupContainer.GetComponent<CanvasGroup>() == null)
        {
            CanvasGroup group = popupContainer.AddComponent<CanvasGroup>();
            group.alpha = 1f; // keep alpha at 1 since we'll animate elements individually
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        // create a blocking canvas with high sorting order if needed
        if (backgroundOverlay != null && backgroundOverlay.canvas == null)
        {
            Canvas overlayCanvas = backgroundOverlay.gameObject.GetComponent<Canvas>();
            if (overlayCanvas == null)
            {
                overlayCanvas = backgroundOverlay.gameObject.AddComponent<Canvas>();
                overlayCanvas.sortingOrder = 50; // high to ensure it's above game elements

                // add graphic raycaster to block inputs
                GraphicRaycaster raycaster = backgroundOverlay.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        // create particle container if not assigned
        if (particleContainer == null && popupContainer != null)
        {
            GameObject particlesObj = new GameObject("ParticleContainer");
            particleContainer = particlesObj.transform;
            particleContainer.SetParent(popupContainer.transform);
            particleContainer.localPosition = Vector3.zero;
            particleContainer.localScale = Vector3.one;

            if (debugMode) Debug.Log("created particle container as it was not assigned");
        }
    }

    // register with levelcontroller
    private void RegisterWithLevelController()
    {
        LevelController levelController = FindFirstObjectByType<LevelController>();
        if (levelController != null)
        {
            if (debugMode) Debug.Log("popupcontroller registered with levelcontroller");

            try
            {
                var field = levelController.GetType().GetField("popupController");
                if (field != null)
                {
                    field.SetValue(levelController, this);
                    if (debugMode) Debug.Log("successfully set popupcontroller reference in levelcontroller");
                }
                else
                {
                    Debug.LogWarning("levelcontroller does not have a popupcontroller field");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("error setting popupcontroller reference: " + e.Message);
            }
        }
        else
        {
            Debug.LogWarning("levelcontroller not found, popup may not appear automatically");
        }
    }

    // static helper method to ensure popupcontroller exists
    public static PopupController EnsureExists()
    {
        if (Instance != null)
        {
            if (Instance.debugMode) Debug.Log("popupcontroller: using existing instance");
            return Instance;
        }

        // try to find an existing instance first
        var existingControllers = GameObject.FindObjectsByType<PopupController>(FindObjectsSortMode.None);

        // check if there are multiple controllers
        if (existingControllers.Length > 1)
        {
            Debug.LogWarning($"found {existingControllers.Length} popupcontroller instances. using the first one and destroying others.");
            Instance = existingControllers[0];

            // destroy duplicates
            for (int i = 1; i < existingControllers.Length; i++)
            {
                Debug.LogWarning($"destroying duplicate popupcontroller: {existingControllers[i].gameObject.name}");
                GameObject.Destroy(existingControllers[i].gameObject);
            }

            return Instance;
        }

        // just one controller
        if (existingControllers.Length == 1)
        {
            Debug.Log("found existing popupcontroller instance");
            Instance = existingControllers[0];
            return Instance;
        }

        // if not found, create a new one
        Debug.Log("creating popupcontroller instance");
        GameObject popupControllerObj = new GameObject("PopupController");
        Instance = popupControllerObj.AddComponent<PopupController>();

        // note: this will need proper references set up in the inspector
        // this is just a fallback to prevent null references
        return Instance;
    }

    // generic method to show any popup type - combined with static version
    public static void ShowPopup(string title, string buttonText, UnityAction buttonAction, float delay = 0f)
    {
        var instance = EnsureExists();
        if (instance == null)
        {
            Debug.LogError("failed to create or find popupcontroller instance");
            return;
        }

        if (instance.debugMode) Debug.Log("showpopup called: " + title + " (with " + delay + "s delay)");

        // block input immediately by showing background overlay with no delay
        if (instance.backgroundOverlay != null)
        {
            instance.backgroundOverlay.gameObject.SetActive(true);
            Color color = instance.backgroundOverlay.color;
            color.a = 0; // start transparent but still block input
            instance.backgroundOverlay.color = color;
        }

        // set title
        if (instance.titleText != null)
        {
            instance.titleText.text = title;
        }

        // set button text
        if (instance.mainButtonText != null)
        {
            instance.mainButtonText.text = buttonText;
        }

        // set button action - important: do this first to ensure it works
        if (instance.mainButton != null)
        {
            if (instance.debugMode) Debug.Log("setting up main button click handler");

            // clear previous listeners and add new one
            instance.mainButton.onClick.RemoveAllListeners();

            // add action with popup closing
            instance.mainButton.onClick.AddListener(() =>
            {
                if (instance.debugMode) Debug.Log("main button clicked!");

                if (instance.useAnimations && !instance.isAnimating)
                {
                    // start animation and delay action
                    instance.StartCoroutine(instance.AnimateButtonClickAndAction(instance.mainButton, () =>
                    {
                        instance.ClosePopup();
                        buttonAction?.Invoke();
                    }));
                }
                else
                {
                    // no animation, just execute
                    instance.ClosePopup();
                    buttonAction?.Invoke();
                }
            });
        }

        // show the popup with delay for the content (but overlay is active immediately)
        if (delay > 0)
            instance.StartCoroutine(instance.DelayedShowPopup(delay));
        else
            instance.ShowPopupVisuals();
    }

    // convenience method for win popup - combined with static version
    public static void ShowWinPopup(float delay = 1.0f)
    {
        var instance = EnsureExists();
        if (instance == null)
        {
            Debug.LogError("failed to create or find popupcontroller instance");
            return;
        }

        // check if popup is already active
        if (instance.isPopupActive)
        {
            if (instance.debugMode) Debug.Log("win popup already active, ignoring duplicate call");
            return;
        }

        instance.isPopupActive = true;

        if (instance.debugMode) Debug.Log("showwinpopup called with delay: " + delay);

        // block input immediately by showing the overlay
        if (instance.backgroundOverlay != null)
        {
            instance.backgroundOverlay.gameObject.SetActive(true);
            Color color = instance.backgroundOverlay.color;
            color.a = 0; // start transparent
            instance.backgroundOverlay.color = color;
        }

        // hide close button for win popup
        if (instance.closeButton != null)
        {
            instance.closeButton.gameObject.SetActive(false);
        }

        // show the actual popup after delay
        ShowPopup(
            "Level Completed!",
            "Main Menu",
            () =>
            {
                if (instance.debugMode) Debug.Log("main menu button action triggered");
                if (GameManager.Instance != null)
                    GameManager.Instance.ReturnToMainMenu();
                else
                    Debug.LogError("gamemanager.instance is null!");
            },
            delay
        );

        // play celebration animations and particles if enabled (after delay)
        if (instance.useAnimations)
        {
            if (instance.debugMode) Debug.Log("starting win celebration sequence");

            instance.StartCoroutine(instance.DelayedAction(delay, () =>
            {
                if (instance.debugMode) Debug.Log("playing win celebration animation");
                // use enhanced celebration with particles
                instance.PlayWinCelebrationWithParticles();
            }));
        }
        else
        {
            if (instance.debugMode) Debug.Log("animations are disabled, skipping celebration");
        }
    }

    // convenience method for final win popup (no next level button) - combined with static version
    public static void ShowFinalWinPopup(float delay = 1.0f)
    {
        // redirect to regular win popup since they now have the same behavior
        ShowWinPopup(delay);
    }

    // convenience method for lose popup - combined with static version
    public static void ShowLosePopup(float delay = 1.0f)
    {
        var instance = EnsureExists();
        if (instance == null)
        {
            Debug.LogError("failed to create or find popupcontroller instance");
            return;
        }

        // check if popup is already active
        if (instance.isPopupActive)
        {
            if (instance.debugMode) Debug.Log("lose popup already active, ignoring duplicate call");
            return;
        }

        instance.isPopupActive = true;

        if (instance.debugMode) Debug.Log("showlosepopup called with delay: " + delay);

        // block input immediately by showing the overlay
        if (instance.backgroundOverlay != null)
        {
            instance.backgroundOverlay.gameObject.SetActive(true);
            Color color = instance.backgroundOverlay.color;
            color.a = 0; // start transparent
            instance.backgroundOverlay.color = color;
        }

        // ensure close button is visible for lose popup
        if (instance.closeButton != null)
        {
            instance.closeButton.gameObject.SetActive(true);
        }

        ShowPopup(
            "Out of Moves!",
            "Try Again",
            () =>
            {
                if (instance.debugMode) Debug.Log("try again button action triggered");
                if (GameManager.Instance != null)
                    GameManager.Instance.RestartLevel();
                else
                    Debug.LogError("gamemanager.instance is null!");
            },
            delay
        );

        // add subtle shake effect for lose popup (after delay)
        if (instance.useAnimations)
        {
            instance.StartCoroutine(instance.DelayedAction(delay, () =>
            {
                if (instance.debugMode) Debug.Log("starting shake animation");
                try
                {
                    instance.StartCoroutine(instance.PlayShakeAnimation());
                }
                catch (Exception e)
                {
                    Debug.LogError("error playing shake animation: " + e.Message);
                }
            }));
        }
    }

    // show popup visuals with proper animation sequence
    private void ShowPopupVisuals()
    {
        if (debugMode) Debug.Log("showpopupvisuals called");

        // stop any ongoing animations
        isAnimating = true;

        // kill any existing animations before starting new animations
        KillAllPopupTweens();

        // ensure popup controller is active
        gameObject.SetActive(true);

        // ensure background overlay is active to block input immediately
        if (backgroundOverlay != null)
        {
            backgroundOverlay.gameObject.SetActive(true);
            backgroundOverlay.color = new Color(backgroundOverlay.color.r, backgroundOverlay.color.g, backgroundOverlay.color.b, 0);
        }

        // make sure popup container exists
        if (popupContainer == null)
        {
            Debug.LogError("popupcontainer is null! check your inspector reference");
            isAnimating = false;
            return;
        }

        // activate the popup container
        popupContainer.SetActive(true);

        // don't set initial scale here - let animations handle it
        // start the simple animation routine
        StartCoroutine(AnimatePopupAppear());
    }

    // animation for popup appearance
    private IEnumerator AnimatePopupAppear()
    {
        if (debugMode) Debug.Log("animatepopupappear called");

        // get canvasgroup component
        CanvasGroup canvasGroup = popupContainer.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = popupContainer.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }

        // get animationmanager instance
        AnimationManager animManager = AnimationManager.Instance;
        if (animManager != null && useAnimations)
        {
            // use animationmanager to animate popup showing
            animManager.AnimatePopupShow(popupContainer, backgroundOverlay);

            // add a delay to wait for animation to complete
            yield return new WaitForSeconds(0.3f);

            // animate title if available
            if (titleText != null)
            {
                animManager.AnimateTitle(titleText);
            }

            // animate buttons sequentially with small delays
            if (mainButton != null)
            {
                animManager.AnimateButtonShow(mainButton, 0.1f);
            }

            if (closeButton != null)
            {
                animManager.AnimateButtonShow(closeButton, 0.2f);
            }
        }
        else
        {
            // fallback if animations disabled or no animationmanager
            // set final values directly
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            if (backgroundOverlay != null)
            {
                backgroundOverlay.color = new Color(
                    backgroundOverlay.color.r,
                    backgroundOverlay.color.g,
                    backgroundOverlay.color.b,
                    0.7f
                );
            }
        }

        // make buttons interactable at the end and ensure their colors are correct
        if (mainButton != null)
        {
            mainButton.interactable = true;
            if (debugMode) Debug.Log("main button set interactable after animation");
        }

        if (closeButton != null)
        {
            closeButton.interactable = true;
        }

        // animation complete
        isAnimating = false;
    }

    // animation for popup disappearance
    private IEnumerator AnimatePopupDisappear()
    {
        // get animationmanager instance
        AnimationManager animManager = AnimationManager.Instance;
        if (animManager != null && useAnimations)
        {
            // use animationmanager to animate popup hiding
            animManager.AnimatePopupHide(popupContainer, backgroundOverlay);

            // add a delay to wait for animation to complete
            yield return new WaitForSeconds(0.25f);
        }
        else
        {
            // fallback if animations disabled or no animationmanager
            // hide elements immediately
            if (popupContainer != null)
            {
                CanvasGroup canvasGroup = popupContainer.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }
            }

            if (backgroundOverlay != null)
            {
                backgroundOverlay.color = new Color(
                    backgroundOverlay.color.r,
                    backgroundOverlay.color.g,
                    backgroundOverlay.color.b,
                    0f
                );
            }
        }

        // hide popup components after animation
        if (popupContainer != null)
            popupContainer.SetActive(false);

        if (backgroundOverlay != null)
            backgroundOverlay.gameObject.SetActive(false);

        // return to main menu when closing
        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToMainMenu();
        else
            Debug.LogError("gamemanager.instance is null in closepopup!");

        // animation complete
        isAnimating = false;
    }

    // button click animation and action
    private IEnumerator AnimateButtonClickAndAction(Button button, Action onComplete)
    {
        // get animationmanager instance
        AnimationManager animManager = AnimationManager.Instance;
        if (animManager != null && useAnimations)
        {
            // use animationmanager to animate button click
            animManager.AnimateButtonClick(button);

            // add a delay to wait for animation to complete
            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            // small delay even if no animation to prevent instant clicks
            yield return new WaitForSeconds(0.1f);
        }

        // execute the callback
        onComplete?.Invoke();
    }

    // celebration animation for win popup with particles
    private void PlayWinCelebrationWithParticles()
    {
        if (popupContainer == null) return;

        // get animationmanager instance
        AnimationManager animManager = AnimationManager.Instance;
        if (animManager != null && useAnimations)
        {
            // use animationmanager to animate win celebration
            // but only pass the UI base for animation, use particleContainer for particles
            animManager.AnimateWinCelebration(popupContainer, particleContainer);

            if (debugMode) Debug.Log("Playing win celebration animation with StarConfetti and AddStarConfetti effects");
        }
        // no need to set scales - let animations handle it
    }

    // special celebration for final win with extra effects
    private void PlayFinalWinCelebrationWithParticles()
    {
        if (popupContainer == null) return;

        // get animationmanager instance
        AnimationManager animManager = AnimationManager.Instance;
        if (animManager != null && useAnimations)
        {
            // use animationmanager to animate win celebration
            // but only pass the UI base for animation, use particleContainer for particles
            animManager.AnimateWinCelebration(popupContainer, particleContainer);

            if (debugMode) Debug.Log("Playing enhanced win celebration for final win with StarConfetti and AddStarConfetti effects");
        }
        // no need to set scales - let animations handle it
    }

    // original simple celebration animation maintained for compatibility
    private void PlaySimpleCelebrationAnimation()
    {
        // redirect to enhanced version for consistency
        PlayWinCelebrationWithParticles();
    }

    // simple shake animation for the lose popup
    private IEnumerator PlayShakeAnimation()
    {
        if (popupContainer == null) yield break;

        // get animationmanager instance
        AnimationManager animManager = AnimationManager.Instance;
        if (animManager != null && useAnimations)
        {
            // use animationmanager to animate shake
            animManager.AnimateShake(popupContainer.transform, 5f, 0.5f);

            // wait for animation to complete
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            // skip animation if disabled
            yield return null;
        }
    }

    // reset ui element scales only if they're zero (emergency fallback)
    private void ResetElementScales()
    {

        // check and reset popup container scale only if zero
        if (popupContainer != null && popupContainer.transform.localScale == Vector3.zero)
        {
            if (debugMode) Debug.Log("emergency fix: popupcontainer had zero scale - resetting to (1,1,1)");
            popupContainer.transform.localScale = Vector3.one;
        }

        // check and reset popup ribbon only if zero
        if (popupRibbon != null && popupRibbon.transform.localScale == Vector3.zero)
        {
            if (debugMode) Debug.Log("emergency fix: popupribbon had zero scale - resetting to (1,1,1)");
            popupRibbon.transform.localScale = Vector3.one;
        }

        // check and reset title text only if zero
        if (titleText != null && titleText.transform.localScale == Vector3.zero)
        {
            if (debugMode) Debug.Log("emergency fix: titletext had zero scale - resetting to (1,1,1)");
            titleText.transform.localScale = Vector3.one;
        }

        // check and reset main button only if zero
        if (mainButton != null && mainButton.transform.localScale == Vector3.zero)
        {
            if (debugMode) Debug.Log("emergency fix: mainbutton had zero scale - resetting to (1,1,1)");
            mainButton.transform.localScale = Vector3.one;
        }

        // check and reset main button text only if zero
        if (mainButtonText != null && mainButtonText.transform.localScale == Vector3.zero)
        {
            if (debugMode) Debug.Log("emergency fix: mainbuttontext had zero scale - resetting to (1,1,1)");
            mainButtonText.transform.localScale = Vector3.one;
        }

        // check and reset close button only if zero
        if (closeButton != null && closeButton.transform.localScale == Vector3.zero)
        {
            if (debugMode) Debug.Log("emergency fix: closebutton had zero scale - resetting to (1,1,1)");
            closeButton.transform.localScale = Vector3.one;
        }

        // check and reset popup base only if zero
        if (popupBase != null && popupBase.transform.localScale == Vector3.zero)
        {
            if (debugMode) Debug.Log("emergency fix: popupbase had zero scale - resetting to (1,1,1)");
            popupBase.transform.localScale = Vector3.one;
        }
    }

    // utility function to delay an action
    private IEnumerator DelayedAction(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }

    // show popup after a delay
    private IEnumerator DelayedShowPopup(float delay)
    {
        if (debugMode) Debug.Log($"delayedshowpopup waiting {delay} seconds");

        yield return new WaitForSeconds(delay);
        ShowPopupVisuals();
    }

    public void ClosePopup()
    {
        if (debugMode) Debug.Log("closepopup called");

        // prevent multiple close calls
        if (isAnimating)
            return;

        isAnimating = true;
        isPopupActive = false; // reset popup active flag when closing

        // disable buttons during close animation
        if (mainButton != null) mainButton.interactable = false;
        if (closeButton != null) closeButton.interactable = false;

        // kill any existing animations before starting close animation
        KillAllPopupTweens();

        // animate popup closing
        StartCoroutine(AnimatePopupDisappear());
    }

    // add ondestroy method to clean up singleton reference
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        // kill any active tweens on popup components
        KillAllPopupTweens();
    }

    // helper method to kill all tweens on popup components
    private void KillAllPopupTweens()
    {
        // kill tweens for all popup components
        if (popupContainer != null)
        {
            DOTween.Kill(popupContainer.transform);
            foreach (Transform child in popupContainer.transform)
            {
                DOTween.Kill(child);
            }
        }

        if (backgroundOverlay != null)
            DOTween.Kill(backgroundOverlay);

        if (titleText != null)
            DOTween.Kill(titleText.transform);

        if (mainButton != null)
            DOTween.Kill(mainButton.transform);

        if (closeButton != null)
            DOTween.Kill(closeButton.transform);

        if (popupBase != null)
            DOTween.Kill(popupBase.transform);

        if (popupRibbon != null)
            DOTween.Kill(popupRibbon.transform);

        if (debugMode) Debug.Log("Killed all popup tweens");
    }
}