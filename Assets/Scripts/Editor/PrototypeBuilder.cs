using System.IO;
using GeometryDash.Game;
using GeometryDash.Obstacles;
using GeometryDash.Player;
using GeometryDash.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GeometryDash.EditorTools
{
    /// <summary>
    /// Generates every asset the prototype needs (sprites, prefabs, scenes, UI,
    /// build settings). Re-running it regenerates MainMenu and Level01.
    /// </summary>
    public static class PrototypeBuilder
    {
        private const string ArtFolder = "Assets/Art";
        private const string SpritesFolder = ArtFolder + "/Sprites";
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string ScenesFolder = "Assets/Scenes";
        private const string MainMenuScenePath = ScenesFolder + "/MainMenu.unity";
        private const string LevelScenePath = ScenesFolder + "/Level01.unity";
        private const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private const int PixelsPerUnit = 64;
        private const float LevelEndX = 195f;

        private static readonly Color Background = new Color(0.04f, 0.02f, 0.10f);
        private static readonly Color NeonCyan = new Color(0.0f, 0.95f, 1.0f);
        private static readonly Color NeonYellow = new Color(1.0f, 0.92f, 0.1f);
        private static readonly Color NeonPink = new Color(1.0f, 0.15f, 0.55f);
        private static readonly Color NeonGreen = new Color(0.2f, 1.0f, 0.4f);
        private static readonly Color NeonBlue = new Color(0.25f, 0.35f, 1.0f);
        private static readonly Color PanelDim = new Color(0f, 0f, 0f, 0.75f);

        private static Sprite squareSprite;
        private static Sprite outlinedSprite;
        private static Sprite triangleSprite;
        private static Material unlitMaterial;
        private static TMP_FontAsset font;

        [MenuItem("Tools/Neon Dash/Build Prototype")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            int groundLayer = EnsureLayer("Ground");
            int playerLayer = EnsureLayer("Player");

            CreateFolders();
            LoadSharedAssets();

            GameObject playerPrefab = BuildPlayerPrefab(playerLayer, groundLayer);
            GameObject blockPrefab = BuildBlockPrefab();
            GameObject spikePrefab = BuildSpikePrefab();
            GameObject goalPrefab = BuildGoalPrefab();

            BuildMainMenuScene();
            BuildLevelScene(playerPrefab, blockPrefab, spikePrefab, goalPrefab, groundLayer);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(LevelScenePath, true)
            };

            EditorSceneManager.OpenScene(MainMenuScenePath);
            Debug.Log("[Neon Dash] Prototype built: MainMenu + Level01 added to Build Settings.");
        }

        #region Shared assets

        private static void CreateFolders()
        {
            foreach (string folder in new[] { ArtFolder, SpritesFolder, PrefabsFolder, ScenesFolder })
            {
                if (AssetDatabase.IsValidFolder(folder)) continue;
                string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
            }
        }

        private static void LoadSharedAssets()
        {
            squareSprite = CreateSprite("Square", (x, y, s) => true, borderPixels: 0);
            outlinedSprite = CreateSprite("SquareOutlined", (x, y, s) => true, borderPixels: 5);
            triangleSprite = CreateSprite("Triangle", (x, y, s) =>
            {
                float halfWidth = (1f - (y + 0.5f) / s) * s * 0.5f;
                return Mathf.Abs(x + 0.5f - s * 0.5f) <= halfWidth;
            }, borderPixels: 0);

            unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        }

        private delegate bool PixelMask(int x, int y, int size);

        /// <summary>White sprite; border pixels are full white, interior is dimmed to fake a neon outline.</summary>
        private static Sprite CreateSprite(string name, PixelMask mask, int borderPixels)
        {
            const int size = PixelsPerUnit;
            string path = $"{SpritesFolder}/{name}.png";

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inside = mask(x, y, size);
                bool isBorder = x < borderPixels || y < borderPixels || x >= size - borderPixels || y >= size - borderPixels;
                float value = borderPixels > 0 && !isBorder ? 0.22f : 1f;
                texture.SetPixel(x, y, inside ? new Color(value, value, value, 1f) : Color.clear);
            }
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // required for tiled draw mode
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0) return existing;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(layer.stringValue)) continue;
                layer.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
            throw new System.InvalidOperationException($"No free layer slot for '{layerName}'.");
        }

        #endregion

        #region Prefabs

        private static GameObject BuildPlayerPrefab(int playerLayer, int groundLayer)
        {
            var root = new GameObject("Player") { layer = playerLayer };

            var body = root.AddComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.freezeRotation = true;

            const string materialPath = ArtFolder + "/PlayerNoFriction.physicsMaterial2D";
            var noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(materialPath);
            if (noFriction == null)
            {
                noFriction = new PhysicsMaterial2D("PlayerNoFriction") { friction = 0f, bounciness = 0f };
                AssetDatabase.CreateAsset(noFriction, materialPath);
            }
            var box = root.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;
            box.sharedMaterial = noFriction;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            AddSprite(visual, squareSprite, NeonYellow, 10);
            var core = new GameObject("Core");
            core.transform.SetParent(visual.transform, false);
            core.transform.localScale = Vector3.one * 0.5f;
            AddSprite(core, squareSprite, NeonCyan, 11);

            var controller = root.AddComponent<PlayerController>();
            root.AddComponent<PlayerCollision>();
            var so = new SerializedObject(controller);
            so.FindProperty("groundLayers").intValue = 1 << groundLayer;
            so.FindProperty("visual").objectReferenceValue = visual.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, "Player");
        }

        private static GameObject BuildBlockPrefab()
        {
            var root = new GameObject("Block");
            AddSprite(root, outlinedSprite, NeonCyan, 5);
            root.AddComponent<BoxCollider2D>().size = Vector2.one;
            SetObstacleType(root.AddComponent<Obstacle>(), ObstacleType.Block);
            return SavePrefab(root, "Block");
        }

        private static GameObject BuildSpikePrefab()
        {
            var root = new GameObject("Spike");
            AddSprite(root, triangleSprite, NeonPink, 5);
            // Forgiving hitbox, smaller than the visual triangle.
            var polygon = root.AddComponent<PolygonCollider2D>();
            polygon.isTrigger = true;
            polygon.SetPath(0, new[] { new Vector2(-0.25f, -0.5f), new Vector2(0f, 0.3f), new Vector2(0.25f, -0.5f) });
            SetObstacleType(root.AddComponent<Obstacle>(), ObstacleType.Spike);
            return SavePrefab(root, "Spike");
        }

        private static GameObject BuildGoalPrefab()
        {
            var root = new GameObject("Goal");
            var trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1f, 30f);
            trigger.offset = new Vector2(0f, 15f);
            root.AddComponent<LevelGoal>();

            var bar = new GameObject("Bar");
            bar.transform.SetParent(root.transform, false);
            bar.transform.localPosition = new Vector3(0f, 6f, 0f);
            bar.transform.localScale = new Vector3(0.3f, 12f, 1f);
            AddSprite(bar, squareSprite, NeonGreen, 4);
            return SavePrefab(root, "Goal");
        }

        private static void SetObstacleType(Obstacle obstacle, ObstacleType type)
        {
            var so = new SerializedObject(obstacle);
            so.FindProperty("type").enumValueIndex = (int)type;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject SavePrefab(GameObject root, string name)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsFolder}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static SpriteRenderer AddSprite(GameObject target, Sprite sprite, Color color, int sortingOrder)
        {
            var renderer = target.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            if (unlitMaterial != null) renderer.sharedMaterial = unlitMaterial;
            return renderer;
        }

        #endregion

        #region Scenes

        private static Camera CreateCamera(Vector3 position)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = position;
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            go.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateGround(float startX, float endX, int groundLayer)
        {
            float width = endX - startX;
            var ground = new GameObject("Ground") { layer = groundLayer };
            ground.transform.position = new Vector3(startX + width * 0.5f, -3f, 0f);
            SpriteRenderer renderer = AddSprite(ground, outlinedSprite, NeonBlue, 0);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(width, 6f);
            ground.AddComponent<BoxCollider2D>().size = renderer.size;

            var edge = new GameObject("GroundEdge");
            edge.transform.SetParent(ground.transform, false);
            edge.transform.localPosition = new Vector3(0f, 3f, 0f);
            edge.transform.localScale = new Vector3(width, 0.08f, 1f);
            AddSprite(edge, squareSprite, Color.white, 1);
        }

        private static void CreateBackgroundGrid(float startX, float endX)
        {
            float width = endX - startX;
            var grid = new GameObject("BackgroundGrid");
            grid.transform.position = new Vector3(startX + width * 0.5f, 6f, 5f);
            grid.transform.localScale = Vector3.one * 3f;
            SpriteRenderer renderer = AddSprite(grid, outlinedSprite, new Color(0.35f, 0.2f, 0.8f, 0.18f), -10);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(width / 3f, 8f);
        }

        private static void BuildMainMenuScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Vector3(0f, 2.5f, -10f));
            CreateBackgroundGrid(-20f, 20f);

            Canvas canvas = CreateCanvas();
            RectTransform root = (RectTransform)canvas.transform;
            CreateText(root, "NEON DASH", 140, NeonCyan, new Vector2(0f, 220f), FontStyles.Bold);
            CreateText(root, "Space / Click to jump", 40, Color.white, new Vector2(0f, 90f));
            Button play = CreateButton(root, "PLAY", new Vector2(0f, -60f), NeonGreen);
            Button exit = CreateButton(root, "EXIT", new Vector2(0f, -200f), NeonPink);

            var menu = new GameObject("MainMenu").AddComponent<MainMenuController>();
            var so = new SerializedObject(menu);
            so.FindProperty("playButton").objectReferenceValue = play;
            so.FindProperty("exitButton").objectReferenceValue = exit;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), MainMenuScenePath);
        }

        private static void BuildLevelScene(GameObject playerPrefab, GameObject blockPrefab, GameObject spikePrefab,
                                            GameObject goalPrefab, int groundLayer)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = CreateCamera(new Vector3(6f, 3.5f, -10f));
            CreateBackgroundGrid(-20f, LevelEndX + 40f);
            CreateGround(-20f, LevelEndX + 40f, groundLayer);

            var startPoint = new GameObject("StartPoint").transform;
            startPoint.position = new Vector3(0f, 0.5f, 0f);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = startPoint.position;

            var obstaclesRoot = new GameObject("Obstacles").transform;
            PlaceLevelLayout(obstaclesRoot, blockPrefab, spikePrefab);

            var goal = (GameObject)PrefabUtility.InstantiatePrefab(goalPrefab);
            goal.transform.position = new Vector3(LevelEndX, 0f, 0f);

            var cameraController = camera.gameObject.AddComponent<CameraController>();
            SetReference(cameraController, "target", player.transform);

            var levelManager = new GameObject("LevelManager").AddComponent<LevelManager>();
            var levelSo = new SerializedObject(levelManager);
            levelSo.FindProperty("player").objectReferenceValue = player.GetComponent<PlayerController>();
            levelSo.FindProperty("startPoint").objectReferenceValue = startPoint;
            levelSo.FindProperty("goal").objectReferenceValue = goal.GetComponent<LevelGoal>();
            levelSo.FindProperty("obstaclesRoot").objectReferenceValue = obstaclesRoot;
            levelSo.ApplyModifiedPropertiesWithoutUndo();

            var gameManager = new GameObject("GameManager").AddComponent<GameManager>();
            var gameSo = new SerializedObject(gameManager);
            gameSo.FindProperty("player").objectReferenceValue = player.GetComponent<PlayerController>();
            gameSo.FindProperty("playerCollision").objectReferenceValue = player.GetComponent<PlayerCollision>();
            gameSo.FindProperty("levelManager").objectReferenceValue = levelManager;
            gameSo.ApplyModifiedPropertiesWithoutUndo();

            BuildGameplayUI(gameManager, levelManager);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), LevelScenePath);
        }

        /// <summary>
        /// Hand-tuned layout for speed 10 / jump height 2.1 (≈4.6 units per jump).
        /// 'S' = spike, 'D' = double spike, 'B' = block.
        /// </summary>
        private static void PlaceLevelLayout(Transform parent, GameObject blockPrefab, GameObject spikePrefab)
        {
            (float x, char kind)[] layout =
            {
                (15, 'S'), (24, 'S'), (32, 'B'), (41, 'D'), (50, 'B'), (58, 'S'), (66, 'B'),
                (74, 'D'), (83, 'B'), (91, 'S'), (99, 'D'), (108, 'B'), (116, 'S'), (124, 'B'),
                (132, 'D'), (141, 'B'), (149, 'S'), (157, 'D'), (166, 'B'), (174, 'S'), (182, 'D')
            };

            foreach ((float x, char kind) in layout)
            {
                switch (kind)
                {
                    case 'B': Place(blockPrefab, parent, x); break;
                    case 'S': Place(spikePrefab, parent, x); break;
                    case 'D': Place(spikePrefab, parent, x); Place(spikePrefab, parent, x + 1f); break;
                }
            }
        }

        private static void Place(GameObject prefab, Transform parent, float x)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = new Vector3(x, 0.5f, 0f);
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(propertyName).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion

        #region UI

        private static void BuildGameplayUI(GameManager gameManager, LevelManager levelManager)
        {
            Canvas canvas = CreateCanvas();
            RectTransform root = (RectTransform)canvas.transform;

            RectTransform hud = CreatePanel(root, "HUD", Color.clear);
            TMP_Text hudProgress = CreateText(hud, "0%", 60, Color.white, Vector2.zero, FontStyles.Bold);
            Anchor((RectTransform)hudProgress.transform, new Vector2(0.5f, 1f), new Vector2(0f, -60f));
            Button pause = CreateButton(hud, "II", Vector2.zero, NeonCyan, new Vector2(110f, 110f));
            Anchor((RectTransform)pause.transform, new Vector2(1f, 1f), new Vector2(-80f, -80f));

            RectTransform pausePanel = CreatePanel(root, "PausePanel", PanelDim);
            CreateText(pausePanel, "PAUSED", 120, NeonCyan, new Vector2(0f, 200f), FontStyles.Bold);
            Button resume = CreateButton(pausePanel, "RESUME", new Vector2(0f, 0f), NeonGreen);
            Button pauseMenu = CreateButton(pausePanel, "MAIN MENU", new Vector2(0f, -140f), NeonPink);

            RectTransform gameOverPanel = CreatePanel(root, "GameOverPanel", PanelDim);
            CreateText(gameOverPanel, "Game Over", 140, NeonPink, new Vector2(0f, 250f), FontStyles.Bold);
            TMP_Text gameOverProgress = CreateText(gameOverPanel, "Progress: 0%", 56, Color.white, new Vector2(0f, 110f));
            Button gameOverRetry = CreateButton(gameOverPanel, "RETRY", new Vector2(0f, -40f), NeonGreen);
            Button gameOverMenu = CreateButton(gameOverPanel, "MAIN MENU", new Vector2(0f, -180f), NeonCyan);

            RectTransform completePanel = CreatePanel(root, "LevelCompletePanel", PanelDim);
            CreateText(completePanel, "Level Complete", 130, NeonGreen, new Vector2(0f, 250f), FontStyles.Bold);
            TMP_Text completeProgress = CreateText(completePanel, "Completed: 100%", 56, Color.white, new Vector2(0f, 110f));
            Button completeRetry = CreateButton(completePanel, "RETRY", new Vector2(0f, -40f), NeonGreen);
            Button completeMenu = CreateButton(completePanel, "MAIN MENU", new Vector2(0f, -180f), NeonCyan);

            pausePanel.gameObject.SetActive(false);
            gameOverPanel.gameObject.SetActive(false);
            completePanel.gameObject.SetActive(false);

            var ui = canvas.gameObject.AddComponent<UIManager>();
            var so = new SerializedObject(ui);
            so.FindProperty("gameManager").objectReferenceValue = gameManager;
            so.FindProperty("levelManager").objectReferenceValue = levelManager;
            so.FindProperty("hudPanel").objectReferenceValue = hud.gameObject;
            so.FindProperty("pausePanel").objectReferenceValue = pausePanel.gameObject;
            so.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel.gameObject;
            so.FindProperty("levelCompletePanel").objectReferenceValue = completePanel.gameObject;
            so.FindProperty("hudProgressText").objectReferenceValue = hudProgress;
            so.FindProperty("gameOverProgressText").objectReferenceValue = gameOverProgress;
            so.FindProperty("levelCompleteProgressText").objectReferenceValue = completeProgress;
            so.FindProperty("pauseButton").objectReferenceValue = pause;
            so.FindProperty("resumeButton").objectReferenceValue = resume;
            SetArray(so.FindProperty("retryButtons"), gameOverRetry, completeRetry);
            SetArray(so.FindProperty("mainMenuButtons"), pauseMenu, gameOverMenu, completeMenu);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(SerializedProperty array, params Object[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.AddComponent<InputSystemUIInputModule>();
            return canvas;
        }

        private static RectTransform CreatePanel(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            if (color.a > 0f) go.AddComponent<Image>().color = color;
            return rect;
        }

        private static TMP_Text CreateText(RectTransform parent, string content, float fontSize, Color color,
                                           Vector2 anchoredPosition, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(1400f, fontSize * 1.4f);
            rect.anchoredPosition = anchoredPosition;

            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false; // never block jump clicks
            return text;
        }

        private static Button CreateButton(RectTransform parent, string label, Vector2 anchoredPosition, Color accent,
                                           Vector2? size = null)
        {
            var go = new GameObject($"{label}Button", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size ?? new Vector2(460f, 110f);
            rect.anchoredPosition = anchoredPosition;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.08f, 0.05f, 0.18f, 0.95f);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = accent;
            outline.effectDistance = new Vector2(4f, -4f);

            var button = go.AddComponent<Button>();

            TMP_Text text = CreateText(rect, label, 52, accent, Vector2.zero, FontStyles.Bold);
            var textRect = (RectTransform)text.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            return button;
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
        }

        #endregion
    }
}
