using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Controls the hover-driven statistics dropdown shown during the betting flow.
/// </summary>
public sealed class StatisticsUIController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly Color ProfitColor = new Color(0.14f, 0.56f, 0.27f, 1f);
    private static readonly Color LossColor = new Color(0.69f, 0.22f, 0.19f, 1f);

    [SerializeField]
    private GameObject dropdownPanel;

    [SerializeField]
    private TextMeshProUGUI totalSpinsText;

    [SerializeField]
    private TextMeshProUGUI totalWageredText;

    [SerializeField]
    private TextMeshProUGUI totalWonText;

    [SerializeField]
    private TextMeshProUGUI netProfitText;

    private Color _defaultNetProfitColor = Color.white;

    private void Awake()
    {
        if (netProfitText != null)
        {
            _defaultNetProfitColor = netProfitText.color;
        }

        if (dropdownPanel != null)
        {
            dropdownPanel.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (dropdownPanel != null)
        {
            dropdownPanel.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (dropdownPanel != null)
        {
            dropdownPanel.SetActive(false);
        }
    }

    public void RefreshStats(PlayerData data)
    {
        if (data == null)
        {
            Debug.LogWarning("StatisticsUIController could not refresh because PlayerData is missing.");
            return;
        }

        if (totalSpinsText != null)
        {
            totalSpinsText.text = $"Total Spins: {data.TotalSpins}";
        }

        if (totalWageredText != null)
        {
            totalWageredText.text = $"Total Wagered: {data.TotalWagered:0.##}";
        }

        if (totalWonText != null)
        {
            totalWonText.text = $"Total Won: {data.TotalWon:0.##}";
        }

        if (netProfitText == null)
        {
            return;
        }

        float netProfit = data.TotalWon - data.TotalWagered;
        netProfitText.text = $"Net Profit: {FormatSignedAmount(netProfit)}";
        netProfitText.color = netProfit > 0f
            ? ProfitColor
            : netProfit < 0f
                ? LossColor
                : _defaultNetProfitColor;
    }

    private string FormatSignedAmount(float amount)
    {
        if (amount > 0f)
        {
            return $"+{amount:0.##}";
        }

        if (amount < 0f)
        {
            return $"{amount:0.##}";
        }

        return "0";
    }
}
