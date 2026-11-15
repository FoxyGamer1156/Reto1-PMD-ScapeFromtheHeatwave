using UnityEngine;

/// <summary>
/// Trigger de sombra. Mientras Marta está dentro la música suena con efecto lofi y:
///  - reduceCalor = true  → el calor BAJA (farolas, palmera del oasis, árbol grande)
///  - reduceCalor = false → el calor solo deja de SUBIR (árboles normales del parque)
/// </summary>
public class ZonaSombra : MonoBehaviour
{
    public bool reduceCalor = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        var e = other.GetComponentInParent<EstadoJugador>();
        if (e) e.EntrarSombra(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var e = other.GetComponentInParent<EstadoJugador>();
        if (e) e.SalirSombra(this);
    }

    void OnDisable()
    {
        if (EstadoJugador.Instancia) EstadoJugador.Instancia.SalirSombra(this);
    }
}
