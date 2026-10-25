using System;
using OutpostAegis.Abilities;
using OutpostAegis.Core;
using OutpostAegis.Data;
using OutpostAegis.Towers;
using OutpostAegis.Visuals;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OutpostAegis.Map
{
    /// <summary>
    /// Fixed tactical build emplacement (Kingdom Rush style):
    /// - BunkerPad (Bridge & Canyon low ground)
    /// - CliffLedge (High ground overlooking convergence; +15% range bonus)
    /// - ArtilleryEmplacement (Rear pits for Basilisk Earthshakers)
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BuildNode : MonoBehaviour
    {
        public static event Action<BuildNode> OnNodeSelected;

        public string nodeName = "Tactical Emplacement";
        public BuildNodeType nodeType = BuildNodeType.BunkerPad;

        public TowerController InstalledTower { get; private set; }
        public bool IsOccupied => InstalledTower != null;

        private SpriteRenderer _padRenderer;
        private SpriteRenderer _rangeCircleRenderer;

        private void Awake()
        {
            _padRenderer = GetComponent<SpriteRenderer>();
            if (_padRenderer == null)
                _padRenderer = gameObject.AddComponent<SpriteRenderer>();

            _padRenderer.sprite = PixelSpriteGenerator.GetBuildNodeSprite(nodeType);
            _padRenderer.sortingOrder = 2;

            BoxCollider2D col = GetComponent<BoxCollider2D>();
            col.size = new Vector2(1.15f, 1.15f);

            CreateRangeIndicator();
        }

        private void CreateRangeIndicator()
        {
            GameObject rangeObj = new GameObject("RangeIndicator");
            rangeObj.transform.SetParent(transform, false);
            _rangeCircleRenderer = rangeObj.AddComponent<SpriteRenderer>();
            _rangeCircleRenderer.sprite = PixelSpriteGenerator.GetCircleSprite(96, hollowRing: true);
            _rangeCircleRenderer.color = new Color(0.35f, 0.85f, 1.0f, 0.55f);
            _rangeCircleRenderer.sortingOrder = 14;
            _rangeCircleRenderer.enabled = false;
        }

        private void OnMouseDown()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (CommandAbilityManager.Instance != null && CommandAbilityManager.Instance.ActiveTargetingAbility != null)
                return;

            OnNodeSelected?.Invoke(this);
        }

        public bool CanBuildTower(TowerDataSO towerData)
        {
            if (towerData == null || IsOccupied) return false;
            return (towerData.allowedNodeTypes & nodeType) != 0;
        }

        public bool TryConstructTower(TowerDataSO towerData)
        {
            if (!CanBuildTower(towerData)) return false;
            if (GameManager.Instance == null) return false;

            int baseCost = towerData.tiers[0].cost;
            if (!GameManager.Instance.SpendRequisition(baseCost))
                return false;

            GameObject towerObj = new GameObject($"Tower_{towerData.displayName}");
            towerObj.transform.SetParent(transform, false);
            towerObj.transform.localPosition = Vector3.zero;

            InstalledTower = towerObj.AddComponent<TowerController>();
            InstalledTower.Initialize(towerData, this);

            SoundManager.Play(SfxType.TowerConstruct, 0.72f);
            ShowRangeIndicator(true);
            return true;
        }

        public bool TryUpgradeInstalledTower()
        {
            if (!IsOccupied || !InstalledTower.CanUpgrade) return false;
            if (GameManager.Instance == null) return false;

            int upgradeCost = InstalledTower.GetNextUpgradeCost();
            if (!GameManager.Instance.SpendRequisition(upgradeCost))
                return false;

            InstalledTower.UpgradeToNextTier();
            SoundManager.Play(SfxType.TowerConstruct, 0.82f);
            ShowRangeIndicator(true);
            return true;
        }

        public void SellInstalledTower()
        {
            if (!IsOccupied) return;

            int totalInvested = InstalledTower.Data.GetTotalInvestedCost(InstalledTower.CurrentTierIndex);
            int refund = Mathf.RoundToInt(totalInvested * 0.70f);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddRequisition(refund);
            }

            Destroy(InstalledTower.gameObject);
            InstalledTower = null;
            ShowRangeIndicator(false);
        }

        public void ShowRangeIndicator(bool visible)
        {
            if (_rangeCircleRenderer == null) return;
            if (!visible || !IsOccupied)
            {
                _rangeCircleRenderer.enabled = false;
                return;
            }

            float diameter = InstalledTower.GetEffectiveRange() * 2f;
            _rangeCircleRenderer.transform.localScale = new Vector3(diameter / 3f, diameter / 3f, 1f);
            _rangeCircleRenderer.enabled = true;
        }
    }
}
