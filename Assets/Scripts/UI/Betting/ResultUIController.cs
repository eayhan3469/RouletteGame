using TMPro;
using UnityEngine;

/// <summary>
/// Presents the round result for a short period before the next betting phase begins.
/// </summary>
public sealed class ResultUIController : MonoBehaviour
{
    private static readonly Color PositiveResultColor = new Color(0.77254903f, 0.5294118f, 0.078431375f, 1f);
    private static readonly Color NegativeResultColor = new Color(0.6901961f, 0.21960784f, 0.1882353f, 1f);
    private static readonly Color NeutralResultColor = new Color(0.23137255f, 0.2901961f, 0.30980393f, 1f);

    [SerializeField]
    private GameObject _root;

    [SerializeField]
    private TextMeshProUGUI _headlineText;

    [SerializeField]
    private TextMeshProUGUI _resultText;

    public void ShowResult(float amountWon)
    {
        SetVisible(true);

        if (_resultText == null)
        {
            Debug.LogWarning("ResultUIController is missing the result text reference.");
            return;
        }

        if (_headlineText != null)
        {
            _headlineText.text = amountWon > 0f
                ? "Round Won"
                : amountWon < 0f
                    ? "Round Lost"
                    : "Round Complete";
        }

        if (amountWon > 0f)
        {
            _resultText.text = $"+{amountWon:0.##}";
            _resultText.color = PositiveResultColor;
            return;
        }

        if (amountWon < 0f)
        {
            _resultText.text = $"{amountWon:0.##}";
            _resultText.color = NegativeResultColor;
            return;
        }

        _resultText.text = "0";
        _resultText.color = NeutralResultColor;
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool isVisible)
    {
        GameObject target = _root != null ? _root : gameObject;
        target.SetActive(isVisible);
    }
}
