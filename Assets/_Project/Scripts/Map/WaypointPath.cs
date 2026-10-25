using System.Collections.Generic;
using OutpostAegis.Data;
using UnityEngine;

namespace OutpostAegis.Map
{
    /// <summary>
    /// Defines a lane path (The Scrap-Road or The Ash-Gully) converging toward the Base Gate.
    /// Supports shortcut waypoint skipping for fast terrain-bypassing units (Stormboyz / Warbikers).
    /// </summary>
    public class WaypointPath : MonoBehaviour
    {
        public LaneId laneId = LaneId.ScrapRoadNorth;
        public string laneDisplayName = "The Scrap-Road";
        public Color gizmoColor = new Color(0.95f, 0.55f, 0.15f);
        public List<Transform> waypoints = new List<Transform>();

        [Tooltip("Optional waypoint indices that represent rough canyon obstacles bypassed by Stormboyz/Warbikers.")]
        public List<int> roughTerrainWaypointIndices = new List<int>();

        public int Count => waypoints.Count;

        public Vector3 GetWaypointPosition(int index)
        {
            if (waypoints.Count == 0) return transform.position;
            index = Mathf.Clamp(index, 0, waypoints.Count - 1);
            return waypoints[index] != null ? waypoints[index].position : transform.position;
        }

        public bool IsRoughTerrainSegment(int waypointIndex)
        {
            return roughTerrainWaypointIndices != null && roughTerrainWaypointIndices.Contains(waypointIndex);
        }

        /// <summary>
        /// Calculates remaining path distance to the Central Plasma Reactor for "First" target priority.
        /// </summary>
        public float CalculateRemainingDistance(Vector3 currentPos, int currentTargetWaypointIndex)
        {
            if (waypoints.Count == 0 || currentTargetWaypointIndex >= waypoints.Count)
                return 0f;

            float dist = Vector3.Distance(currentPos, GetWaypointPosition(currentTargetWaypointIndex));
            for (int i = currentTargetWaypointIndex; i < waypoints.Count - 1; i++)
            {
                dist += Vector3.Distance(GetWaypointPosition(i), GetWaypointPosition(i + 1));
            }
            return dist;
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Count < 2) return;
            Gizmos.color = gizmoColor;
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                if (waypoints[i] != null && waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                    Gizmos.DrawSphere(waypoints[i].position, 0.12f);
                }
            }
            if (waypoints[waypoints.Count - 1] != null)
            {
                Gizmos.DrawSphere(waypoints[waypoints.Count - 1].position, 0.18f);
            }
        }
    }
}
