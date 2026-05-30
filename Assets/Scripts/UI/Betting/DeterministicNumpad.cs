using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the deterministic roulette number selection panel.
/// A selected number stays highlighted until another number or the random button is chosen.
/// </summary>
public sealed class DeterministicNumpad : MonoBehaviour
{
    [Serializable]
    private sealed class NumpadButtonEntry
    {
        [Min(0)]
        public int Value = 0;

        public Button Button = null;
    }

    private const int DoubleZeroValue = 37;

    [Header("Buttons")]
    [SerializeField]
    private List<NumpadButtonEntry> _numberButtons = new List<NumpadButtonEntry>();

    [SerializeField]
    private Button _randomButton;

    [Header("Visuals")]
    [SerializeField]
    private RouletteNumberColorCatalog _rouletteNumberColorCatalog;

    [SerializeField]
    private Color _defaultButtonColor = Color.white;

    [SerializeField]
    private Color _highlightButtonColor = new Color(0.95f, 0.82f, 0.2f, 1f);

    [SerializeField]
    private Color _redButtonColor = new Color(0.72f, 0.13f, 0.16f, 1f);

    [SerializeField]
    private Color _blackButtonColor = new Color(0.12f, 0.13f, 0.15f, 1f);

    [SerializeField]
    private Color _greenButtonColor = new Color(0.09f, 0.45f, 0.25f, 1f);

    [SerializeField]
    private Color _darkTextColor = new Color(0.08f, 0.08f, 0.08f, 1f);

    [SerializeField]
    private Color _lightTextColor = new Color(0.96f, 0.95f, 0.9f, 1f);

    [SerializeField]
    private Color _highlightTextColor = new Color(0.08f, 0.08f, 0.08f, 1f);

    public int SelectedNumber { get; private set; } = -1;

    private bool _isEuropeanLayout = true;

    private void Awake()
    {
        ValidateCatalogReference();
        RegisterNumberButtons();
        RegisterRandomButton();
        RefreshVisualState();
    }

    private void OnDestroy()
    {
        UnregisterNumberButtons();
        UnregisterRandomButton();
    }

    /// <summary>
    /// Configures the numpad for the selected roulette type.
    /// European mode hides the double-zero button and resets the current selection to Random.
    /// </summary>
    public void InitializeNumpad(bool isEuropean)
    {
        _isEuropeanLayout = isEuropean;

        NumpadButtonEntry doubleZeroEntry = GetEntryByValue(DoubleZeroValue);

        if (doubleZeroEntry != null && doubleZeroEntry.Button != null)
        {
            doubleZeroEntry.Button.gameObject.SetActive(!isEuropean);
        }

        SelectRandom();
    }

    public void SelectRandom()
    {
        SelectedNumber = -1;
        RefreshVisualState();
    }

    private void RegisterNumberButtons()
    {
        for (int i = 0; i < _numberButtons.Count; i++)
        {
            NumpadButtonEntry entry = _numberButtons[i];

            if (entry == null || entry.Button == null)
            {
                continue;
            }

            int selectedValue = entry.Value;
            entry.Button.onClick.AddListener(() => HandleNumberButtonClicked(selectedValue));
        }
    }

    private void UnregisterNumberButtons()
    {
        for (int i = 0; i < _numberButtons.Count; i++)
        {
            NumpadButtonEntry entry = _numberButtons[i];

            if (entry == null || entry.Button == null)
            {
                continue;
            }

            entry.Button.onClick.RemoveAllListeners();
        }
    }

    private void RegisterRandomButton()
    {
        if (_randomButton == null)
        {
            Debug.LogWarning("DeterministicNumpad is missing the Random button reference.");
            return;
        }

        _randomButton.onClick.AddListener(SelectRandom);
    }

    private void UnregisterRandomButton()
    {
        if (_randomButton != null)
        {
            _randomButton.onClick.RemoveListener(SelectRandom);
        }
    }

    private void HandleNumberButtonClicked(int selectedValue)
    {
        SelectedNumber = selectedValue;
        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        for (int i = 0; i < _numberButtons.Count; i++)
        {
            NumpadButtonEntry entry = _numberButtons[i];

            if (entry == null || entry.Button == null)
            {
                continue;
            }

            bool isSelected = SelectedNumber == entry.Value;
            if (isSelected)
            {
                SetButtonVisual(entry.Button, _highlightButtonColor, _highlightTextColor);
                continue;
            }

            ApplyNumberButtonVisual(entry);
        }

        if (_randomButton != null)
        {
            bool isRandomSelected = SelectedNumber == -1;
            Color backgroundColor = isRandomSelected ? _highlightButtonColor : _defaultButtonColor;
            Color textColor = isRandomSelected ? _highlightTextColor : _darkTextColor;
            SetButtonVisual(_randomButton, backgroundColor, textColor);
        }
    }

    private void ApplyNumberButtonVisual(NumpadButtonEntry entry)
    {
        Color backgroundColor = _defaultButtonColor;
        Color textColor = _darkTextColor;

        if (_rouletteNumberColorCatalog != null &&
            _rouletteNumberColorCatalog.TryGetPocketColor(entry.Value, out RoulettePocketColor pocketColor))
        {
            switch (pocketColor)
            {
                case RoulettePocketColor.Red:
                    backgroundColor = _redButtonColor;
                    textColor = _lightTextColor;
                    break;
                case RoulettePocketColor.Black:
                    backgroundColor = _blackButtonColor;
                    textColor = _lightTextColor;
                    break;
                case RoulettePocketColor.Green:
                    backgroundColor = _greenButtonColor;
                    textColor = _lightTextColor;
                    break;
            }
        }

        if (_isEuropeanLayout && entry.Value == DoubleZeroValue)
        {
            backgroundColor = _defaultButtonColor;
            textColor = _darkTextColor;
        }

        SetButtonVisual(entry.Button, backgroundColor, textColor);
    }

    private void SetButtonVisual(Button targetButton, Color backgroundColor, Color textColor)
    {
        if (targetButton == null)
        {
            return;
        }

        Image targetImage = targetButton.targetGraphic as Image;

        if (targetImage == null)
        {
            targetImage = targetButton.GetComponent<Image>();
        }

        if (targetImage != null)
        {
            targetImage.color = backgroundColor;
        }

        TMP_Text buttonText = targetButton.GetComponentInChildren<TMP_Text>();

        if (buttonText != null)
        {
            buttonText.color = textColor;
        }
    }

    private void ValidateCatalogReference()
    {
        if (_rouletteNumberColorCatalog == null)
        {
            Debug.LogWarning("DeterministicNumpad is missing the Roulette Number Color Catalog reference.");
        }
    }

    private NumpadButtonEntry GetEntryByValue(int value)
    {
        for (int i = 0; i < _numberButtons.Count; i++)
        {
            NumpadButtonEntry entry = _numberButtons[i];

            if (entry != null && entry.Value == value)
            {
                return entry;
            }
        }

        return null;
    }
}
