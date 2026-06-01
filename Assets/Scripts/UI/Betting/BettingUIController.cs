using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the overall betting UI by coordinating the deterministic numpad,
/// balance/total bet display, and the spin trigger.
/// </summary>
public sealed class BettingUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private DeterministicNumpad _deterministicNumpad;

    [SerializeField]
    private TextMeshProUGUI _balanceText;

    [SerializeField]
    private TextMeshProUGUI _totalBetText;

    [SerializeField]
    private Button _spinButton;

    [SerializeField]
    private Button _menuButton;

    public event Action<int> OnSpinTriggered;
    public event Action OnMenuTriggered;

    public DeterministicNumpad DeterministicNumpad => _deterministicNumpad;

    private void Awake()
    {
        RegisterSpinButton();
        RegisterMenuButton();
    }

    private void OnDestroy()
    {
        UnregisterSpinButton();
        UnregisterMenuButton();
    }

    public void Initialize(bool isEuropean, float balance, float totalBet)
    {
        if (_deterministicNumpad != null)
        {
            _deterministicNumpad.InitializeNumpad(isEuropean);
        }
        else
        {
            Debug.LogWarning("BettingUIController is missing the DeterministicNumpad reference.");
        }

        UpdateBalanceText(balance);
        UpdateTotalBetText(totalBet);
    }

    public void UpdateBalanceText(float balance)
    {
        if (_balanceText == null)
        {
            Debug.LogWarning("BettingUIController is missing the Balance text reference.");
            return;
        }

        _balanceText.text = $"Balance: {balance:0.##}";
    }

    public void UpdateTotalBetText(float totalBet)
    {
        if (_totalBetText == null)
        {
            Debug.LogWarning("BettingUIController is missing the Total Bet text reference.");
            return;
        }

        _totalBetText.text = $"Total Bet: {totalBet:0.##}";
    }

    private void RegisterSpinButton()
    {
        if (_spinButton == null)
        {
            Debug.LogWarning("BettingUIController is missing the Spin button reference.");
            return;
        }

        _spinButton.onClick.RemoveListener(HandleSpinClicked);
        _spinButton.onClick.AddListener(HandleSpinClicked);
    }

    private void UnregisterSpinButton()
    {
        if (_spinButton != null)
        {
            _spinButton.onClick.RemoveListener(HandleSpinClicked);
        }
    }

    private void RegisterMenuButton()
    {
        if (_menuButton == null)
        {
            Debug.LogWarning("BettingUIController is missing the Menu button reference.");
            return;
        }

        _menuButton.onClick.RemoveListener(HandleMenuClicked);
        _menuButton.onClick.AddListener(HandleMenuClicked);
    }

    private void UnregisterMenuButton()
    {
        if (_menuButton != null)
        {
            _menuButton.onClick.RemoveListener(HandleMenuClicked);
        }
    }

    private void HandleSpinClicked()
    {
        int selectedNumber = _deterministicNumpad != null ? _deterministicNumpad.SelectedNumber : -1;
        OnSpinTriggered?.Invoke(selectedNumber);
    }

    private void HandleMenuClicked()
    {
        OnMenuTriggered?.Invoke();
    }
}
