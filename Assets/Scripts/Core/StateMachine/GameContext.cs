using UnityEngine;

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

    [Header("Bootstrap")]
    [SerializeField]
    [Min(0f)]
    private float _defaultStartingBalance = 1000f;

    [Header("Gameplay")]
    [SerializeField]
    private ChipManager _chipManager;

    private StateMachine _stateMachine;

    public StateMachine StateMachine => _stateMachine;
    public MainMenuController MainMenuController => _mainMenuController;
    public float DefaultStartingBalance => _defaultStartingBalance;
    public ChipManager ChipManager => _chipManager;
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
}
