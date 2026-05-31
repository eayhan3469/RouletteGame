using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Scene-level composition root for the deterministic roulette flow.
/// It owns shared scene references for future managers and delegates state
/// ownership and transitions to the standalone StateMachine.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameContext : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private GameObject _mainMenuPanel;

    [SerializeField]
    private MainMenuController _mainMenuController;

    [SerializeField]
    private GameObject _bettingUiPanel;

    [SerializeField]
    private BettingUIController _bettingUIController;

    [SerializeField]
    private GameObject _deterministicSelectionRoot;

    [SerializeField]
    [FormerlySerializedAs("_resolutionUIController")]
    private ResultUIController _resultUIController;

    [SerializeField]
    private StatisticsUIController _statisticsUIController;

    [Header("Bootstrap")]
    [SerializeField]
    [Min(0f)]
    private float _defaultStartingBalance = 1000f;

    [Header("Gameplay")]
    [SerializeField]
    private ChipManager _chipManager;

    [SerializeField]
    private BetManager _betManager;

    [SerializeField]
    private WheelController _wheelController;

    private StateMachine _stateMachine;

    public StateMachine StateMachine => _stateMachine;
    public MainMenuController MainMenuController => _mainMenuController;
    public BettingUIController BettingUIController => _bettingUIController;
    public ResultUIController ResultUIController => _resultUIController;
    public StatisticsUIController StatisticsUIController => _statisticsUIController;
    public float DefaultStartingBalance => _defaultStartingBalance;
    public ChipManager ChipManager => _chipManager;
    public BetManager BetManager => _betManager;
    public WheelController WheelController => _wheelController;
    public PlayerData PlayerData { get; private set; }

    private void Awake()
    {
        _stateMachine = new StateMachine();
    }

    private void Start()
    {
        _stateMachine.ChangeState(new InitializeState(this, _stateMachine));
    }

    private void Update()
    {
        _stateMachine.Tick();
    }

    public void SetMainMenuVisible(bool isVisible)
    {
        if (_mainMenuPanel == null)
        {
            Debug.LogWarning("GameContext is missing the Main Menu panel reference.");
            return;
        }

        _mainMenuPanel.SetActive(isVisible);
    }

    public void SetPlayerData(PlayerData playerData)
    {
        PlayerData = playerData;
    }

    public void SetBettingUiVisible(bool isVisible)
    {
        if (_bettingUiPanel == null)
        {
            Debug.LogWarning("GameContext is missing the Betting UI panel reference.");
        }

        if (_bettingUiPanel != null)
        {
            _bettingUiPanel.SetActive(isVisible);
        }

        if (_deterministicSelectionRoot == null)
        {
            Debug.LogWarning("GameContext is missing the Deterministic Selection root reference.");
            return;
        }

        _deterministicSelectionRoot.SetActive(isVisible);
    }

    public void SavePendingSpinBets()
    {
        if (PlayerData == null)
        {
            return;
        }

        PlayerData.SavedRoundPhase = PlayerData.RoundPhase.None;
        PlayerData.PendingSpinTargetNumber = -1;
        PlayerData.SavedBets = _betManager != null
            ? _betManager.CreateSavedBetsSnapshot()
            : new System.Collections.Generic.List<PlayerData.SavedBetData>();
        SaveLoadManager.Save(PlayerData);
    }

    public void ClearPendingSpinBets()
    {
        if (PlayerData == null)
        {
            return;
        }

        PlayerData.SavedRoundPhase = PlayerData.RoundPhase.None;
        PlayerData.PendingSpinTargetNumber = -1;
        PlayerData.SavedBets = new System.Collections.Generic.List<PlayerData.SavedBetData>();
    }
}
