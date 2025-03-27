using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MatchFinder : MonoBehaviour
{
    // reference to the grid manager
    [SerializeField] private GridManager gridManager;

    // reference to controllers
    [SerializeField] private FallingController fallingController;
    [SerializeField] private GridFiller gridFiller;
    [SerializeField] private LevelController levelController;
    [SerializeField] private AnimationManager animationManager;

    // minimum number of matching cubes required
    [SerializeField] private int minMatchCount = 2;
    [SerializeField] private int rocketMatchCount = 4; // minimum count needed for a rocket match

    // time to wait after setting rocket indicators before scanning again
    [SerializeField] private float indicatorAnimationDelay = 0.3f;

    // invalid move animation settings
    [Header("Invalid Move Animation")]
    [SerializeField] private bool enableInvalidMoveAnimation = true;
    [SerializeField] private float invalidMoveDuration = 0.1f;
    [SerializeField] private float invalidMoveStrength = 0.1f;

    [Header("Debug Settings")]
    [SerializeField] private bool debugMode = false;

    // store potential matches
    private List<MatchGroup> potentialMatches = new List<MatchGroup>();

    // flag to prevent multiple simultaneous operations
    private bool isProcessing = false;

    // Store the last tapped position for invalid move handling
    private Vector2Int lastTapPosition;

    // helper for debug logging
    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[MatchFinder] {message}");
        }
    }

    // helper for debug warnings
    private void DebugLogWarning(string message)
    {
        if (debugMode)
        {
            Debug.LogWarning($"[MatchFinder] {message}");
        }
    }

    private void Awake()
    {
        // try to find components if not already assigned in inspector
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        if (fallingController == null)
        {
            fallingController = FindFirstObjectByType<FallingController>();
        }

        if (gridFiller == null)
        {
            gridFiller = FindFirstObjectByType<GridFiller>();
        }

        if (levelController == null)
        {
            levelController = FindFirstObjectByType<LevelController>();
        }

        if (animationManager == null)
        {
            animationManager = FindFirstObjectByType<AnimationManager>();
        }
    }

    private void Start()
    {
        // Setup event listener for when falling creates empty spaces
        if (fallingController != null)
        {
            fallingController.OnEmptySpacesReady += OnEmptySpacesReady;
        }

        // Setup grid filler completion listener
        if (gridFiller != null)
        {
            gridFiller.OnFillingComplete += OnFillingComplete;
        }
    }

    // Event handler for when falling creates empty spaces
    private void OnEmptySpacesReady(Dictionary<int, int> emptySpacesPerColumn)
    {
        // Only handle this event if we're not already processing matches
        if (isProcessing)
        {
            Debug.Log("Cannot fill empty spaces - still processing matches");
            return;
        }

        // Fill the empty spaces immediately - don't wait for falling to complete
        if (gridFiller != null && emptySpacesPerColumn.Count > 0)
        {
            StartCoroutine(gridFiller.FillSpecificEmptyCells(emptySpacesPerColumn));
        }
        else
        {
            // No spaces to fill, just scan for matches when falling completes
            // This will happen through the fallingController's ProcessFalling completion
        }
    }

    // Event handler for when grid filling is complete
    private void OnFillingComplete()
    {
        // Check if we can scan for matches (both falling and filling must be done)
        CheckForScanReady();
    }

    // Check if we're ready to scan for matches
    private void CheckForScanReady()
    {
        // Only scan if all animations are complete (falling, filling, and rockets)
        if (fallingController != null && gridFiller != null && animationManager != null)
        {
            if (!fallingController.HasActiveAnimations() &&
                !gridFiller.HasActiveAnimations() &&
                !animationManager.HasActiveRocketAnimations() &&
                !isProcessing)
            {
                ScanGridForMatches();
            }
        }
        else
        {
            // If any controller isn't available, proceed with what we have
            ScanGridForMatches();
        }
    }

    // find matches starting from a specific cube
    public List<Cube> FindMatches(Cube startCube)
    {
        if (startCube == null || gridManager == null)
        {
            Debug.LogError("FindMatches called with null startCube or gridManager");
            return new List<Cube>();
        }

        // get starting position and color
        Vector2Int startPos = startCube.GetGridPosition();
        Cube.CubeColor targetColor = startCube.GetColor();

        // create lists for tracking
        List<Cube> matchedCubes = new List<Cube>();
        HashSet<Vector2Int> visitedPositions = new HashSet<Vector2Int>();

        // add the starting cube to matches
        matchedCubes.Add(startCube);

        // start breadth-first search
        Queue<Vector2Int> positionsToCheck = new Queue<Vector2Int>();
        positionsToCheck.Enqueue(startPos);
        visitedPositions.Add(startPos);

        while (positionsToCheck.Count > 0)
        {
            Vector2Int currentPos = positionsToCheck.Dequeue();

            // get cube at this position
            Cube cube = GetCubeAtPosition(currentPos);
            if (cube == null) continue;

            if (cube.GetColor() == targetColor)
            {
                // add to matched list if not already in the list
                if (!matchedCubes.Contains(cube))
                    matchedCubes.Add(cube);

                // check adjacent cells
                TryAddAdjacentPosition(currentPos.x + 1, currentPos.y, targetColor, positionsToCheck, visitedPositions);
                TryAddAdjacentPosition(currentPos.x - 1, currentPos.y, targetColor, positionsToCheck, visitedPositions);
                TryAddAdjacentPosition(currentPos.x, currentPos.y + 1, targetColor, positionsToCheck, visitedPositions);
                TryAddAdjacentPosition(currentPos.x, currentPos.y - 1, targetColor, positionsToCheck, visitedPositions);
            }
        }

        // only return the list if it contains enough matches
        if (matchedCubes.Count >= minMatchCount)
            return matchedCubes;

        // not enough matches
        return new List<Cube>();
    }

    // get cube at a specific position
    private Cube GetCubeAtPosition(Vector2Int position)
    {
        if (gridManager == null)
            return null;

        GridCell cell = gridManager.GetCell(position.x, position.y);
        if (cell == null || cell.IsEmpty())
            return null;

        GridItem item = cell.GetItem();
        if (item == null || !(item is Cube))
            return null;

        return (Cube)item;
    }

    // helper method to try adding a position to the check queue
    private void TryAddAdjacentPosition(int x, int y, Cube.CubeColor targetColor, Queue<Vector2Int> queue, HashSet<Vector2Int> visited)
    {
        Vector2Int pos = new Vector2Int(x, y);

        // skip if already visited
        if (visited.Contains(pos))
            return;

        // check if there's a matching cube at this position
        Cube cube = GetCubeAtPosition(pos);
        if (cube != null && cube.GetColor() == targetColor)
        {
            // add to queue and mark as visited
            queue.Enqueue(pos);
            visited.Add(pos);
        }
    }

    // scan the entire grid for matches and store them
    public void ScanGridForMatches()
    {
        // clear previous matches
        potentialMatches.Clear();

        // track positions we've already checked to avoid duplicates
        HashSet<Vector2Int> processedPositions = new HashSet<Vector2Int>();

        // check each position in the grid
        for (int x = 0; x < gridManager.GetWidth(); x++)
        {
            for (int y = 0; y < gridManager.GetHeight(); y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                // skip if already processed
                if (processedPositions.Contains(pos))
                    continue;

                // get the cube at this position
                Cube cube = GetCubeAtPosition(pos);
                if (cube == null)
                    continue;

                // find all matches starting from this cube
                List<Cube> matches = FindMatches(cube);

                // if we found a valid match
                if (matches.Count >= minMatchCount)
                {
                    // create a match group
                    MatchGroup matchGroup = new MatchGroup(matches, cube.GetColor());
                    potentialMatches.Add(matchGroup);

                    // mark all positions in this match as processed
                    foreach (Cube matchCube in matches)
                    {
                        processedPositions.Add(matchCube.GetGridPosition());
                    }

                    // Set rocket indicator ONLY if group has 4+ cubes
                    if (matches.Count >= rocketMatchCount)
                    {
                        UpdateRocketIndicatorsForGroup(matchGroup);
                    }
                    else
                    {
                        matchGroup.SetRocketIndicator(false);
                    }

                }
                else
                {
                    cube.ShowRocketIndicator(false);
                }
            }
        }

        Debug.Log($"Found {potentialMatches.Count} potential matches, including {potentialMatches.Count(m => m.IsRocketMatch)} rocket matches");
    }

    // Update rocket indicators for a match group based on its size
    private void UpdateRocketIndicatorsForGroup(MatchGroup matchGroup)
    {
        if (matchGroup == null)
            return;

        // We're only calling this method for groups with size >= rocketMatchCount
        // So we always want to show the indicator
        matchGroup.SetRocketIndicator(true);
    }

    // get the match at a position, if any
    public MatchGroup GetMatchAtPosition(Vector2Int position)
    {
        foreach (MatchGroup match in potentialMatches)
        {
            foreach (Cube cube in match.MatchedCubes)
            {
                if (cube.GetGridPosition() == position)
                {
                    return match;
                }
            }
        }
        return null;
    }

    // Process a match when player taps a cube
    public bool ProcessMatch(Vector2Int tapPosition)
    {
        // Cache tap position for invalid move handling
        lastTapPosition = tapPosition;

        // Prevent multiple simultaneous matches
        if (isProcessing)
        {
            DebugLog($"Ignoring tap at {tapPosition} - already processing a match");
            return false;
        }

        // Start processing - set flag early to prevent race conditions
        isProcessing = true;

        DebugLog($"Processing match at position {tapPosition}");

        // Get the match group at this position (null if no match)
        MatchGroup matchGroup = GetMatchAtPosition(tapPosition);

        // Handle invalid move if no match
        if (matchGroup == null)
        {
            DebugLog($"No match found at position {tapPosition}");
            OnInvalidMove();
            isProcessing = false;
            return false;
        }

        // Set the clicked position in the match group (for rocket creation)
        matchGroup.ClickedPosition = tapPosition;

        // Determine if we should create a rocket based on match length
        bool createRocket = matchGroup.MatchLength >= rocketMatchCount;

        if (createRocket)
        {
            DebugLog($"Valid match with {matchGroup.MatchLength} cubes - will create rocket");
        }
        else
        {
            DebugLog($"Valid match with {matchGroup.MatchLength} cubes");
        }

        // Start processing the match
        StartCoroutine(ProcessMatchSequence(matchGroup, createRocket));

        return true;
    }

    private IEnumerator ProcessMatchSequence(MatchGroup matchGroup, bool createRocket = false)
    {
        DebugLog($"Starting match sequence with {matchGroup.MatchLength} cubes (createRocket={createRocket})");


        // Process the destruction with animation
        List<Vector2Int> affectedPositions = matchGroup.ProcessDestruction(createRocket);

        if (createRocket)
        {
            yield return new WaitForSeconds(0.15f);
        }


        DebugLog($"Match destruction processed, affecting {affectedPositions.Count} positions");

        // Check effects on obstacles without waiting for animation to complete
        if (levelController != null)
        {
            levelController.CheckMatchEffectOnObstacles(affectedPositions);
        }
        else
        {
            DebugLogWarning("LevelController is null, cannot check obstacle effects");
        }

        // Wait for destruction animation to complete
        yield return StartCoroutine(WaitForDestructionAnimation(matchGroup));

        DebugLog("Destruction animation completed");

        // Notify level controller about match completion
        if (levelController != null)
        {
            levelController.OnMatchProcessed(matchGroup.MatchLength);
        }
        else
        {
            DebugLogWarning("LevelController is null, cannot process match completion");
        }

        // Remove this match from our list
        potentialMatches.Remove(matchGroup);

        DebugLog("Match processing completed");

        // Now processing is complete, allow new matches
        isProcessing = false;
    }

    // Helper to wait for destruction animation to finish
    private IEnumerator WaitForDestructionAnimation(MatchGroup matchGroup)
    {
        float waitTime = 0.3f; // Default animation wait time

        // Use animation manager's duration if available
        if (animationManager != null)
        {
            waitTime = animationManager.GetCubeDestroyDuration();
            DebugLog($"Using animation manager's cube destroy duration: {waitTime}s");
        }

        DebugLog($"Waiting {waitTime}s for destruction animation to complete");
        yield return new WaitForSeconds(waitTime / 2);
    }

    // reset all rocket indicators
    private void ResetAllRocketIndicators()
    {
        // check each cube in the grid
        for (int x = 0; x < gridManager.GetWidth(); x++)
        {
            for (int y = 0; y < gridManager.GetHeight(); y++)
            {
                Cube cube = GetCubeAtPosition(new Vector2Int(x, y));
                if (cube != null)
                {
                    // check if cube is part of a 4+ match group
                    bool isInLargeMatch = potentialMatches.Any(match =>
                        match.MatchLength >= 4 && match.MatchedCubes.Contains(cube));

                    // only reset indicator if not in a 4+ match
                    if (!isInLargeMatch)
                    {
                        cube.ShowRocketIndicator(false);
                    }
                }
            }
        }

        // give time for animations to complete
        StartCoroutine(DelayedScanAfterIndicatorReset());
    }

    // Allow time for indicator animations to complete before further operations
    private IEnumerator DelayedScanAfterIndicatorReset()
    {
        // Wait for indicator animations to finish
        yield return new WaitForSeconds(indicatorAnimationDelay);

        // Any additional post-reset operations could go here
    }

    // set the grid manager reference
    public void SetGridManager(GridManager manager)
    {
        gridManager = manager;
    }

    // handle invalid moves
    private void OnInvalidMove()
    {
        DebugLog($"Invalid move detected at position {lastTapPosition}");

        // Skip if invalid move animations are disabled
        if (!enableInvalidMoveAnimation)
        {
            DebugLog("Invalid move animation skipped (disabled in settings)");
            return;
        }

        // Skip if no grid manager
        if (gridManager == null)
        {
            DebugLogWarning("Cannot animate invalid move: GridManager is null");
            return;
        }

        // Get the cube from last tap position
        Cube cube = GetCubeAtPosition(lastTapPosition);
        if (cube == null)
        {
            DebugLogWarning($"Cannot animate invalid move: No cube found at position {lastTapPosition}");
            return;
        }

        // Check if cube is on cooldown
        if (!cube.CanPlayInvalidMoveAnimation())
        {
            DebugLog($"Skipping invalid move animation: cube is on cooldown");
            return;
        }

        // Skip if no animation manager
        if (animationManager == null)
        {
            DebugLogWarning("Cannot animate invalid move: AnimationManager is null");
            return;
        }

        DebugLog($"Playing invalid move animation for cube at {lastTapPosition} (duration={invalidMoveDuration}, strength={invalidMoveStrength})");

        // Mark animation as played on the cube
        cube.MarkInvalidMovePlayed();

        // Use configurable animation parameters
        animationManager.AnimateInvalidMove(cube, invalidMoveDuration, invalidMoveStrength);
    }
}
