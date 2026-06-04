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

    [Header("Presentation")]
    [SerializeField]
    private RouletteAudioFeedbackController _audioFeedbackController;

    [SerializeField]
    private RouletteVfxManager _vfxManager;

    [Header("Bootstrap")]
    [SerializeField]
    [Min(0f)]
    private float _defaultStartingBalance = 1000f;

    [Header("Gameplay")]
    [SerializeField]
    private ChipManager _chipManager;

    private StateMachine _stateMachine;
    private RouletteTableVariant _activeTable;

    public StateMachine StateMachine => _stateMachine;
    public MainMenuController MainMenuController => _mainMenuController;
    public BettingUIController BettingUIController => _bettingUIController;
    public ResultUIController ResultUIController => _resultUIController;
    public StatisticsUIController StatisticsUIController => _statisticsUIController;
    public RouletteAudioFeedbackController AudioFeedbackController => _audioFeedbackController;
    public RouletteVfxManager VfxManager => _vfxManager;
    public float DefaultStartingBalance => _defaultStartingBalance;
    public ChipManager ChipManager => _chipManager;
    public RouletteTableVariant ActiveTable => _activeTable;
    public BetManager BetManager => _activeTable != null ? _activeTable.BetManager : null;
    public WheelController WheelController => _activeTable != null ? _activeTable.WheelController : null;
    public bool IsChipInteractionEnabled { get; private set; }
    public GameSaveData SaveData { get; private set; }
    public PlayerData PlayerData { get; private set; }
    public RouletteVariant ActiveRouletteVariant { get; private set; } = RouletteVariant.European;

    private void Awake()
    {
        EnsureAudioFeedbackController();
        EnsureVfxManager();
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

    public void SetSaveData(GameSaveData saveData)
    {
        SaveData = saveData ?? new GameSaveData();
        SaveData.EnsureProfiles();
    }

    public void SetPlayerData(PlayerData playerData)
    {
        PlayerData = playerData;
    }

    public void SetActiveProfile(RouletteVariant variant)
    {
        if (SaveData == null)
        {
            SetSaveData(new GameSaveData());
        }

        ActiveRouletteVariant = variant;
        SaveData.LastSelectedVariant = variant;
        PlayerData = SaveData.GetProfile(variant);
    }

    public bool SetActiveTable(RouletteVariant variant, RouletteTableVariant tableVariant)
    {
        if (tableVariant == null)
        {
            Debug.LogError($"GameContext could not activate a table for {variant} because the table reference is missing.");
            return false;
        }

        if (tableVariant.BetManager == null || tableVariant.WheelController == null)
        {
            Debug.LogError($"Active {variant} table is missing BetManager or WheelController references.");
            return false;
        }

        ActiveRouletteVariant = variant;
        _activeTable = tableVariant;
        return true;
    }

    public void SetChipInteractionEnabled(bool isEnabled)
    {
        IsChipInteractionEnabled = isEnabled;
    }

    private void EnsureAudioFeedbackController()
    {
        if (_audioFeedbackController != null)
        {
            return;
        }

        _audioFeedbackController = GetComponent<RouletteAudioFeedbackController>();

        if (_audioFeedbackController == null)
        {
            _audioFeedbackController = gameObject.AddComponent<RouletteAudioFeedbackController>();
        }
    }

    private void EnsureVfxManager()
    {
        if (_vfxManager == null)
        {
            _vfxManager = FindFirstObjectByType<RouletteVfxManager>();
        }

        if (_vfxManager == null)
        {
            Debug.LogWarning("GameContext is missing the RouletteVfxManager reference.");
            return;
        }

        _vfxManager.StopAndClearAll();
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

    public void SaveCurrentBettingState()
    {
        if (PlayerData == null)
        {
            return;
        }

        BetManager activeBetManager = BetManager;
        PlayerData.SavedRoundPhase = activeBetManager != null && activeBetManager.ActiveBets.Count > 0
            ? PlayerData.RoundPhase.Betting
            : PlayerData.RoundPhase.None;
        PlayerData.PendingSpinTargetNumber = -1;
        PlayerData.SavedBets = activeBetManager != null
            ? activeBetManager.CreateSavedBetsSnapshot()
            : new System.Collections.Generic.List<PlayerData.SavedBetData>();
        SaveActiveGameData();
    }

    public void SavePendingSpinBets(int pendingSpinTargetNumber)
    {
        if (PlayerData == null)
        {
            return;
        }

        PlayerData.SavedRoundPhase = PlayerData.RoundPhase.Spinning;
        PlayerData.PendingSpinTargetNumber = pendingSpinTargetNumber;
        BetManager activeBetManager = BetManager;
        PlayerData.SavedBets = activeBetManager != null
            ? activeBetManager.CreateSavedBetsSnapshot()
            : new System.Collections.Generic.List<PlayerData.SavedBetData>();
        SaveActiveGameData();
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

    public void SaveActiveGameData()
    {
        if (SaveData == null)
        {
            SetSaveData(new GameSaveData());
        }

        SaveData.LastSelectedVariant = ActiveRouletteVariant;
        SaveLoadManager.Save(SaveData);
    }
}
