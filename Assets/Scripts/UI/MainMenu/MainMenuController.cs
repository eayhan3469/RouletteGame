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
    private Toggle _europeanRouletteToggle;

    public event Action<RouletteVariant> PlayButtonClicked;
    public event Action ClearSaveButtonClicked;

    private void Awake()
    {
        RegisterPlayButtonListener();
        RegisterClearSaveButtonListener();
    }

    private void OnDestroy()
    {
        UnregisterPlayButtonListener();
        UnregisterClearSaveButtonListener();
    }

    /// <summary>
    /// Applies the persisted roulette mode to the menu so the UI reflects the current save data.
    /// </summary>
    public void SetSelectedRouletteType(RouletteVariant rouletteVariant)
    {
        if (_europeanRouletteToggle == null)
        {
            Debug.LogWarning("MainMenuController is missing the roulette type toggle reference.");
            return;
        }

        _europeanRouletteToggle.isOn = rouletteVariant == RouletteVariant.European;
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
        RouletteVariant selectedVariant = _europeanRouletteToggle == null || _europeanRouletteToggle.isOn
            ? RouletteVariant.European
            : RouletteVariant.American;
        PlayButtonClicked?.Invoke(selectedVariant);
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
}
