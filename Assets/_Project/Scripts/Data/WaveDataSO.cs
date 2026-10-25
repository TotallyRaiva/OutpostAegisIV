using System;
using System.Collections.Generic;
using UnityEngine;

namespace OutpostAegis.Data
{
    [Serializable]
    public struct WaveSpawnGroup
    {
        public string groupNote;
        public EnemyDataSO enemyData;
        public LaneId lane;
        public int count;
        [Tooltip("Seconds between each Ork in this group.")]
        public float spawnInterval;
        [Tooltip("Delay in seconds before this group starts spawning after the previous group starts.")]
        public float delayBeforeGroup;
    }

    [CreateAssetMenu(fileName = "WaveData_", menuName = "Outpost Aegis-IV/Wave Data")]
    public class WaveDataSO : ScriptableObject
    {
        public int waveNumber = 1;
        public string waveTitle = "Scout Party";
        [TextArea(2, 3)]
        public string incomingTransmissionText = "Vox-Caster: Greenskin vanguard sighted in the Scrap-Road!";
        public int completionRequisitionBonus = 50;
        public int completionCommandPointBonus = 1;
        public List<WaveSpawnGroup> spawnGroups = new List<WaveSpawnGroup>();
    }
}
