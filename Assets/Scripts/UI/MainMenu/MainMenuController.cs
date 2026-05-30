using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the main menu interactions before the game enters the betting flow.
/// Uses standard Unity UI components and exposes a simple play event with the
/// selected roulette type.
/// </summary>
public sealed class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private Button _playButton;

    [SerializeField]
    private Toggle _europeanRouletteToggle;

    public event Action<bool> PlayButtonClicked;

    private void Awake()
    {
        RegisterPlayButtonListener();
    }

    private void OnDestroy()
    {
        UnregisterPlayButtonListener();
    }

    /// <summary>
    /// Applies the persisted roulette mode to the menu so the UI reflects the current save data.
    /// </summary>
    public void SetSelectedRouletteType(bool isEuropeanRoulette)
    {
        if (_europeanRouletteToggle == null)
        {
            Debug.LogWarning("MainMenuController is missing the roulette type toggle reference.");
            return;
        }

        _europeanRouletteToggle.isOn = isEuropeanRoulette;
    }

    private void HandlePlayButtonClicked()
    {
        bool isEuropeanRoulette = _europeanRouletteToggle == null || _europeanRouletteToggle.isOn;
        PlayButtonClicked?.Invoke(isEuropeanRoulette);
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

    private void UnregisterPlayButtonListener()
    {
        if (_playButton != null)
        {
            _playButton.onClick.RemoveListener(HandlePlayButtonClicked);
        }
    }
}
