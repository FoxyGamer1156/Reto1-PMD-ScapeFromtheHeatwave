using UnityEngine;

/// <summary>
/// Al esprintar salen rayitas desde el lado hacia el que corre Marta y cruzan la pantalla hacia atrás,
/// como el viento. Sin assets: usa un sprite blanco creado en código.
/// </summary>
[RequireComponent(typeof(Controles))]
public class EfectoViento : MonoBehaviour
{
    public int maximo = 30;
    public float velocidadMinima = 8f;       // velocidad a partir de la cual se ven
    public float rayasPorSegundo = 40f;
    public Vector2 largo = new Vector2(1.2f, 3.2f);
    public Vector2 grosor = new Vector2(.03f, .07f);
    public Vector2 velocidadRaya = new Vector2(12f, 22f);
    public Color color = new Color(1f, 1f, 1f, .55f);
    public int ordenDibujo = 40;

    class Raya { public Transform t; public SpriteRenderer sr; public float vida, total, vel, dir; public bool viva; }

    Controles ctrl;
    Rigidbody2D rb;
    Raya[] pool;
    Sprite sprite;
    float intensidad, acumulado;

    void Awake()
    {
        ctrl = GetComponent<Controles>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f), 4f);
        var contenedor = new GameObject("RayasViento").transform;
        pool = new Raya[maximo];
        for (int i = 0; i < maximo; i++)
        {
            var go = new GameObject("raya");
            go.transform.SetParent(contenedor, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = ordenDibujo; sr.enabled = false;
            pool[i] = new Raya { t = go.transform, sr = sr };
        }
    }

    void OnDestroy()
    {
        if (pool != null && pool.Length > 0 && pool[0].t) Destroy(pool[0].t.parent.gameObject);
    }

    void Update()
    {
        if (pool == null || Time.timeScale == 0f) return;
        float dt = Time.deltaTime;
        float vx = rb.linearVelocity.x;
        bool corre = ctrl.EstaCorriendo && Mathf.Abs(vx) > velocidadMinima;
        intensidad = Mathf.MoveTowards(intensidad, corre ? 1f : 0f, dt * 4f);

        if (intensidad > .05f)
        {
            acumulado += rayasPorSegundo * intensidad * dt;
            while (acumulado >= 1f) { acumulado -= 1f; Lanzar(Mathf.Sign(vx)); }
        }

        foreach (var r in pool)
        {
            if (!r.viva) continue;
            r.vida -= dt;
            if (r.vida <= 0f) { r.viva = false; r.sr.enabled = false; continue; }
            r.t.position += Vector3.left * (r.dir * r.vel * dt);     // se van hacia atrás
            var c = color; c.a *= Mathf.Sin(Mathf.PI * (1f - r.vida / r.total));   // entran y salen suaves
            r.sr.color = c;
        }
    }

    void Lanzar(float dir)
    {
        Raya r = null;
        foreach (var p in pool) if (!p.viva) { r = p; break; }
        if (r == null) return;

        var cam = Camera.main;
        float mediaAncho = cam ? cam.orthographicSize * cam.aspect : 10f;
        float mediaAlto = cam ? cam.orthographicSize : 6f;
        Vector3 c = cam ? cam.transform.position : transform.position;

        float x = c.x + dir * mediaAncho * Random.Range(.55f, 1.05f);
        float y = c.y + Random.Range(-mediaAlto * .9f, mediaAlto * .9f);
        r.t.position = new Vector3(x, y, 0f);
        r.t.localScale = new Vector3(Random.Range(largo.x, largo.y), Random.Range(grosor.x, grosor.y), 1f);
        r.dir = dir; r.vel = Random.Range(velocidadRaya.x, velocidadRaya.y);
        r.total = r.vida = Random.Range(.35f, .7f);
        r.viva = true; r.sr.enabled = true;
    }
}
