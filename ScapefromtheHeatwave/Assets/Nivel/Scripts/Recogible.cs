using UnityEngine;

/// <summary>Gorra o botella en el suelo. Al tocarla Marta pasa al inventario (si está vacío).</summary>
public class Recogible : MonoBehaviour
{
    public TipoObjeto tipo = TipoObjeto.Botella;
    public Sprite icono;                 // si está vacío usa el sprite del SpriteRenderer
    public float flotacion = 0.1f;       // sube y baja para llamar la atención
    Vector3 posBase;

    void Start() { posBase = transform.position; }

    void Update()
    {
        transform.position = posBase + Vector3.up * (Mathf.Sin(Time.time * 3f) * flotacion);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var inv = other.GetComponentInParent<Inventario>();
        if (!inv) return;
        var sr = GetComponent<SpriteRenderer>();
        var sprite = icono ? icono : (sr ? sr.sprite : null);
        if (inv.TryRecoger(tipo, sprite)) Destroy(gameObject);
    }
}
