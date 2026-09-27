using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Construye TODO el proyecto con un clic:  menú  La Fogata > Construir proyecto completo
///  - Configura los sprites (Sprite 2D, 100 PPU)
///  - Crea fuentes TextMeshPro a partir de los .ttf
///  - Crea animaciones (4 bucles de 8 frames), Animator del jugador (Idle/Walk/Jump)
///  - Crea las escenas MainMenu, Game y Credits con cámara, UI, colisiones, scripts y botones enlazados
///  - Registra las escenas en Build Settings
/// Es seguro volver a ejecutarlo: regenera las escenas y los assets generados.
public static class LaFogataBuilder
{
    const string SpritesDir = "Assets/Sprites";
    const string FontsDir = "Assets/Fonts";
    const string AnimDir = "Assets/Animations";
    const string ScenesDir = "Assets/Scenes";
    const string MatDir = "Assets/Materials";

    class AnimSet { public RuntimeAnimatorController player, fire, river, stars; }

    static AnimSet anim;
    static TMP_FontAsset mainFont;
    static TMP_FontAsset[] allFonts = new TMP_FontAsset[0];
    static Sprite uiSprite;
    static Material spriteMat;
    static PhysicsMaterial2D noFriction;

    // ------------------------------------------------------------------ entrada
    [MenuItem("La Fogata/Construir proyecto completo")]
    public static void BuildAll()
    {
        if (TMP_Settings.instance == null)
        {
            EditorUtility.DisplayDialog("La Fogata",
                "Falta importar los recursos de TextMeshPro.\n\nVe a  Window > TextMeshPro > Import TMP Essential Resources,  impórtalos y vuelve a ejecutar este menú.",
                "Entendido");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        try
        {
            ShowProgress("Preparando carpetas...", 0.05f);
            foreach (var d in new[] { AnimDir, ScenesDir, MatDir }) Directory.CreateDirectory(d);
            AssetDatabase.Refresh();

            ShowProgress("Configurando sprites...", 0.15f);
            ConfigureSprites();
            if (S("bg.png") == null)
            {
                EditorUtility.DisplayDialog("La Fogata", "No encuentro los sprites en Assets/Sprites. Copia la carpeta Assets completa del proyecto.", "OK");
                return;
            }

            spriteMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            ShowProgress("Creando fuentes TextMeshPro...", 0.30f);
            allFonts = BuildFonts();
            mainFont = allFonts.Length > 0 ? allFonts[0] : null;

            ShowProgress("Creando animaciones...", 0.45f);
            noFriction = BuildPhysicsMaterial();
            BuildAnimations();

            ShowProgress("Construyendo escena MainMenu...", 0.60f);
            BuildMainMenu();
            ShowProgress("Construyendo escena Game...", 0.75f);
            BuildGame();
            ShowProgress("Construyendo escena Credits...", 0.90f);
            BuildCredits();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenesDir + "/MainMenu.unity", true),
                new EditorBuildSettingsScene(ScenesDir + "/Game.unity", true),
                new EditorBuildSettingsScene(ScenesDir + "/Credits.unity", true),
            };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenesDir + "/MainMenu.unity");
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("La Fogata",
                "¡Proyecto construido!\n\nSe abrió la escena MainMenu. Pulsa Play para probarlo.\nEn Game view prueba distintas resoluciones (16:9, 4:3, 9:16).", "Genial");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[La Fogata] Error al construir: " + e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    static void ShowProgress(string msg, float p) => EditorUtility.DisplayProgressBar("La Fogata", msg, p);

    // ------------------------------------------------------------------ sprites
    static Sprite S(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/" + file);

    static Sprite[] Frames(string prefix, int n)
    {
        var a = new Sprite[n];
        for (int i = 0; i < n; i++) a[i] = S(prefix + "_" + i + ".png");
        return a;
    }

    static void ConfigureSprites()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.alphaIsTransparency = true;
            imp.maxTextureSize = 4096;
            imp.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = Path.GetFileName(path).StartsWith("fire_")
                ? (int)SpriteAlignment.BottomCenter
                : (int)SpriteAlignment.Center;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
        }
    }

    // ------------------------------------------------------------------ fuentes
    static TMP_FontAsset[] BuildFonts()
    {
        var list = new List<TMP_FontAsset>();
        foreach (var n in new[] { "DejaVuSans", "DejaVuSerif", "DejaVuSansMono" })
        {
            string assetPath = FontsDir + "/" + n + " SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) { list.Add(existing); continue; }

            var font = AssetDatabase.LoadAssetAtPath<Font>(FontsDir + "/" + n + ".ttf");
            if (font == null) continue;
            try
            {
                var fa = TMP_FontAsset.CreateFontAsset(font);
                if (fa == null) continue;
                fa.name = n + " SDF";
                AssetDatabase.CreateAsset(fa, assetPath);
                if (fa.atlasTextures != null)
                    foreach (var tex in fa.atlasTextures)
                    {
                        tex.name = n + " Atlas";
                        AssetDatabase.AddObjectToAsset(tex, fa);
                    }
                if (fa.material != null)
                {
                    fa.material.name = n + " Material";
                    AssetDatabase.AddObjectToAsset(fa.material, fa);
                }
                EditorUtility.SetDirty(fa);
                list.Add(fa);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[La Fogata] No se pudo crear la fuente " + n + ": " + e.Message);
            }
        }
        AssetDatabase.SaveAssets();
        return list.ToArray();
    }

    static PhysicsMaterial2D BuildPhysicsMaterial()
    {
        string path = MatDir + "/SinFriccion.physicsMaterial2D";
        AssetDatabase.DeleteAsset(path);
        var m = new PhysicsMaterial2D("SinFriccion") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // ------------------------------------------------------------------ animaciones
    static AnimationClip MakeClip(string name, Sprite[] frames, float fps)
    {
        string path = AnimDir + "/" + name + ".anim";
        AssetDatabase.DeleteAsset(path);

        var clip = new AnimationClip { name = name, frameRate = fps };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        int extra = frames.Length > 1 ? 1 : 0;   // repite el primer frame al final para que el bucle dure exacto
        var keys = new ObjectReferenceKeyframe[frames.Length + extra];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        if (extra == 1)
            keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[0] };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var cs = AnimationUtility.GetAnimationClipSettings(clip);
        cs.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, cs);

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static RuntimeAnimatorController SingleClipController(string name, AnimationClip clip)
    {
        string path = AnimDir + "/" + name + ".controller";
        AssetDatabase.DeleteAsset(path);
        return AnimatorController.CreateAnimatorControllerAtPathWithClip(path, clip);
    }

    static void ConfigureTransition(AnimatorStateTransition t)
    {
        t.hasExitTime = false;
        t.hasFixedDuration = true;
        t.duration = 0f;
    }

    static RuntimeAnimatorController BuildPlayerController(AnimationClip idle, AnimationClip walk, AnimationClip jump)
    {
        string path = AnimDir + "/Jugador.controller";
        AssetDatabase.DeleteAsset(path);
        var c = AnimatorController.CreateAnimatorControllerAtPath(path);
        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        c.AddParameter(new AnimatorControllerParameter
        {
            name = "Grounded",
            type = AnimatorControllerParameterType.Bool,
            defaultBool = true
        });

        var sm = c.layers[0].stateMachine;
        var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
        var sWalk = sm.AddState("Walk"); sWalk.motion = walk;
        var sJump = sm.AddState("Jump"); sJump.motion = jump;
        sm.defaultState = sIdle;

        var t1 = sIdle.AddTransition(sWalk); ConfigureTransition(t1);
        t1.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var t2 = sWalk.AddTransition(sIdle); ConfigureTransition(t2);
        t2.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        var t3 = sm.AddAnyStateTransition(sJump); ConfigureTransition(t3);
        t3.canTransitionToSelf = false;
        t3.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");

        var t4 = sJump.AddTransition(sIdle); ConfigureTransition(t4);
        t4.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
        return c;
    }

    static void BuildAnimations()
    {
        anim = new AnimSet();
        var idle = MakeClip("Jugador_Idle", new[] { S("player_idle.png") }, 1f);
        var jump = MakeClip("Jugador_Salto", new[] { S("player_jump.png") }, 1f);
        var walk = MakeClip("Jugador_Caminar", Frames("player_walk", 8), 12f);
        anim.player = BuildPlayerController(idle, walk, jump);
        anim.fire = SingleClipController("Fogata", MakeClip("Fogata_Bucle", Frames("fire", 8), 12f));
        anim.river = SingleClipController("Rio", MakeClip("Rio_Bucle", Frames("river", 8), 8f));
        anim.stars = SingleClipController("Estrellas", MakeClip("Estrellas_Bucle", Frames("stars", 8), 6f));
    }

    // ------------------------------------------------------------------ helpers de escena
    static Scene NewScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    static void SaveScene(Scene scene, string name) => EditorSceneManager.SaveScene(scene, ScenesDir + "/" + name + ".unity");

    static Camera MakeCamera(Color bg)
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bg;
        go.AddComponent<AudioListener>();
        go.AddComponent<CameraFit>();          // pantalla para distintos dispositivos
        go.AddComponent<Physics2DRaycaster>(); // necesario para arrastrar sprites
        return cam;
    }

    static void MakeEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    static Canvas MakeCanvas()
    {
        var go = new GameObject("Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static SpriteRenderer MakeSprite(string name, Sprite sprite, Vector2 pos, int order)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        if (spriteMat != null) sr.sharedMaterial = spriteMat;
        return sr;
    }

    static BoxCollider2D AddBox(GameObject go, Vector2 size, Vector2 offset, bool trigger)
    {
        var c = go.AddComponent<BoxCollider2D>();
        c.size = size;
        c.offset = offset;
        c.isTrigger = trigger;
        return c;
    }

    static Animator AddAnimator(GameObject go, RuntimeAnimatorController ctrl)
    {
        var a = go.AddComponent<Animator>();
        a.runtimeAnimatorController = ctrl;
        return a;
    }

    static void MakeSky(Vector2 moonPos, Vector2 windowPos)
    {
        MakeSprite("Fondo", S("bg.png"), Vector2.zero, -100);
        MakeSprite("Luna", S("moon.png"), moonPos, -90);

        // Máscara: las estrellas animadas solo se ven dentro del círculo (Sprite Mask)
        var maskGo = new GameObject("MascaraVentana");
        maskGo.transform.position = new Vector3(windowPos.x, windowPos.y, 0f);
        maskGo.AddComponent<SpriteMask>().sprite = S("mask_circle.png");

        var stars = MakeSprite("Estrellas (bucle 8 frames)", S("stars_0.png"), windowPos, -80);
        stars.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        AddAnimator(stars.gameObject, anim.stars);
    }

    // ------------------------------------------------------------------ helpers de UI
    static RectTransform NewUI(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI MakeText(string name, Transform parent, string text, float size,
        Vector2 anchor, Vector2 pos, Vector2 box, TextAlignmentOptions align)
    {
        var rt = NewUI(name, parent, anchor, pos, box);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (mainFont != null) t.font = mainFont;
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    static Button MakeButton(string name, Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize)
    {
        var rt = NewUI(name, parent, anchor, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = uiSprite;
        img.type = Image.Type.Sliced;
        img.color = new Color(0.16f, 0.22f, 0.40f, 0.92f);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;

        var lbl = MakeText("Texto", rt, label, fontSize, new Vector2(0.5f, 0.5f), Vector2.zero, size, TextAlignmentOptions.Center);
        Stretch(lbl.rectTransform);
        return b;
    }

    static RectTransform MakePanel(string name, Transform parent, Color color)
    {
        var rt = NewUI(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(rt);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return rt;
    }

    static readonly Vector2 C = new Vector2(0.5f, 0.5f);
    static readonly Vector2 TL = new Vector2(0f, 1f);
    static readonly Vector2 TR = new Vector2(1f, 1f);
    static readonly Vector2 TC = new Vector2(0.5f, 1f);
    static readonly Vector2 BL = new Vector2(0f, 0f);
    static readonly Vector2 BR = new Vector2(1f, 0f);

    // Fila de botones de fuentes: A-, A+, Fuente, Restablecer
    static void MakeFontButtons(Transform parent, FontController fc, float y)
    {
        var bMinus = MakeButton("Btn_A-", parent, "A-", C, new Vector2(-430f, y), new Vector2(180f, 100f), 44f);
        var bPlus = MakeButton("Btn_A+", parent, "A+", C, new Vector2(-230f, y), new Vector2(180f, 100f), 44f);
        var bFont = MakeButton("Btn_Fuente", parent, "Fuente", C, new Vector2(50f, y), new Vector2(300f, 100f), 40f);
        var bReset = MakeButton("Btn_Restablecer", parent, "Restablecer", C, new Vector2(380f, y), new Vector2(320f, 100f), 36f);
        UnityEventTools.AddVoidPersistentListener(bMinus.onClick, fc.Decrease);
        UnityEventTools.AddVoidPersistentListener(bPlus.onClick, fc.Increase);
        UnityEventTools.AddVoidPersistentListener(bFont.onClick, fc.NextFont);
        UnityEventTools.AddVoidPersistentListener(bReset.onClick, fc.ResetFont);
    }

    static GameObject MakeSystems(out SceneNavigator nav, out FontController fc)
    {
        var go = new GameObject("Sistemas");
        nav = go.AddComponent<SceneNavigator>();
        fc = go.AddComponent<FontController>();
        fc.fonts = allFonts;
        return go;
    }

    static void MakeGround(float centerX, string sprite, string name)
    {
        var g = MakeSprite(name, S(sprite), new Vector2(centerX, -4f), 1);
        AddBox(g.gameObject, g.sprite.bounds.size, Vector2.zero, false);
    }

    static SpriteRenderer MakeFire(Vector2 pos, float scale)
    {
        var fire = MakeSprite("Fogata (bucle 8 frames)", S("fire_0.png"), pos, 4);
        fire.transform.localScale = new Vector3(scale, scale, 1f);
        AddAnimator(fire.gameObject, anim.fire);
        return fire;
    }

    // ------------------------------------------------------------------ escena: MainMenu
    static void BuildMainMenu()
    {
        var scene = NewScene();
        MakeCamera(new Color(0.09f, 0.07f, 0.09f));
        MakeEventSystem();
        MakeSky(new Vector2(6.3f, 3.2f), new Vector2(5.4f, 0.4f));
        MakeGround(-6f, "ground_left.png", "Suelo izquierdo");
        MakeGround(6f, "ground_left.png", "Suelo derecho");
        MakeFire(new Vector2(-5.2f, -3.5f), 1.6f);

        SceneNavigator nav; FontController fc;
        MakeSystems(out nav, out fc);

        var canvas = MakeCanvas();
        var targets = new List<TMP_Text>();

        targets.Add(MakeText("Titulo", canvas.transform, "La Fogata Perdida", 120f, TC, new Vector2(0f, -110f), new Vector2(1700f, 170f), TextAlignmentOptions.Center));
        targets.Add(MakeText("Subtitulo", canvas.transform, "Ayuda al campista a encender la fogata", 48f, TC, new Vector2(0f, -290f), new Vector2(1500f, 80f), TextAlignmentOptions.Center));

        var bPlay = MakeButton("Btn_Jugar", canvas.transform, "Jugar", C, new Vector2(0f, -30f), new Vector2(520f, 120f), 56f);
        var bOpt = MakeButton("Btn_Opciones", canvas.transform, "Opciones", C, new Vector2(0f, -170f), new Vector2(520f, 120f), 56f);
        var bQuit = MakeButton("Btn_Salir", canvas.transform, "Salir", C, new Vector2(0f, -310f), new Vector2(520f, 120f), 56f);

        // Panel de opciones (fuentes)
        var panel = MakePanel("PanelOpciones", canvas.transform, new Color(0f, 0f, 0f, 0.82f));
        targets.Add(MakeText("TituloOpciones", panel, "Opciones de texto", 80f, C, new Vector2(0f, 260f), new Vector2(1400f, 120f), TextAlignmentOptions.Center));
        targets.Add(MakeText("Muestra", panel, "Texto de muestra: ñandú, canción, ¡fogata!", 52f, C, new Vector2(0f, 110f), new Vector2(1700f, 100f), TextAlignmentOptions.Center));
        MakeFontButtons(panel, fc, -40f);
        var bClose = MakeButton("Btn_Cerrar", panel, "Cerrar", C, new Vector2(0f, -220f), new Vector2(420f, 110f), 50f);
        panel.gameObject.SetActive(false);

        UnityEventTools.AddStringPersistentListener(bPlay.onClick, nav.LoadScene, "Game");
        UnityEventTools.AddBoolPersistentListener(bOpt.onClick, panel.gameObject.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(bClose.onClick, panel.gameObject.SetActive, false);
        UnityEventTools.AddVoidPersistentListener(bQuit.onClick, nav.Quit);

        fc.targets = targets.ToArray();
        SaveScene(scene, "MainMenu");
    }

    // ------------------------------------------------------------------ escena: Game
    static void BuildGame()
    {
        var scene = NewScene();
        MakeCamera(new Color(0.09f, 0.07f, 0.09f));
        MakeEventSystem();
        MakeSky(new Vector2(6.3f, 3.2f), new Vector2(-4.6f, 2.6f));

        // --- Nivel: piso, río, plataforma y muros (colisiones alineadas al sprite)
        MakeGround(-6f, "ground_left.png", "Suelo izquierdo");    // x: -12 .. 0
        MakeGround(7f, "ground_right.png", "Suelo derecho");      // x:   2 .. 12

        var platform = MakeSprite("Plataforma", S("platform.png"), new Vector2(-2.3f, -2.6f), 1);
        AddBox(platform.gameObject, platform.sprite.bounds.size, Vector2.zero, false);

        foreach (var x in new[] { -7.75f, 7.75f })
        {
            var wall = MakeSprite(x < 0 ? "Muro izquierdo" : "Muro derecho", S("wall.png"), new Vector2(x, 0f), 2);
            AddBox(wall.gameObject, wall.sprite.bounds.size, Vector2.zero, false);
        }

        var river = MakeSprite("Rio (bucle 8 frames)", S("river_0.png"), new Vector2(1f, -4.05f), 3);
        AddAnimator(river.gameObject, anim.river);
        AddBox(river.gameObject, river.sprite.bounds.size, Vector2.zero, true);
        river.gameObject.AddComponent<Hazard>();

        // --- Fogata (zona de destino del arrastre)
        var fire = MakeFire(new Vector2(-6.3f, -3.5f), 1f);
        var zone = fire.gameObject.AddComponent<CircleCollider2D>();
        zone.isTrigger = true; zone.radius = 0.9f; zone.offset = new Vector2(0f, 0.6f);
        fire.gameObject.AddComponent<FireZone>();

        // --- Troncos arrastrables
        var logPositions = new[] { new Vector2(-0.7f, -3.26f), new Vector2(-2.3f, -2.16f), new Vector2(5.0f, -3.26f) };
        for (int i = 0; i < logPositions.Length; i++)
        {
            var log = MakeSprite("Tronco " + (i + 1), S("log.png"), logPositions[i], 12);
            AddBox(log.gameObject, log.sprite.bounds.size, Vector2.zero, true);
            log.gameObject.AddComponent<DragSprite>();
        }

        // --- Jugador
        var pSr = MakeSprite("Jugador", S("player_idle.png"), new Vector2(-4.6f, -2.98f), 10);
        var rb = pSr.gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = AddBox(pSr.gameObject, new Vector2(0.5f, 0.94f), new Vector2(0f, -0.01f), false);
        col.sharedMaterial = noFriction;
        var pAnimator = AddAnimator(pSr.gameObject, anim.player);
        var player = pSr.gameObject.AddComponent<PlayerController>();
        player.animator = pAnimator;
        player.speed = 5f;
        player.jumpForce = 10f;

        // --- Sistemas
        SceneNavigator nav; FontController fc;
        MakeSystems(out nav, out fc);
        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        gm.woodNeeded = 3;

        // --- UI (todo con anclas para distintos tamaños de pantalla)
        var canvas = MakeCanvas();
        var targets = new List<TMP_Text>();

        gm.woodText = MakeText("Txt_Lena", canvas.transform, "Leña: 0/3", 50f, TL, new Vector2(40f, -30f), new Vector2(600f, 70f), TextAlignmentOptions.Left);
        gm.timeText = MakeText("Txt_Tiempo", canvas.transform, "Tiempo: 0s", 42f, TL, new Vector2(40f, -100f), new Vector2(600f, 60f), TextAlignmentOptions.Left);
        targets.Add(gm.woodText);
        targets.Add(gm.timeText);
        targets.Add(MakeText("Txt_Ayuda", canvas.transform, "Arrastra los troncos a la fogata", 40f, TC, new Vector2(0f, -30f), new Vector2(1000f, 60f), TextAlignmentOptions.Center));

        var bPause = MakeButton("Btn_Pausa", canvas.transform, "Pausa", TR, new Vector2(-40f, -30f), new Vector2(220f, 90f), 40f);
        var bLeft = MakeButton("Btn_Izquierda", canvas.transform, "<", BL, new Vector2(40f, 40f), new Vector2(170f, 170f), 80f);
        var bRight = MakeButton("Btn_Derecha", canvas.transform, ">", BL, new Vector2(240f, 40f), new Vector2(170f, 170f), 80f);
        var bJump = MakeButton("Btn_Saltar", canvas.transform, "SALTAR", BR, new Vector2(-40f, 40f), new Vector2(320f, 170f), 56f);

        var mvL = bLeft.gameObject.AddComponent<MoveButton>(); mvL.player = player; mvL.direction = -1;
        var mvR = bRight.gameObject.AddComponent<MoveButton>(); mvR.player = player; mvR.direction = 1;
        UnityEventTools.AddVoidPersistentListener(bJump.onClick, player.Jump);
        UnityEventTools.AddVoidPersistentListener(bPause.onClick, gm.TogglePause);

        // Panel de pausa
        var pause = MakePanel("PanelPausa", canvas.transform, new Color(0f, 0f, 0f, 0.82f));
        targets.Add(MakeText("TituloPausa", pause, "Pausa", 90f, C, new Vector2(0f, 330f), new Vector2(1000f, 130f), TextAlignmentOptions.Center));
        var bResume = MakeButton("Btn_Reanudar", pause, "Reanudar", C, new Vector2(0f, 170f), new Vector2(520f, 110f), 52f);
        targets.Add(MakeText("TxtFuentes", pause, "Tamaño y tipo de letra", 44f, C, new Vector2(0f, 60f), new Vector2(1200f, 70f), TextAlignmentOptions.Center));
        MakeFontButtons(pause, fc, -50f);
        var bMenu = MakeButton("Btn_Menu", pause, "Menú principal", C, new Vector2(0f, -220f), new Vector2(520f, 110f), 50f);
        UnityEventTools.AddVoidPersistentListener(bResume.onClick, gm.TogglePause);
        UnityEventTools.AddStringPersistentListener(bMenu.onClick, nav.LoadScene, "MainMenu");
        pause.gameObject.SetActive(false);
        gm.pausePanel = pause.gameObject;

        // Panel de victoria
        var win = MakePanel("PanelVictoria", canvas.transform, new Color(0f, 0f, 0f, 0.78f));
        targets.Add(MakeText("TituloVictoria", win, "¡La fogata volvió a arder!", 90f, C, new Vector2(0f, 200f), new Vector2(1700f, 140f), TextAlignmentOptions.Center));
        var bNext = MakeButton("Btn_Continuar", win, "Continuar", C, new Vector2(0f, 0f), new Vector2(520f, 110f), 52f);
        var bAgain = MakeButton("Btn_Reiniciar", win, "Jugar de nuevo", C, new Vector2(0f, -140f), new Vector2(520f, 110f), 48f);
        UnityEventTools.AddStringPersistentListener(bNext.onClick, nav.LoadScene, "Credits");
        UnityEventTools.AddVoidPersistentListener(bAgain.onClick, nav.ReloadScene);
        win.gameObject.SetActive(false);
        gm.winPanel = win.gameObject;

        fc.targets = targets.ToArray();
        SaveScene(scene, "Game");
    }

    // ------------------------------------------------------------------ escena: Credits
    static void BuildCredits()
    {
        var scene = NewScene();
        MakeCamera(new Color(0.09f, 0.07f, 0.09f));
        MakeEventSystem();
        MakeSky(new Vector2(-6.3f, 3.2f), new Vector2(5.4f, 1.6f));
        MakeGround(-6f, "ground_left.png", "Suelo izquierdo");
        MakeGround(6f, "ground_left.png", "Suelo derecho");
        MakeFire(new Vector2(0f, -3.5f), 1.8f);

        SceneNavigator nav; FontController fc;
        MakeSystems(out nav, out fc);

        var canvas = MakeCanvas();
        var targets = new List<TMP_Text>();
        targets.Add(MakeText("Titulo", canvas.transform, "Créditos", 110f, TC, new Vector2(0f, -100f), new Vector2(1400f, 150f), TextAlignmentOptions.Center));
        targets.Add(MakeText("Texto", canvas.transform,
            "La Fogata Perdida\nProyecto de Unity 2D\n\nProgramación y diseño: TU NOMBRE AQUÍ\nArte: sprites generados para este proyecto",
            52f, C, new Vector2(0f, 90f), new Vector2(1500f, 420f), TextAlignmentOptions.Center));
        var bMenu = MakeButton("Btn_Menu", canvas.transform, "Volver al menú", BL, new Vector2(40f, 40f), new Vector2(460f, 110f), 46f);
        UnityEventTools.AddStringPersistentListener(bMenu.onClick, nav.LoadScene, "MainMenu");

        fc.targets = targets.ToArray();
        SaveScene(scene, "Credits");
    }
}
