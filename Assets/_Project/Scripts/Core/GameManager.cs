using System;
using System.Collections.Generic;
using OutpostAegis.Data;
using UnityEngine;

namespace OutpostAegis.Core
{
    /// <summary>
    /// Central coordinator for Requisition economy, Command Points, Central Plasma Reactor integrity,
    /// and overall Siege Victory/Defeat state.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Starting Economy & Reactor")]
        [SerializeField] private int startingRequisition = 320;
        [SerializeField] private int startingCommandPoints = 3;
        [SerializeField] private int maxCommandPoints = 10;
        [SerializeField] private int maxReactorHealth = 20;

        [Header("Available Tower Arsenal")]
        public List<TowerDataSO> availableTowers = new List<TowerDataSO>();

        [Header("Available Command Abilities")]
        public List<AbilityDataSO> availableAbilities = new List<AbilityDataSO>();

        public int Requisition { get; private set; }
        public int CommandPoints { get; private set; }
        public int MaxCommandPoints => maxCommandPoints;
        public int ReactorHealth { get; private set; }
        public int MaxReactorHealth => maxReactorHealth;
        public GameState CurrentState { get; private set; } = GameState.Preparing;

        public event Action<int> OnRequisitionChanged;
        public event Action<int> OnCommandPointsChanged;
        public event Action<int, int> OnReactorHealthChanged;
        public event Action<GameState> OnGameStateChanged;
        public event Action<string> OnVoxTransmission;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            ResetMatchState();
        }

        public void ResetMatchState()
        {
            Time.timeScale = 1f;
            Requisition = startingRequisition;
            CommandPoints = startingCommandPoints;
            ReactorHealth = maxReactorHealth;
            SetGameState(GameState.Preparing);

            OnRequisitionChanged?.Invoke(Requisition);
            OnCommandPointsChanged?.Invoke(CommandPoints);
            OnReactorHealthChanged?.Invoke(ReactorHealth, maxReactorHealth);
            BroadcastVox("Outpost Aegis-IV online. Fortify the Scrap-Road and Ash-Gully before the Ork WAAAGH! strikes.");
        }

        public bool CanAffordRequisition(int amount) => Requisition >= amount;

        public bool SpendRequisition(int amount)
        {
            if (Requisition < amount) return false;
            Requisition -= amount;
            OnRequisitionChanged?.Invoke(Requisition);
            return true;
        }

        public void AddRequisition(int amount)
        {
            if (amount <= 0) return;
            Requisition += amount;
            OnRequisitionChanged?.Invoke(Requisition);
        }

        public bool CanAffordCommandPoints(int amount) => CommandPoints >= amount;

        public bool SpendCommandPoints(int amount)
        {
            if (CommandPoints < amount) return false;
            CommandPoints -= amount;
            OnCommandPointsChanged?.Invoke(CommandPoints);
            return true;
        }

        public void AddCommandPoints(int amount)
        {
            if (amount <= 0) return;
            CommandPoints = Mathf.Min(maxCommandPoints, CommandPoints + amount);
            OnCommandPointsChanged?.Invoke(CommandPoints);
        }

        public void DamagePlasmaReactor(int damage)
        {
            if (CurrentState == GameState.Defeat || CurrentState == GameState.Victory) return;

            ReactorHealth = Mathf.Max(0, ReactorHealth - damage);
            OnReactorHealthChanged?.Invoke(ReactorHealth, maxReactorHealth);
            BroadcastVox($"WARNING: Ork forces breached the Base Gate! Central Plasma Reactor at {ReactorHealth}/{maxReactorHealth} integrity!");

            if (ReactorHealth <= 0)
            {
                SetGameState(GameState.Defeat);
                BroadcastVox("CRITICAL FAILURE: Central Plasma Reactor destroyed. Outpost Aegis-IV has fallen to the WAAAGH!.");
            }
        }

        public void DeclareVictory()
        {
            if (CurrentState == GameState.Defeat) return;
            SetGameState(GameState.Victory);
            BroadcastVox("VICTORY FOR THE IMPERIUM! The Ork Warboss lies broken and the WAAAGH! is routed!");
        }

        public void SetGameState(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnGameStateChanged?.Invoke(CurrentState);
        }

        public void BroadcastVox(string message)
        {
            OnVoxTransmission?.Invoke(message);
        }

        public void ToggleFastForward()
        {
            Time.timeScale = Mathf.Approximately(Time.timeScale, 1f) ? 2f : 1f;
        }
    }
}
