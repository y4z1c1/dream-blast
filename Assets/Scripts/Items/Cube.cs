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
    private bool hasReachedTarget;
    private bool isAnimating;

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
        // kill any running animations and reset state when destroyed
        ResetAnimationState();
    }

    private void OnDisable()
    {
        // ensure cube is properly reset if disabled during animation
        ResetAnimationState();
    }

    private void OnEnable()
    {
        // ensure cube is in proper state when enabled
        ResetAnimationState();
    }

    private void OnMouseDown()
    {
        if (!gridManager.TapEnabled)
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

        // reset falling state
        hasReachedTarget = false;

        // update sorting order based on position
        UpdateSortingOrder();
    }

    // check if the cube has reached its target position during falling
    public bool HasReachedTarget() => hasReachedTarget;

    // set whether the cube has reached its target position
    public void SetHasReachedTarget(bool reached) => hasReachedTarget = reached;

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
        if (isRocketIndicatorShown == show || isAnimating)
            return;

        // update state
        isRocketIndicatorShown = show;

        // use animation manager to handle animation
        AnimationManager animManager = AnimationManager.Instance;
        if (animManager != null)
        {
            isAnimating = true;
            animManager.AnimateCubeRocketIndicator(this, show, defaultSprite, rocketSprite, glowAnimDuration, glowIntensity);

            // for non-animated transitions, we need to reset the animation flag
            if (!show)
            {
                isAnimating = false;
            }
            else
            {
                // for animated transitions, set a timer to reset the flag
                StartCoroutine(ResetAnimatingFlag(glowAnimDuration));
            }
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

    // reset animation state and cleanup
    private void ResetAnimationState()
    {
        // reset animation flag
        isAnimating = false;

        // reset color to white
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }

    // helper to reset the animating flag after a delay
    private IEnumerator ResetAnimatingFlag(float delay)
    {
        yield return new WaitForSeconds(delay);
        isAnimating = false;
    }
}