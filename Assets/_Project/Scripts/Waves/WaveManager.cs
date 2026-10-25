using System;
using System.Collections;
using System.Collections.Generic;
using OutpostAegis.Core;
using OutpostAegis.Data;
using OutpostAegis.Enemies;
using OutpostAegis.Map;
using UnityEngine;

namespace OutpostAegis.Waves
{
    /// <summary>
    /// Orchestrates the 20 Ork WAAAGH! waves across The Scrap-Road (North) and The Ash-Gully (South).
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Converging Canyon Lanes")]
        public WaypointPath scrapRoadNorthLane;
        public WaypointPath ashGullySouthLane;

        [Header("20 Ork WAAAGH! Waves")]
        public List<WaveDataSO> waves = new List<WaveDataSO>();

        public int CurrentWaveIndex { get; private set; } = -1;
        public int DisplayWaveNumber => Mathf.Clamp(CurrentWaveIndex + 1, 1, Mathf.Max(1, waves.Count));
        public int TotalWaves => waves.Count;
        public bool IsSpawning { get; private set; }

        public event Action<int, int, string> OnWaveChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;

            if (GameManager.Instance.CurrentState == GameState.WaveInProgress &&
                !IsSpawning &&
                EnemyController.ActiveEnemies.Count == 0)
            {
                CompleteCurrentWave();
            }
        }

        public void StartNextWave()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState == GameState.Victory ||
                GameManager.Instance.CurrentState == GameState.Defeat)
                return;

            if (IsSpawning || EnemyController.ActiveEnemies.Count > 0)
                return;

            if (CurrentWaveIndex + 1 >= waves.Count)
            {
                GameManager.Instance.DeclareVictory();
                return;
            }

            CurrentWaveIndex++;
            WaveDataSO wave = waves[CurrentWaveIndex];

            GameManager.Instance.SetGameState(GameState.WaveInProgress);
            GameManager.Instance.BroadcastVox($"WAVE {wave.waveNumber}/{waves.Count} — {wave.waveTitle}: {wave.incomingTransmissionText}");
            OnWaveChanged?.Invoke(wave.waveNumber, waves.Count, wave.waveTitle);

            StartCoroutine(SpawnWaveRoutine(wave));
        }

        private IEnumerator SpawnWaveRoutine(WaveDataSO wave)
        {
            IsSpawning = true;

            for (int g = 0; g < wave.spawnGroups.Count; g++)
            {
                WaveSpawnGroup group = wave.spawnGroups[g];
                if (group.delayBeforeGroup > 0f)
                {
                    yield return new WaitForSeconds(group.delayBeforeGroup);
                }

                if (group.enemyData == null) continue;

                WaypointPath lanePath = group.lane == LaneId.ScrapRoadNorth
                    ? scrapRoadNorthLane
                    : ashGullySouthLane;

                for (int i = 0; i < group.count; i++)
                {
                    if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Defeat)
                    {
                        IsSpawning = false;
                        yield break;
                    }

                    SpawnSingleEnemy(group.enemyData, lanePath);

                    if (i < group.count - 1 && group.spawnInterval > 0f)
                    {
                        yield return new WaitForSeconds(group.spawnInterval);
                    }
                }
            }

            IsSpawning = false;
        }

        private void SpawnSingleEnemy(EnemyDataSO enemyData, WaypointPath path)
        {
            GameObject orkObj = new GameObject($"Ork_{enemyData.displayName}");
            EnemyController controller = orkObj.AddComponent<EnemyController>();
            controller.Initialize(enemyData, path);
        }

        private void CompleteCurrentWave()
        {
            WaveDataSO completedWave = waves[CurrentWaveIndex];
            GameManager.Instance.AddRequisition(completedWave.completionRequisitionBonus);
            GameManager.Instance.AddCommandPoints(completedWave.completionCommandPointBonus);

            if (CurrentWaveIndex >= waves.Count - 1)
            {
                GameManager.Instance.DeclareVictory();
            }
            else
            {
                GameManager.Instance.SetGameState(GameState.Preparing);
                GameManager.Instance.BroadcastVox(
                    $"Wave {completedWave.waveNumber} repelled! +{completedWave.completionRequisitionBonus} Requisition, +{completedWave.completionCommandPointBonus} CP. Prepare for Wave {completedWave.waveNumber + 1}.");
            }
        }
    }
}
