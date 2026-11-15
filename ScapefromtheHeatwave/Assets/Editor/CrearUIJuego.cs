using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Genera la UI:
///   Tools > Scape From the Heatwave > Crear HUD + Pausa      (en la escena abierta, p. ej. PruebaPersonaje)
///   Tools > Scape From the Heatwave > Crear Menu Principal   (crea Assets/Scenes/MenuPrincipal.unity)
/// Ambas escenas se añaden solas a Build Settings (el menú queda la primera).
/// </summary>
public static class CrearUIJuego
{
    const string M = "Tools/Scape From the Heatwave/";
    const string RutaMenu = "Assets/Scenes/MenuPrincipal.unity";
    static Font fuente;
    static DefaultControls.Resources res;
    static Sprite redondeado;

    static void Preparar()
    {
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        redondeado = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        res = new DefaultControls.Resources
        {
            standard = redondeado,
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
        };
    }

    // ───────────────────────── HUD + PAUSA ─────────────────────────
    [MenuItem(M + "Crear HUD + Pausa")]
    public static void CrearHUD()
    {
        var escena = SceneManager.GetActiveScene();
        if (Object.FindFirstObjectByType<HUDController>() != null)
        {
            EditorUtility.DisplayDialog("HUD ya creado", "Esta escena ya tiene un HUDController. Bórralo (objeto UI_Juego) si quieres regenerarlo.", "OK");
            return;
        }
        Preparar();
        var canvas = NuevoCanvas("UI_Juego");
        var hudGO = Estirado("HUD", canvas.transform);
        var hud = hudGO.gameObject.AddComponent<HUDController>();

        // Barra de calor (amarillo → naranja → rojo)
        var gCalor = new Gradient();
        gCalor.SetKeys(
            new[] { new GradientColorKey(new Color(1f, .86f, .2f), 0f), new GradientColorKey(new Color(1f, .5f, .1f), .5f), new GradientColorKey(new Color(.9f, .1f, .1f), 1f) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        hud.barraCalor = CrearBarra(hudGO, "BarraCalor", "CALOR", new Vector2(30, -30), gCalor, 0f);

        // Barra de hidratación (azul claro → azul intenso)
        var gAgua = new Gradient();
        gAgua.SetKeys(
            new[] { new GradientColorKey(new Color(.75f, .9f, 1f), 0f), new GradientColorKey(new Color(.1f, .55f, 1f), 1f) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        hud.barraHidratacion = CrearBarra(hudGO, "BarraHidratacion", "HIDRATACIÓN", new Vector2(30, -80), gAgua, 1f);

        // Cuadrado de inventario vacío, arriba a la derecha (estilo Mario Kart)
        var marco = Hijo("Inventario", hudGO, V(1, 1), V(1, 1), V(1, 1), new Vector2(-30, -30), new Vector2(170, 170));
        var imgMarco = Imagen(marco, new Color(0, 0, 0, .45f));
        imgMarco.sprite = redondeado; imgMarco.type = Image.Type.Sliced;
        var borde = marco.gameObject.AddComponent<Outline>();
        borde.effectColor = new Color(1, 1, 1, .9f); borde.effectDistance = new Vector2(3, -3);
        var icono = Hijo("IconoObjeto", marco, V(0, 0), V(1, 1), V(.5f, .5f), Vector2.zero, Vector2.zero);
        icono.offsetMin = new Vector2(16, 16); icono.offsetMax = new Vector2(-16, -16);
        var imgIcono = Imagen(icono, Color.white);
        imgIcono.preserveAspect = true; imgIcono.raycastTarget = false; imgIcono.enabled = false;
        hud.iconoObjeto = imgIcono;

        // Menú de pausa
        var pausa = canvas.gameObject.AddComponent<MenuPausa>();
        var panel = Estirado("PanelPausa", canvas.transform);
        Imagen(panel, new Color(0, 0, 0, .7f));
        pausa.panel = panel.gameObject;

        var ventana = Hijo("Ventana", panel, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), Vector2.zero, new Vector2(700, 640));
        var imgV = Imagen(ventana, new Color(.13f, .13f, .16f, .97f));
        imgV.sprite = redondeado; imgV.type = Image.Type.Sliced;

        Texto("Titulo", ventana, "PAUSA", 64, TextAnchor.MiddleCenter, Color.white, V(.5f, 1), V(.5f, 1), V(.5f, 1), new Vector2(0, -30), new Vector2(600, 90));
        Texto("EtiquetaMusica", ventana, "Música", 32, TextAnchor.MiddleLeft, Color.white, V(.5f, 1), V(.5f, 1), V(.5f, 1), new Vector2(0, -150), new Vector2(520, 40));
        pausa.sliderMusica = CrearSlider(ventana, "SliderMusica", new Vector2(0, -200));
        Texto("EtiquetaEfectos", ventana, "Efectos de sonido", 32, TextAnchor.MiddleLeft, Color.white, V(.5f, 1), V(.5f, 1), V(.5f, 1), new Vector2(0, -260), new Vector2(520, 40));
        pausa.sliderEfectos = CrearSlider(ventana, "SliderEfectos", new Vector2(0, -310));

        Boton(ventana, "BotonContinuar", "Continuar", new Vector2(0, -400), new Vector2(380, 80), pausa.Continuar);
        Boton(ventana, "BotonSalirMenu", "Salir al menú", new Vector2(0, -500), new Vector2(380, 80), pausa.SalirAlMenu);

        // Botón de pausa (Android), a la izquierda del inventario
        Boton(hudGO, "BotonPausa", "II", new Vector2(-230, -30), new Vector2(90, 90), pausa.Alternar, V(1, 1), V(1, 1), V(1, 1));

        Selection.activeGameObject = canvas.gameObject;
        EditorSceneManager.MarkSceneDirty(escena);

        if (!string.IsNullOrEmpty(escena.path)) AnadirABuild(escena.path, false);
        else Debug.LogWarning("La escena no está guardada: guárdala y añádela a Build Settings.");
    }

    // ───────────────────────── MENÚ PRINCIPAL ─────────────────────────
    [MenuItem(M + "Crear Menu Principal")]
    static void CrearMenuPrincipal()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaMenu) != null &&
            !EditorUtility.DisplayDialog("MenuPrincipal ya existe", "Ya hay una escena " + RutaMenu + ". ¿Sobrescribirla?", "Sobrescribir", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Preparar();
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var canvas = NuevoCanvas("UI_MenuPrincipal");
        var menu = canvas.gameObject.AddComponent<MenuPrincipal>();

        // Fondo provisional (cámbialo por una imagen del nivel urbano, p. ej. Elementos_Escenario/Background)
        var fondo = Estirado("Fondo", canvas.transform);
        Imagen(fondo, new Color(.96f, .55f, .18f, 1f));

        Texto("Titulo", canvas.transform, "SCAPE FROM THE HEATWAVE", 110, TextAnchor.MiddleCenter, Color.white, V(.5f, 1), V(.5f, 1), V(.5f, 1), new Vector2(0, -90), new Vector2(1700, 260));
        Texto("Subtitulo", canvas.transform, "Verano de 2050 · Llega al refugio climático", 38, TextAnchor.MiddleCenter, new Color(1, 1, 1, .9f), V(.5f, 1), V(.5f, 1), V(.5f, 1), new Vector2(0, -340), new Vector2(1200, 60));

        Boton(canvas.transform, "BotonJugar", "JUGAR", new Vector2(0, -60), new Vector2(460, 100), menu.Jugar, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), 44);
        Boton(canvas.transform, "BotonSalir", "SALIR", new Vector2(0, -200), new Vector2(460, 100), menu.Salir, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), 44);

        EditorSceneManager.SaveScene(escena, RutaMenu);
        AnadirABuild(RutaMenu, true);
        Selection.activeGameObject = canvas.gameObject;
    }

    // ───────────────────────── Build Settings ─────────────────────────
    static void AnadirABuild(string ruta, bool alPrincipio)
    {
        var lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool yaEsta = lista.Exists(s => s.path == ruta);
        if (yaEsta && !alPrincipio) return;
        lista.RemoveAll(s => s.path == ruta);
        var nueva = new EditorBuildSettingsScene(ruta, true);
        if (alPrincipio) lista.Insert(0, nueva); else lista.Add(nueva);
        EditorBuildSettings.scenes = lista.ToArray();
    }

    // ───────────────────────── Helpers ─────────────────────────
    static Vector2 V(float x, float y) => new Vector2(x, y);

    static Canvas NuevoCanvas(string nombre)
    {
        var go = new GameObject(nombre, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, nombre);
        var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay;
        var s = go.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080); s.matchWidthOrHeight = .5f;
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
            Undo.RegisterCreatedObjectUndo(es, "EventSystem");
        }
        return c;
    }

    static RectTransform Hijo(string nombre, Transform padre, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 tam)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = aMin; r.anchorMax = aMax; r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = tam;
        return r;
    }

    static RectTransform Estirado(string nombre, Transform padre) =>
        Hijo(nombre, padre, V(0, 0), V(1, 1), V(.5f, .5f), Vector2.zero, Vector2.zero);

    static Image Imagen(RectTransform r, Color c) { var i = r.gameObject.AddComponent<Image>(); i.color = c; return i; }

    static Text Texto(string nombre, Transform padre, string txt, int tam, TextAnchor al, Color col,
                      Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var r = Hijo(nombre, padre, aMin, aMax, pivot, pos, size);
        var t = r.gameObject.AddComponent<Text>();
        t.font = fuente; t.text = txt; t.fontSize = tam; t.alignment = al; t.color = col; t.raycastTarget = false;
        return t;
    }

    static BarraUI CrearBarra(Transform padre, string nombre, string etiqueta, Vector2 pos, Gradient g, float inicial)
    {
        var fondo = Hijo(nombre, padre, V(0, 1), V(0, 1), V(0, 1), pos, new Vector2(440, 36));
        Imagen(fondo, new Color(0, 0, 0, .55f));
        var borde = fondo.gameObject.AddComponent<Outline>();
        borde.effectColor = new Color(1, 1, 1, .9f); borde.effectDistance = new Vector2(2, -2);

        var interior = Estirado("Interior", fondo);
        interior.offsetMin = new Vector2(4, 4); interior.offsetMax = new Vector2(-4, -4);
        var relleno = Hijo("Relleno", interior, V(0, 0), V(1, 1), V(0, .5f), Vector2.zero, Vector2.zero);
        var imgRelleno = Imagen(relleno, Color.white);

        var t = Texto("Etiqueta", fondo, etiqueta, 22, TextAnchor.MiddleLeft, Color.white, V(0, 0), V(1, 1), V(.5f, .5f), Vector2.zero, Vector2.zero);
        t.rectTransform.offsetMin = new Vector2(12, 0); t.rectTransform.offsetMax = Vector2.zero;
        var sombra = t.gameObject.AddComponent<Shadow>(); sombra.effectColor = new Color(0, 0, 0, .8f);

        var b = fondo.gameObject.AddComponent<BarraUI>();
        b.relleno = relleno; b.imagenRelleno = imgRelleno; b.colorSegunValor = g; b.valor = inicial;
        return b;
    }

    static Slider CrearSlider(Transform padre, string nombre, Vector2 pos)
    {
        var go = DefaultControls.CreateSlider(res);
        go.name = nombre; go.transform.SetParent(padre, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = r.pivot = V(.5f, 1); r.anchoredPosition = pos; r.sizeDelta = new Vector2(520, 36);
        var s = go.GetComponent<Slider>(); s.minValue = .0001f; s.maxValue = 1f; s.value = 1f;
        return s;
    }

    static Button Boton(Transform padre, string nombre, string texto, Vector2 pos, Vector2 tam, UnityAction accion,
                        Vector2? aMin = null, Vector2? aMax = null, Vector2? pivot = null, int fontSize = 34)
    {
        var go = DefaultControls.CreateButton(res);
        go.name = nombre; go.transform.SetParent(padre, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = aMin ?? V(.5f, 1); r.anchorMax = aMax ?? V(.5f, 1); r.pivot = pivot ?? V(.5f, 1);
        r.anchoredPosition = pos; r.sizeDelta = tam;
        var t = go.GetComponentInChildren<Text>();
        t.font = fuente; t.text = texto; t.fontSize = fontSize; t.fontStyle = FontStyle.Bold;
        var b = go.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(b.onClick, accion);
        return b;
    }
}
