using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Tools > Scape From the Heatwave > Crear Nivel Completo (biomas)
/// Duplica PruebaPersonaje en Assets/Scenes/Nivel1.unity y construye TODO el nivel:
///   1 Barrio residencial · 2 Avenida principal · 3 Zona industrial · 4 Parque y refugio
///   - terreno con tiles distintos por bioma (RedCity, Desert, Caves, Grassland)
///   - coches, fuente, chorros de aire caliente, árboles y decoración
///   - fondo en carrusel con parallax que cambia de bioma
/// y al final encadena: arreglo de colisiones, HUD + pausa, y calor/sombras/objetos/música.
/// </summary>
public static class CrearNivelLargo
{
    const string Origen = "Assets/Scenes/PruebaPersonaje.unity";
    const string Destino = "Assets/Scenes/Nivel1.unity";
    const string CarpetaTilesets = "Assets/Elementos_Escenario/22 Digital/Pixel Tiles/Sprites/Tiles 500% Larger/";
    const string CarpetaTiles = "Assets/Nivel/Tiles";
    const string CarpetaSprites = "Assets/Nivel/Sprites/";

    const int XInicio = -9;       // primera columna
    const int Largo = 1500;       // columnas totales (1 columna = 0,8 u)
    const int Suelo = -1;         // fila de la superficie plana (superficie a y = 0)
    const int Fondo = -4;         // última fila de tierra
    const float Celda = 0.8f;
    const int Semilla = 2050;     // cambia el número para otro nivel distinto

    // Dónde cambia cada bioma (fracción del nivel)
    static readonly float[] Cortes = { 0f, .22f, .50f, .80f, 1f };
    static readonly string[] Nombres = { "Barrio residencial", "Avenida principal", "Zona industrial", "Parque y refugio" };

    class Estilo
    {
        public string ts;                 // tileset
        public int[] c1, c2, c3;          // {borde izq, centro, borde der} a 1, 2 y 3+ tiles de profundidad
        public int pL, pM, pR, bajo;      // plataforma: izq, centro, der, bajo (−1 = ninguno)
        public int[] deco;                // tiles decorativos sobre el suelo
    }

    static readonly Estilo[] Estilos =
    {
        new Estilo { ts = "RedCity",   c1 = new[]{7,1,8},   c2 = new[]{7,2,8},   c3 = new[]{7,5,8},    pL = 9,  pM = 10, pR = 11, bajo = -1, deco = new[]{12,13} },
        new Estilo { ts = "Desert",    c1 = new[]{5,1,14},  c2 = new[]{5,8,14},  c3 = new[]{5,20,14},  pL = 21, pM = 0,  pR = 22, bajo = 4,  deco = new[]{23} },
        new Estilo { ts = "Caves",     c1 = new[]{2,2,2},   c2 = new[]{5,5,5},   c3 = new[]{6,6,6},    pL = 10, pM = 11, pR = 12, bajo = -1, deco = new[]{13} },
        new Estilo { ts = "Grassland", c1 = new[]{1,2,1},   c2 = new[]{7,13,14}, c3 = new[]{18,19,18}, pL = 21, pM = 0,  pR = 22, bajo = 12, deco = new[]{23} },
    };

    static readonly Dictionary<string, TileBase> cacheTiles = new Dictionary<string, TileBase>();

    [MenuItem("Tools/Scape From the Heatwave/Crear Nivel Completo (biomas)")]
    static void Crear()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Origen) == null)
        {
            EditorUtility.DisplayDialog("Falta la escena", "No encuentro " + Origen, "OK");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Destino) != null &&
            !EditorUtility.DisplayDialog("Nivel1 ya existe", "Se va a reconstruir " + Destino + " desde cero (HUD y calor incluidos). ¿Continuar?", "Continuar", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        cacheTiles.Clear();
        var sprCoche = ConfigurarCalorNivel.PrepararSprite(CarpetaSprites + "coche.png", 100);
        var sprArbol = ConfigurarCalorNivel.PrepararSprite(CarpetaSprites + "arbol.png", 100);
        var sprFuente = ConfigurarCalorNivel.PrepararSprite(CarpetaSprites + "fuente.png", 100);
        var sprCuadrado = ConfigurarCalorNivel.PrepararSprite(CarpetaSprites + "cuadrado.png", 4);
        var sprFarola = ConfigurarCalorNivel.PrepararSprite(CarpetaSprites + "farola.png", 100);
        var sprCactus = ConfigurarCalorNivel.PrepararSprite(CarpetaSprites + "cactus.png", 100);
        var sprPalmera = ConfigurarCalorNivel.PrepararSprite(CarpetaSprites + "palmera.png", 100);
        var sprLaguito = ConfigurarCalorNivel.PrepararSprite(CarpetaSprites + "laguito.png", 100);
        var sprRejilla = ConfigurarCalorNivel.PrepararSprite(CarpetaSprites + "rejilla.png", 100);
        if (!sprCoche || !sprArbol || !sprFuente || !sprCuadrado || !sprFarola || !sprCactus || !sprPalmera || !sprLaguito || !sprRejilla)
        {
            EditorUtility.DisplayDialog("Faltan sprites", "Copia también Assets/Nivel/Sprites completo (coche, arbol, fuente, cuadrado, farola, cactus, palmera, laguito, rejilla).", "OK");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Destino) != null) AssetDatabase.DeleteAsset(Destino);
        AssetDatabase.CopyAsset(Origen, Destino);
        var escena = EditorSceneManager.OpenScene(Destino, OpenSceneMode.Single);

        var mundo = GameObject.Find("Mundo");
        var tm = mundo ? mundo.GetComponent<Tilemap>() : null;
        if (!tm)
        {
            EditorUtility.DisplayDialog("Falta el Tilemap", "No encuentro el objeto 'Mundo' con Tilemap en la escena.", "OK");
            return;
        }

        // ───────── 1) Diseño del terreno ─────────
        var alturas = new Dictionary<int, int>();
        var plataformas = new List<(int x, int n, int y)>();
        var coches = new List<int>();
        var checkpoints = new List<int>();
        var rng = new System.Random(Semilla);
        int x = XInicio, ultimoCP = XInicio;

        int[] corte = new int[5];
        for (int i = 0; i < 5; i++) corte[i] = XInicio + Mathf.RoundToInt(Largo * Cortes[i]);
        int BiomaDe(int col) { for (int i = 3; i >= 1; i--) if (col >= corte[i]) return i; return 0; }

        void Llano(int n, int top = Suelo) { for (int i = 0; i < n; i++) alturas[x++] = top; }
        void Foso(int n) { x += n; }
        void Muro(int h, int grosor) { Llano(grosor, Suelo + h); }
        void Escalera(int h)
        {
            for (int k = 1; k <= h; k++) Llano(3, Suelo + k);
            Llano(5, Suelo + h);
            for (int k = h - 1; k >= 0; k--) Llano(3, Suelo + k);
        }
        void Plataformas(int b)
        {
            int cuantas = b == 2 ? rng.Next(4, 7) : rng.Next(3, 5);
            int k = rng.Next(1, 3);
            x += b == 2 ? rng.Next(3, 5) : 3;
            for (int i = 0; i < cuantas; i++)
            {
                int w = b == 2 ? rng.Next(2, 4) : 3;          // la zona industrial: plataformas más estrechas y separadas
                int hueco = b == 2 ? rng.Next(3, 5) : 3;
                plataformas.Add((x, w, k - 1));
                x += w + hueco;
                k = Mathf.Clamp(k + rng.Next(-1, 2), 1, 3);
            }
        }
        int AnchoFoso(int b)
        {
            switch (b)
            {
                case 0: return rng.Next(3, 6);
                case 1: return rng.Next(4, 9);
                case 2: return rng.Next(5, 10);
                default: return rng.Next(3, 7);
            }
        }

        // pesos: 0 foso · 1 muro · 2 escalera · 3 plataformas · 4 doble foso · 5 coches · 6 solo llano
        float[][] pesos =
        {
            new[]{1.5f, 2.0f, 2.0f, 0.0f, 0.0f, 0.0f, 4.0f},   // barrio: pocas amenazas
            new[]{2.0f, 1.0f, 0.5f, 1.0f, 1.5f, 3.5f, 0.5f},   // avenida: coches y alcantarillas
            new[]{2.0f, 1.5f, 1.0f, 4.0f, 1.5f, 0.0f, 0.0f},   // industrial: plataformas difíciles
            new[]{1.5f, 0.5f, 2.0f, 1.5f, 0.0f, 0.0f, 3.0f},   // parque: tramo final tranquilo
        };
        int Elegir(int b)
        {
            var w = pesos[b]; double t = rng.NextDouble() * w.Sum();
            for (int i = 0; i < w.Length; i++) { t -= w[i]; if (t <= 0) return i; }
            return 6;
        }

        Llano(22);   // tramo inicial plano, donde aparece Marta
        while (x < XInicio + Largo - 60)
        {
            int b = BiomaDe(x);
            int pista = b == 0 ? rng.Next(10, 18) : rng.Next(8, 15);
            Llano(pista);
            if (x - ultimoCP > 150) { checkpoints.Add(x - pista / 2); ultimoCP = x; }

            switch (Elegir(b))
            {
                case 0: Foso(AnchoFoso(b)); break;
                case 1: Muro(b == 2 ? 1 + rng.Next(0, 2) : 1, rng.Next(2, 4)); break;
                case 2: Escalera(b == 2 ? rng.Next(1, 3) : 1); break;
                case 3: Plataformas(b); break;
                case 4: Foso(AnchoFoso(b)); Llano(rng.Next(4, 7)); Foso(AnchoFoso(b)); break;
                case 5:
                    Llano(2); coches.Add(x); Llano(3);
                    if (rng.NextDouble() < .35) { Llano(1); coches.Add(x); Llano(3); }
                    Llano(2); break;
                default: Llano(rng.Next(6, 12)); break;
            }
        }
        Llano(40);
        int xMeta = x - 20;

        // ───────── 2) Pintar el terreno con los tiles de cada bioma ─────────
        tm.ClearAllTiles();
        var suelo = new HashSet<Vector2Int>();
        foreach (var kv in alturas)
            for (int y = kv.Value; y >= Fondo; y--) suelo.Add(new Vector2Int(kv.Key, y));

        foreach (var c in suelo)
        {
            var e = Estilos[BiomaDe(c.x)];
            int d = alturas[c.x] - c.y;
            bool izq = suelo.Contains(new Vector2Int(c.x - 1, c.y));
            bool der = suelo.Contains(new Vector2Int(c.x + 1, c.y));
            int id;
            if (d == 0) id = 0;
            else
            {
                int[] clase = d == 1 ? e.c1 : d == 2 ? e.c2 : e.c3;
                id = (!izq && der) ? clase[0] : (izq && !der) ? clase[2] : clase[1];
            }
            tm.SetTile(new Vector3Int(c.x, c.y, 0), CargarTile(e.ts, id));
        }

        foreach (var pl in plataformas)
        {
            var e = Estilos[BiomaDe(pl.x)];
            for (int i = 0; i < pl.n; i++)
            {
                int id = i == 0 ? e.pL : (i == pl.n - 1 ? e.pR : e.pM);
                tm.SetTile(new Vector3Int(pl.x + i, pl.y, 0), CargarTile(e.ts, id));
                if (e.bajo >= 0 && i > 0 && i < pl.n - 1)
                    tm.SetTile(new Vector3Int(pl.x + i, pl.y - 1, 0), CargarTile(e.ts, e.bajo));
            }
        }
        tm.CompressBounds();

        // ───────── 3) Objetos de cada bioma ─────────
        bool LlanoR(int cx, int r)
        {
            for (int i = -r; i <= r; i++) if (!alturas.TryGetValue(cx + i, out var h) || h != Suelo) return false;
            return true;
        }
        int Cerca(int deseado, int r, int desde, int hasta)
        {
            for (int d = 0; d < 250; d++)
            {
                int a = deseado + d; if (a >= desde && a < hasta && LlanoR(a, r)) return a;
                a = deseado - d; if (a >= desde && a < hasta && LlanoR(a, r)) return a;
            }
            return -1;
        }
        var ocupadas = new HashSet<int>();
        void Ocupar(int c, int r) { for (int i = -r; i <= r; i++) ocupadas.Add(c + i); }

        foreach (var c in coches) Ocupar(c + 1, 3);

        var raiz = new GameObject("ElementosBiomas");

        // Piezas del escenario
        GameObject Pieza(string nombre, Transform padre, Vector3 pos, Sprite spr, int orden, Vector3? escala = null)
        {
            var g = new GameObject(nombre);
            g.transform.SetParent(padre);
            g.transform.position = pos;
            if (escala.HasValue) g.transform.localScale = escala.Value;
            if (spr) { var rend = g.AddComponent<SpriteRenderer>(); rend.sprite = spr; rend.sortingOrder = orden; }
            return g;
        }
        void Zona(GameObject g, Vector2 tam, Vector2 off)
        {
            var bc = g.AddComponent<BoxCollider2D>(); bc.isTrigger = true; bc.size = tam; bc.offset = off;
        }

        // BARRIO: fuente con farola = checkpoint (la fuente hidrata, la farola da sombra)
        int cf = Cerca(corte[0] + Mathf.RoundToInt((corte[1] - corte[0]) * .38f), 4, corte[0] + 60, corte[1] - 20);
        if (cf >= 0)
        {
            Ocupar(cf, 4);
            float wx = (cf + 0.5f) * Celda;
            var raizF = new GameObject("FuenteConFarola"); raizF.transform.SetParent(raiz.transform);
            var agua = Pieza("Fuente", raizF.transform, new Vector3(wx - 0.7f, 0.48f, 0f), sprFuente, -1);
            Zona(agua, new Vector2(2.2f, 1.6f), new Vector2(0f, .3f)); agua.AddComponent<Fuente>();
            var farola = Pieza("Farola", raizF.transform, new Vector3(wx + 0.9f, 1.28f, 0f), sprFarola, -1);
            Zona(farola, new Vector2(3.4f, 3.2f), new Vector2(-.9f, -.1f)); farola.AddComponent<ZonaSombra>();
            var cp = Pieza("Checkpoint", raizF.transform, new Vector3(wx, 2f, 0f), null, 0);
            Zona(cp, new Vector2(4f, 6f), Vector2.zero); cp.AddComponent<Checkpoint>().puntoReaparicion = new Vector2(wx, .5f);
        }

        // BARRIO: una farola suelta (sombra)
        int cl = Cerca(corte[0] + Mathf.RoundToInt((corte[1] - corte[0]) * .72f), 3, corte[0] + 60, corte[1] - 20);
        if (cl >= 0)
        {
            Ocupar(cl, 3);
            var farola = Pieza("Farola", raiz.transform, new Vector3((cl + 0.5f) * Celda, 1.28f, 0f), sprFarola, -1);
            Zona(farola, new Vector2(3.4f, 3.2f), new Vector2(0f, -.1f)); farola.AddComponent<ZonaSombra>();
        }

        // AVENIDA (desierto): oasis con palmera = checkpoint, única sombra y única fuente de la zona
        int co = Cerca(corte[1] + Mathf.RoundToInt((corte[2] - corte[1]) * .5f), 5, corte[1] + 30, corte[2] - 30);
        if (co >= 0)
        {
            Ocupar(co, 5);
            float wx = (co + 0.5f) * Celda;
            var oasis = new GameObject("Oasis"); oasis.transform.SetParent(raiz.transform);
            var lago = Pieza("Laguito", oasis.transform, new Vector3(wx - 0.6f, 0.22f, 0f), sprLaguito, -1);
            Zona(lago, new Vector2(3.4f, 1.0f), new Vector2(0f, .3f)); lago.AddComponent<Fuente>().hidratacionPorSegundo = .25f;
            var palma = Pieza("Palmera", oasis.transform, new Vector3(wx + 1.7f, 1.6f, 0f), sprPalmera, -2);
            Zona(palma, new Vector2(5.4f, 3.6f), new Vector2(-1.7f, .2f)); palma.AddComponent<ZonaSombra>();
            var cp = Pieza("Checkpoint", oasis.transform, new Vector3(wx, 2f, 0f), null, 0);
            Zona(cp, new Vector2(4f, 6f), Vector2.zero); cp.AddComponent<Checkpoint>().puntoReaparicion = new Vector2(wx, .5f);
        }

        // AVENIDA: cactus grandes
        int nCactus = 0;
        for (int c = corte[1] + 12; c < corte[2] - 8; c += rng.Next(18, 34))
        {
            int a = Cerca(c, 1, corte[1] + 5, corte[2] - 5);
            if (a < 0 || ocupadas.Contains(a)) continue;
            Ocupar(a, 2); nCactus++;
            Pieza("Cactus_" + nCactus, raiz.transform, new Vector3((a + .5f) * Celda, 1.12f, 0f), sprCactus, -1);
        }

        // Coches (avenida)
        var colores = new[] { new Color(.85f, .2f, .2f), new Color(.95f, .95f, .9f), new Color(.25f, .45f, .85f), new Color(.95f, .75f, .2f) };
        foreach (var c in coches)
        {
            var go = new GameObject("Coche"); go.transform.SetParent(raiz.transform);
            go.transform.position = new Vector3((c + 1.5f) * Celda, 0.44f, 0f);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprCoche; sr.sortingOrder = 3;
            sr.color = colores[rng.Next(colores.Length)];
            var bc = go.AddComponent<BoxCollider2D>(); bc.size = new Vector2(2.3f, .72f); bc.offset = new Vector2(0, -.04f);
        }

        // Chorros de aire caliente (zona industrial)
        int nChorros = 0;
        for (int c = corte[2] + 30; c < corte[3] - 20; c += rng.Next(55, 95))
        {
            int a = Cerca(c, 3, corte[2] + 10, corte[3] - 10);
            if (a < 0 || ocupadas.Contains(a)) continue;
            Ocupar(a, 3); nChorros++;
            var go = new GameObject("AireCaliente_" + nChorros); go.transform.SetParent(raiz.transform);
            go.transform.position = new Vector3((a + 0.5f) * Celda, 2.6f, 0f);
            var bc = go.AddComponent<BoxCollider2D>(); bc.isTrigger = true; bc.size = new Vector2(1.6f, 5f);
            Pieza("Rejilla", go.transform, new Vector3((a + 0.5f) * Celda, 0.15f, 0f), sprRejilla, 2);   // la rendija de la que sale el calor
            var vis = new GameObject("Visual"); vis.transform.SetParent(go.transform, false);
            vis.transform.localScale = new Vector3(1.6f, 5f, 1f);
            var sr = vis.AddComponent<SpriteRenderer>(); sr.sprite = sprCuadrado; sr.sortingOrder = 18;
            var ac = go.AddComponent<AireCaliente>();
            ac.visual = sr; ac.desfase = (float)(rng.NextDouble() * ac.periodo);
        }

        // PARQUE: un árbol enorme hacia el medio de la zona, el único que REDUCE el calor
        int cg = Cerca(corte[3] + (XInicio + Largo - corte[3]) / 2 - 20, 6, corte[3] + 20, XInicio + Largo - 80);
        if (cg >= 0)
        {
            Ocupar(cg, 6);
            var grande = Pieza("ArbolGrande", raiz.transform, new Vector3((cg + 0.5f) * Celda, 3.1f, 0f), sprArbol, -2, new Vector3(2.2f, 2.2f, 1f));
            Zona(grande, new Vector2(2.8f, 3.0f), new Vector2(0f, .2f)); grande.AddComponent<ZonaSombra>();
        }

        // PARQUE: árboles normales (solo frenan el calor, no lo reducen)
        int nArboles = 0;
        for (int c = corte[3] + 10; c < XInicio + Largo - 70; c += rng.Next(22, 38))
        {
            int a = Cerca(c, 2, corte[3] + 5, XInicio + Largo - 60);
            if (a < 0 || ocupadas.Contains(a)) continue;
            Ocupar(a, 3); nArboles++;
            var go = new GameObject("Arbol_" + nArboles); go.transform.SetParent(raiz.transform);
            go.transform.position = new Vector3((a + 0.5f) * Celda, 1.4f, 0f);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprArbol; sr.sortingOrder = -2;
            var bc = go.AddComponent<BoxCollider2D>(); bc.isTrigger = true; bc.size = new Vector2(2.6f, 3.2f); bc.offset = new Vector2(0, .2f);
            go.AddComponent<ZonaSombra>().reduceCalor = false;
        }

        // Decoración sin colisión (farolas, cactus, rocas, hierba...)
        var decoGO = new GameObject("Decoracion");
        decoGO.transform.SetParent(tm.transform.parent, false);
        var tmDeco = decoGO.AddComponent<Tilemap>();
        decoGO.AddComponent<TilemapRenderer>().sortingOrder = -1;
        int ultimaDeco = -99;
        foreach (var kv in alturas.OrderBy(k => k.Key))
        {
            int c = kv.Key;
            if (kv.Value != Suelo || ocupadas.Contains(c) || c - ultimaDeco < 4 || rng.NextDouble() > .12) continue;
            var e = Estilos[BiomaDe(c)];
            tmDeco.SetTile(new Vector3Int(c, 0, 0), CargarTile(e.ts, e.deco[rng.Next(e.deco.Length)]));
            ultimaDeco = c;
        }

        // ───────── 4) Muerte, checkpoints y meta ─────────
        var nivel = new GameObject("ElementosNivel");
        var zm = new GameObject("ZonaMuerte");
        zm.transform.SetParent(nivel.transform);
        float ancho = (Largo + 120) * Celda;
        zm.transform.position = new Vector3(XInicio * Celda + ancho / 2f - 20f, -9f, 0f);
        var bcZ = zm.AddComponent<BoxCollider2D>(); bcZ.isTrigger = true; bcZ.size = new Vector2(ancho + 80f, 8f);
        zm.AddComponent<ZonaMuerte>();

        for (int i = 0; i < checkpoints.Count; i++)
        {
            float wx = (checkpoints[i] + 0.5f) * Celda;
            var go = new GameObject("Checkpoint_" + (i + 1)); go.transform.SetParent(nivel.transform);
            go.transform.position = new Vector3(wx, 2f, 0f);
            var bc = go.AddComponent<BoxCollider2D>(); bc.isTrigger = true; bc.size = new Vector2(1f, 10f);
            go.AddComponent<Checkpoint>().puntoReaparicion = new Vector2(wx, 0.5f);
        }

        var meta = new GameObject("Meta"); meta.transform.SetParent(nivel.transform);
        meta.transform.position = new Vector3((xMeta + 0.5f) * Celda, 4f, 0f);
        var bcM = meta.AddComponent<BoxCollider2D>(); bcM.isTrigger = true; bcM.size = new Vector2(2f, 12f);
        meta.AddComponent<Meta>();

        // ───────── 5) Fondo en carrusel ─────────
        CrearFondo(corte);

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        var lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!lista.Exists(s => s.path == Destino)) lista.Add(new EditorBuildSettingsScene(Destino, true));
        EditorBuildSettings.scenes = lista.ToArray();
        AssetDatabase.SaveAssets();

        Debug.Log($"Nivel1: {Largo} columnas, 4 biomas, {coches.Count} coches, {nChorros} chorros de aire caliente, {nArboles} árboles, {checkpoints.Count} checkpoints.");

        // ───────── 6) Encadenar el resto ─────────
        ArreglarColisiones.Arreglar();
        CrearUIJuego.CrearHUD();
        ConfigurarCalorNivel.Configurar();
    }

    // ───────────────────────── Fondo ─────────────────────────
    static void CrearFondo(int[] corte)
    {
        Sprite S(string nombre) => BuscarSprite(nombre);
        var cielo = S("SKY_NO_SUN"); var sol = S("IMG_0181");
        var lejos = S("BACKGROUND"); var medio = S("MIDDLEGROUND"); var cerca = S("FOREGROUND");
        var industrial = S("IndustrialBG_Full");
        if (!cielo || !lejos || !medio || !cerca)
            Debug.LogWarning("Faltan algunos sprites de fondo en Assets/Elementos_Escenario/Background; el fondo saldrá incompleto.");

        CapaFondo Capa(string n, Sprite s, float par, float esc, float y, int orden, Color? col = null) =>
            new CapaFondo { nombre = n, sprite = s, parallax = par, escala = esc, yBase = y, orden = orden, color = col ?? Color.white };
        // El sol no está centrado en su imagen (2000x2500): compensamos para colocarlo donde queremos
        CapaFondo Sol(float esc, float dx, float dy, Color col) => new CapaFondo
        {
            nombre = "Sol", sprite = sol, unica = true, escala = esc, orden = -95, color = col,
            desplazamiento = new Vector2(dx + 1.87f * esc, dy - 0.18f * esc)
        };
        float W(int c) => c * Celda;

        var go = new GameObject("Fondo");
        var fb = go.AddComponent<FondoBiomas>();
        fb.cielo = cielo;
        fb.biomas = new[]
        {
            new ConjuntoBioma { nombre = Nombres[0], xInicio = W(corte[0]), xFin = W(corte[1]), tinteCielo = Color.white,
                capas = new[] { Sol(.7f, 7f, 3.5f, Color.white),
                                Capa("Lejos", lejos, .12f, .5f, -2.5f, -80),
                                // Edificios enormes que pasan casi tan rápido como tú, como si corrieras frente a ellos
                                Capa("Medio", medio, .55f, 1.5f, -4f, -70, new Color(.8f, .75f, .85f)),
                                Capa("Cerca", cerca, .85f, 1.9f, -5f, -60, new Color(.65f, .5f, .55f)) } },
            new ConjuntoBioma { nombre = Nombres[1], xInicio = W(corte[1]), xFin = W(corte[2]), tinteCielo = new Color(1f, .82f, .6f),
                capas = new[] { Sol(1.3f, 6f, 3f, new Color(1f, .85f, .6f)),
                                Capa("Medio", medio, .2f, .6f, -2.8f, -70),
                                Capa("Cerca", cerca, .45f, .8f, -2.5f, -60) } },
            new ConjuntoBioma { nombre = Nombres[2], xInicio = W(corte[2]), xFin = W(corte[3]), tinteCielo = new Color(1f, .75f, .6f),
                capas = new[] { Capa("Industria", industrial, .1f, 3.9f, -3.5f, -90),
                                Capa("Cerca", cerca, .5f, .8f, -2.5f, -60, new Color(.35f, .25f, .28f, .9f)) } },
            new ConjuntoBioma { nombre = Nombres[3], xInicio = W(corte[3]), xFin = W(corte[4]), tinteCielo = new Color(.85f, 1f, .9f),
                capas = new[] { Sol(.9f, 7f, 3.5f, Color.white),
                                Capa("Lejos", lejos, .12f, .5f, -2.5f, -80, new Color(.75f, 1f, .85f)),
                                Capa("Medio", medio, .25f, .6f, -2.8f, -70, new Color(.7f, .95f, .8f)) } },
        };
    }

    static Sprite BuscarSprite(string nombre)
    {
        foreach (var guid in AssetDatabase.FindAssets(nombre + " t:Sprite"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(ruta).ToLower() == nombre.ToLower())
                return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        }
        return null;
    }

    // ───────────────────────── Tiles ─────────────────────────
    // Grassland ya trae Tile assets; para el resto los creamos a partir de los sprites del PNG.
    static TileBase CargarTile(string ts, int i)
    {
        string clave = ts + "_" + i;
        if (cacheTiles.TryGetValue(clave, out var t) && t) return t;

        var tile = AssetDatabase.LoadAssetAtPath<TileBase>(CarpetaTilesets + $"Tiles_{ts}_Size_500_Percent_{i}.asset");
        if (!tile)
        {
            string ruta = $"{CarpetaTiles}/{ts}_{i}.asset";
            tile = AssetDatabase.LoadAssetAtPath<TileBase>(ruta);
            if (!tile)
            {
                var sprite = AssetDatabase.LoadAllAssetsAtPath(CarpetaTilesets + $"Tiles_{ts}_Size_500_Percent.png")
                    .OfType<Sprite>().FirstOrDefault(s => s.name == $"Tiles_{ts}_Size_500_Percent_{i}");
                if (!sprite) { Debug.LogError($"No encuentro el sprite {ts} #{i}"); return null; }
                if (!AssetDatabase.IsValidFolder(CarpetaTiles)) AssetDatabase.CreateFolder("Assets/Nivel", "Tiles");
                var nuevo = ScriptableObject.CreateInstance<Tile>();
                nuevo.sprite = sprite; nuevo.colliderType = Tile.ColliderType.Sprite;
                AssetDatabase.CreateAsset(nuevo, ruta);
                tile = nuevo;
            }
        }
        cacheTiles[clave] = tile;
        return tile;
    }
}
