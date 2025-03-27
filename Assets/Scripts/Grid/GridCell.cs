using UnityEngine;

public class GridCell : MonoBehaviour
{
    // grid coordinates
    private int x;
    private int y;

    // reference to the grid manager
    private GridManager gridManager;

    // current item in this cell
    private GridItem currentItem;

    // initialize the cell
    public void Initialize(int gridX, int gridY, GridManager manager)
    {
        x = gridX;
        y = gridY;
        gridManager = manager;
    }

    // set an item in this cell
    public void SetItem(GridItem item)
    {
        // skip if same item
        if (currentItem == item)
            return;

        // clear existing item if present
        if (currentItem != null && currentItem != item)
        {
            // check if this cell was the one storing the reference
            Vector2Int itemPos = currentItem.GetGridPosition();
            if (itemPos.x == x && itemPos.y == y)
            {
                // detach from this cell
                currentItem = null;
            }
        }

        // update current item reference
        currentItem = item;

        if (item != null)
        {
            // set the grid manager reference
            if (gridManager != null)
            {
                item.SetGridManager(gridManager);
            }

            // update item's world position
            item.transform.position = transform.position;

            // update item's grid position
            Vector2Int oldPos = item.GetGridPosition();
            if (oldPos.x != x || oldPos.y != y)
            {
                // handle old cell reference
                if (gridManager != null)
                {
                    GridCell oldCell = gridManager.GetCell(oldPos.x, oldPos.y);
                    if (oldCell != null && oldCell != this && oldCell.GetItem() == item)
                    {
                        oldCell.ClearItem();
                    }
                }

                // now update the item's position
                item.SetGridPosition(x, y);
            }
        }
    }

    // clear the item from this cell
    public void ClearItem()
    {
        currentItem = null;
    }

    // check if cell is empty
    public bool IsEmpty()
    {
        return currentItem == null;
    }

    // get the current item
    public GridItem GetItem()
    {
        return currentItem;
    }

    // get cell's grid coordinates
    public Vector2Int GetCoordinates()
    {
        return new Vector2Int(x, y);
    }
}