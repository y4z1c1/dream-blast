using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // singleton instance
    public static GameManager Instance { get; private set; }

    // scene names
    private const string MAIN_SCENE = "MainScene";
    private const string LEVEL_SCENE = "LevelScene";

    // save data constants
    private const string LEVEL_KEY = "CurrentLevel";
    private const int DEFAULT_LEVEL = 1;

    // ui references
    [SerializeField] private TextMeshProUGUI levelButtonText;
    [SerializeField] private GameObject levelButtonObj;
    [SerializeField] private Button levelButton;

    // debug level setting
    [SerializeField] private int debugLevelToSet = 1;

    private void Awake()
    {
        // set up singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // register for scene loaded events
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // unregister from scene loaded events when destroyed
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        // debug level control with number keys
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
            {
                SetLevel(i);
                Debug.Log("Level set to: " + i);
                UpdateLevelButtonText();
            }
        }

        // start level with return key
        if (Input.GetKeyDown(KeyCode.Return))
        {
            StartLevel();
        }

    }

    // called when a scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // if we return to main scene, find and update levelButtonText reference
        if (scene.name == MAIN_SCENE)
        {
            // find the levelButtonText in the scene
            GameObject levelButton = GameObject.Find("LevelButton");
            if (levelButton != null)
            {
                // store button references
                levelButtonObj = levelButton;
                this.levelButton = levelButton.GetComponent<UnityEngine.UI.Button>();

                // reconnect the text reference
                levelButtonText = levelButton.GetComponentInChildren<TextMeshProUGUI>();

                // ensure button is active but initially disable interaction until animation finishes
                levelButtonObj.SetActive(true);
                if (this.levelButton != null)
                    this.levelButton.interactable = false;

                // reconnect the button click handler
                if (this.levelButton != null)
                {
                    // clear existing listeners and add our click handler
                    this.levelButton.onClick.RemoveAllListeners();
                    this.levelButton.onClick.AddListener(OnLevelButtonClick);
                }

                // update the text with current level
                UpdateLevelButtonText();

                // animate button appearance
                AnimateLevelButtonAppear();


            }
        }
        else if (scene.name == LEVEL_SCENE)
        {
            // kill any ongoing animations to prevent issues when returning to main scene
            if (levelButtonObj != null)
            {
                DOTween.Kill(levelButtonObj.transform);
            }


        }
    }

    private void Start()
    {
        // update the level button text
        UpdateLevelButtonText();
    }

    // animate the level button appearing from bottom
    private void AnimateLevelButtonAppear()
    {
        // validation
        if (levelButtonObj == null)
            return;

        // try to get animation manager
        AnimationManager animManager = AnimationManager.Instance;

        if (animManager != null)
        {
            // ensure button is active but initially disable interaction until animation finishes
            levelButtonObj.SetActive(true);
            if (levelButton != null)
                levelButton.interactable = false;

            // use animation manager for appearance animation
            animManager.AnimateLevelButtonAppearance(
                levelButtonObj.transform,
                0.6f,  // duration
                400f,   // offset from bottom
                () =>
                {
                    // re-enable button interaction after animation completes
                    if (levelButton != null)
                        levelButton.interactable = true;
                }
            );
        }
        else
        {
            // fallback if animation manager not available
            // make sure button is active and visible
            levelButtonObj.SetActive(true);

            // ensure any canvas group is fully visible
            CanvasGroup canvasGroup = levelButtonObj.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            // enable interaction immediately
            if (levelButton != null)
                levelButton.interactable = true;
        }
    }

    // load the level scene
    public void StartLevel()
    {
        // kill all particles before scene transition
        ParticleManager particleManager = FindObjectOfType<ParticleManager>();
        if (particleManager != null)
        {
            particleManager.StopAllParticles();
        }

        // load the level scene
        SceneManager.LoadScene(LEVEL_SCENE);
    }

    // reload the current level scene
    public void RestartLevel()
    {
        // kill all particles before scene transition
        ParticleManager particleManager = FindObjectOfType<ParticleManager>();
        if (particleManager != null)
        {
            particleManager.StopAllParticles();
        }

        // reload the current level scene
        SceneManager.LoadScene(LEVEL_SCENE);
    }

    // return to the main menu
    public void ReturnToMainMenu()
    {
        // kill all particles before scene transition
        ParticleManager particleManager = FindObjectOfType<ParticleManager>();
        if (particleManager != null)
        {
            particleManager.StopAllParticles();
        }

        // load the main menu scene
        SceneManager.LoadScene(MAIN_SCENE);
    }

    // complete the current level and return to main menu
    public void CompleteLevel()
    {
        // get current level
        int currentLevel = GetCurrentLevel();

        // increase level
        currentLevel++;

        // save new level
        SaveCurrentLevel(currentLevel);

        StartLevel();
    }

    // set a specific level (for inspector/debugging)
    public void SetLevel(int level)
    {
        SaveCurrentLevel(level);
    }

    // get the current level
    public int GetCurrentLevel()
    {
        return PlayerPrefs.GetInt(LEVEL_KEY, DEFAULT_LEVEL);
    }

    // save the current level
    public void SaveCurrentLevel(int level)
    {
        PlayerPrefs.SetInt(LEVEL_KEY, level);
        PlayerPrefs.Save();
    }

    // check if all levels are completed
    public bool AreAllLevelsCompleted()
    {
        return GetCurrentLevel() > 10;
    }

    // debug level utilities
    [ContextMenu("Set Debug Level")]
    public void SetDebugLevel()
    {
        SetLevel(debugLevelToSet);
        Debug.Log("Level set to: " + debugLevelToSet);

        // Update UI
        UpdateLevelButtonText();
    }

    // ui management
    public void UpdateLevelButtonText()
    {
        if (levelButtonText != null)
        {
            if (AreAllLevelsCompleted())
            {
                levelButtonText.text = "Finished";
            }
            else
            {
                levelButtonText.text = "Level : " + GetCurrentLevel();
            }
        }
    }

    // button click handlers
    public void OnLevelButtonClick()
    {
        if (AreAllLevelsCompleted())
            return;

        // immediately disable interaction to prevent multiple clicks
        if (levelButton != null)
        {
            levelButton.interactable = false;
        }

        // First play the click animation
        PlayLevelButtonClickAnimation(() =>
        {
            // Try to get animation manager
            AnimationManager animManager = AnimationManager.Instance;
            if (animManager != null)
            {
                // Play exit animation before loading level
                animManager.AnimateLevelButtonExit(levelButtonObj.transform, 0.8f, 400f, () =>
                {
                    // Load the level scene after animation completes
                    StartLevel();
                });
            }
            else
            {
                // If no animation manager, just load the level
                StartLevel();
            }
        });
    }

    // Play the same button click animation as used in PopupController
    private void PlayLevelButtonClickAnimation(System.Action onComplete)
    {
        if (levelButtonObj == null || levelButton == null)
        {

            onComplete?.Invoke();
            return;
        }

        // check if AnimationManager is available
        AnimationManager animManager = AnimationManager.Instance;
        if (animManager != null)
        {

            // Use button click animation from the AnimationManager
            animManager.AnimateButtonClick(levelButton);

            // Wait for the animation to complete before continuing
            // The animation duration is controlled by AnimationManager
            StartCoroutine(WaitForButtonAnimation(onComplete));
        }
        else
        {


            onComplete?.Invoke();
        }
    }

    // Helper to wait for button animation to complete
    private IEnumerator WaitForButtonAnimation(System.Action onComplete)
    {

        yield return new WaitForSeconds(0.1f);

        // Complete
        onComplete?.Invoke();
    }



    // button click handler for the win button
    public void OnWinButtonClick()
    {
        CompleteLevel();
    }

    // button click handler for the lose button
    public void OnLoseButtonClick()
    {
        ReturnToMainMenu();
    }


}
