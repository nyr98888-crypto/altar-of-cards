using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// One-click builder for the 2.5D card battle sandbox scene.
/// Menu: Tools → Altar of Cards → Create / Refresh Battle Sandbox Scene
/// </summary>
public static class BattleSandboxSetup
{
    private const string ScenePath = "Assets/Scenes/BattleSandbox.unity";
    private const string LeftSpritePath = "Assets/Sprites/Fighters/Fighter_Left.png";
    private const string RightSpritePath = "Assets/Sprites/Fighters/Fighter_Right.png";
    private const string BlackMaterialPath = "Assets/Materials/BlackBackground.mat";

    [MenuItem("Tools/Altar of Cards/Create / Refresh Battle Sandbox Scene")]
    public static void CreateOrRefreshScene()
    {
        EnsureFolders();
        ConfigureFighterSprites();
        Material blackMat = EnsureBlackMaterial();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraGo = new GameObject("Main Camera");
            mainCamera = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            cameraGo.tag = "MainCamera";
        }

        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = Color.black;
        mainCamera.orthographic = false;
        mainCamera.fieldOfView = 60f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 100f;

        CameraController cameraController = mainCamera.GetComponent<CameraController>();
        if (cameraController == null)
        {
            cameraController = mainCamera.gameObject.AddComponent<CameraController>();
        }

        SerializedObject cameraSo = new SerializedObject(cameraController);
        cameraSo.FindProperty("fov").floatValue = 60f;
        cameraSo.FindProperty("cameraPosition").vector3Value = new Vector3(0f, 0f, -5f);
        cameraSo.FindProperty("cameraRotation").vector3Value = Vector3.zero;
        cameraSo.ApplyModifiedPropertiesWithoutUndo();

        // Remove default directional light glare for a flat card look, keep one soft fill.
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            Object.DestroyImmediate(light.gameObject);
        }

        GameObject lightGo = new GameObject("Fill Light");
        Light fill = lightGo.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = Color.white;
        fill.intensity = 1.2f;
        fill.shadows = LightShadows.None;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "BlackBackground";
        Object.DestroyImmediate(background.GetComponent<MeshCollider>());
        background.transform.position = new Vector3(0f, 0f, 2f);
        background.transform.localScale = new Vector3(16f, 9f, 1f);
        MeshRenderer bgRenderer = background.GetComponent<MeshRenderer>();
        bgRenderer.sharedMaterial = blackMat;
        bgRenderer.shadowCastingMode = ShadowCastingMode.Off;
        bgRenderer.receiveShadows = false;

        Sprite leftSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LeftSpritePath);
        Sprite rightSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RightSpritePath);

        FighterCard left = CreateFighter(
            "LeftFighter",
            leftSprite,
            idlePosition: new Vector3(-2.4f, 0f, 0f),
            idleRotation: new Vector3(0f, 15f, 0f),
            attackDirection: Vector3.right,
            sortingOrder: 10);

        FighterCard right = CreateFighter(
            "RightFighter",
            rightSprite,
            idlePosition: new Vector3(2.4f, 0f, 0f),
            idleRotation: new Vector3(0f, -15f, 0f),
            attackDirection: Vector3.left,
            sortingOrder: 10);

        GameObject managerGo = new GameObject("BattleTestManager");
        BattleTestManager manager = managerGo.AddComponent<BattleTestManager>();
        SerializedObject managerSo = new SerializedObject(manager);
        managerSo.FindProperty("leftFighter").objectReferenceValue = left;
        managerSo.FindProperty("rightFighter").objectReferenceValue = right;
        managerSo.FindProperty("clashPoint").vector3Value = Vector3.zero;
        managerSo.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        bool alreadyListed = false;
        foreach (EditorBuildSettingsScene buildScene in buildScenes)
        {
            if (buildScene.path == ScenePath)
            {
                alreadyListed = true;
                break;
            }
        }

        if (!alreadyListed)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(buildScenes)
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }

        Selection.activeGameObject = managerGo;
        Debug.Log("Battle Sandbox scene ready at " + ScenePath +
                  "\nControls — Left: A/S/D/F | Right: J/K/L/; | Clash: Space");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scripts"))
        {
            AssetDatabase.CreateFolder("Assets", "Scripts");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Scripts/Battle"))
        {
            AssetDatabase.CreateFolder("Assets/Scripts", "Battle");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
        {
            AssetDatabase.CreateFolder("Assets", "Sprites");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Sprites/Fighters"))
        {
            AssetDatabase.CreateFolder("Assets/Sprites", "Fighters");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
    }

    private static void ConfigureFighterSprites()
    {
        ConfigureSpriteImporter(LeftSpritePath);
        ConfigureSpriteImporter(RightSpritePath);
        AssetDatabase.ImportAsset(LeftSpritePath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(RightSpritePath, ImportAssetOptions.ForceUpdate);
    }

    private static void ConfigureSpriteImporter(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("Missing fighter sprite at " + path);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.spritePixelsPerUnit = 500f;
        importer.SaveAndReimport();
    }

    private static Material EnsureBlackMaterial()
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(BlackMaterialPath);
        if (existing != null)
        {
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material mat = new Material(shader);
        mat.name = "BlackBackground";
        mat.color = Color.black;

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", Color.black);
        }

        AssetDatabase.CreateAsset(mat, BlackMaterialPath);
        return mat;
    }

    private static FighterCard CreateFighter(
        string name,
        Sprite sprite,
        Vector3 idlePosition,
        Vector3 idleRotation,
        Vector3 attackDirection,
        int sortingOrder)
    {
        GameObject root = new GameObject(name);
        FighterCard card = root.AddComponent<FighterCard>();

        GameObject visual = new GameObject("CardSprite");
        visual.transform.SetParent(root.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = sortingOrder;

        SerializedObject so = new SerializedObject(card);
        so.FindProperty("cardSprite").objectReferenceValue = renderer;
        so.FindProperty("defaultColor").colorValue = Color.white;
        so.FindProperty("telegraphColor").colorValue = new Color(1f, 0.55f, 0.2f, 1f);
        so.FindProperty("idlePosition").vector3Value = idlePosition;
        so.FindProperty("idleRotation").vector3Value = idleRotation;
        so.FindProperty("idleScale").vector3Value = new Vector3(1.8f, 1.8f, 1.8f);
        so.FindProperty("attackDirection").vector3Value = attackDirection;
        so.FindProperty("bobSpeed").floatValue = 2f;
        so.FindProperty("bobHeight").floatValue = 0.08f;
        so.FindProperty("lungeDistance").floatValue = 1.2f;
        so.FindProperty("lungeDuration").floatValue = 0.12f;
        so.FindProperty("recoveryDuration").floatValue = 0.25f;
        so.FindProperty("attackTilt").vector3Value = new Vector3(20f, 0f, 0f);
        so.FindProperty("recoilDistance").floatValue = 0.4f;
        so.FindProperty("recoilDuration").floatValue = 0.15f;
        so.FindProperty("wobbleShakeStrength").floatValue = 10f;
        so.ApplyModifiedPropertiesWithoutUndo();

        root.transform.localPosition = idlePosition;
        root.transform.localRotation = Quaternion.Euler(idleRotation);
        root.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);

        return card;
    }
}
