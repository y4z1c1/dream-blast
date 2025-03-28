using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private MoveCounter moveCounter;
    [SerializeField] private MatchFinder matchFinder;
    public PopupController popupController;
    [SerializeField] private LevelParser levelParser;
    [SerializeField] private FallingController fallingController;
    [SerializeField] private GridFiller gridFiller;
    private AnimationManager animationManager;

    [Header("Debug Settings")]
    [SerializeField] private bool debugMode = false;

    [Header("Prefabs")]
    [SerializeField] private GameObject redCubePrefab;
    [SerializeField] private GameObject greenCubePrefab;
    [SerializeField] private GameObject blueCubePrefab;
    [SerializeField] private GameObject yellowCubePrefab;
    [SerializeField] private GameObject boxObstaclePrefab;
    [SerializeField] private GameObject stoneObstaclePrefab;
    [SerializeField] private GameObject vaseObstaclePrefab;

    // list of all obstacles in the level
    private List<Obstacle> obstacles = new List<Obstacle>();

    // reference to obstacle counter UI
    private ObstacleCounterUI obstacleCounter;

    // current level data
    private LevelData currentLevelData;

    private bool isGameOver = false;
    private bool isDelayingFalling = false;
    private bool isPopupShown = false;

    // helper method for debug logging
    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[LevelController] {message}");
        }
    }

    // helper method for debug warnings
    private void DebugLogWarning(string message)
    {
        if (debugMode)
        {
            Debug.LogWarning($"[LevelController] {message}");
        }
    }

    // helper method for debug errors
    private void DebugLogError(string message)
    {
        // Always log errors regardless of debug mode
        Debug.LogError($"[LevelController] {message}");
    }

    private void Awake()
    {
        // find references if not set in inspector
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (matchFinder == null)
            matchFinder = FindFirstObjectByType<MatchFinder>();

        // get reference to AnimationManager
        animationManager = AnimationManager.Instance;
        if (animationManager == null)
        {
            DebugLogWarning("AnimationManager instance not found!");
        }

        // set up cross-references
        if (gridManager != null && matchFinder != null)
        {
            // explicitly set the gridmanager reference in matchfinder
            matchFinder.SetGridManager(gridManager);
        }
    }

    private void Start()
    {
        isGameOver = false;

        // get level parser if not assigned
        if (levelParser == null)
            levelParser = GetComponent<LevelParser>();

        // find popup controller if not assigned
        if (popupController == null)
            popupController = FindFirstObjectByType<PopupController>();

        // load the current level
        LoadCurrentLevel();
    }

    // Register the ObstacleCounterUI with the LevelController
    public void RegisterObstacleCounter(ObstacleCounterUI counter)
    {
        obstacleCounter = counter;
        DebugLog("ObstacleCounterUI registered with LevelController");

        // If we already have obstacles, initialize the counter
        if (obstacles.Count > 0)
        {
            InitializeObstacleCounter();
        }
    }

    // Initialize the obstacle counter with current obstacle data
    private void InitializeObstacleCounter()
    {
        if (obstacleCounter != null)
        {
            DebugLog($"Initializing ObstacleCounterUI with {obstacles.Count} obstacles");
            obstacleCounter.InitializeObstacleTypes(obstacles);
        }
    }

    // load the current level from savemanager
    public void LoadCurrentLevel()
    {
        // check if levelscene is loaded first or game started properly
        if (GameManager.Instance == null)
        {
            DebugLogError("GameManager.Instance is null! Make sure GameManager exists in the scene and is properly initialized.");
            LoadLevel(1);
            return;
        }

        // get current level from savemanager
        int currentLevel = GameManager.Instance.GetCurrentLevel();

        // load the level data
        LoadLevel(currentLevel);
    }

    // load a specific level
    public void LoadLevel(int levelNumber)
    {
        // load level data from parser
        currentLevelData = levelParser.LoadLevel(levelNumber);

        if (currentLevelData == null)
        {
            DebugLogError($"Failed to load level {levelNumber}");
            return;
        }

        // configure grid dimensions
        ConfigureLevel();
    }

    // configure the level based on loaded data
    private void ConfigureLevel()
    {
        // set move counter
        if (moveCounter != null)
        {
            moveCounter.SetMaxMoves(currentLevelData.move_count);
        }

        // initialize grid with the correct dimensions
        if (gridManager != null)
        {
            DebugLog("Initializing grid...");

            // Initialize grid with callback for when initialization is complete
            gridManager.InitializeGrid(
                currentLevelData.grid_width,
                currentLevelData.grid_height,
                OnGridInitializationComplete
            );

            // We'll populate the grid in the callback now, not immediately
        }
        else
        {
            DebugLogError("GridManager is null, cannot initialize grid!");
        }
    }

    // Callback for when grid initialization is complete
    private void OnGridInitializationComplete(bool success)
    {
        if (success)
        {
            DebugLog("Grid initialization successful, proceeding with grid population");

            // re-set the gridmanager reference in matchfinder after grid recreation
            if (matchFinder != null)
            {
                matchFinder.SetGridManager(gridManager);
                DebugLog("Re-set GridManager reference in MatchFinder after grid initialization");
            }

            // Now that grid is fully initialized, we can safely populate it
            PopulateGrid();
        }
        else
        {
            DebugLogError("Grid initialization failed!");
        }
    }

    // populate the grid with initial tiles
    private void PopulateGrid()
    {
        if (currentLevelData == null || gridManager == null) return;


        gridManager.ResetTapEnabled();

        // populate grid with initial tiles based on level data
        for (int x = 0; x < currentLevelData.grid_width; x++)
        {
            for (int y = 0; y < currentLevelData.grid_height; y++)
            {
                // get the item type from level data
                string itemType = currentLevelData.GetGridItem(x, y);

                DebugLog($"Populating grid at position ({x},{y}) with item type: {itemType}");

                // get the cell at this position
                GridCell cell = gridManager.GetCell(x, y);

                if (cell != null)
                {
                    // create the appropriate item based on type
                    CreateGridItem(itemType, cell, x, y);
                }
            }
        }

        // Now that all obstacles are created, initialize the obstacle counter
        InitializeObstacleCounter();

        // Animate the grid appearing from the bottom
        gridManager.AnimateGridAppearance();

        // scan for potential matches
        if (matchFinder != null)
        {
            matchFinder.ScanGridForMatches();
        }
    }

    // create a grid item based on its type
    private void CreateGridItem(string itemType, GridCell cell, int x, int y)
    {
        switch (itemType.ToLower())
        {
            case "r":
                CreateCube(cell, x, y, Cube.CubeColor.Red);
                break;
            case "g":
                CreateCube(cell, x, y, Cube.CubeColor.Green);
                break;
            case "b":
                CreateCube(cell, x, y, Cube.CubeColor.Blue);
                break;
            case "y":
                CreateCube(cell, x, y, Cube.CubeColor.Yellow);
                break;
            case "rand":
                CreateRandomCube(cell, x, y);
                break;
            case "bo":
                CreateBoxObstacle(cell, x, y);
                break;
            case "vro":
                CreateRocket(cell, x, y, Rocket.RocketDirection.Vertical);
                break;
            case "hro":
                CreateRocket(cell, x, y, Rocket.RocketDirection.Horizontal);
                break;
            case "s":
                CreateStoneObstacle(cell, x, y);
                break;
            case "v":
                CreateVaseObstacle(cell, x, y);
                break;
            case "empty":
                // leave cell empty
                break;
            default:
                DebugLogWarning($"Unknown item type: {itemType}. Leaving cell empty.");
                break;
        }
    }

    // create a cube with specific color
    private void CreateCube(GridCell cell, int x, int y, Cube.CubeColor color)
    {
        // get the appropriate cube prefab for this color
        GameObject cubePrefab = GetCubePrefabForColor(color);

        if (cubePrefab == null)
        {
            DebugLogError($"No prefab found for cube color: {color}");
            return;
        }

        // get the exact world position for this cell
        Vector3 cellPosition = cell.transform.position;

        // instantiate the cube at the exact cell position
        GameObject cubeObject = Instantiate(cubePrefab, cellPosition, Quaternion.identity);
        cubeObject.name = $"Cube_{color}_{x}_{y}";
        Cube cube = cubeObject.GetComponent<Cube>();

        if (cube == null)
        {
            DebugLogError("Created cube object has no Cube component!");
            Destroy(cubeObject);
            return;
        }

        // parent the cube to the grid cell's parent (which is gridCellsContainer)
        cubeObject.transform.SetParent(cell.transform.parent);

        // initialize the cube
        cube.Initialize(x, y, color, gridManager, matchFinder);

        // explicitly set the cube in the cell
        cell.SetItem(cube);

        // verify the setup is correct
        GridItem itemInCell = cell.GetItem();
        if (itemInCell != cube)
        {
            DebugLogWarning($"Cube at [{x},{y}] not properly registered with cell. Fixing connection...");
            cell.SetItem(cube);
        }
    }

    // get cube prefab based on color
    private GameObject GetCubePrefabForColor(Cube.CubeColor color)
    {
        switch (color)
        {
            case Cube.CubeColor.Red:
                return redCubePrefab;
            case Cube.CubeColor.Green:
                return greenCubePrefab;
            case Cube.CubeColor.Blue:
                return blueCubePrefab;
            case Cube.CubeColor.Yellow:
                return yellowCubePrefab;
            default:
                return redCubePrefab; // default fallback
        }
    }

    // create a cube with random color
    private void CreateRandomCube(GridCell cell, int x, int y)
    {
        // create a cube with random color
        Cube.CubeColor randomColor = (Cube.CubeColor)Random.Range(0, 4); // assuming 4 colors
        CreateCube(cell, x, y, randomColor);
    }

    // create a box obstacle
    private void CreateBoxObstacle(GridCell cell, int x, int y)
    {
        // get the exact world position for this cell
        Vector3 cellPosition = cell.transform.position;

        // instantiate the box obstacle prefab at the exact cell position
        GameObject obstacleObject = Instantiate(boxObstaclePrefab, cellPosition, Quaternion.identity);
        obstacleObject.name = $"BoxObstacle_{x}_{y}";
        BoxObstacle obstacle = obstacleObject.GetComponent<BoxObstacle>();

        // parent the obstacle to maintain proper hierarchy
        obstacleObject.transform.SetParent(cell.transform.parent);

        // set up the obstacle
        if (obstacle != null)
        {
            obstacle.SetGridPosition(x, y);
            obstacle.Initialize(gridManager, this);

            // set the obstacle in the cell
            cell.SetItem(obstacle);

            // verify the setup is correct
            GridItem itemInCell = cell.GetItem();
            if (itemInCell != obstacle)
            {
                DebugLogWarning($"Obstacle at [{x},{y}] not properly registered with cell. Fixing connection...");
                cell.SetItem(obstacle);
            }
        }
    }

    // create a stone obstacle
    private void CreateStoneObstacle(GridCell cell, int x, int y)
    {
        // get the exact world position for this cell
        Vector3 cellPosition = cell.transform.position;

        // instantiate the stone obstacle prefab at the exact cell position
        GameObject obstacleObject = Instantiate(stoneObstaclePrefab, cellPosition, Quaternion.identity);
        obstacleObject.name = $"StoneObstacle_{x}_{y}";
        StoneObstacle obstacle = obstacleObject.GetComponent<StoneObstacle>();

        // parent the obstacle to maintain proper hierarchy
        obstacleObject.transform.SetParent(cell.transform.parent);

        // set up the obstacle
        if (obstacle != null)
        {
            obstacle.SetGridPosition(x, y);
            obstacle.Initialize(gridManager, this);

            // set the obstacle in the cell
            cell.SetItem(obstacle);

            // verify the setup is correct
            GridItem itemInCell = cell.GetItem();
            if (itemInCell != obstacle)
            {
                DebugLogWarning($"Stone obstacle at [{x},{y}] not properly registered with cell. Fixing connection...");
                cell.SetItem(obstacle);
            }
        }
    }

    // create a vase obstacle
    private void CreateVaseObstacle(GridCell cell, int x, int y)
    {
        // get the exact world position for this cell
        Vector3 cellPosition = cell.transform.position;

        // instantiate the vase obstacle prefab at the exact cell position
        GameObject obstacleObject = Instantiate(vaseObstaclePrefab, cellPosition, Quaternion.identity);
        obstacleObject.name = $"VaseObstacle_{x}_{y}";
        VaseObstacle obstacle = obstacleObject.GetComponent<VaseObstacle>();

        // parent the obstacle to maintain proper hierarchy
        obstacleObject.transform.SetParent(cell.transform.parent);

        // set up the obstacle
        if (obstacle != null)
        {
            obstacle.SetGridPosition(x, y);
            obstacle.Initialize(gridManager, this);

            // set the obstacle in the cell
            cell.SetItem(obstacle);

            // verify the setup is correct
            GridItem itemInCell = cell.GetItem();
            if (itemInCell != obstacle)
            {
                DebugLogWarning($"Vase obstacle at [{x},{y}] not properly registered with cell. Fixing connection...");
                cell.SetItem(obstacle);
            }
        }
    }

    // create a rocket
    private void CreateRocket(GridCell cell, int x, int y, Rocket.RocketDirection direction)
    {
        // find the rocket prefab - adjust this path to match your project structure
        GameObject rocketPrefab = Resources.Load<GameObject>("Prefabs/Rocket");

        if (rocketPrefab == null)
        {
            DebugLogError("Rocket prefab not found! Make sure your prefab is in the Resources folder.");
            CreateRandomCube(cell, x, y); // fallback to random cube
            return;
        }

        // get the exact world position for this cell
        Vector3 cellPosition = cell.transform.position;

        // instantiate the rocket at the exact cell position
        GameObject rocketObject = Instantiate(rocketPrefab, cellPosition, Quaternion.identity);
        rocketObject.name = $"Rocket_{direction}_{x}_{y}";
        Rocket rocket = rocketObject.GetComponent<Rocket>();

        // parent the rocket to maintain proper hierarchy
        rocketObject.transform.SetParent(cell.transform.parent);

        // set up the rocket
        if (rocket != null)
        {
            // set the direction
            rocket.SetDirection(direction);

            // initialize the rocket
            rocket.Initialize(x, y, gridManager, this);

            // set the rocket in the cell
            cell.SetItem(rocket);

            // verify the setup is correct
            GridItem itemInCell = cell.GetItem();
            if (itemInCell != rocket)
            {
                DebugLogWarning($"Rocket at [{x},{y}] not properly registered with cell. Fixing connection...");
                cell.SetItem(rocket);
            }
        }
    }

    // called when a match is processed
    public void OnMatchProcessed(int matchCount)
    {
        DebugLog($"OnMatchProcessed called with matchCount: {matchCount}");

        // decrement move counter
        moveCounter.UseMove();

        // process the match normally without animation checks
        StartCoroutine(ProcessAfterMatch());

        // check win/lose conditions
        CheckGameState();

        if (isGameOver) return;

    }

    // called when a match is processed
    public void OnMatchProcessedChainReaction(int chainReactionCount)
    {
        moveCounter.UseMove();

        // process the match normally without animation checks
        StartCoroutine(ProcessAfterChainReaction(chainReactionCount));

        // check win/lose conditions
        CheckGameState();

        if (isGameOver) return;
    }

    private IEnumerator ProcessAfterChainReaction(int chainReactionCount)
    {
        DebugLog("Processing after chain reaction - hasTriggeredAnotherRocket, waiting for " + (0.03f * chainReactionCount) + " seconds");
        yield return new WaitForSeconds(0.03f * chainReactionCount);

        DebugLog("Processing after chain reaction - calling ProcessAfterMatch");
        StartCoroutine(ProcessAfterMatch());
    }

    private IEnumerator ProcessAfterMatch()
    {
        DebugLog("Processing after match");

        // wait for obstacle destruction and rocket animations to complete
        while (isDelayingFalling || (animationManager != null && animationManager.HasActiveRocketAnimations()))
        {
            if (isDelayingFalling)
            {
                DebugLog($"isDelayingFalling: {isDelayingFalling}");
            }
            yield return null;
        }

        // process falling - this will trigger grid filling simultaneously 
        if (fallingController != null)
        {
            DebugLog("Processing falling");
            yield return fallingController.ProcessFalling();
        }
        else
        {
            DebugLogWarning("Warning: fallingController is null");
        }

        // wait until all animations are complete (falling, filling, and rocket)
        while ((fallingController != null && fallingController.HasActiveAnimations()) ||
                (gridFiller != null && gridFiller.HasActiveAnimations()) ||
                (animationManager != null && animationManager.HasActiveRocketAnimations()))
        {
            yield return null;
        }

        DebugLog("All animations complete");

        // check for empty cells before scanning for matches
        yield return CheckAndProcessEmptyCells();

        // rescan for matches
        if (matchFinder != null)
        {
            DebugLog("Scanning grid for matches");
            matchFinder.ScanGridForMatches();
        }
        else
        {
            DebugLogWarning("Warning: matchFinder is null");
        }
        gridManager.DecrementTapEnabled();
    }

    // check for empty cells and process falling if needed
    private IEnumerator CheckAndProcessEmptyCells()
    {
        if (gridManager == null)
        {
            DebugLogWarning("Warning: gridManager is null in CheckAndProcessEmptyCells");
            yield break;
        }

        // check if there are any empty cells
        bool hasEmptyCells = false;
        for (int x = 0; x < currentLevelData.grid_width; x++)
        {
            for (int y = 0; y < currentLevelData.grid_height; y++)
            {
                GridCell cell = gridManager.GetCell(x, y);
                if (cell != null && cell.GetItem() == null)
                {
                    hasEmptyCells = true;
                    break;
                }
            }
            if (hasEmptyCells) break;
        }

        // if there are empty cells, process falling
        if (hasEmptyCells)
        {
            DebugLogWarning("⚠️ Empty cells detected, processing falling");
            if (fallingController != null)
            {
                yield return fallingController.ProcessFalling();
            }
            else
            {
                DebugLogWarning("Warning: fallingController is null in CheckAndProcessEmptyCells");
            }
        }
    }

    private void CheckGameState()
    {
        // check if we ran out of moves
        if (!moveCounter.HasMovesLeft())
        {
            // game over - player lost
            if (obstacles.Count > 0)
            {
                GameOver(false);
            }
            else
            {
                // no obstacles left, so player won
                GameOver(true);
            }
            return;
        }

        // check if all obstacles are cleared (level complete condition)
        if (obstacles.Count == 0 && !isGameOver)
        {
            GameOver(true);
        }
    }

    private void GameOver(bool win)

    {

        gridManager.IncrementTapEnabled(100);

        isGameOver = true;

        DebugLog("Game Over - " + win);

        if (isPopupShown) return; // prevent showing popup multiple times

        if (win)
        {
            int currentLevel = GameManager.Instance.GetCurrentLevel();
            int nextLevel = currentLevel + 1;

            // check if this is the last level
            int totalLevels = levelParser.GetTotalLevelCount();
            bool isLastLevel = nextLevel > totalLevels;

            DebugLog("Level completed!");

            // Show win panel
            if (popupController != null)
            {
                DebugLog($"Showing win popup from LevelController. isLastLevel: {isLastLevel}, totalLevels: {totalLevels}, nextLevel: {nextLevel}");
                // show appropriate popup based on whether this is the last level

                GameManager.Instance.SetLevel(nextLevel);
                PopupController.ShowWinPopup(0.8f); // show normal win popup with next level button

                isPopupShown = true; // mark popup as shown
            }
            else
            {
                DebugLogError("Cannot show win popup - popupController is null");
                AdvanceToNextLevel(); // if no popup, advance directly
            }
        }
        else
        {
            DebugLog("Game Over - Out of moves!");

            // Show lose panel
            if (popupController != null)
            {
                DebugLog("Showing lose popup from LevelController");
                PopupController.ShowLosePopup(0.5f);
                isPopupShown = true; // mark popup as shown
            }
            else
            {
                DebugLogError("Cannot show lose popup - popupController is null");
            }
        }
    }

    // advance to the next level
    public void AdvanceToNextLevel()
    {
        int currentLevel = GameManager.Instance.GetCurrentLevel();
        int nextLevel = currentLevel + 1;

        // check if we've reached the end of available levels
        int totalLevels = levelParser.GetTotalLevelCount();
        if (nextLevel > totalLevels)
        {
            DebugLog("All levels completed!");
            // handle game completion (e.g., show final celebration, return to main menu)
            GameManager.Instance.ReturnToMainMenu(); // this should handle the "Finished" state
        }
        else
        {
            // save the next level
            GameManager.Instance.SetLevel(nextLevel);

            // return to main menu which will show the new level
            GameManager.Instance.StartLevel();
        }
    }




    public void ReturnToMainMenu()
    {
        // return to main menu
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
        else
        {
            Debug.LogError("GameManager.Instance is null!");
            // fallback if gamemanager is not found
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
        }
    }

    public void OnRetryButtonClicked()
    {
        // restart level
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartLevel();
        }
        else
        {
            Debug.LogError("GameManager.Instance is null!");
            // fallback if gamemanager is not found
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    // register an obstacle with the level
    public void RegisterObstacle(Obstacle obstacle)
    {
        if (!obstacles.Contains(obstacle))
        {
            obstacles.Add(obstacle);
            DebugLog($"Obstacle registered. Total obstacles: {obstacles.Count}");
        }
    }

    // unregister an obstacle (when destroyed)
    public void UnregisterObstacle(Obstacle obstacle)
    {
        if (obstacles.Contains(obstacle))
        {
            obstacles.Remove(obstacle);
            DebugLog($"Obstacle unregistered. Remaining obstacles: {obstacles.Count}");

            // Notify obstacle counter of the destruction
            if (obstacleCounter != null)
            {
                obstacleCounter.OnObstacleDestroyed(obstacle);
            }

            // check if all obstacles are cleared
            CheckVictoryCondition();
        }
    }

    // check if a match affects any obstacles
    public void CheckMatchEffectOnObstacles(List<Vector2Int> matchPositions)
    {
        // create a unique id for this match group
        int matchGroupId = System.DateTime.Now.GetHashCode();

        // create a copy of the list to iterate through
        List<Obstacle> obstaclesCopy = new List<Obstacle>(obstacles);
        bool anyObstacleDestroyed = false;

        foreach (Obstacle obstacle in obstaclesCopy)
        {
            if (obstacle == null) continue;

            if (obstacle is BoxObstacle boxObstacle)
            {
                // check each match position against this box
                foreach (Vector2Int matchPos in matchPositions)
                {
                    if (boxObstacle.CheckForAdjacentMatch(matchPos))
                    {
                        anyObstacleDestroyed = true;
                    }
                }
            }
            else if (obstacle is VaseObstacle vaseObstacle)
            {
                // check each match position against this vase
                // pass the match group id to ensure only one damage per match group
                foreach (Vector2Int matchPos in matchPositions)
                {
                    if (vaseObstacle.CheckForAdjacentMatch(matchPos, matchGroupId))
                    {
                        anyObstacleDestroyed = true;
                    }
                }
            }
            // stone obstacles don't take damage from matches, so no need to check them
        }

        // if any obstacle was destroyed, delay the falling process
        /*if (anyObstacleDestroyed)
        {
            StartCoroutine(DelayFallingAfterObstacleDestruction());
        }*/
    }

    private IEnumerator DelayFallingAfterObstacleDestruction()
    {
        // flag to block falling temporarily
        isDelayingFalling = true;

        // Get delay from AnimationManager or use a default value if not available
        float delay = 0.3f; // Default fallback value
        if (animationManager != null)
        {
            delay = animationManager.GetObstacleDestroyDuration();
            DebugLog($"Using obstacle destruction delay from AnimationManager: {delay}");
        }
        else
        {
            DebugLogWarning("AnimationManager not available, using default delay value");
        }

        // wait for the specified delay
        yield return new WaitForSeconds(delay / 2);

        // re-enable falling
        isDelayingFalling = false;
    }

    // check if all obstacles are cleared
    private void CheckVictoryCondition()
    {
        // only check if game is not already over and if there's no popup shown
        if (obstacles.Count == 0 && !isGameOver && !isPopupShown)
        {
            // all obstacles cleared - player won!
            GameOver(true);
        }
    }

    public int GetRemainingObstacleCount()
    {
        return obstacles.Count;
    }

    // get all obstacles in the level
    public List<Obstacle> GetObstacles()
    {
        return new List<Obstacle>(obstacles);
    }

    private void Update()
    {
        // return to main menu when Q key is pressed (works in any mode)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("Returning to main menu via Q key");
            ReturnToMainMenu();
            return;
        }

        // skip debug keys if not in debug mode
        if (!debugMode) return;

        // test: press p key to force show win popup
        if (Input.GetKeyDown(KeyCode.P) && !isPopupShown)
        {
            DebugLog("Force showing win popup via P key");

            // show popup and set flag
            PopupController.ShowWinPopup(0.1f);
            isPopupShown = true;
        }

        // restart level when R key is pressed
        if (Input.GetKeyDown(KeyCode.R))
        {
            DebugLog("Restarting level via R key");
            OnRetryButtonClicked();
        }

        // print grid contents when space key is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DebugLog("Printing grid contents");
            gridManager.PrintGridContents();
        }


    }
}