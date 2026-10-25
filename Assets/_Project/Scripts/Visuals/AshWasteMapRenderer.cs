using UnityEngine;

namespace OutpostAegis.Visuals
{
    /// <summary>
    /// Constructs and animates the 32 PPU Pixel-Art Ash-Waste battlefield environment:
    /// - Dithered Ash-Waste terrain & elevated Cliff Ledge plateaus
    /// - Animated 4-frame Molten Promethium River tiles
    /// - Riveted Narrow Steel Bridge with Imperial hazard stripes
    /// - Fortified Refinery Bastion Walls & pulsing Central Plasma Reactor Core
    /// </summary>
    public class AshWasteMapRenderer : MonoBehaviour
    {
        private void Start()
        {
            BuildPixelEnvironment();
        }

        public void BuildPixelEnvironment()
        {
            // Clear any legacy flat placeholder strips if present
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name != "AshGully_CoverTrench")
                {
                    if (Application.isPlaying)
                        Destroy(child.gameObject);
                    else
                        DestroyImmediate(child.gameObject);
                }
            }

            // 1. Dithered Ash-Waste & Cliff Ledge Pixel Tilemap Backdrop
            GameObject backdrop = new GameObject("PixelTilemap_AshWasteBackdrop");
            backdrop.transform.SetParent(transform, false);
            backdrop.transform.position = Vector3.zero;
            SpriteRenderer bgSr = backdrop.AddComponent<SpriteRenderer>();
            bgSr.sprite = PixelSpriteGenerator.GetAshWasteBattlefieldSprite();
            bgSr.sortingOrder = -5;

            // 2. Animated Molten Promethium River (tiled vertically from Y = -6 to +6 at X = -2.2)
            GameObject riverRoot = new GameObject("PromethiumRiver_AnimatedTiles");
            riverRoot.transform.SetParent(transform, false);
            Sprite[] riverFrames = PixelSpriteGenerator.GetPromethiumRiverFrames();

            for (int y = -6; y <= 6; y++)
            {
                GameObject tile = new GameObject($"LavaTile_{y}");
                tile.transform.SetParent(riverRoot.transform, false);
                tile.transform.position = new Vector3(-2.2f, y, 0f);
                tile.transform.localScale = new Vector3(1.15f, 1.02f, 1f);

                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sortingOrder = -2;

                PixelSpriteAnimator anim = tile.AddComponent<PixelSpriteAnimator>();
                anim.SetLoopAnimation(riverFrames, fps: 6f, randomStartFrame: false);
            }

            // 3. Narrow Promethium Bridge (Steel Girder Deck + Hazard Chevrons)
            GameObject bridge = new GameObject("NarrowBridge_PixelDeck");
            bridge.transform.SetParent(transform, false);
            bridge.transform.position = new Vector3(-2.2f, 2.2f, 0f);
            SpriteRenderer bridgeSr = bridge.AddComponent<SpriteRenderer>();
            bridgeSr.sprite = PixelSpriteGenerator.GetNarrowBridgeSprite();
            bridgeSr.sortingOrder = 1;

            // 4. Animated Central Plasma Reactor & Refinery Bastion Gate (East side)
            GameObject reactor = new GameObject("CentralPlasmaReactor_AnimatedCore");
            reactor.transform.SetParent(transform, false);
            reactor.transform.position = new Vector3(7.55f, 0.2f, 0f);

            SpriteRenderer reactorSr = reactor.AddComponent<SpriteRenderer>();
            reactorSr.sortingOrder = 4;

            PixelSpriteAnimator reactorAnim = reactor.AddComponent<PixelSpriteAnimator>();
            reactorAnim.SetLoopAnimation(PixelSpriteGenerator.GetPlasmaReactorFrames(), fps: 5f, randomStartFrame: false);
        }
    }
}
