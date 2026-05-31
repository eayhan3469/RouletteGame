using TMPro;
using UnityEngine;

/// <summary>
/// Presents the round result for a short period before the next betting phase begins.
/// </summary>
public sealed class ResultUIController : MonoBehaviour
{
    [Header("Result Colors")]
    [SerializeField] private Color PositiveResultColor;
    [SerializeField] private Color NegativeResultColor;
    [SerializeField] private Color NeutralResultColor;
    [Header("Pocket Colors")]
    [SerializeField] private Color RedPocketColor;
    [SerializeField] private Color BlackPocketColor;
    [SerializeField] private Color GreenPocketColor;

    [Header("UI References")]
    [SerializeField] private GameObject _root;
    [SerializeField] private RouletteNumberColorCatalog _rouletteNumberColorCatalog;
    [SerializeField] private TextMeshProUGUI _headlineText;
    [SerializeField] private TextMeshProUGUI _winningNumberText;
    [SerializeField] private TextMeshProUGUI _resultText;

    public void ShowResult(float amountWon, int winningNumber)
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

        UpdateWinningNumberText(winningNumber);

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

    private void UpdateWinningNumberText(int winningNumber)
    {
        if (_winningNumberText == null)
        {
            return;
        }

        _winningNumberText.text = $"Winning Number: {FormatWinningNumber(winningNumber)}";
        _winningNumberText.color = GetWinningNumberColor(winningNumber);
    }

    private Color GetWinningNumberColor(int winningNumber)
    {
        if (_rouletteNumberColorCatalog == null ||
            !_rouletteNumberColorCatalog.TryGetPocketColor(winningNumber, out RoulettePocketColor pocketColor))
        {
            return NeutralResultColor;
        }

        switch (pocketColor)
        {
            case RoulettePocketColor.Red:
                return RedPocketColor;

            case RoulettePocketColor.Black:
                return BlackPocketColor;

            case RoulettePocketColor.Green:
                return GreenPocketColor;

            default:
                return NeutralResultColor;
        }
    }

    private string FormatWinningNumber(int winningNumber)
    {
        return winningNumber == 37 ? "00" : winningNumber.ToString();
    }
}
