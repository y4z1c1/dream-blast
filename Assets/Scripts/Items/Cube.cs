using UnityEngine;
using System.Collections;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
public class Cube : GridItem
{
    // enum for cube colors
    public enum CubeColor
    {
        Red,
        Green,
        Blue,
        Yellow
    }

    [Header("Cube Properties")]
    [SerializeField] private CubeColor color;

    [Header("Sprites")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite rocketSprite;

    [Header("Animation Settings")]
    [SerializeField] private float glowAnimDuration = 0.9f;
    [SerializeField] private float glowIntensity = 1.2f;

    // cached components
    private SpriteRenderer spriteRenderer;

    // cached references
    private MatchFinder matchFinder;
    private new GridManager gridManager;

    // state tracking
    private bool isRocketIndicatorShown;

    // invalid move animation cooldown
    private float lastInvalidMoveTime;
    [SerializeField] private float invalidMoveCooldown = 2f;

    private void Awake()
    {
        // cache the sprite renderer component
        spriteRenderer = GetComponent<SpriteRenderer>();

        // set default sprite
        if (defaultSprite != null)
        {
            spriteRenderer.sprite = defaultSprite;
        }
    }

    private void OnDestroy()
    {

    }

    private void OnDisable()
    {

    }

    private void OnEnable()
    {

    }

    private void OnMouseDown()
    {
        Debug.Log("Cube at " + GetGridPosition() + " OnMouseDown");
        if (!canInteract || !gridManager.TapEnabled)
        {
            Debug.Log("Cube at " + GetGridPosition() + " tap disabled - not processing tap");
            return;
        }
        Debug.Log("Cube at " + GetGridPosition() + " Processing tap");
        OnTap();
    }

    // initialize a cube with all necessary properties
    public void Initialize(int x, int y, CubeColor cubeColor, GridManager gm, MatchFinder mf)
    {
        // set grid position
        SetGridPosition(x, y);

        // set color
        color = cubeColor;

        // set references
        SetGridManager(gm);
        SetMatchFinder(mf);

        // register with cell
        RegisterWithCell();


        // update sorting order based on position
        UpdateSortingOrder();
    }


    // override setgridposition to update sorting order whenever position changes
    public override void SetGridPosition(int x, int y)
    {
        base.SetGridPosition(x, y);
    }

    // set the color of this cube
    public void SetColor(CubeColor newColor) => color = newColor;

    // get the color of this cube
    public CubeColor GetColor() => color;

    // override the ontap method from griditem
    public override void OnTap()
    {
        if (matchFinder == null)
        {
            // try to find matchfinder if not set
            matchFinder = FindFirstObjectByType<MatchFinder>();
            if (matchFinder == null)
            {
                Debug.LogError("Cube: Could not find MatchFinder in scene!");
                return;
            }
        }

        // process the match at this position
        matchFinder.ProcessMatch(GetGridPosition());
    }

    // set the matchfinder reference
    public void SetMatchFinder(MatchFinder finder)
    {
        if (finder == null)
        {
            Debug.LogError("Cube: Attempting to set null MatchFinder reference!");
            return;
        }

        matchFinder = finder;
    }

    // get the matchfinder reference
    public MatchFinder GetMatchFinder()
    {
        return matchFinder;
    }

    // show or hide the rocket indicator with glow effect
    public void ShowRocketIndicator(bool show)
    {
        // skip if state isn't changing or animation already in progress
        if (isRocketIndicatorShown == show)
            return;

        // update state
        isRocketIndicatorShown = show;

        // use animation manager to handle animation
        AnimationManager animManager = AnimationManager.Instance;
        if (animManager != null)
        {
            animManager.AnimateCubeRocketIndicator(this, show, defaultSprite, rocketSprite, glowAnimDuration, glowIntensity);
        }
        else
        {
            // fallback if animation manager not available
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = show ? rocketSprite : defaultSprite;
            }
        }
    }

    // set the gridmanager reference
    public new void SetGridManager(GridManager manager)
    {
        if (manager == null)
        {
            Debug.LogError("Cube: Attempting to set null GridManager reference!");
            return;
        }

        gridManager = manager;

        // if position is already set, try to register with cell
        Vector2Int pos = GetGridPosition();
        if (pos.x >= 0 && pos.y >= 0)
        {
            RegisterWithCell();
        }
    }

    // get the gridmanager reference
    public new GridManager GetGridManager()
    {
        return gridManager;
    }

    // check if the cube can play invalid move animation (not on cooldown)
    public bool CanPlayInvalidMoveAnimation()
    {
        float timeSinceLastPlay = Time.time - lastInvalidMoveTime;
        return timeSinceLastPlay >= invalidMoveCooldown;
    }

    // mark invalid move animation as played (update timestamp)
    public void MarkInvalidMovePlayed()
    {
        lastInvalidMoveTime = Time.time;
    }

    // override getspriterenderer from base class
    protected override SpriteRenderer GetSpriteRenderer() => spriteRenderer;

    // register cube with corresponding cell
    private void RegisterWithCell()
    {
        if (gridManager == null)
        {
            Debug.LogError("Cube: Cannot register with cell - GridManager reference is null!");
            return;
        }

        Vector2Int pos = GetGridPosition();
        GridCell cell = gridManager.GetCell(pos.x, pos.y);

        if (cell != null)
        {
            cell.SetItem(this);
        }
        else
        {
            Debug.LogError($"Cube: Failed to register with cell at ({pos.x}, {pos.y}) - cell is null!");
        }
    }


    [ContextMenu("Print All Attributes")]
    public void PrintAllAttributes()
    {
        // get grid position
        Vector2Int pos = GetGridPosition();

        // print all attributes
        Debug.Log($"Cube Attributes at position ({pos.x}, {pos.y}):\n" +
                  $"Color: {color}\n" +
                  $"Can Interact: {canInteract}\n" +
                  $"Is Rocket Indicator Shown: {isRocketIndicatorShown}\n" +
                  $"Last Invalid Move Time: {lastInvalidMoveTime}\n" +
                  $"Invalid Move Cooldown: {invalidMoveCooldown}\n" +
                  $"Glow Animation Duration: {glowAnimDuration}\n" +
                  $"Glow Intensity: {glowIntensity}\n" +
                  $"Has Grid Manager: {gridManager != null}\n" +
                  $"Has Match Finder: {matchFinder != null}\n" +
                  $"Has Sprite Renderer: {spriteRenderer != null}");
    }
}