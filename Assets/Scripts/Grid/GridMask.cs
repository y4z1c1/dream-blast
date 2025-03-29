using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// grid mask is a class that handles the mask of the grid in order to hide cubes that are outside the grid
[RequireComponent(typeof(SpriteMask))]
public class GridMask : MonoBehaviour
{
    // reference to grid background
    private Transform gridBackgroundTransform;

    // offset from the top edge
    public float topOffset = 0.1f;

    // scale multiplier for initial state
    [SerializeField] private float initialScaleMultiplier = 100f;

    // debug mode toggle
    [SerializeField] private bool debugMode = false;

    // scale tracking variables
    private bool isInitialLargeScale = true;
    private Vector3 savedNormalScale;
    private bool hasCalculatedNormalScale = false;

    // components
    private SpriteMask spriteMask;
    private SpriteRenderer backgroundRenderer;

    private void Awake()
    {
        spriteMask = GetComponent<SpriteMask>();
    }

    // initialize the grid mask 
    public void Initialize(Transform gridBackground)
    {
        gridBackgroundTransform = gridBackground;

        if (gridBackgroundTransform != null)
        {
            backgroundRenderer = gridBackgroundTransform.GetComponent<SpriteRenderer>();

            if (backgroundRenderer != null)
            {
                // calculate and save normal scale
                CalculateAndSaveNormalScale();

                // apply large scale
                isInitialLargeScale = true;
                ApplyLargeScale();

                // initially hide the mask if the background is hidden
                gameObject.SetActive(backgroundRenderer.gameObject.activeSelf);

                Debug.Log("[GridMask] Successfully initialized with grid background (using large initial scale)");
            }
            else
            {
                Debug.LogError("[GridMask] Grid background has no SpriteRenderer component!");
            }
        }
        else
        {
            Debug.LogError("[GridMask] No grid background transform provided!");
        }
    }

    private void Update()
    {
        // only update if we're still in initial large scale mode
        if (isInitialLargeScale && backgroundRenderer != null && backgroundRenderer.gameObject.activeInHierarchy)
        {
            // update position
            UpdateMaskPosition();

            // calculate and save normal scale if it changes
            CalculateAndSaveNormalScale();

            // re-apply large scale
            ApplyLargeScale();

            // debug logging if enabled
            if (debugMode)
            {
                Debug.Log($"[GridMask] Position: {transform.position}, Scale: {transform.localScale}");
            }
        }
    }

    // update just the position (always follows background)
    private void UpdateMaskPosition()
    {
        if (spriteMask.sprite != null && gridBackgroundTransform != null)
        {
            // match the mask's position to the background
            transform.position = new Vector3(
                gridBackgroundTransform.position.x,
                gridBackgroundTransform.position.y + topOffset,
                gridBackgroundTransform.position.z - 0.01f // slightly in front
            );

            // match the rotation of the background
            transform.rotation = gridBackgroundTransform.rotation;

            // debug logging if enabled
            if (debugMode)
            {
                Debug.Log($"[GridMask] Updated position to: {transform.position}");
            }
        }
    }

    // calculate normal scale and save it
    private void CalculateAndSaveNormalScale()
    {
        if (spriteMask.sprite != null && backgroundRenderer != null)
        {

            // for 9-slice backgrounds, use size
            float width = backgroundRenderer.size.x;
            float height = backgroundRenderer.size.y;

            savedNormalScale = new Vector3(
                width / spriteMask.sprite.bounds.size.x,
                height / spriteMask.sprite.bounds.size.y,
                1f
            );


            hasCalculatedNormalScale = true;
        }
    }

    // apply large scale based on saved normal scale
    private void ApplyLargeScale()
    {
        if (hasCalculatedNormalScale)
        {
            transform.localScale = new Vector3(
                savedNormalScale.x * initialScaleMultiplier,
                savedNormalScale.y * initialScaleMultiplier,
                savedNormalScale.z
            );

            transform.localPosition = new Vector3(
                0f,
                -2.1f,
                0.1f
            );
        }
    }

    // call this when the grid animation completes to return to normal scale
    public void ResetToNormalScale()
    {
        Debug.Log("[GridMask] ResetToNormalScale called");

        // stop auto-updating in update
        isInitialLargeScale = false;

        // apply normal scale if we have it saved
        if (hasCalculatedNormalScale)
        {
            transform.localScale = savedNormalScale;
            Debug.Log($"[GridMask] Scale reset to normal: {savedNormalScale}");
        }
        else
        {
            Debug.LogWarning("[GridMask] Cannot reset scale - no saved normal scale available");
        }
    }
}