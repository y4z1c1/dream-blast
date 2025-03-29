using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

// obstacle counter ui is a class that represents the obstacle counter in the game.
public class ObstacleCounterUI : MonoBehaviour
{
    [System.Serializable]
    public class ObstacleCounter
    {
        public string obstacleType; // "box", "stone", "vase"
        public Image iconImage;
        public TextMeshProUGUI countText;
        public GameObject counterObject;
        public GameObject checkmarkObject; // checkmark to show when completed
    }

    [Header("Obstacle Counters")]
    [SerializeField] private List<ObstacleCounter> counters = new List<ObstacleCounter>();

    // layout settings in order to make the counter look good
    [Header("Layout Settings")]
    [SerializeField] private float scaleTwoOrLess = 0.03f;
    [SerializeField] private float scaleThree = 0.02f;
    [SerializeField] private int paddingTwoOrLess = 29;
    [SerializeField] private int paddingThree = 27;
    [SerializeField] private GridLayoutGroup gridLayout;

    [Header("References")]
    [SerializeField] private LevelController levelController;

    // set of obstacle types initially present in the level
    private HashSet<string> availableObstacleTypes = new HashSet<string>();
    private Dictionary<string, int> obstacleCounts = new Dictionary<string, int>();
    private bool isInitialized = false;

    private void Start()
    {
        // hide all counters and checkmarks initially
        foreach (var counter in counters)
        {
            if (counter.counterObject != null)
                counter.counterObject.SetActive(false);

            if (counter.checkmarkObject != null)
                counter.checkmarkObject.SetActive(false);
        }

        // find level controller if not assigned
        if (levelController == null)
            levelController = FindFirstObjectByType<LevelController>();

        // register with level controller for updates
        if (levelController != null)
        {
            levelController.RegisterObstacleCounter(this);
        }
        else
        {
            Debug.LogError("[ObstacleCounterUI] No LevelController found!");
        }
    }

    // initialize with obstacle data from LevelController
    public void InitializeObstacleTypes(List<Obstacle> obstacles)
    {
        if (obstacles == null) return;

        availableObstacleTypes.Clear();
        obstacleCounts.Clear();

        // process obstacles from the level controller
        foreach (Obstacle obstacle in obstacles)
        {
            string type = GetObstacleType(obstacle);
            availableObstacleTypes.Add(type);

            // count obstacles of each type
            if (obstacleCounts.ContainsKey(type))
                obstacleCounts[type]++;
            else
                obstacleCounts[type] = 1;
        }

        isInitialized = true;
        Debug.Log($"[ObstacleCounterUI] Initialized with {availableObstacleTypes.Count} obstacle types: {string.Join(", ", availableObstacleTypes)}");

        // update the UI
        UpdateCounters();
    }

    // called by LevelController when an obstacle is destroyed
    public void OnObstacleDestroyed(Obstacle obstacle)
    {
        if (!isInitialized) return;

        string type = GetObstacleType(obstacle);

        if (obstacleCounts.ContainsKey(type) && obstacleCounts[type] > 0)
        {
            obstacleCounts[type]--;
            Debug.Log($"[ObstacleCounterUI] Obstacle {type} destroyed. Remaining: {obstacleCounts[type]}");

            // update the UI
            UpdateCounters();
        }
    }

    // update the UI based on current obstacle counts
    public void UpdateCounters()
    {
        if (!isInitialized) return;

        // count active counters for layout
        int activeTypes = 0;

        // update each counter
        foreach (var counter in counters)
        {
            if (counter.counterObject == null)
                continue;

            // check if this type was available in the level
            bool typeAvailableInLevel = availableObstacleTypes.Contains(counter.obstacleType);

            // get count for this type
            int count = 0;
            obstacleCounts.TryGetValue(counter.obstacleType, out count);

            if (!typeAvailableInLevel)
            {
                // This type was never in the level, hide counter
                counter.counterObject.SetActive(false);
            }
            else if (count > 0)
            {
                // Type is in level and has obstacles remaining
                counter.counterObject.SetActive(true);

                // Show count text
                if (counter.countText != null)
                {
                    counter.countText.text = count.ToString();
                    counter.countText.gameObject.SetActive(true);
                }

                // Hide checkmark
                if (counter.checkmarkObject != null)
                    counter.checkmarkObject.SetActive(false);

                activeTypes++;
            }
            else
            {
                // Type was in level but is now cleared - show checkmark
                counter.counterObject.SetActive(true);

                // Hide count
                if (counter.countText != null)
                    counter.countText.gameObject.SetActive(false);

                // Show checkmark
                if (counter.checkmarkObject != null)
                    counter.checkmarkObject.SetActive(true);

                activeTypes++;
            }
        }

        // update layout based on number of active counters
        UpdateLayout(activeTypes);
    }

    private void UpdateLayout(int activeTypes)
    {
        if (gridLayout == null)
            return;

        // force the layout update to apply immediately    
        Canvas.ForceUpdateCanvases();

        // update Y position based on number of obstacles using RectTransform
        RectTransform rectTransform = GetComponent<RectTransform>();
        Vector2 anchoredPosition = rectTransform.anchoredPosition;
        anchoredPosition.y = (activeTypes == 3) ? -27.1f : -23.8f;
        rectTransform.anchoredPosition = anchoredPosition;

        // apply different scale and padding based on number of active counters
        if (activeTypes <= 2)
        {
            // for 1-2 obstacles: set individual counter scales to 0.03
            SetCounterScales(scaleTwoOrLess);

            // set layout padding
            gridLayout.padding.bottom = paddingTwoOrLess;

            // for 1-2 items, use a single row layout
            gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
        }
        else
        {
            // for 3 obstacles: set individual counter scales to 0.02
            SetCounterScales(scaleThree);

            // set layout padding
            gridLayout.padding.bottom = paddingThree;

            // explicitly set constraints for 3 items (2 in first row, 1 in second row)
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            // rearrange counters to ensure third is centered
            RearrangeCountersIfNeeded(GetActiveCounterObjects());
        }

        // apply the layout immediately
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    // Set scale for all active counter objects
    private void SetCounterScales(float scale)
    {
        foreach (var counter in counters)
        {
            if (counter.counterObject != null && counter.counterObject.activeSelf)
            {
                counter.counterObject.transform.localScale = new Vector3(scale, scale, scale);
            }
        }
    }

    // get array of active counter objects
    private GameObject[] GetActiveCounterObjects()
    {
        List<GameObject> activeObjects = new List<GameObject>();

        foreach (ObstacleCounter counter in counters)
        {
            if (counter.counterObject != null && counter.counterObject.activeSelf)
            {
                activeObjects.Add(counter.counterObject);
            }
        }

        return activeObjects.ToArray();
    }

    // rearrange counters to ensure proper layout
    private void RearrangeCountersIfNeeded(GameObject[] activeCounters)
    {
        if (activeCounters.Length == 3)
        {
            // ensure the siblingIndex is set correctly for proper layout
            // the first two will be in the first row, the third in the second row
            for (int i = 0; i < activeCounters.Length; i++)
            {
                activeCounters[i].transform.SetSiblingIndex(i);
            }
        }
    }

    private string GetObstacleType(Obstacle obstacle)
    {
        // determine type based on class
        if (obstacle is BoxObstacle) return "box";
        if (obstacle is StoneObstacle) return "stone";
        if (obstacle is VaseObstacle) return "vase";

        return "unknown";
    }
}