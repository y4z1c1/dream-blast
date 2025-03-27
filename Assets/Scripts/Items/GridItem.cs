using UnityEngine;
using System.Collections;

public abstract class GridItem : MonoBehaviour
{
    // grid position
    protected int gridX;
    protected int gridY;

    // reference to grid manager
    protected GridManager gridManager;

    // get sprite renderer in derived classes
    protected virtual SpriteRenderer GetSpriteRenderer()
    {
        return GetComponent<SpriteRenderer>();
    }

    // set the grid position
    public virtual void SetGridPosition(int x, int y)
    {
        gridX = x;
        gridY = y;

        // update sorting order when position changes
        UpdateSortingOrder();
    }

    // get the grid position
    public Vector2Int GetGridPosition()
    {
        return new Vector2Int(gridX, gridY);
    }

    // set reference to the grid manager
    public void SetGridManager(GridManager manager)
    {
        // check if manager is null
        if (manager == null)
        {
            Debug.LogError($"Attempt to set null GridManager on item at ({gridX}, {gridY})");
        }

        gridManager = manager;

        // update sorting order when grid manager changes
        UpdateSortingOrder();
    }

    // get reference to the grid manager
    public GridManager GetGridManager()
    {
        return gridManager;
    }

    // update the sprite renderer's sorting order based on row position
    protected virtual void UpdateSortingOrder()
    {
        SpriteRenderer spriteRenderer = GetSpriteRenderer();
        if (spriteRenderer == null) return;

        // get the current position
        Vector2Int pos = GetGridPosition();

        if (gridManager != null)
        {
            // simply use the Y value for sorting order
            // higher row = higher sorting order
            int sortingOrder = pos.y;

            // set the sorting order
            spriteRenderer.sortingOrder = sortingOrder;
        }
    }

    // abstract method that must be implemented by derived classes
    public abstract void OnTap();
}