using UnityEngine;

/// <summary>
/// Plataforma que va y viene entre dos puntos.
/// Los puntos A y B tienen que ser objetos vacíos que NO sean hijos de la plataforma
/// (si no, se moverían con ella). Cuando Marta se sube, se hace hija de la plataforma
/// para viajar con ella.
/// </summary>
public class PlataformaMovil : MonoBehaviour
{
    public Transform puntoA;
    public Transform puntoB;
    public float velocidad = 2f;

    private Transform destino;

    /// <summary>
    /// Coloca la plataforma en el punto A y la manda hacia el B.
    /// </summary>
    void Start()
    {
        transform.position = puntoA.position;
        destino = puntoB;
    }

    /// <summary>
    /// Mueve la plataforma hacia el destino y, al llegar, cambia al otro punto.
    /// </summary>
    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, destino.position, velocidad * Time.deltaTime);

        if (Vector3.Distance(transform.position, destino.position) < 0.05f)
        {
            if (destino == puntoB)
            {
                destino = puntoA;
            }
            else
            {
                destino = puntoB;
            }
        }
    }

    /// <summary>
    /// Cuando el jugador se sube, lo hace hijo de la plataforma para que se mueva con ella.
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }

    /// <summary>
    /// Cuando el jugador se baja, deja de ser hijo de la plataforma.
    /// </summary>
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }
}
