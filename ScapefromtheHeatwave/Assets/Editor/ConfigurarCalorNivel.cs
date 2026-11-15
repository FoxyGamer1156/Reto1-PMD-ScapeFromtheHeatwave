using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Tools > Scape From the Heatwave > Añadir Calor, Sombras y Objetos
/// Trabaja sobre la escena abierta (Nivel1). Es repetible: borra lo que creó la vez anterior y lo vuelve a crear.
///  - Añade EstadoJugador + Inventario a Marta
///  - Coloca sombras de edificios, una gorra y botellas, repartidas según los biomas
///  - Crea la música (MusicaNivel) y el filtro rojo/naranja de pantalla
/// </summary>
public static class ConfigurarCalorNivel
{
    // ── Ajusta aquí cuántos hay ──
    // sombras de edificio por bioma: barrio, avenida (mucho sol), industrial, parque (ahí dan sombra los árboles)
    // (ahora las sombras son objetos: farolas, palmera del oasis y árboles, los crea CrearNivelLargo; aquí 0)
    static readonly int[] SombrasPorBioma = { 0, 0, 0, 0 };
    // objetos: (bioma, posición dentro del bioma 0..1)
    static readonly (int bioma, float f)[] Gorras = { (0, .80f) };
    static readonly (int bioma, float f)[] Botellas = { (1, .70f), (2, .50f) };

    const string CarpetaSprites = "Assets/Nivel/Sprites/";
    const string Raiz = "ElementosCalor";
    const string CanvasFiltro = "UI_FiltroCalor";
    const float Celda = 0.8f;

    [MenuItem("Tools/Scape From the Heatwave/Añadir Calor, Sombras y Objetos")]
    public static void Configurar()
    {
        var marta = Object.FindFirstObjectByType<Controles>();
        var mundo = GameObject.Find("Mundo");
        var tm = mundo ? mundo.GetComponent<Tilemap>() : null;
        if (!marta || !tm)
        {
            EditorUtility.DisplayDialog("Escena equivocada", "Abre Nivel1 (necesita a Marta y el Tilemap 'Mundo').", "OK");
            return;
        }

        var sprGorra = PrepararSprite(CarpetaSprites + "gorra.png", 100);
        var sprBotella = PrepararSprite(CarpetaSprites + "botella.png", 100);
        var sprCuadrado = PrepararSprite(CarpetaSprites + "cuadrado.png", 4);
        if (!sprGorra || !sprBotella || !sprCuadrado)
        {
            EditorUtility.DisplayDialog("Faltan sprites", "No encuentro gorra.png, botella.png o cuadrado.png en " + CarpetaSprites, "OK");
            return;
        }

        foreach (var n in new[] { Raiz, CanvasFiltro })
        {
            var viejo = GameObject.Find(n);
            if (viejo) Object.DestroyImmediate(viejo);
        }

        if (!marta.GetComponent<EstadoJugador>()) Undo.AddComponent<EstadoJugador>(marta.gameObject);
        if (!marta.GetComponent<Inventario>()) Undo.AddComponent<Inventario>(marta.gameObject);
        if (!marta.GetComponent<EfectoViento>()) Undo.AddComponent<EfectoViento>(marta.gameObject);
        if (!marta.GetComponent<SonidosPersonaje>()) Undo.AddComponent<SonidosPersonaje>(marta.gameObject);

        // ── Dónde hay suelo llano ──
        tm.CompressBounds();
        var b = tm.cellBounds;
        bool Llano(int x) => tm.GetTile(new Vector3Int(x, -1, 0)) != null && tm.GetTile(new Vector3Int(x, 0, 0)) == null;
        bool Tramo(int cx, int radio) { for (int x = cx - radio; x <= cx + radio; x++) if (!Llano(x)) return false; return true; }
        int Buscar(int deseado, int radio, int desde, int hasta)
        {
            for (int d = 0; d < 400; d++)
            {
                int a = deseado + d; if (a >= desde && a < hasta && Tramo(a, radio)) return a;
                a = deseado - d; if (a >= desde && a < hasta && Tramo(a, radio)) return a;
            }
            return -1;
        }

        // ── Rango de columnas de cada bioma (si no hay FondoBiomas, todo el nivel es un solo tramo) ──
        var fondo = Object.FindFirstObjectByType<FondoBiomas>();
        int nB = (fondo && fondo.biomas != null && fondo.biomas.Length > 0) ? fondo.biomas.Length : 1;
        int colIni = b.xMin + 40, colFin = b.xMax - 60;   // dejamos libres el inicio y la meta
        (int a, int z) Rango(int i)
        {
            if (nB == 1) return (colIni, colFin);
            int a = i == 0 ? colIni : Mathf.RoundToInt(fondo.biomas[i].xInicio / Celda);
            int z = i == nB - 1 ? colFin : Mathf.RoundToInt(fondo.biomas[i].xFin / Celda);
            return (Mathf.Max(a, colIni), Mathf.Min(z, colFin));
        }
        int ColEn(int bioma, float f)
        {
            var r = Rango(Mathf.Clamp(bioma, 0, nB - 1));
            return Mathf.RoundToInt(Mathf.Lerp(r.a, r.z, f));
        }

        var raiz = new GameObject(Raiz);

        // ── Sombras de edificios ──
        int sombrasPuestas = 0;
        for (int bi = 0; bi < nB; bi++)
        {
            int n = nB == 1 ? 8 : (bi < SombrasPorBioma.Length ? SombrasPorBioma[bi] : 0);
            var r = Rango(bi);
            for (int j = 1; j <= n; j++)
            {
                int cx = Buscar(ColEn(bi, j / (float)(n + 1)), 7, r.a, r.z);
                if (cx < 0) continue;
                sombrasPuestas++;
                var go = new GameObject("ZonaSombra_" + sombrasPuestas);
                go.transform.SetParent(raiz.transform);
                go.transform.position = new Vector3((cx + 0.5f) * Celda, 3f, 0f);
                var bc = go.AddComponent<BoxCollider2D>();
                bc.isTrigger = true; bc.size = new Vector2(15 * Celda, 6f);
                go.AddComponent<ZonaSombra>();

                var vis = new GameObject("Visual");
                vis.transform.SetParent(go.transform, false);
                vis.transform.localScale = new Vector3(15 * Celda, 6f, 1f);
                var sr = vis.AddComponent<SpriteRenderer>();
                sr.sprite = sprCuadrado; sr.color = new Color(.05f, .08f, .25f, .32f); sr.sortingOrder = 20;
            }
        }

        // ── Objetos ──
        int objetos = 0;
        void Poner(TipoObjeto tipo, Sprite spr, int bioma, float f, string nombre)
        {
            var r = Rango(Mathf.Clamp(bioma, 0, nB - 1));
            int cx = Buscar(ColEn(bioma, f), 2, r.a, r.z);
            if (cx < 0) return;
            var go = new GameObject(nombre);
            go.transform.SetParent(raiz.transform);
            go.transform.position = new Vector3((cx + 0.5f) * Celda, 0.7f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr; sr.sortingOrder = 15;
            var bc = go.AddComponent<BoxCollider2D>();
            bc.isTrigger = true; bc.size = new Vector2(0.9f, 0.9f);
            var rec = go.AddComponent<Recogible>();
            rec.tipo = tipo; rec.icono = spr;
            objetos++;
        }
        for (int i = 0; i < Gorras.Length; i++) Poner(TipoObjeto.Gorra, sprGorra, Gorras[i].bioma, Gorras[i].f, "Gorra_" + (i + 1));
        for (int i = 0; i < Botellas.Length; i++) Poner(TipoObjeto.Botella, sprBotella, Botellas[i].bioma, Botellas[i].f, "Botella_" + (i + 1));

        // ── Música (con los filtros lofi / estrés) ──
        var musica = new GameObject("MusicaNivel");
        musica.transform.SetParent(raiz.transform);
        var fuente = musica.AddComponent<AudioSource>();
        fuente.playOnAwake = false; fuente.loop = true;
        var mn = musica.AddComponent<MusicaNivel>();
        var clip = BuscarCancion();
        mn.cancion = clip;

        // ── Filtro rojo/naranja de pantalla (por debajo del HUD) ──
        var cv = new GameObject(CanvasFiltro, typeof(Canvas));
        var canvas = cv.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = -10;
        var img = new GameObject("Filtro", typeof(RectTransform), typeof(Image));
        img.transform.SetParent(cv.transform, false);
        var rt = (RectTransform)img.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var im = img.GetComponent<Image>();
        im.raycastTarget = false; im.color = new Color(1f, .55f, .15f, .05f);
        cv.AddComponent<FiltroCalor>().imagen = im;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log($"Calor configurado: {sombrasPuestas} sombras, {objetos} objetos. Canción: {(clip ? clip.name : "NINGUNA")}");

        if (!clip)
            EditorUtility.DisplayDialog("Falta la canción",
                "No hay ningún audio en el proyecto.\n\nMete tu archivo (mp3/wav) en Assets, selecciona ElementosCalor > MusicaNivel y arrástralo al campo 'Cancion'. También puedes volver a ejecutar esta herramienta.", "OK");
    }

    /// <summary>Ruta completa del PNG (p. ej. Assets/Nivel/Sprites/gorra.png). Lo deja importado como Sprite.</summary>
    internal static Sprite PrepararSprite(string ruta, float ppu)
    {
        var ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (ti == null) return null;
        if (ti.textureType != TextureImporterType.Sprite || !Mathf.Approximately(ti.spritePixelsPerUnit, ppu))
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    static AudioClip BuscarCancion()
    {
        var clips = AssetDatabase.FindAssets("t:AudioClip")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.StartsWith("Assets/") && !p.StartsWith("Assets/Resources/Sonidos"))
            .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
            .Where(c => c != null).ToList();
        string[] pistas = { "nivel", "level", "break", "heat", "calor" };
        return clips.FirstOrDefault(c => pistas.Any(k => c.name.ToLower().Contains(k))) ?? clips.FirstOrDefault();
    }
}
