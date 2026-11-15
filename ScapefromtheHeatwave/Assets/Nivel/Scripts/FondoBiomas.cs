using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CapaFondo
{
    public string nombre;
    public Sprite sprite;
    [Tooltip("0 = pegada a la pantalla (muy lejana), 1 = se mueve como el mundo")]
    public float parallax = 0.2f;
    public float escala = 1f;
    [Tooltip("Y mundo del borde inferior de la capa (con la cámara en yReferencia)")]
    public float yBase = -2f;
    public int orden = -80;
    public Color color = Color.white;
    [Tooltip("Una sola imagen fija a la cámara (el sol) en vez de carrusel")]
    public bool unica;
    public Vector2 desplazamiento;
}

[System.Serializable]
public class ConjuntoBioma
{
    public string nombre;
    [Tooltip("X mundo donde empieza el bioma (el primero ignora este valor)")]
    public float xInicio;
    [Tooltip("X mundo donde termina el bioma (el último ignora este valor)")]
    public float xFin;
    public Color tinteCielo = Color.white;
    public CapaFondo[] capas;
}

/// <summary>
/// Fondo de parallax en carrusel, con un conjunto de capas distinto por bioma.
/// Cada capa se repite en espejo (copias alternas volteadas), así no se ve la costura.
/// Al cruzar de un bioma a otro las capas se funden suavemente.
/// </summary>
[DefaultExecutionOrder(1000)]
public class FondoBiomas : MonoBehaviour
{
    public Sprite cielo;
    public ConjuntoBioma[] biomas;
    public float anchoTransicion = 16f;
    public float yReferencia = 1f;
    public int copias = 7;

    class CapaRuntime
    {
        public CapaFondo datos;
        public int bioma;
        public Transform raiz;
        public SpriteRenderer[] renderers;
        public float ancho, alto;
    }

    readonly List<CapaRuntime> capas = new List<CapaRuntime>();
    SpriteRenderer cieloRenderer;
    Transform cam;
    float[] pesos;

    void Start()
    {
        if (biomas == null || biomas.Length == 0) return;
        pesos = new float[biomas.Length];

        if (cielo)
        {
            var go = new GameObject("Cielo");
            go.transform.SetParent(transform, false);
            cieloRenderer = go.AddComponent<SpriteRenderer>();
            cieloRenderer.sprite = cielo;
            cieloRenderer.sortingOrder = -100;
            var s = cielo.bounds.size;
            go.transform.localScale = new Vector3(50f / s.x, 30f / s.y, 1f);   // cubre pantalla incluso con zoom out
        }

        for (int i = 0; i < biomas.Length; i++)
        {
            if (biomas[i].capas == null) continue;
            foreach (var d in biomas[i].capas)
            {
                if (d == null || !d.sprite) continue;
                var c = new CapaRuntime { datos = d, bioma = i };
                var raiz = new GameObject($"{biomas[i].nombre} - {d.nombre}").transform;
                raiz.SetParent(transform, false);
                c.raiz = raiz;
                var tam = d.sprite.bounds.size;
                c.ancho = tam.x * d.escala; c.alto = tam.y * d.escala;

                int n = d.unica ? 1 : copias;
                c.renderers = new SpriteRenderer[n];
                for (int k = 0; k < n; k++)
                {
                    var h = new GameObject("c" + k);
                    h.transform.SetParent(raiz, false);
                    h.transform.localScale = new Vector3(d.escala, d.escala, 1f);
                    var sr = h.AddComponent<SpriteRenderer>();
                    sr.sprite = d.sprite; sr.sortingOrder = d.orden;
                    c.renderers[k] = sr;
                }
                capas.Add(c);
            }
        }
    }

    float Peso(int i, float x)
    {
        float t = anchoTransicion * 0.5f;
        float a = i == 0 ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(biomas[i].xInicio - t, biomas[i].xInicio + t, x));
        float b = i == biomas.Length - 1 ? 1f : 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(biomas[i].xFin - t, biomas[i].xFin + t, x));
        return Mathf.Min(a, b);
    }

    /// <summary>Índice del bioma en una posición X (0..3).</summary>
    public int BiomaEn(float x)
    {
        for (int i = biomas.Length - 1; i > 0; i--) if (x >= biomas[i].xInicio) return i;
        return 0;
    }

    void LateUpdate()
    {
        if (pesos == null) return;
        if (!cam) { var c = Camera.main; if (!c) return; cam = c.transform; }
        Vector3 p = cam.position;

        for (int i = 0; i < pesos.Length; i++) pesos[i] = Peso(i, p.x);

        if (cieloRenderer)
        {
            Color t = Color.black;
            for (int i = 0; i < pesos.Length; i++) t += biomas[i].tinteCielo * pesos[i];
            t.a = 1f;
            cieloRenderer.color = t;
            cieloRenderer.transform.position = new Vector3(p.x, p.y, 0f);
        }

        foreach (var c in capas)
        {
            float w = pesos[c.bioma];
            bool visible = w > 0.01f;
            foreach (var r in c.renderers) r.enabled = visible;
            if (!visible) continue;

            var d = c.datos;
            Color col = d.color; col.a *= w;

            if (d.unica)
            {
                c.renderers[0].color = col;
                c.raiz.position = new Vector3(p.x + d.desplazamiento.x, p.y + d.desplazamiento.y, 0f);
                continue;
            }

            float periodo = 2f * c.ancho;                       // la copia espejo hace que el ciclo dure 2 anchos
            float desfase = Mathf.Repeat(p.x * d.parallax, periodo);
            c.raiz.position = new Vector3(p.x, d.yBase + c.alto * 0.5f + (p.y - yReferencia) * (1f - d.parallax), 0f);

            int mitad = c.renderers.Length / 2;
            for (int k = 0; k < c.renderers.Length; k++)
            {
                int idx = k - mitad;
                var r = c.renderers[k];
                r.color = col;
                r.flipX = (((idx % 2) + 2) % 2) == 1;
                r.transform.localPosition = new Vector3(idx * c.ancho - desfase, 0f, 0f);
            }
        }
    }
}
