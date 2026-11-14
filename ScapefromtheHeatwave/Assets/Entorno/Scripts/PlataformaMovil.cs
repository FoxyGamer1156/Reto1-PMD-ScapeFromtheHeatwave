using UnityEngine;

/// <summary>
/// Plataforma que va y viene en línea recta desde donde se coloca en la escena.
/// El vector "recorrido" indica hacia dónde y cuánto se mueve:
/// (3, 0) horizontal, (0, 2) vertical, (3, 2) diagonal.
/// Mientras Marta está encima, le pasa a <see cref="Controles"/> la velocidad horizontal
/// de la plataforma para que viaje con ella sin dejar de poder moverse libremente.
/// En vertical, la propia plataforma la empuja hacia arriba y la gravedad la baja.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlataformaMovil : MonoBehaviour
{
    public Vector2 recorrido = new Vector2(3f, 0f);
    public float velocidad = 2f;

    private Rigidbody2D rb;
    private Controles jugador;
    private Vector2 puntoInicio;
    private Vector2 puntoFinal;
    private Vector2 destino;

    /// <summary>
    /// Prepara el Rigidbody (Kinematic e interpolado para que se vea suave),
    /// calcula los dos extremos a partir de la posición inicial y empieza yendo al final.
    /// </summary>
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        puntoInicio = rb.position;
        puntoFinal = puntoInicio + recorrido;
        destino = puntoFinal;
    }

    /// <summary>
    /// En cada paso de física mueve la plataforma hacia el destino,
    /// pasa su velocidad al jugador si está encima y, al llegar, cambia de extremo.
    /// </summary>
    void FixedUpdate()
    {
        Vector2 posicionAnterior = rb.position;
        Vector2 posicionNueva = Vector2.MoveTowards(posicionAnterior, destino, velocidad * Time.fixedDeltaTime);
        rb.MovePosition(posicionNueva);

        if (jugador != null)
        {
            float velocidadX = (posicionNueva.x - posicionAnterior.x) / Time.fixedDeltaTime;
            jugador.velocidadPlataforma = velocidadX;
        }

        if (Vector2.Distance(posicionNueva, destino) < 0.01f)
        {
            if (destino == puntoFinal)
            {
                destino = puntoInicio;
            }
            else
            {
                destino = puntoFinal;
            }
        }
    }

    /// <summary>
    /// Cuando el jugador toca la plataforma, guarda su script de controles.
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            jugador = collision.gameObject.GetComponent<Controles>();
        }
    }

    /// <summary>
    /// Cuando el jugador deja de tocar la plataforma, le quita la velocidad de la plataforma.
    /// </summary>
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && jugador != null)
        {
            jugador.velocidadPlataforma = 0f;
            jugador = null;
        }
    }

    /// <summary>
    /// Dibuja en la vista Scene una línea con el recorrido de la plataforma.
    /// </summary>
    void OnDrawGizmos()
    {
        Vector2 inicio = transform.position;
        if (Application.isPlaying)
        {
            inicio = puntoInicio;
        }

        Vector2 final = inicio + recorrido;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(inicio, final);
        Gizmos.DrawWireSphere(final, 0.2f);
    }
}
