using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteMask))]
public class GridMask : MonoBehaviour
{
    // Reference to grid background
    private Transform gridBackgroundTransform;

    // Customizable properties
    public float topOffset = 0.1f; // Offset from the top edge

    // Scale multiplier for initial state
    [SerializeField] private float initialScaleMultiplier = 100f;

    // Scale tracking variables
    private bool isInitialLargeScale = true;
    private Vector3 savedNormalScale;
    private bool hasCalculatedNormalScale = false;

    // Components
    private SpriteMask spriteMask;
    private SpriteRenderer backgroundRenderer;

    private void Awake()
    {
        spriteMask = GetComponent<SpriteMask>();
    }

    public void Initialize(Transform gridBackground)
    {
        gridBackgroundTransform = gridBackground;

        if (gridBackgroundTransform != null)
        {
            backgroundRenderer = gridBackgroundTransform.GetComponent<SpriteRenderer>();

            if (backgroundRenderer != null)
            {
                // Calculate and save normal scale
                CalculateAndSaveNormalScale();

                // Apply large scale
                isInitialLargeScale = true;
                ApplyLargeScale();

                // Initially hide the mask if the background is hidden
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
        // Only update if we're still in initial large scale mode
        if (isInitialLargeScale && backgroundRenderer != null && backgroundRenderer.gameObject.activeInHierarchy)
        {
            // Update position
            UpdateMaskPosition();

            // Calculate and save normal scale if it changes
            CalculateAndSaveNormalScale();

            // Re-apply large scale
            ApplyLargeScale();
        }
    }

    // Update just the position (always follows background)
    private void UpdateMaskPosition()
    {
        if (spriteMask.sprite != null && gridBackgroundTransform != null)
        {
            // Match the mask's position to the background
            transform.position = new Vector3(
                gridBackgroundTransform.position.x,
                gridBackgroundTransform.position.y + topOffset,
                gridBackgroundTransform.position.z - 0.01f // Slightly in front
            );

            // Match the rotation of the background
            transform.rotation = gridBackgroundTransform.rotation;
        }
    }

    // Calculate normal scale and save it
    private void CalculateAndSaveNormalScale()
    {
        if (spriteMask.sprite != null && backgroundRenderer != null)
        {
            // Calculate normal scale
            if (backgroundRenderer.drawMode == SpriteDrawMode.Sliced ||
                backgroundRenderer.drawMode == SpriteDrawMode.Tiled)
            {
                // For 9-slice backgrounds, use size
                float width = backgroundRenderer.size.x;
                float height = backgroundRenderer.size.y;

                savedNormalScale = new Vector3(
                    width / spriteMask.sprite.bounds.size.x,
                    height / spriteMask.sprite.bounds.size.y,
                    1f
                );
            }
            else
            {
                // For standard backgrounds, use scale
                savedNormalScale = new Vector3(
                    gridBackgroundTransform.localScale.x,
                    gridBackgroundTransform.localScale.y,
                    gridBackgroundTransform.localScale.z
                );
            }

            hasCalculatedNormalScale = true;
        }
    }

    // Apply large scale based on saved normal scale
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

    // Call this when the grid animation completes to return to normal scale
    public void ResetToNormalScale()
    {
        Debug.Log("[GridMask] ResetToNormalScale called");

        // Stop auto-updating in Update
        isInitialLargeScale = false;

        // Apply normal scale if we have it saved
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