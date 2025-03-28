using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FallingController : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    // event to notify when empty spaces are ready to be filled
    public delegate void EmptySpacesReadyHandler(Dictionary<int, int> emptySpacesPerColumn);
    public event EmptySpacesReadyHandler OnEmptySpacesReady;

    private bool isFalling = false;
    private int activeAnimationCount = 0;

    // reference to animation manager
    private AnimationManager animationManager;

    private void Awake()
    {
        // get animation manager reference
        animationManager = AnimationManager.Instance;
    }

    // main function to make items fall
    public IEnumerator ProcessFalling()
    {
        Debug.Log("[FallingController] Starting ProcessFalling");

        gridManager.IncrementTapEnabled();


        isFalling = true;
        Debug.Log("[FallingController] Set isFalling to true");

        yield return StartCoroutine(WaitForAnimationsToComplete());

        // calculate all movements before processing
        Debug.Log("[FallingController] Calculating falling positions");
        Dictionary<Vector2Int, Vector2Int> fallingPositions = CalculateFallingPositions();
        Debug.Log($"[FallingController] Found {fallingPositions.Count} positions to process");

        // reset animation counter
        activeAnimationCount = 0;
        Debug.Log("[FallingController] Reset animation counter");

        // process the falling with calculated positions
        Debug.Log("[FallingController] Processing falling positions");
        ProcessFallingPositions(fallingPositions);

        // calculate empty spaces at the top for filling immediately 
        // (don't wait for animations to complete)
        Debug.Log("[FallingController] Calculating empty spaces");
        Dictionary<int, int> emptySpaces = CalculateEmptySpaces();
        Debug.Log($"[FallingController] Found empty spaces in {emptySpaces.Count} columns");
        OnEmptySpacesReady?.Invoke(emptySpaces);

        gridManager.DecrementTapEnabled();


        // wait for all animations to complete
        Debug.Log("[FallingController] Waiting for animations to complete");
        yield return StartCoroutine(WaitForAnimationsToComplete());


        isFalling = false;
        Debug.Log("[FallingController] Falling complete");

        yield return new WaitForSeconds(0.1f);
    }



    // wait for all animations to complete
    private IEnumerator WaitForAnimationsToComplete()
    {
        yield return new WaitForSeconds(0.1f);
    }

    // process falling with pre-calculated positions (no animations)
    public void ProcessFallingPositions(Dictionary<Vector2Int, Vector2Int> fallingPositions)
    {
        // skip if no positions to process
        if (fallingPositions.Count == 0)
            return;

        // organize falling items by target row (y-coordinate)
        Dictionary<int, List<KeyValuePair<Vector2Int, Vector2Int>>> rowBatches = new Dictionary<int, List<KeyValuePair<Vector2Int, Vector2Int>>>();

        // group by target row
        foreach (var position in fallingPositions)
        {
            int targetRow = position.Value.y;

            if (!rowBatches.ContainsKey(targetRow))
                rowBatches[targetRow] = new List<KeyValuePair<Vector2Int, Vector2Int>>();

            rowBatches[targetRow].Add(position);
        }

        // get sorted rows (bottom to top)
        List<int> sortedRows = new List<int>(rowBatches.Keys);
        sortedRows.Sort();

        // process each row as a batch, starting from bottom
        foreach (int row in sortedRows)
        {
            List<GridItem> batchItems = new List<GridItem>();
            List<Vector3> startPositions = new List<Vector3>();
            List<Vector3> endPositions = new List<Vector3>();

            // prepare all items in this row
            foreach (var kvp in rowBatches[row])
            {
                Vector2Int currentPos = kvp.Key;
                Vector2Int targetPos = kvp.Value;

                GridCell sourceCell = gridManager.GetCell(currentPos.x, currentPos.y);
                GridCell targetCell = gridManager.GetCell(targetPos.x, targetPos.y);

                // check if source cell still has an item and target cell is empty
                if (sourceCell == null || targetCell == null || sourceCell.IsEmpty() || !targetCell.IsEmpty())
                    continue;

                // get the item to move
                GridItem itemToMove = sourceCell.GetItem();
                if (itemToMove == null)
                    continue;

                itemToMove.SetCanInteract(false);

                // update grid data immediately for correct simulation
                sourceCell.ClearItem();
                targetCell.SetItem(itemToMove);

                // update item position in grid data, but not visually yet
                itemToMove.SetGridPosition(targetPos.x, targetPos.y);

                // collect data for batch animation
                batchItems.Add(itemToMove);
                startPositions.Add(sourceCell.transform.position);
                endPositions.Add(targetCell.transform.position);
            }

            // animate the entire batch for this row
            if (batchItems.Count > 0)
            {
                activeAnimationCount++;

                if (animationManager != null)
                {
                    // animate through animation manager
                    animationManager.AnimateItemsBatchFalling(
                        batchItems,
                        startPositions,
                        endPositions,
                        () =>
                        {
                            // callback when batch animation completes
                            activeAnimationCount--;

                            // mark cubes as ready after animation completes
                            foreach (var item in batchItems)
                            {
                                if (item is Cube cube)
                                {
                                    cube.SetCanInteract(true);
                                }
                            }
                        }
                    );
                }
                else
                {
                    // fallback if animation manager not available
                    for (int i = 0; i < batchItems.Count; i++)
                    {
                        batchItems[i].transform.position = endPositions[i];

                        // mark cube as ready immediately
                        if (batchItems[i] is Cube cube)
                        {
                            cube.SetCanInteract(true);
                        }
                    }

                    activeAnimationCount--;
                }
            }
        }
    }

    // calculate positions before and after falling
    private Dictionary<Vector2Int, Vector2Int> CalculateFallingPositions()
    {
        Dictionary<Vector2Int, Vector2Int> fallingMap = new Dictionary<Vector2Int, Vector2Int>();

        // process each column independently
        for (int x = 0; x < gridManager.GetWidth(); x++)
        {
            // bottom-up approach - track the next available empty position
            int emptyPos = 0;

            // first pass: find the lowest empty position
            while (emptyPos < gridManager.GetHeight())
            {
                GridCell cell = gridManager.GetCell(x, emptyPos);
                if (cell != null && cell.IsEmpty())
                    break;
                emptyPos++;
            }

            // if no empty positions or reached top, skip this column
            if (emptyPos >= gridManager.GetHeight())
                continue;

            // second pass: process items that can fall
            for (int y = emptyPos + 1; y < gridManager.GetHeight(); y++)
            {
                GridCell cell = gridManager.GetCell(x, y);

                // skip empty cells
                if (cell == null || cell.IsEmpty())
                    continue;

                GridItem item = cell.GetItem();

                // skip non-falling obstacles
                if (item is Obstacle && !(item is VaseObstacle))
                {
                    // non-falling obstacles block falling items above them
                    // so we reset the empty position to the cell after this obstacle
                    emptyPos = y + 1;
                    continue;
                }

                // record the movement from original to new position
                Vector2Int originalPos = item.GetGridPosition();
                Vector2Int newPos = new Vector2Int(x, emptyPos);

                fallingMap[originalPos] = newPos;

                // update the next empty position
                emptyPos++;
            }
        }

        return fallingMap;
    }


    // calculate empty spaces at the top of each column
    private Dictionary<int, int> CalculateEmptySpaces()
    {
        Dictionary<int, int> emptySpaces = new Dictionary<int, int>();

        // For each column, count empty spaces from top down
        for (int x = 0; x < gridManager.GetWidth(); x++)
        {
            int emptyCount = 0;

            for (int y = gridManager.GetHeight() - 1; y >= 0; y--)
            {
                GridCell cell = gridManager.GetCell(x, y);
                if (cell != null && cell.IsEmpty())
                    emptyCount++;
                else
                    break; // Stop at first non-empty cell
            }

            if (emptyCount > 0)
                emptySpaces[x] = emptyCount;
        }

        return emptySpaces;
    }


    // public accessor for falling state
    public bool IsFalling() => isFalling;

    // public method to check if animations are still running
    public bool HasActiveAnimations()
    {
        return activeAnimationCount > 0;
    }
}