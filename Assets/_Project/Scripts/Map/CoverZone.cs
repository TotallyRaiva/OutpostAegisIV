using System.Collections.Generic;
using UnityEngine;

namespace OutpostAegis.Map
{
    /// <summary>
    /// Marks ash-waste canyon rubble or trench sections where Ork infantry gain partial cover
    /// (ignored by Hellhound Flame-Bunkers and Basilisk Artillery).
    /// </summary>
    public class CoverZone : MonoBehaviour
    {
        public static readonly List<CoverZone> ActiveZones = new List<CoverZone>();

        public string zoneName = "Ash-Gully Scrap Trench";
        public float radius = 2.0f;

        private void OnEnable()
        {
            if (!ActiveZones.Contains(this)) ActiveZones.Add(this);
        }

        private void OnDisable()
        {
            ActiveZones.Remove(this);
        }

        public static bool IsPositionInCover(Vector3 worldPos)
        {
            for (int i = 0; i < ActiveZones.Count; i++)
            {
                CoverZone zone = ActiveZones[i];
                if (zone != null && Vector2.Distance(worldPos, zone.transform.position) <= zone.radius)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.6f, 0.5f, 0.3f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
