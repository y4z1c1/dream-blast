using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class GridManager : MonoBehaviour
{
    // grid dimensions
    [SerializeField] private int gridWidth = 6;
    [SerializeField] private int gridHeight = 8;

    // grid background
    [SerializeField] private SpriteRenderer gridBackgroundPrefab;
    private SpriteRenderer gridBackground;

    // masking
    [Header("Masking")]
    [SerializeField] private GameObject maskPrefab;
    [SerializeField] private bool useSpriteMask = true;
    private GridMask gridMask;

    // cell prefab and size
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private float cellSize = 1.0f;

    // grid data
    private GridCell[,] grid;

    // reference to grid's parent transform
    private Transform gridContainer;

    // separate container for grid cells
    private Transform gridCellsContainer;

    // game over flag
    private int tapEnabled = 0;

    // animation parameters
    private Vector3 initialGridPosition;
    private Vector3 targetGridPosition;
    private Vector3 initialBgPosition;  // background initial position
    private Vector3 targetBgPosition;   // background target position
    private bool isAnimatingAppearance = false;
    private float animationDuration = 0.8f;
    private float animationTimer = 0f;

    // animation manager reference
    private AnimationManager animationManager;

    // set and get game over
    // get tap enabled state
    public bool TapEnabled { get => tapEnabled == 0; }

    // increment tap enabled counter
    public void IncrementTapEnabled()
    {
        // increment tap enabled counter
        tapEnabled++;
        Debug.Log($"[GridManager] ⬆️ Incremented tapEnabled to {tapEnabled}");
    }

    // decrement tap enabled counter 
    public void DecrementTapEnabled()
    {
        // decrement tap enabled counter
        if (tapEnabled > 0)
        {
            tapEnabled--;
            Debug.Log($"[GridManager] ⬇️ Decremented tapEnabled to {tapEnabled}");
        }
    }

    // increment tapenabled by a specific amount
    public void IncrementTapEnabled(int amount)
    {
        tapEnabled += amount;
        Debug.Log($"[GridManager] ⬆️ Incremented tapEnabled by {amount} to {tapEnabled}");
    }

    // decrement tapenabled by a specific amount
    public void DecrementTapEnabled(int amount)
    {
        tapEnabled -= amount;
        Debug.Log($"[GridManager] ⬇️ Decremented tapEnabled by {amount} to {tapEnabled}");
    }

    // reset tap enabled
    public void ResetTapEnabled()
    {
        tapEnabled = 0;
        Debug.Log($"[GridManager] ⬇️ Reset tapEnabled to {tapEnabled}");
    }

    private void Awake()
    {
        // initialize the grid
        gridContainer = transform;

        // get animation manager reference
        animationManager = AnimationManager.Instance;
    }

    private void Start()
    {
        // create the grid when the game starts
        CreateGrid();
    }

    // creates the grid with the specified dimensions
    public void CreateGrid()
    {
        Debug.Log($"[GridManager] Starting grid creation with dimensions: {gridWidth}x{gridHeight}");

        // create background first
        CreateGridBackground();

        // create the mask
        CreateGridMask();

        // create a container for grid cells
        GameObject cellsContainer = new GameObject("GridCellsContainer");
        cellsContainer.transform.SetParent(transform);
        cellsContainer.transform.localPosition = Vector3.zero;
        gridCellsContainer = cellsContainer.transform;

        // hide cells container initially
        gridCellsContainer.gameObject.SetActive(false);

        // initialize the grid array
        grid = new GridCell[gridWidth, gridHeight];

        // calculate grid center for positioning
        Vector2 gridCenter = new Vector2(
            (gridWidth - 1) * cellSize * 0.5f,
            (gridHeight - 1) * cellSize * 0.5f
        );

        // create cells
        int createdCells = 0;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // calculate position with offset
                Vector3 position = new Vector3(
                    x * cellSize - gridCenter.x,
                    y * cellSize - gridCenter.y - 2f, // place the grid 2 units down
                    0f
                );

                // create cell gameobject
                GameObject cellObject = Instantiate(cellPrefab, position, Quaternion.identity, gridCellsContainer);
                cellObject.name = $"Cell_{x}_{y}";

                // add gridcell component
                GridCell cell = cellObject.GetComponent<GridCell>();
                if (cell == null)
                {
                    cell = cellObject.AddComponent<GridCell>();
                    Debug.Log($"[GridManager] Added missing GridCell component to cell at ({x}, {y})");
                }

                // initialize cell
                cell.Initialize(x, y, this);

                // store in grid array
                grid[x, y] = cell;
                createdCells++;
            }
        }

        Debug.Log($"[GridManager] Grid creation complete: Created {createdCells} cells in a {gridWidth}x{gridHeight} grid");

        // Verify grid was created properly
        if (IsGridCreated())
        {
            Debug.Log($"[GridManager] Grid validation successful: grid[0,0] exists and is properly initialized");
        }
        else
        {
            Debug.LogError($"[GridManager] Grid validation failed: grid[0,0] is null or improperly initialized");
        }
    }

    // creates the background for the grid
    private void CreateGridBackground()
    {
        // destroy any existing background first
        if (gridBackground != null)
        {
            Destroy(gridBackground.gameObject);
            gridBackground = null;
        }

        // check for any existing background object
        Transform existingBackground = transform.Find("GridBackground");
        if (existingBackground != null)
        {
            Destroy(existingBackground.gameObject);
        }

        // calculate the size needed for the background with padding
        float width = gridWidth * cellSize;
        float height = gridHeight * cellSize;

        // add padding around the grid 
        float padding = cellSize * 0.15f;

        // instantiate the background
        gridBackground = Instantiate(gridBackgroundPrefab, transform.position, Quaternion.identity, transform);

        // position it with the same offset as the grid (-2f on y axis)
        gridBackground.transform.localPosition = new Vector3(0, -2f, 0.1f);

        // hide background initially
        gridBackground.gameObject.SetActive(false);

        // if using 9-slice scaling, set the size directly
        if (gridBackgroundPrefab.drawMode == SpriteDrawMode.Sliced)
        {
            gridBackground.drawMode = SpriteDrawMode.Sliced;
            gridBackground.size = new Vector2(width + padding * 2, height + padding * 2);
        }
        else
        {
            // otherwise scale it based on the sprite's original size with padding
            Vector2 originalSize = gridBackgroundPrefab.sprite.bounds.size;
            Vector3 scale = new Vector3(
                (width + padding * 2) / originalSize.x,
                (height + padding * 2) / originalSize.y,
                1f
            );
            gridBackground.transform.localScale = scale;
        }

        // set sorting order to 0
        gridBackground.sortingOrder = 0;

        // name it appropriately
        gridBackground.name = "GridBackground";
    }

    // creates the mask for the grid
    private void CreateGridMask()
    {
        // only create if masking is enabled
        if (!useSpriteMask || maskPrefab == null)
            return;

        // clear any existing masks
        Transform existingMask = transform.Find("GridMask");
        if (existingMask != null)
            Destroy(existingMask.gameObject);

        // create the new mask
        GameObject maskObject = Instantiate(maskPrefab, transform.position, Quaternion.identity, transform);
        maskObject.name = "GridMask";

        // get and initialize the GridMask component
        gridMask = maskObject.GetComponent<GridMask>();
        if (gridMask == null)
            gridMask = maskObject.AddComponent<GridMask>();

        // initialize with reference to the grid background
        Transform bgTransform = transform.Find("GridBackground");
        if (bgTransform != null)
        {
            gridMask.Initialize(bgTransform);
        }
        else
        {
            Debug.LogError("[GridManager] Could not find GridBackground for masking");
        }
    }

    // get a cell at the specified grid coordinates
    public GridCell GetCell(int x, int y)
    {
        // check if coordinates are within bounds
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
        {
            return grid[x, y];
        }

        return null;
    }

    // get a list of adjacent cells (up, down, left, right)
    public List<GridCell> GetAdjacentCells(int x, int y)
    {
        List<GridCell> adjacentCells = new List<GridCell>();

        // check each direction
        GridCell cellUp = GetCell(x, y + 1);
        GridCell cellDown = GetCell(x, y - 1);
        GridCell cellLeft = GetCell(x - 1, y);
        GridCell cellRight = GetCell(x + 1, y);

        // add if not null
        if (cellUp != null) adjacentCells.Add(cellUp);
        if (cellDown != null) adjacentCells.Add(cellDown);
        if (cellLeft != null) adjacentCells.Add(cellLeft);
        if (cellRight != null) adjacentCells.Add(cellRight);

        return adjacentCells;
    }

    // convert world position to grid coordinates
    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        // calculate grid center for positioning
        Vector2 gridCenter = new Vector2(
            (gridWidth - 1) * cellSize * 0.5f,
            (gridHeight - 1) * cellSize * 0.5f
        );

        // get grid offset - always -2f on y axis in this implementation
        float gridYOffset = -2f;

        // adjust for the grid offset
        Vector3 adjustedPosition = new Vector3(
            worldPosition.x,
            worldPosition.y - gridYOffset, // subtract the negative offset (adding 2)
            worldPosition.z
        );

        // calculate grid coordinates
        int x = Mathf.RoundToInt((adjustedPosition.x + gridCenter.x) / cellSize);
        int y = Mathf.RoundToInt((adjustedPosition.y + gridCenter.y) / cellSize);

        // validate calculation with debug info for troubleshooting
        Vector3 calculatedWorldPos = GridToWorldPosition(x, y);

        x = Mathf.Clamp(x, 0, gridWidth - 1);
        y = Mathf.Clamp(y, 0, gridHeight - 1);

        return new Vector2Int(x, y);
    }

    // convert grid coordinates to world position
    public Vector3 GridToWorldPosition(int x, int y)
    {
        // calculate grid center for positioning
        Vector2 gridCenter = new Vector2(
            (gridWidth - 1) * cellSize * 0.5f,
            (gridHeight - 1) * cellSize * 0.5f
        );

        // get grid offset - always -2f on y axis in this implementation 
        float gridYOffset = -2f;

        // calculate world position with the grid y-offset
        return new Vector3(
            x * cellSize - gridCenter.x,
            y * cellSize - gridCenter.y + gridYOffset, // add the offset (-2f)
            0f
        );
    }

    // getters for grid dimensions
    public int GetWidth() => gridWidth;
    public int GetHeight() => gridHeight;

    // initialize grid with custom dimensions
    public void InitializeGrid(int width, int height, System.Action<bool> onInitializationComplete = null)
    {
        // update grid dimensions
        gridWidth = width;
        gridHeight = height;

        // Start the coroutine to handle grid initialization
        StartCoroutine(InitializeGridCoroutine(onInitializationComplete));
    }

    // Coroutine to handle grid initialization asynchronously
    private IEnumerator InitializeGridCoroutine(System.Action<bool> onInitializationComplete)
    {
        // Clear existing grid and items
        ClearGrid();

        // Wait a frame to ensure everything is properly cleared
        yield return null;

        // Create the new grid
        CreateGrid();

        // Wait a frame to ensure everything is properly created
        yield return null;

        // Validate to ensure grid was created properly
        bool success = IsGridCreated();

        if (!success)
        {
            Debug.LogError("Failed to create grid properly in InitializeGrid!");
        }
        else
        {
            Debug.Log($"Grid initialized successfully with dimensions: {gridWidth}x{gridHeight}");
        }

        // Invoke the callback if provided
        onInitializationComplete?.Invoke(success);
    }

    // clear the existing grid and its contents
    private void ClearGrid()
    {
        // if there's an existing grid
        if (grid != null)
        {
            // destroy all cell gameobjects
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (grid[x, y] != null)
                    {
                        // if there's an item in the cell, destroy it
                        GridItem item = grid[x, y].GetItem();
                        if (item != null)
                        {
                            Destroy(item.gameObject);
                        }

                        // destroy the cell gameobject
                        Destroy(grid[x, y].gameObject);
                    }
                }
            }
        }

        // If gridCellsContainer exists, destroy it to create a new one
        if (gridCellsContainer != null)
        {
            Destroy(gridCellsContainer.gameObject);
            gridCellsContainer = null;
        }

        // destroy the mask if it exists
        Transform existingMask = transform.Find("GridMask");
        if (existingMask != null)
        {
            Destroy(existingMask.gameObject);
            gridMask = null;
        }

        // Don't destroy the background here, it will be handled in CreateGridBackground

        // clear any other child objects except don't try to destroy the background here
        // as we'll handle it in CreateGridBackground
        foreach (Transform child in transform)
        {
            // skip the background as we'll handle it separately
            if (child.name != "GridBackground")
            {
                Destroy(child.gameObject);
            }
        }
    }

    // Checks if the grid has been properly created and initialized
    public bool IsGridCreated()
    {
        // Check if grid array exists
        if (grid == null)
        {
            Debug.LogError("[GridManager] Grid validation failed: grid array is null");
            return false;
        }

        // Check if grid dimensions match expected dimensions
        if (grid.GetLength(0) != gridWidth || grid.GetLength(1) != gridHeight)
        {
            Debug.LogError($"[GridManager] Grid validation failed: grid dimensions mismatch. Expected: {gridWidth}x{gridHeight}, Actual: {grid.GetLength(0)}x{grid.GetLength(1)}");
            return false;
        }

        // Check if at least the first cell exists
        if (grid[0, 0] == null)
        {
            Debug.LogError("[GridManager] Grid validation failed: grid[0,0] is null");
            return false;
        }

        // Check a sample of cells throughout the grid to ensure they're properly created
        // Check corners and center cells for larger grids
        bool allCellsValid = true;

        // Check corners
        if (grid[0, 0] == null || grid[0, gridHeight - 1] == null ||
            grid[gridWidth - 1, 0] == null || grid[gridWidth - 1, gridHeight - 1] == null)
        {
            Debug.LogError("[GridManager] Grid validation failed: one or more corner cells are null");
            allCellsValid = false;
        }

        // For larger grids, also check the center
        if (gridWidth > 2 && gridHeight > 2)
        {
            int centerX = gridWidth / 2;
            int centerY = gridHeight / 2;
            if (grid[centerX, centerY] == null)
            {
                Debug.LogError("[GridManager] Grid validation failed: center cell is null");
                allCellsValid = false;
            }
        }

        return allCellsValid;
    }

    // animate grid appearing from bottom
    public void AnimateGridAppearance()
    {
        // disable tap until animation completes
        IncrementTapEnabled();

        // make sure the mask exists and is active
        if (gridMask != null && useSpriteMask)
        {
            gridMask.gameObject.SetActive(true);
        }

        // Create a completion callback that resets the mask scale and decrements tapEnabled
        System.Action onComplete = () =>
        {
            Debug.Log("[GridManager] Animation complete callback triggered");

            // Reset mask scale when animation completes
            if (gridMask != null && useSpriteMask)
            {
                Debug.Log("[GridManager] Calling ResetToNormalScale on GridMask");
                gridMask.ResetToNormalScale();
            }
            else
            {
                Debug.Log("[GridManager] Cannot reset mask scale: mask is null or disabled");
            }

            // Decrement tapEnabled counter
            Debug.Log("[GridManager] Animation complete, decrementing tapEnabled");
            DecrementTapEnabled();
        };

        // call animation manager with our modified callback
        animationManager.AnimateGridAppearance(
            gridCellsContainer,
            gridBackground ? gridBackground.transform : null,
            0.5f,  // duration
            40f,   // offset
            onComplete  // our modified callback
        );
    }

    private void Update()
    {
        // Only handle animation manually if not using animation manager
        if (isAnimatingAppearance && animationManager == null)
        {
            animationTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(animationTimer / animationDuration);

            // use smooth step for easing
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // update cells position
            gridCellsContainer.position = Vector3.Lerp(initialGridPosition, targetGridPosition, smoothProgress);

            // update background position if it exists
            if (gridBackground != null)
            {
                gridBackground.transform.position = Vector3.Lerp(initialBgPosition, targetBgPosition, smoothProgress);
            }

            // check if animation is complete
            if (progress >= 1f)
            {
                isAnimatingAppearance = false;
                DecrementTapEnabled();
            }
        }
    }

    public void PrintGridContents()
    {
        StringBuilder gridDisplay = new StringBuilder();

        // Create top border
        string topBorder = "┌";
        for (int x = 0; x < gridWidth; x++)
        {
            topBorder += "────────────";
            if (x < gridWidth - 1)
                topBorder += "┬";
        }
        topBorder += "┐";
        gridDisplay.AppendLine(topBorder);

        // Display each row
        for (int y = gridHeight - 1; y >= 0; y--)
        {
            // Create content row
            StringBuilder rowContent = new StringBuilder("│");
            for (int x = 0; x < gridWidth; x++)
            {
                GridCell cell = grid[x, y];
                string cellContent;

                if (cell == null)
                {
                    cellContent = "   NULL   ";
                }
                else
                {
                    GridItem item = cell.GetItem();
                    cellContent = item != null ? $" {item.name.PadRight(8)} " : "   empty   ";
                }

                rowContent.Append(cellContent).Append("│");
            }
            gridDisplay.AppendLine(rowContent.ToString());

            // Add row separator (except after the last row)
            if (y < gridHeight - 1)
            {
                string rowSeparator = "├";
                for (int x = 0; x < gridWidth; x++)
                {
                    rowSeparator += "────────────";
                    if (x < gridWidth - 1)
                        rowSeparator += "┼";
                }
                rowSeparator += "┤";
                gridDisplay.AppendLine(rowSeparator);
            }
        }

        // Create bottom border
        string bottomBorder = "└";
        for (int x = 0; x < gridWidth; x++)
        {
            bottomBorder += "────────────";
            if (x < gridWidth - 1)
                bottomBorder += "┴";
        }
        bottomBorder += "┘";
        gridDisplay.AppendLine(bottomBorder);

        // Output the entire grid at once
        Debug.Log(gridDisplay.ToString());
    }
}
