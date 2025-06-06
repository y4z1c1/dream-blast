using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// rocket is a class that represents a rocket in the grid.
public class Rocket : GridItem
{
    // rocket direction
    public enum RocketDirection
    {
        Horizontal,
        Vertical
    }

    [SerializeField] protected RocketDirection direction;
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected Sprite horizontalSprite;
    [SerializeField] protected Sprite verticalSprite;

    // references
    protected LevelController levelController;
    protected MatchFinder matchFinder;
    protected FallingController fallingController;
    protected GridFiller gridFiller;
    protected AnimationManager animationManager;

    // animation settings
    [SerializeField] private bool useAnimations = true;

    // flag to track explosion state
    private bool isExploding = false;
    private int activeExplosions = 0;

    // track if this rocket has triggered another rocket
    private bool hasTriggeredAnotherRocket = false;

    // track if this rocket was triggered by another rocket
    private bool wasTriggeredByAnotherRocket = false;

    // track number of chain reactions
    private int chainReactionCount = 0;

    private bool calledOnMatchProcessed = false;

    protected virtual void Awake()
    {
        // get reference to sprite renderer if not assigned
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // apply the correct sprite based on initial direction
        ApplyDirectionSprite();

        // get reference to animation manager
        animationManager = AnimationManager.Instance;
    }

    // OnMouseDown handler for clicking the rocket
    private void OnMouseDown()
    {
        if (!isExploding && canInteract && gridManager.TapEnabled)
        {
            Debug.Log($"Rocket clicked at position {GetGridPosition()}");
            OnTap();
        }
        else
        {
            Debug.Log($"Rocket at position {GetGridPosition()} is exploding or cannot interact");
            Debug.Log($"isExploding: {isExploding}, canInteract: {canInteract}, gridManager.TapEnabled: {gridManager.TapEnabled}");
        }
    }

    // set the rocket direction
    public void SetDirection(RocketDirection newDirection)
    {
        direction = newDirection;
        ApplyDirectionSprite();
    }

    // get the current direction
    public RocketDirection GetDirection()
    {
        return direction;
    }

    // apply the correct sprite based on direction
    public void ApplyDirectionSprite()
    {
        if (spriteRenderer != null)
        {
            if (direction == RocketDirection.Horizontal && horizontalSprite != null)
            {
                spriteRenderer.sprite = horizontalSprite;
            }
            else if (direction == RocketDirection.Vertical && verticalSprite != null)
            {
                spriteRenderer.sprite = verticalSprite;
            }
        }
    }

    // initialize the rocket
    public virtual void Initialize(int x, int y, GridManager gridManagerRef, LevelController levelControllerRef = null)
    {
        // set grid position
        SetGridPosition(x, y);

        // set references
        SetGridManager(gridManagerRef);
        levelController = levelControllerRef;

        // if no level controller was provided, try to find one
        if (levelController == null)
            levelController = FindFirstObjectByType<LevelController>();

        // get references to other controllers
        matchFinder = FindFirstObjectByType<MatchFinder>();
        fallingController = FindFirstObjectByType<FallingController>();
        gridFiller = FindFirstObjectByType<GridFiller>();
    }

    // override the ontap method from griditem
    public override void OnTap()
    {
        if (isExploding)
        {
            Debug.Log("Rocket is exploding, skipping tap");
            return;
        }



        // first check for rocket combinations
        if (!CheckForRocketCombination())
        {
            // if no combinations, just explode normally
            Explode();
        }
    }

    // explode the rocket
    public virtual void Explode()
    {
        if (isExploding) return;
        isExploding = true;

        if (wasTriggeredByAnotherRocket)
        {
            StartCoroutine(WaitForExplosion(this, 0.1f));
        }

        Debug.Log($"Rocket exploding at {GetGridPosition()}");

        // get our grid position
        Vector2Int pos = GetGridPosition();

        // immediately hide the rocket sprite
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        // play initial explosion effect at rocket position
        if (animationManager != null)
            animationManager.AnimateRocketExplosion(transform.position);
        else
            Debug.LogWarning("AnimationManager not found, skipping explosion animation");

        // clear our own cell
        GridCell cell = gridManager.GetCell(pos.x, pos.y);
        if (cell != null)
            cell.ClearItem();

        // estimate total chain reaction count by counting all rockets in the grid
        if (!wasTriggeredByAnotherRocket)
        {
            chainReactionCount = CountTotalRocketsInGrid();
            Debug.Log($"[Rocket] Estimated chain reaction count: {chainReactionCount}");
        }

        // create paths for explosion
        List<Vector2Int> path1 = new List<Vector2Int>();
        List<Vector2Int> path2 = new List<Vector2Int>();

        // populate the paths based on direction - starting from the rocket position
        path1.Add(pos); // add rocket position first
        path2.Add(pos); // add rocket position first

        // calculate off-screen positions based on direction
        Vector2Int offScreenPos1, offScreenPos2;
        if (direction == RocketDirection.Horizontal)
        {
            // path 1: left direction
            for (int x = pos.x - 1; x >= 0; x--)
            {
                path1.Add(new Vector2Int(x, pos.y));
            }
            offScreenPos1 = new Vector2Int(-1, pos.y); // one unit left of grid

            // path 2: right direction
            for (int x = pos.x + 1; x < gridManager.GetWidth(); x++)
            {
                path2.Add(new Vector2Int(x, pos.y));
            }
            offScreenPos2 = new Vector2Int(gridManager.GetWidth(), pos.y); // one unit right of grid
        }
        else // vertical
        {
            // path 1: down direction
            for (int y = pos.y - 1; y >= 0; y--)
            {
                path1.Add(new Vector2Int(pos.x, y));
            }
            offScreenPos1 = new Vector2Int(pos.x, -1); // one unit below grid

            // path 2: up direction
            for (int y = pos.y + 1; y < gridManager.GetHeight(); y++)
            {
                path2.Add(new Vector2Int(pos.x, y));
            }
            offScreenPos2 = new Vector2Int(pos.x, gridManager.GetHeight()); // one unit above grid
        }

        // add off-screen positions to paths
        path1.Add(offScreenPos1);
        path2.Add(offScreenPos2);

        // process explosions with or without animations
        activeExplosions = 2; // track two explosion paths

        if (useAnimations)
        {
            // use animations
            StartCoroutine(AnimateExplosionPath(path1));
            StartCoroutine(AnimateExplosionPath(path2));
        }
        else
        {
            // process immediately without animations
            ProcessExplosionPath(path1);
            ProcessExplosionPath(path2);
        }
    }

    // animate an explosion path with projectile
    protected virtual IEnumerator AnimateExplosionPath(List<Vector2Int> path)
    {
        if (path.Count <= 1) // skip if only contains rocket position
        {
            ExplosionComplete();
            yield break;
        }

        // use the animation manager to create a projectile
        if (animationManager != null)
        {
            // calculate off-screen world position for the last point
            Vector2Int lastPos = path[path.Count - 1];
            Vector3 lastWorldPos;

            // if this is an off-screen position
            if (lastPos.x < 0 || lastPos.x >= gridManager.GetWidth() ||
                lastPos.y < 0 || lastPos.y >= gridManager.GetHeight())
            {
                // get the direction from the last two points
                Vector2Int direction = lastPos - path[path.Count - 2];

                // extend the last position in the same direction
                lastPos = lastPos + (direction * 10);
            }

            // convert to world position
            lastWorldPos = new Vector3(lastPos.x, lastPos.y, 0);

            // replace the last position with the extended position
            path[path.Count - 1] = lastPos;

            animationManager.AnimateRocketProjectile(
                this,
                path,
                (hitPosition) =>
                {
                    // don't process the rocket position itself or off-screen positions
                    if ((hitPosition.x != GetGridPosition().x || hitPosition.y != GetGridPosition().y) &&
                        hitPosition.x >= 0 && hitPosition.x < gridManager.GetWidth() &&
                        hitPosition.y >= 0 && hitPosition.y < gridManager.GetHeight())
                    {
                        ProcessExplosionAtPosition(hitPosition);
                    }
                }
            );
        }
        else
        {
            Debug.LogWarning("AnimationManager not found, processing path without animation");
            ProcessExplosionPath(path);
            yield break;
        }

        // add a small delay to wait for animation to complete
        yield return new WaitForSeconds(0.5f);

        // mark this path complete
        ExplosionComplete();
    }

    // process an explosion path immediately without animation
    protected virtual void ProcessExplosionPath(List<Vector2Int> path)
    {
        // skip the first point which is the rocket position
        for (int i = 1; i < path.Count; i++)
        {
            ProcessExplosionAtPosition(path[i]);
        }

        // mark this explosion path as complete
        ExplosionComplete();
    }

    // process explosion at a specific position
    protected virtual void ProcessExplosionAtPosition(Vector2Int position)
    {
        GridCell cell = gridManager.GetCell(position.x, position.y);
        if (cell == null) return;

        if (!cell.IsEmpty())
        {
            GridItem item = cell.GetItem();

            if (item is Cube)
            {
                // get the cube
                Cube cube = item as Cube;

                // destroy the cube with animation
                cell.ClearItem();

                if (useAnimations && animationManager != null)
                {
                    animationManager.AnimateCubeDestruction(cube);
                }
                else
                {
                    Destroy(cube.gameObject);
                }
            }
            else if (item is Rocket)
            {
                // debug rocket chain reaction
                Debug.Log($"Chain reaction: rocket at ({position.x}, {position.y})");

                // Flag that this rocket has triggered another rocket
                hasTriggeredAnotherRocket = true;

                // increment chain reaction count
                chainReactionCount++;

                // trigger chain reaction - get reference before clearing cell
                Rocket rocket = item as Rocket;

                // mark the triggered rocket
                rocket.SetTriggeredByAnotherRocket(true);

                // clear cell before rocket explodes to prevent recursion issues
                cell.ClearItem();

                // hide the sprite (set it invisible) to prevent visual artifacts during chain reaction
                SpriteRenderer rocketSprite = rocket.GetComponent<SpriteRenderer>();
                if (rocketSprite != null)
                    rocketSprite.enabled = false;

                // increment active explosion counter for the chain reaction
                activeExplosions++;

                rocket.Explode();


            }
            else if (item is Obstacle)
            {
                // damage the obstacle
                Obstacle obstacle = item as Obstacle;
                obstacle.TakeDamageFromRocket();
            }
        }
    }

    private IEnumerator WaitForExplosion(Rocket rocket, float delay)
    {
        yield return new WaitForSeconds(delay);
        rocket.Explode();
    }

    // mark an explosion path as complete and check if all explosions are done
    protected virtual void ExplosionComplete()
    {

        // process grid updates after explosion
        if (!wasTriggeredByAnotherRocket)
            StartCoroutine(ProcessGridUpdates());

        else
        {
            // destroy this rocket if it hasn't been destroyed yet
            if (gameObject != null)
                Destroy(gameObject);
        }

    }

    // process grid updates after explosion
    private IEnumerator ProcessGridUpdates()
    {

        // destroy this rocket if it hasn't been destroyed yet
        if (gameObject != null)
            Destroy(gameObject);

        gridManager.DecrementTapEnabled();
        if (calledOnMatchProcessed)
            yield break;

        calledOnMatchProcessed = true;

        Debug.Log("[Rocket] Processing grid updates");
        // mark as not exploding
        isExploding = false;

        // notify level controller only if this rocket wasn't triggered by another rocket
        if (levelController != null && !wasTriggeredByAnotherRocket && hasTriggeredAnotherRocket)
        {
            levelController.OnMatchProcessedChainReaction(chainReactionCount);
        }
        else if (levelController != null && !wasTriggeredByAnotherRocket && !hasTriggeredAnotherRocket)
        {
            levelController.OnMatchProcessed(1);
        }



        yield return null;
    }



    // called when this rocket is damaged by another rocket or match
    public virtual void TakeDamage()
    {
        // just trigger the explosion
        Explode();
    }

    // check for adjacent rockets to create combinations
    public virtual bool CheckForRocketCombination()
    {
        Vector2Int pos = GetGridPosition();
        List<Rocket> connectedRockets = new List<Rocket>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        // add starting position to queue
        queue.Enqueue(pos);
        visited.Add(pos);

        // directions to check: right, left, up, down
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        // bfs to find all connected rockets
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // check all four directions
            foreach (Vector2Int dir in directions)
            {
                Vector2Int checkPos = current + dir;

                // skip if already visited
                if (visited.Contains(checkPos))
                    continue;

                GridCell cell = gridManager.GetCell(checkPos.x, checkPos.y);

                if (cell != null && !cell.IsEmpty())
                {
                    GridItem item = cell.GetItem();
                    if (item is Rocket rocket)
                    {
                        // found a rocket, add to connected group
                        connectedRockets.Add(rocket);

                        // mark as visited
                        visited.Add(checkPos);

                        // add to queue to check its neighbors
                        queue.Enqueue(checkPos);
                    }
                }
            }
        }

        // if we found connected rockets, create a combination
        if (connectedRockets.Count > 0)
        {
            TriggerRocketCombination(connectedRockets);
            return true;
        }

        return false;
    }

    // trigger a rocket combination - create 12 distinct paths 
    protected virtual void TriggerRocketCombination(List<Rocket> adjacentRockets)
    {
        if (isExploding) return;
        isExploding = true;

        gridManager.IncrementTapEnabled();

        Debug.Log("Triggering rocket combination!");

        // get our position
        Vector2Int pos = GetGridPosition();

        // create a list of all rockets involved (including this one)
        List<Rocket> allRockets = new List<Rocket>(adjacentRockets);
        allRockets.Add(this);

        // use the special combine animation via animation manager
        if (animationManager != null)
        {
            animationManager.AnimateRocketCombineSpecial(
                allRockets,
                pos,
                (success) =>
                {
                    if (success)
                    {
                        // create directional paths for the combination explosion
                        List<List<Vector2Int>> explosionPaths = new List<List<Vector2Int>>();

                        // create 12 distinct paths

                        // 3 paths going up (from left, center, right)
                        for (int xOffset = -1; xOffset <= 1; xOffset++)
                        {
                            int startX = pos.x + xOffset;
                            if (startX < 0 || startX >= gridManager.GetWidth()) continue;

                            List<Vector2Int> upPath = new List<Vector2Int>();
                            upPath.Add(new Vector2Int(startX, pos.y));
                            for (int y = pos.y + 1; y < gridManager.GetHeight(); y++)
                            {
                                upPath.Add(new Vector2Int(startX, y));
                            }
                            // add off-screen position
                            if (upPath.Count > 1)
                            {
                                // direction is up
                                Vector2Int direction = new Vector2Int(0, 1);
                                upPath.Add(upPath[upPath.Count - 1] + (direction * 10));
                                explosionPaths.Add(upPath);
                            }
                        }

                        // 3 paths going down (from left, center, right)
                        for (int xOffset = -1; xOffset <= 1; xOffset++)
                        {
                            int startX = pos.x + xOffset;
                            if (startX < 0 || startX >= gridManager.GetWidth()) continue;

                            List<Vector2Int> downPath = new List<Vector2Int>();
                            downPath.Add(new Vector2Int(startX, pos.y));
                            for (int y = pos.y - 1; y >= 0; y--)
                            {
                                downPath.Add(new Vector2Int(startX, y));
                            }
                            // add off-screen position
                            if (downPath.Count > 1)
                            {
                                // direction is down
                                Vector2Int direction = new Vector2Int(0, -1);
                                downPath.Add(downPath[downPath.Count - 1] + (direction * 10));
                                explosionPaths.Add(downPath);
                            }
                        }

                        // 3 paths going left (from top, center, bottom)
                        for (int yOffset = -1; yOffset <= 1; yOffset++)
                        {
                            int startY = pos.y + yOffset;
                            if (startY < 0 || startY >= gridManager.GetHeight()) continue;

                            List<Vector2Int> leftPath = new List<Vector2Int>();
                            leftPath.Add(new Vector2Int(pos.x, startY));
                            for (int x = pos.x - 1; x >= 0; x--)
                            {
                                leftPath.Add(new Vector2Int(x, startY));
                            }
                            // add off-screen position
                            if (leftPath.Count > 1)
                            {
                                // direction is left
                                Vector2Int direction = new Vector2Int(-1, 0);
                                leftPath.Add(leftPath[leftPath.Count - 1] + (direction * 10));
                                explosionPaths.Add(leftPath);
                            }
                        }

                        // 3 paths going right (from top, center, bottom)
                        for (int yOffset = -1; yOffset <= 1; yOffset++)
                        {
                            int startY = pos.y + yOffset;
                            if (startY < 0 || startY >= gridManager.GetHeight()) continue;

                            List<Vector2Int> rightPath = new List<Vector2Int>();
                            rightPath.Add(new Vector2Int(pos.x, startY));
                            for (int x = pos.x + 1; x < gridManager.GetWidth(); x++)
                            {
                                rightPath.Add(new Vector2Int(x, startY));
                            }
                            // add off-screen position
                            if (rightPath.Count > 1)
                            {
                                // direction is right
                                Vector2Int direction = new Vector2Int(1, 0);
                                rightPath.Add(rightPath[rightPath.Count - 1] + (direction * 10));
                                explosionPaths.Add(rightPath);
                            }
                        }

                        // process with or without animations
                        if (useAnimations)
                        {
                            // use animations for combination
                            activeExplosions = 1; // track the combination explosion

                            animationManager.AnimateRocketCombination(
                                pos,
                                gridManager,
                                explosionPaths,
                                (hitPos) =>
                                {
                                    // don't process the rocket position itself or off-screen positions
                                    if ((hitPos.x != pos.x || hitPos.y != pos.y) &&
                                        hitPos.x >= 0 && hitPos.x < gridManager.GetWidth() &&
                                        hitPos.y >= 0 && hitPos.y < gridManager.GetHeight())
                                    {
                                        ProcessExplosionAtPosition(hitPos);
                                    }
                                },
                                () => { ExplosionCompleteForCombination(adjacentRockets); }
                            );
                        }
                        else
                        {
                            // process all paths immediately (no animations)
                            activeExplosions = explosionPaths.Count;

                            foreach (List<Vector2Int> path in explosionPaths)
                            {
                                ProcessExplosionPath(path);
                            }

                            // destroy adjacent rocket objects
                            foreach (Rocket rocket in adjacentRockets)
                            {
                                Destroy(rocket.gameObject);
                            }
                        }
                    }
                    else
                    {
                        // if animation failed, process immediately without animations
                        ExplosionCompleteForCombination(adjacentRockets);
                    }
                }
            );
        }
        else
        {
            Debug.LogWarning("AnimationManager not found, processing combination without animation");
            ExplosionCompleteForCombination(adjacentRockets);
        }
    }

    // mark combination explosion as complete
    protected virtual void ExplosionCompleteForCombination(List<Rocket> adjacentRockets)
    {
        Debug.Log("[Rocket] Combination explosion complete");

        // destroy adjacent rocket objects
        foreach (Rocket rocket in adjacentRockets)
        {
            if (rocket != null && rocket.gameObject != null)
                Destroy(rocket.gameObject);
        }

        // process updates since this is an edge rocket (no more rockets to trigger)
        StartCoroutine(ProcessGridUpdates());
    }

    // public accessor for explosion state
    public bool IsExploding()
    {
        return isExploding;
    }

    // set the triggered by another rocket flag
    public void SetTriggeredByAnotherRocket(bool value)
    {
        wasTriggeredByAnotherRocket = value;
    }

    // count total rockets in the grid
    private int CountTotalRocketsInGrid()
    {
        int count = 0;
        for (int x = 0; x < gridManager.GetWidth(); x++)
        {
            for (int y = 0; y < gridManager.GetHeight(); y++)
            {
                GridCell cell = gridManager.GetCell(x, y);
                if (cell != null && !cell.IsEmpty())
                {
                    GridItem item = cell.GetItem();
                    if (item is Rocket)
                    {
                        count++;
                    }
                }
            }
        }
        return count;
    }


    // print all attributes for debugging

    [ContextMenu("Print All Attributes")]
    public void PrintAllAttributes()
    {
        // get grid position
        Vector2Int pos = GetGridPosition();

        // print all attributes
        Debug.Log($"Rocket Attributes at position ({pos.x}, {pos.y}):\n" +
                  $"Can Interact: {canInteract}\n" +
                  $"Has Grid Manager: {gridManager != null}\n" +
                  $"Has Match Finder: {matchFinder != null}\n" +
                  $"Has Sprite Renderer: {spriteRenderer != null}");
    }
}