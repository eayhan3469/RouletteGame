using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Shared world-space highlight that is moved and resized to the currently hovered bet spot.
/// Bets with covered numbers can fan out to per-number highlights, while bounds
/// highlighting remains as a fallback when needed.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Transform))]
public sealed class BetSpotHighlighter : MonoBehaviour
{
    private readonly Dictionary<int, BetSpot> _straightNumberSpots = new Dictionary<int, BetSpot>();
    private readonly List<BetSpot> _activeNumberHighlightSpots = new List<BetSpot>();

    [SerializeField]
    private SpriteRenderer _spriteRenderer;

    [SerializeField]
    private Sprite _highlightSprite;

    [SerializeField]
    private Color _highlightColor = new Color(1f, 0.9f, 0.2f, 0.35f);

    [SerializeField]
    [Min(0f)]
    private float _verticalOffset = 0.02f;

    [SerializeField]
    private Vector2 _sizePadding = new Vector2(0.02f, 0.02f);

    [SerializeField]
    private int _numberHighlightSortingOrder = 55;

    public void ShowFor(BetSpot betSpot)
    {
        if (betSpot == null)
        {
            Hide();
            return;
        }

        EnsureRenderer();
        HideNumberHighlights();

        if (TryShowCoveredNumberHighlights(betSpot))
        {
            _spriteRenderer.enabled = false;
            return;
        }

        ShowSharedBoundsHighlight(betSpot);
    }

    public void Hide()
    {
        HideNumberHighlights();

        if (_spriteRenderer == null)
        {
            return;
        }

        _spriteRenderer.enabled = false;
    }

    private void Awake()
    {
        EnsureRenderer();
        CacheStraightNumberSpots();
        Hide();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureRenderer();

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _highlightColor;
        }
    }
#endif

    private void EnsureRenderer()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (_highlightSprite == null)
        {
            _highlightSprite = CreateFallbackSprite();
        }

        _spriteRenderer.sprite = _highlightSprite;
        _spriteRenderer.color = _highlightColor;
        _spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        _spriteRenderer.sortingOrder = 50;
    }

    private void CacheStraightNumberSpots()
    {
        _straightNumberSpots.Clear();

        BetSpot[] betSpots = FindObjectsByType<BetSpot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < betSpots.Length; i++)
        {
            BetSpot betSpot = betSpots[i];

            if (betSpot == null || !betSpot.IsStraightNumberSpot)
            {
                continue;
            }

            int straightNumber = betSpot.StraightNumber;

            if (_straightNumberSpots.ContainsKey(straightNumber))
            {
                Debug.LogWarning($"BetSpotHighlighter found more than one straight BetSpot for number {straightNumber}. Using the first one.");
                continue;
            }

            _straightNumberSpots.Add(straightNumber, betSpot);
        }
    }

    private bool TryShowCoveredNumberHighlights(BetSpot betSpot)
    {
        int[] coveredNumbers = betSpot.CoveredNumbers;

        if (coveredNumbers == null || coveredNumbers.Length == 0)
        {
            return false;
        }

        bool hasShownAnyHighlight = false;

        for (int i = 0; i < coveredNumbers.Length; i++)
        {
            int coveredNumber = coveredNumbers[i];

            if (!_straightNumberSpots.TryGetValue(coveredNumber, out BetSpot straightNumberSpot) ||
                straightNumberSpot == null ||
                !straightNumberSpot.HasNumberHighlightRenderer)
            {
                continue;
            }

            straightNumberSpot.SetNumberHighlightVisible(true, _highlightColor, _numberHighlightSortingOrder);
            _activeNumberHighlightSpots.Add(straightNumberSpot);
            hasShownAnyHighlight = true;
        }

        return hasShownAnyHighlight;
    }

    private void HideNumberHighlights()
    {
        for (int i = 0; i < _activeNumberHighlightSpots.Count; i++)
        {
            if (_activeNumberHighlightSpots[i] != null)
            {
                _activeNumberHighlightSpots[i].SetNumberHighlightVisible(false, _highlightColor, _numberHighlightSortingOrder);
            }
        }

        _activeNumberHighlightSpots.Clear();
    }

    private void ShowSharedBoundsHighlight(BetSpot betSpot)
    {
        Bounds bounds = betSpot.GetWorldBounds();
        Vector3 worldPosition = bounds.center;
        worldPosition.y = bounds.max.y + _verticalOffset;

        transform.position = worldPosition;
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        _spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        _spriteRenderer.size = new Vector2(
            bounds.size.x + _sizePadding.x,
            bounds.size.z + _sizePadding.y);
        _spriteRenderer.enabled = true;
    }

    private Sprite CreateFallbackSprite()
    {
        Rect rect = new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height);
        return Sprite.Create(
            Texture2D.whiteTexture,
            rect,
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            Vector4.zero);
    }
}
