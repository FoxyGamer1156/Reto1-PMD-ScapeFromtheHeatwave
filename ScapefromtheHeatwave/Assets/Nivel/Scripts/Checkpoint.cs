using UnityEngine;

/// <summary>Inicio y último punto de control alcanzado (se reinicia al cargar el nivel).</summary>
public static class PuntoControl
{
    public static bool HayPunto { get; private set; }
    public static Vector2 Punto { get; private set; }
    public static Vector2 Inicio { get; private set; }

    public static void Establecer(Vector2 p) { Punto = p; HayPunto = true; }
    public static void EstablecerInicio(Vector2 p) { Inicio = p; }
    public static void Reiniciar() { HayPunto = false; }

    /// <summary>Coloca a Marta en el último checkpoint (o en el inicio), la frena y le suma calorExtra (0..1).</summary>
    public static void Reaparecer(Controles marta, float calorExtra = 0f)
    {
        Vector2 destino = HayPunto ? Punto : Inicio;
        var rb = marta.GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.position = destino;
        marta.transform.position = destino;
        var estado = marta.GetComponent<EstadoJugador>();
        if (estado && calorExtra > 0f) estado.AnadirCalor(calorExtra);
    }
}

/// <summary>Trigger: al pasar Marta, guarda dónde reaparecerá si cae.</summary>
public class Checkpoint : MonoBehaviour
{
    public Vector2 puntoReaparicion;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<Controles>() == null) return;
        bool nuevo = !PuntoControl.HayPunto || PuntoControl.Punto != puntoReaparicion;
        PuntoControl.Establecer(puntoReaparicion);
        if (nuevo) SFX.Play("checkpoint", .8f, 0f);
    }
}
