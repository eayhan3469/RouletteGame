using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the main menu interactions before the game enters the betting flow.
/// Uses standard Unity UI components and exposes a simple play event with the
/// selected roulette variant.
/// </summary>
public sealed class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private Button _playButton;

    [SerializeField]
    private Button _clearSaveButton;

    [SerializeField]
    private ToggleGroup _rouletteTypeToggleGroup;

    [SerializeField]
    private Toggle _europeanRouletteToggle;

    [SerializeField]
    private Toggle _americanRouletteToggle;

    private bool _isApplyingRouletteTypeSelection;

    public event Action<RouletteVariant> PlayButtonClicked;
    public event Action ClearSaveButtonClicked;

    private void Awake()
    {
        RegisterPlayButtonListener();
        RegisterClearSaveButtonListener();
        RegisterRouletteTypeToggleListeners();
        ConfigureRouletteTypeToggleGroup();
    }

    private void OnDestroy()
    {
        UnregisterPlayButtonListener();
        UnregisterClearSaveButtonListener();
        UnregisterRouletteTypeToggleListeners();
    }

    /// <summary>
    /// Applies the persisted roulette mode to the menu so the UI reflects the current save data.
    /// </summary>
    public void SetSelectedRouletteType(RouletteVariant rouletteVariant)
    {
        if (_europeanRouletteToggle == null && _americanRouletteToggle == null)
        {
            Debug.LogWarning("MainMenuController is missing roulette type toggle references.");
            return;
        }

        ApplyRouletteTypeSelection(rouletteVariant);
    }

    public void SetClearSaveButtonInteractable(bool isInteractable)
    {
        if (_clearSaveButton == null)
        {
            Debug.LogWarning("MainMenuController is missing the Clear Save button reference.");
            return;
        }

        _clearSaveButton.interactable = isInteractable;
    }

    private void HandlePlayButtonClicked()
    {
        PlayButtonClicked?.Invoke(GetSelectedRouletteVariant());
    }

    private void HandleClearSaveButtonClicked()
    {
        ClearSaveButtonClicked?.Invoke();
    }

    private void RegisterPlayButtonListener()
    {
        if (_playButton != null)
        {
            _playButton.onClick.RemoveListener(HandlePlayButtonClicked);
            _playButton.onClick.AddListener(HandlePlayButtonClicked);
        }
        else
        {
            Debug.LogWarning("MainMenuController is missing a Play button reference.");
        }
    }

    private void RegisterClearSaveButtonListener()
    {
        if (_clearSaveButton != null)
        {
            _clearSaveButton.onClick.RemoveListener(HandleClearSaveButtonClicked);
            _clearSaveButton.onClick.AddListener(HandleClearSaveButtonClicked);
        }
        else
        {
            Debug.LogWarning("MainMenuController is missing a Clear Save button reference.");
        }
    }

    private void RegisterRouletteTypeToggleListeners()
    {
        if (_europeanRouletteToggle != null)
        {
            _europeanRouletteToggle.onValueChanged.RemoveListener(HandleEuropeanRouletteToggleChanged);
            _europeanRouletteToggle.onValueChanged.AddListener(HandleEuropeanRouletteToggleChanged);
        }

        if (_americanRouletteToggle != null)
        {
            _americanRouletteToggle.onValueChanged.RemoveListener(HandleAmericanRouletteToggleChanged);
            _americanRouletteToggle.onValueChanged.AddListener(HandleAmericanRouletteToggleChanged);
        }
    }

    private void ConfigureRouletteTypeToggleGroup()
    {
        if (_rouletteTypeToggleGroup == null)
        {
            return;
        }

        _rouletteTypeToggleGroup.allowSwitchOff = false;

        if (_europeanRouletteToggle != null)
        {
            _europeanRouletteToggle.group = _rouletteTypeToggleGroup;
        }

        if (_americanRouletteToggle != null)
        {
            _americanRouletteToggle.group = _rouletteTypeToggleGroup;
        }
    }

    private void UnregisterPlayButtonListener()
    {
        if (_playButton != null)
        {
            _playButton.onClick.RemoveListener(HandlePlayButtonClicked);
        }
    }

    private void UnregisterClearSaveButtonListener()
    {
        if (_clearSaveButton != null)
        {
            _clearSaveButton.onClick.RemoveListener(HandleClearSaveButtonClicked);
        }
    }

    private void UnregisterRouletteTypeToggleListeners()
    {
        if (_europeanRouletteToggle != null)
        {
            _europeanRouletteToggle.onValueChanged.RemoveListener(HandleEuropeanRouletteToggleChanged);
        }

        if (_americanRouletteToggle != null)
        {
            _americanRouletteToggle.onValueChanged.RemoveListener(HandleAmericanRouletteToggleChanged);
        }
    }

    private void HandleEuropeanRouletteToggleChanged(bool isOn)
    {
        if (!isOn || _isApplyingRouletteTypeSelection)
        {
            return;
        }

        ApplyRouletteTypeSelection(RouletteVariant.European);
    }

    private void HandleAmericanRouletteToggleChanged(bool isOn)
    {
        if (!isOn || _isApplyingRouletteTypeSelection)
        {
            return;
        }

        ApplyRouletteTypeSelection(RouletteVariant.American);
    }

    private void ApplyRouletteTypeSelection(RouletteVariant rouletteVariant)
    {
        _isApplyingRouletteTypeSelection = true;

        if (_europeanRouletteToggle != null)
        {
            _europeanRouletteToggle.SetIsOnWithoutNotify(rouletteVariant == RouletteVariant.European);
        }

        if (_americanRouletteToggle != null)
        {
            _americanRouletteToggle.SetIsOnWithoutNotify(rouletteVariant == RouletteVariant.American);
        }

        _isApplyingRouletteTypeSelection = false;
    }

    private RouletteVariant GetSelectedRouletteVariant()
    {
        if (_americanRouletteToggle != null && _americanRouletteToggle.isOn)
        {
            return RouletteVariant.American;
        }

        return RouletteVariant.European;
    }
}
