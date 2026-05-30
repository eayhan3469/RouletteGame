using UnityEngine;

/// <summary>
/// Shared world-space highlight that is moved and resized to the currently hovered bet spot.
/// This avoids needing a dedicated highlight object on every single betting collider.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Transform))]
public sealed class BetSpotHighlighter : MonoBehaviour
{
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

    public void ShowFor(BetSpot betSpot)
    {
        if (betSpot == null)
        {
            Hide();
            return;
        }

        EnsureRenderer();

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

    public void Hide()
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        _spriteRenderer.enabled = false;
    }

    private void Awake()
    {
        EnsureRenderer();
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
