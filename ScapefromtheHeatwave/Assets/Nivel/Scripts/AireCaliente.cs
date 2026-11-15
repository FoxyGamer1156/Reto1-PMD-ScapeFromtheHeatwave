using UnityEngine;

/// <summary>
/// Chorro de aire caliente periódico (zona industrial). Se avisa durante 1 s antes de activarse;
/// mientras está activo y Marta está dentro, sube el calor extra.
/// </summary>
public class AireCaliente : MonoBehaviour
{
    public float periodo = 7f;
    public float duracionActiva = 3f;
    public float desfase = 0f;
    [Tooltip("Calor extra por segundo mientras está activo")]
    public float calorPorSegundo = 0.12f;
    public SpriteRenderer visual;
    public Color color = new Color(1f, .55f, .15f);

    EstadoJugador dentro;

    void OnTriggerEnter2D(Collider2D other)
    {
        var e = other.GetComponentInParent<EstadoJugador>();
        if (e) dentro = e;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<EstadoJugador>() == dentro) dentro = null;
    }

    void Update()
    {
        float t = Mathf.Repeat(Time.time + desfase, periodo);
        bool activo = t < duracionActiva;
        float aviso = Mathf.InverseLerp(periodo - 1f, periodo, t);          // último segundo: avisa

        if (activo && dentro) dentro.AnadirCalor(calorPorSegundo * Time.deltaTime);

        if (visual)
        {
            float alfa = activo ? .30f + .10f * Mathf.Sin(Time.time * 14f) : .04f + .16f * aviso;
            var c = color; c.a = alfa; visual.color = c;
        }
    }
}
