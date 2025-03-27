using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GridFiller : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject[] cubePrefabs;
    [SerializeField] private MatchFinder matchFinder;

    [Header("Settings")]
    [SerializeField] private float spawnHeightOffset = 1.5f;
    [SerializeField] private float rowSpacing = 1.5f;

    private bool isFillingInProgress = false;
    private int activeAnimationCount = 0;

    // reference to animation manager
    private AnimationManager animationManager;

    // event to notify when filling is complete
    public delegate void FillingCompleteHandler();
    public event FillingCompleteHandler OnFillingComplete;

    private void Awake()
    {
        if (matchFinder == null)
        {
            matchFinder = FindFirstObjectByType<MatchFinder>();
        }
        animationManager = AnimationManager.Instance;
    }

    // check if there are active animations running
    public bool HasActiveAnimations()
    {
        return activeAnimationCount > 0;
    }

    // Calculate spawn height based on grid's top row position
    private float CalculateMinSpawnHeight()
    {
        // Get the top row's y-position in world space
        float topRowY = 0;

        if (gridManager != null && gridManager.GetHeight() > 0)
        {
            // Get a cell from the top row
            GridCell topCell = gridManager.GetCell(0, gridManager.GetHeight() - 1);
            if (topCell != null)
            {
                topRowY = topCell.transform.position.y;
            }
        }

        // Add offset to position cubes above the grid
        return topRowY + spawnHeightOffset;
    }

    // Fill all empty cells in the grid, spawning one row at a time starting from the bottom
    public IEnumerator FillEmptyCells()
    {
        if (isFillingInProgress)
            yield break;

        isFillingInProgress = true;

        // Find all empty cells that aren't blocked by obstacles
        Dictionary<int, List<GridCell>> emptyCellsByColumn = FindAllEmptyCells();

        // Find maximum number of cells to fill in any column
        int maxEmptyCells = 0;
        foreach (var kvp in emptyCellsByColumn)
        {
            maxEmptyCells = Mathf.Max(maxEmptyCells, kvp.Value.Count);
        }

        // Calculate min spawn height once
        float minSpawnHeight = CalculateMinSpawnHeight();

        // Organize cells by their vertical position in their respective columns
        // This is to ensure we spawn cells row by row starting from the bottom
        Dictionary<int, Dictionary<int, GridCell>> cellsByRowFromBottom = new Dictionary<int, Dictionary<int, GridCell>>();

        foreach (var kvp in emptyCellsByColumn)
        {
            int columnX = kvp.Key;
            List<GridCell> columnCells = kvp.Value;

            // For each cell in this column, determine its vertical position from the bottom
            for (int i = 0; i < columnCells.Count; i++)
            {
                GridCell cell = columnCells[i];

                // Determine the row index from bottom (0 is the bottommost empty cell in this column)
                int rowFromBottom = columnCells.Count - 1 - i;

                // Add to the appropriate row dictionary
                if (!cellsByRowFromBottom.ContainsKey(rowFromBottom))
                {
                    cellsByRowFromBottom[rowFromBottom] = new Dictionary<int, GridCell>();
                }

                cellsByRowFromBottom[rowFromBottom][columnX] = cell;
            }
        }

        // Now spawn row by row, starting from the bottom (row 0)
        for (int rowFromBottom = 0; rowFromBottom < maxEmptyCells; rowFromBottom++)
        {
            if (!cellsByRowFromBottom.ContainsKey(rowFromBottom))
                continue;

            // Calculate spawn height for this row
            float spawnHeight = minSpawnHeight + (rowFromBottom * rowSpacing);

            // Spawn all cubes in this row at the same time
            foreach (var cellKvp in cellsByRowFromBottom[rowFromBottom])
            {
                int columnX = cellKvp.Key;
                GridCell cell = cellKvp.Value;
                Vector2Int cellPos = cell.GetCoordinates();

                // Spawn cube at the calculated height for this row
                SpawnCubeForCell(cell, cellPos.x, cellPos.y, spawnHeight);
            }

            // Wait a bit before spawning the next row
            yield return new WaitForSeconds(0.15f);
        }

        isFillingInProgress = false;

        // Inform MatchFinder that grid has been filled
        if (matchFinder != null)
        {
            matchFinder.ScanGridForMatches();
        }
    }

    // Fill specific empty cells, spawning one row at a time starting from the bottom
    public IEnumerator FillSpecificEmptyCells(Dictionary<int, int> emptySpacesPerColumn)
    {
        if (isFillingInProgress)
            yield break;

        isFillingInProgress = true;

        // Find cells blocked by obstacles
        HashSet<Vector2Int> blockedCells = FindCellsBlockedByObstacles();

        // Find maximum number of cells to fill in any column
        int maxEmptyCells = 0;
        foreach (var kvp in emptySpacesPerColumn)
        {
            maxEmptyCells = Mathf.Max(maxEmptyCells, kvp.Value);
        }

        // Calculate min spawn height once
        float minSpawnHeight = CalculateMinSpawnHeight();

        // First, collect all cells we need to fill
        Dictionary<int, Dictionary<int, List<GridCell>>> cellsByColumn = new Dictionary<int, Dictionary<int, List<GridCell>>>();

        foreach (var kvp in emptySpacesPerColumn)
        {
            int columnX = kvp.Key;
            int emptyCount = kvp.Value;

            for (int i = 0; i < emptyCount; i++)
            {
                int y = gridManager.GetHeight() - 1 - i;

                if (y >= 0)
                {
                    GridCell cell = gridManager.GetCell(columnX, y);
                    Vector2Int cellPos = new Vector2Int(columnX, y);

                    if (cell != null && cell.IsEmpty() && !blockedCells.Contains(cellPos))
                    {
                        if (!cellsByColumn.ContainsKey(columnX))
                        {
                            cellsByColumn[columnX] = new Dictionary<int, List<GridCell>>();
                        }

                        // Store this cell in a list for its column, where the key is the depth from top
                        if (!cellsByColumn[columnX].ContainsKey(i))
                        {
                            cellsByColumn[columnX][i] = new List<GridCell>();
                        }

                        cellsByColumn[columnX][i].Add(cell);
                    }
                }
            }
        }

        // Reorganize cells by row from bottom
        Dictionary<int, Dictionary<int, GridCell>> cellsByRowFromBottom = new Dictionary<int, Dictionary<int, GridCell>>();

        foreach (var columnKvp in cellsByColumn)
        {
            int columnX = columnKvp.Key;
            var depthMap = columnKvp.Value;

            // Count total cells in this column
            int totalCellsInColumn = 0;
            foreach (var depthKvp in depthMap)
            {
                totalCellsInColumn += depthKvp.Value.Count;
            }

            // Now map each cell to its position from bottom
            int currentPositionFromBottom = 0;

            // Start from the bottom of the grid (highest depth value)
            List<int> depthKeys = new List<int>(depthMap.Keys);
            depthKeys.Sort((a, b) => b.CompareTo(a)); // Sort descending

            foreach (int depth in depthKeys)
            {
                foreach (GridCell cell in depthMap[depth])
                {
                    // Add this cell to the appropriate row from bottom
                    if (!cellsByRowFromBottom.ContainsKey(currentPositionFromBottom))
                    {
                        cellsByRowFromBottom[currentPositionFromBottom] = new Dictionary<int, GridCell>();
                    }

                    cellsByRowFromBottom[currentPositionFromBottom][columnX] = cell;
                    currentPositionFromBottom++;
                }
            }
        }

        // Now spawn row by row, starting from the bottom (row 0)
        for (int rowFromBottom = 0; rowFromBottom < maxEmptyCells; rowFromBottom++)
        {
            if (!cellsByRowFromBottom.ContainsKey(rowFromBottom))
                continue;

            // Calculate spawn height for this row
            float spawnHeight = minSpawnHeight + (rowFromBottom * rowSpacing);

            // Spawn all cubes in this row at the same time
            foreach (var cellKvp in cellsByRowFromBottom[rowFromBottom])
            {
                int columnX = cellKvp.Key;
                GridCell cell = cellKvp.Value;
                Vector2Int cellPos = cell.GetCoordinates();

                // Spawn cube at the calculated height for this row
                SpawnCubeForCell(cell, cellPos.x, cellPos.y, spawnHeight);
            }

            // Wait a bit before spawning the next row
            yield return new WaitForSeconds(0.05f);
        }

        isFillingInProgress = false;

        // Inform MatchFinder that grid has been filled
        if (matchFinder != null)
        {
            matchFinder.ScanGridForMatches();
        }
    }

    // Find all empty cells in the grid, grouped by column
    private Dictionary<int, List<GridCell>> FindAllEmptyCells()
    {
        Dictionary<int, List<GridCell>> emptyCellsByColumn = new Dictionary<int, List<GridCell>>();

        // Get cells blocked by obstacles
        HashSet<Vector2Int> blockedCells = FindCellsBlockedByObstacles();

        // Collect valid empty cells (non-blocked)
        for (int x = 0; x < gridManager.GetWidth(); x++)
        {
            int emptyInColumn = 0;

            for (int y = gridManager.GetHeight() - 1; y >= 0; y--)
            {
                GridCell cell = gridManager.GetCell(x, y);
                Vector2Int cellPos = new Vector2Int(x, y);

                if (cell != null && cell.IsEmpty())
                {
                    bool isBlocked = blockedCells.Contains(cellPos);

                    if (!isBlocked)
                    {
                        // Add to column list
                        if (!emptyCellsByColumn.ContainsKey(x))
                        {
                            emptyCellsByColumn[x] = new List<GridCell>();
                        }

                        emptyCellsByColumn[x].Add(cell);
                        emptyInColumn++;
                    }
                }
            }

            if (emptyInColumn > 0)
            {
                Debug.Log($"Column {x} has {emptyInColumn} cells to fill with new cubes");
            }
        }

        return emptyCellsByColumn;
    }

    // Spawn a cube for a cell 
    private Cube SpawnCubeForCell(GridCell cell, int x, int y, float spawnHeight)
    {
        if (cubePrefabs.Length == 0)
            return null;

        // Choose a random cube prefab
        int randomIndex = Random.Range(0, cubePrefabs.Length);
        GameObject cubePrefab = cubePrefabs[randomIndex];

        // Get the exact cell position
        Vector3 cellPosition = cell.transform.position;

        // Set a start position using the passed spawn height
        // Use the cell's x position but the consistent spawn height
        Vector3 startPosition = new Vector3(cellPosition.x, spawnHeight, cellPosition.z);

        // Instantiate the cube
        GameObject cubeObject = Instantiate(cubePrefab, startPosition, Quaternion.identity);
        Cube cube = cubeObject.GetComponent<Cube>();

        if (cube != null)
        {
            // Get cube color from prefab or set a random one
            Cube.CubeColor cubeColor = GetCubeColorFromPrefab(cubePrefab, randomIndex);

            // Set up the cube with position and color
            cube.Initialize(x, y, cubeColor, gridManager, matchFinder);

            // Set cube in the cell
            cell.SetItem(cube);

            // Animate the cube falling to its target position using animation manager
            animateSpawnedCube(cube, startPosition, cellPosition);

            return cube;
        }

        return null;
    }

    // Animate cube falling to target position through animation manager
    private void animateSpawnedCube(Cube cube, Vector3 startPosition, Vector3 targetPosition)
    {
        Debug.Log("Animating cube spawn");

        // Increment active animation count
        activeAnimationCount++;

        if (animationManager != null)
        {
            // Use animation manager to handle the animation
            animationManager.AnimateCubeSpawn(
                cube,
                startPosition,
                targetPosition,
                () =>
                {
                    // Mark cube as ready when animation completes
                    cube.SetHasReachedTarget(true);
                    activeAnimationCount--; // Decrement active animation count

                    // Check if all animations are complete
                    if (activeAnimationCount <= 0 && !isFillingInProgress)
                    {
                        // Small delay to ensure synchronization with other systems
                        DOTween.Sequence().AppendInterval(0.05f).OnComplete(() =>
                        {
                            OnFillingComplete?.Invoke();
                        });
                    }
                }
            );
        }
        else
        {
            // Fallback if animation manager is not available
            cube.transform.position = targetPosition;
            cube.SetHasReachedTarget(true);
            activeAnimationCount--;

            // Check if all animations are complete
            if (activeAnimationCount <= 0 && !isFillingInProgress)
            {
                OnFillingComplete?.Invoke();
            }
        }
    }

    // Helper to get cube color from prefab
    private Cube.CubeColor GetCubeColorFromPrefab(GameObject prefab, int prefabIndex)
    {
        // Try to get color from prefab
        Cube prefabCube = prefab.GetComponent<Cube>();
        if (prefabCube != null)
        {
            return prefabCube.GetColor();
        }

        // Fallback to index-based color
        switch (prefabIndex % 4)
        {
            case 0: return Cube.CubeColor.Red;
            case 1: return Cube.CubeColor.Green;
            case 2: return Cube.CubeColor.Blue;
            case 3: return Cube.CubeColor.Yellow;
            default: return Cube.CubeColor.Red;
        }
    }

    // Helper method to find cells that are blocked by obstacles
    private HashSet<Vector2Int> FindCellsBlockedByObstacles()
    {
        // All cells start as potentially fillable
        HashSet<Vector2Int> blockedCells = new HashSet<Vector2Int>();

        // Process each column top to bottom
        for (int x = 0; x < gridManager.GetWidth(); x++)
        {
            bool blockingObstacleFound = false;

            // Scan from top to bottom
            for (int y = gridManager.GetHeight() - 1; y >= 0; y--)
            {
                GridCell cell = gridManager.GetCell(x, y);
                if (cell == null) continue;

                if (!cell.IsEmpty())
                {
                    GridItem item = cell.GetItem();

                    // If we encounter a non-moving obstacle, mark all cells below as blocked
                    if (item is Obstacle && !(item is VaseObstacle))
                    {
                        blockingObstacleFound = true;
                    }
                }

                // If we've found a blocking obstacle and this cell is empty, mark it as blocked
                if (blockingObstacleFound && cell.IsEmpty())
                {
                    blockedCells.Add(new Vector2Int(x, y));
                }
            }
        }

        return blockedCells;
    }
}