using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Creates the finished lab from the curated assets. Runs only with a pending marker.</summary>
[InitializeOnLoad]
public static class Lab1ProjectBuilder
{
    private const string Root = "Assets/_Project/";
    private const string Pending = "Library/Lab1FullSetup.pending";
    private static TMP_FontAsset font;
    private static Material spriteMaterial;

    static Lab1ProjectBuilder() { EditorApplication.delayCall += RunPending; }

    private static void RunPending()
    {
        if (!File.Exists(Pending)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += RunPending; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.isPlaying = false; EditorApplication.delayCall += RunPending; return; }
        File.Delete(Pending);
        try { BuildProject(); }
        catch (Exception ex)
        {
            File.WriteAllText("Library/Lab1SetupError.txt", ex.ToString());
            Debug.LogException(ex);
        }
    }

    [MenuItem("Tools/Space Explorer/Rebuild Complete Lab 1")]
    public static void BuildProject()
    {
        // Preserve the saved and currently edited scenes before regenerating the lab.
        EditorSceneManager.SaveOpenScenes();
        string backup = Path.GetFullPath("../Backups/Lab1-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(backup);
        foreach (string path in Directory.GetFiles(Root + "Scenes", "*.unity"))
            File.Copy(path, Path.Combine(backup, Path.GetFileName(path)), true);
        foreach (string path in Directory.GetFiles(Root + "Prefabs", "*.prefab"))
            File.Copy(path, Path.Combine(backup, Path.GetFileName(path)), true);

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null) throw new Exception("TMP Essential Resources font not available.");
        spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/SpaceSprites.mat");
        if (spriteMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            spriteMaterial = new Material(shader);
            AssetDatabase.CreateAsset(spriteMaterial, Root + "Materials/SpaceSprites.mat");
        }
        Lab1AssetImporter.ConfigureExistingSprites();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject laser = SpriteObject("Laser", "Lasers/laserBlue01", Vector3.zero, 5);
        Body(laser);
        laser.AddComponent<BoxCollider2D>().isTrigger = true;
        laser.AddComponent<LaserProjectile>();
        GameObject laserPrefab = PrefabUtility.SaveAsPrefabAsset(laser, Root + "Prefabs/Laser.prefab");
        UnityEngine.Object.DestroyImmediate(laser);

        GameObject rock = SpriteObject("Asteroid", "Asteroids/meteorBrown_big1", Vector3.zero, 2);
        Body(rock).angularDamping = 0;
        rock.AddComponent<CircleCollider2D>().radius = 0.42f;
        rock.AddComponent<Asteroid>();
        GameObject rockPrefab = PrefabUtility.SaveAsPrefabAsset(rock, Root + "Prefabs/Asteroid.prefab");
        UnityEngine.Object.DestroyImmediate(rock);

        GameObject star = SpriteObject("Star", "Collectibles/star_gold", Vector3.zero, 3);
        star.AddComponent<CircleCollider2D>().isTrigger = true;
        star.AddComponent<StarCollectible>();
        GameObject starPrefab = PrefabUtility.SaveAsPrefabAsset(star, Root + "Prefabs/Star.prefab");
        UnityEngine.Object.DestroyImmediate(star);

        MakeGameplay(laserPrefab, rockPrefab, starPrefab);
        MakeMenu();
        MakeEndGame();
        string[] paths = { Root + "Scenes/MainMenu.unity", Root + "Scenes/Gameplay.unity", Root + "Scenes/EndGame.unity" };
        EditorBuildSettings.scenes = paths.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        PlayerSettings.productName = "Space Explorer";
        PlayerSettings.companyName = "PRU213 Lab";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(paths[0]);
        File.WriteAllText("Library/Lab1SetupComplete.txt", "Created 3 scenes, 3 prefabs, HUD, menus, instructions, sounds and scene transitions. Backup: " + backup);

        string buildPath = Path.GetFullPath("../Builds/SpaceExplorer/SpaceExplorer.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(buildPath));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = paths, locationPathName = buildPath, target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        File.WriteAllText("Library/Lab1BuildReport.txt", report.summary.result + " | Errors: " + report.summary.totalErrors + " | " + buildPath);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed; inspect Unity Console.");
    }

    private static void MakeGameplay(GameObject laser, GameObject rock, GameObject star)
    {
        NewScene("darkPurple");
        GameObject player = SpriteObject("Player", "Player/playerShip1_blue", new Vector3(0,-3,0), 10);
        Body(player).constraints = RigidbodyConstraints2D.FreezeRotation;
        player.AddComponent<PolygonCollider2D>();
        PlayerController controller = player.AddComponent<PlayerController>();
        Transform firePoint = new GameObject("FirePoint").transform;
        firePoint.SetParent(player.transform, false);
        firePoint.localPosition = new Vector3(0,0.5f,0);
        Set(controller, "laserPrefab", laser.GetComponent<LaserProjectile>());
        Set(controller, "firePoint", firePoint);
        AsteroidSpawner rocks = new GameObject("AsteroidSpawner").AddComponent<AsteroidSpawner>();
        Set(rocks, "asteroidPrefab", rock.GetComponent<Asteroid>()); Set(rocks,"player",player.transform);
        StarSpawner stars = new GameObject("StarSpawner").AddComponent<StarSpawner>();
        Set(stars,"starPrefab",star.GetComponent<StarCollectible>()); Set(stars,"player",player.transform);
        Canvas canvas = MakeCanvas();
        TextMeshProUGUI score = Text(canvas.transform,"ScoreText","SCORE: 0",32,new Vector2(0,315),new Vector2(1100,60));
        score.alignment = TextAlignmentOptions.Left;
        Text(canvas.transform,"Controls","WASD / ARROWS   MOVE     |     SPACE   FIRE",18,new Vector2(0,-330),new Vector2(1100,40));
        GameManager manager = new GameObject("GameManager").AddComponent<GameManager>();
        Set(manager,"scoreText",score);
        Set(manager,"laserSound",Clip("Laser")); Set(manager,"collectSound",Clip("CollectStar"));
        Set(manager,"explosionSound",Clip("Explosion")); Set(manager,"gameOverSound",Clip("GameOver"));
        manager.GetComponent<AudioSource>().volume = 0.3f;
        manager.GetComponent<AudioSource>().playOnAwake = false;
        SceneUI ui = AddSceneUI(canvas);
        Fade(canvas,ui);
        Save("Gameplay");
    }

    private static void MakeMenu()
    {
        NewScene("blue"); Canvas canvas = MakeCanvas(); SceneUI ui = AddSceneUI(canvas);
        SpriteObject("HeroShip","Player/playerShip1_blue",new Vector3(-5,0,0),10).transform.localScale = Vector3.one * 2.8f;
        SpriteObject("MenuAsteroid","Asteroids/meteorGrey_big1",new Vector3(5,2,0),2).transform.localScale = Vector3.one * 2;
        Text(canvas.transform,"Title","SPACE EXPLORER",64,new Vector2(0,225),new Vector2(1100,100));
        Text(canvas.transform,"Subtitle","DODGE ASTEROIDS. COLLECT STARS. EXPLORE SPACE.",20,new Vector2(0,145),new Vector2(1100,50));
        Button(canvas.transform,"PLAY",new Vector2(0,35),ui.PlayGame);
        Button(canvas.transform,"INSTRUCTIONS",new Vector2(0,-50),ui.ShowInstructions);
        Button(canvas.transform,"QUIT",new Vector2(0,-135),ui.QuitGame);
        Text(canvas.transform,"Credits","PRU213 LAB 1   |   ART & AUDIO: KENNEY (CC0)",16,new Vector2(0,-325),new Vector2(1100,40));
        GameObject panel = Panel(canvas.transform,"Instructions",new Color(0.015f,0.025f,0.07f,0.98f));
        Text(panel.transform,"Heading","HOW TO PLAY",44,new Vector2(0,220),new Vector2(1000,80));
        Text(panel.transform,"InstructionsText","WASD / ARROW KEYS  -  Move your spaceship\nSPACE (hold)  -  Fire lasers\n\nCollect a star: +10 points\nLasers destroy asteroids\nCrash into an asteroid: -5 points and GAME OVER\n\nStay inside the screen. Survive and collect stars!",25,new Vector2(0,15),new Vector2(1000,330));
        Button(panel.transform,"BACK",new Vector2(0,-235),ui.HideInstructions);
        Set(ui,"instructionsPanel",panel); panel.SetActive(false);
        Fade(canvas,ui); Save("MainMenu");
    }

    private static void MakeEndGame()
    {
        NewScene("black"); Canvas canvas = MakeCanvas(); SceneUI ui = AddSceneUI(canvas);
        Text(canvas.transform,"Title","MISSION COMPLETE",55,new Vector2(0,230),new Vector2(1100,90));
        Text(canvas.transform,"Subtitle","YOUR SHIP HIT AN ASTEROID",22,new Vector2(0,155),new Vector2(1000,50));
        Set(ui,"resultText",Text(canvas.transform,"FinalScoreText","FINAL SCORE  0",42,new Vector2(0,65),new Vector2(1100,80)));
        Button(canvas.transform,"PLAY AGAIN",new Vector2(0,-40),ui.PlayGame);
        Button(canvas.transform,"MAIN MENU",new Vector2(0,-125),ui.ReturnToMenu);
        Button(canvas.transform,"QUIT",new Vector2(0,-210),ui.QuitGame);
        Fade(canvas,ui); Save("EndGame");
    }

    private static void NewScene(string background)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        GameObject camera = new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));
        camera.tag = "MainCamera"; camera.transform.position = new Vector3(0,0,-10);
        Camera cam = camera.GetComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 5;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.015f,0.02f,0.05f);
        GameObject bg = SpriteObject("Background","Backgrounds/"+background,Vector3.zero,-100);
        SpriteRenderer renderer = bg.GetComponent<SpriteRenderer>();
        bg.transform.localScale = new Vector3(20f/renderer.sprite.bounds.size.x,12f/renderer.sprite.bounds.size.y,1);
    }
    private static Rigidbody2D Body(GameObject obj)
    {
        Rigidbody2D body = obj.AddComponent<Rigidbody2D>(); body.gravityScale = 0;
        body.linearDamping = 0; body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        return body;
    }
    private static GameObject SpriteObject(string name,string sprite,Vector3 position,int order)
    {
        GameObject obj = new GameObject(name); obj.transform.position = position;
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Sprites/Kenney/"+sprite+".png");
        if(renderer.sprite == null) throw new Exception("Missing sprite: "+sprite);
        renderer.sharedMaterial = spriteMaterial; renderer.sortingOrder = order; return obj;
    }
    private static Canvas MakeCanvas()
    {
        GameObject obj = new GameObject("Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        Canvas canvas = obj.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = obj.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = 0.5f;
        GameObject events = new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions(); return canvas;
    }
    private static SceneUI AddSceneUI(Canvas canvas)
    {
        SceneUI ui = new GameObject("SceneUI").AddComponent<SceneUI>();
        Set(ui,"clickSound",Clip("UIClick")); ui.GetComponent<AudioSource>().playOnAwake = false; return ui;
    }
    private static TextMeshProUGUI Text(Transform parent,string name,string text,float size,Vector2 position,Vector2 dimensions)
    {
        GameObject obj = new GameObject(name,typeof(RectTransform)); obj.transform.SetParent(parent,false);
        TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>(); label.font = font; label.text = text;
        label.fontSize = size; label.alignment = TextAlignmentOptions.Center; label.color = Color.white;
        label.raycastTarget = false; label.rectTransform.sizeDelta = dimensions; label.rectTransform.anchoredPosition = position;
        return label;
    }
    private static void Button(Transform parent,string title,Vector2 position,UnityAction action)
    {
        GameObject obj = new GameObject(title,typeof(RectTransform),typeof(Image),typeof(UnityEngine.UI.Button)); obj.transform.SetParent(parent,false);
        RectTransform rect = obj.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(330,64); rect.anchoredPosition = position;
        obj.GetComponent<Image>().color = new Color(0.06f,0.28f,0.48f,0.98f);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(obj.GetComponent<UnityEngine.UI.Button>().onClick,action);
        Text(obj.transform,"Label",title,25,Vector2.zero,new Vector2(310,60));
    }
    private static GameObject Panel(Transform parent,string name,Color color)
    {
        GameObject obj = new GameObject(name,typeof(RectTransform),typeof(Image)); obj.transform.SetParent(parent,false);
        RectTransform rect = obj.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero; obj.GetComponent<Image>().color = color; return obj;
    }
    private static void Fade(Canvas canvas,SceneUI ui)
    {
        GameObject overlay = Panel(canvas.transform,"SceneFade",Color.black);
        CanvasGroup group = overlay.AddComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false;
        Set(ui,"fadeOverlay",group);
    }
    private static AudioClip Clip(string name) { return AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/Kenney/"+name+".ogg"); }
    private static void Set(UnityEngine.Object obj,string field,UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(obj); serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Save(string name) { EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Root+"Scenes/"+name+".unity"); }
}
