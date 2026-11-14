using UnityEngine;

/// <summary>
/// Barras de calor e hidratación de Marta.
/// El calor sube al sol y baja en la sombra; si llega al máximo, se pierde la partida.
/// La hidratación se gasta al andar, correr y saltar, y se recupera en las fuentes;
/// si llega a 0, Marta no puede correr hasta recuperar un poco.
/// Las zonas del nivel se reconocen por su tag: Sombra, Fuente, Lava y Meta.
/// </summary>
public class EstadoJugador : MonoBehaviour
{
    [Header("Calor")]
    public float calor = 0f;
    public float calorMaximo = 100f;
    public float subidaCalorAlSol = 4f;
    public float bajadaCalorEnSombra = 8f;

    [Header("Hidratación")]
    public float hidratacion = 100f;
    public float hidratacionMaxima = 100f;
    public float gastoAndando = 1f;
    public float gastoCorriendo = 4f;
    public float gastoPorSalto = 2f;
    public float recuperacionEnFuente = 15f;
    public float hidratacionParaVolverACorrer = 25f;

    [Header("Muerte")]
    public float segundosAntesDeDerrota = 2f;

    private Controles controles;
    private Animator animator;
    private bool enSombra = false;
    private bool enFuente = false;
    private float tiempoProteccion = 0f;
    private float multiplicadorProteccion = 1f;
    private bool muerto = false;

    /// <summary>
    /// Guarda los componentes que se usan después.
    /// </summary>
    void Start()
    {
        controles = GetComponent<Controles>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Cada frame actualiza el calor, la hidratación y la protección, y comprueba la insolación.
    /// </summary>
    void Update()
    {
        if (muerto) return;

        ActualizarCalor();
        ActualizarHidratacion();
        ActualizarProteccion();

        if (calor >= calorMaximo)
        {
            Morir();
        }
    }

    /// <summary>
    /// Sube el calor al sol (menos si Marta está protegida) o lo baja en la sombra.
    /// </summary>
    private void ActualizarCalor()
    {
        if (enSombra)
        {
            calor = calor - bajadaCalorEnSombra * Time.deltaTime;
        }
        else
        {
            float subida = subidaCalorAlSol;
            if (tiempoProteccion > 0f)
            {
                subida = subida * multiplicadorProteccion;
            }
            calor = calor + subida * Time.deltaTime;
        }

        calor = Mathf.Clamp(calor, 0f, calorMaximo);
    }

    /// <summary>
    /// Gasta hidratación al moverse, la recupera en las fuentes y decide si Marta puede correr.
    /// </summary>
    private void ActualizarHidratacion()
    {
        if (controles.EstaCorriendo)
        {
            hidratacion = hidratacion - gastoCorriendo * Time.deltaTime;
        }
        else if (controles.SeMueve)
        {
            hidratacion = hidratacion - gastoAndando * Time.deltaTime;
        }

        if (enFuente)
        {
            hidratacion = hidratacion + recuperacionEnFuente * Time.deltaTime;
        }

        hidratacion = Mathf.Clamp(hidratacion, 0f, hidratacionMaxima);

        if (hidratacion <= 0f)
        {
            controles.puedeCorrer = false;
        }
        else if (hidratacion >= hidratacionParaVolverACorrer)
        {
            controles.puedeCorrer = true;
        }
    }

    /// <summary>
    /// Temporizador de la protección: le resta el tiempo que pasa hasta llegar a 0.
    /// </summary>
    private void ActualizarProteccion()
    {
        if (tiempoProteccion > 0f)
        {
            tiempoProteccion = tiempoProteccion - Time.deltaTime;
        }
    }

    /// <summary>
    /// Resta hidratación al saltar. Lo llama <see cref="Controles"/>.
    /// </summary>
    public void GastarPorSalto()
    {
        hidratacion = Mathf.Max(0f, hidratacion - gastoPorSalto);
    }

    /// <summary>
    /// Recupera hidratación de golpe (por ejemplo, al beber una botella de agua).
    /// </summary>
    /// <param name="cantidad">Puntos de hidratación que se recuperan.</param>
    public void Beber(float cantidad)
    {
        hidratacion = Mathf.Min(hidratacionMaxima, hidratacion + cantidad);
    }

    /// <summary>
    /// Hace que durante un tiempo el calor suba más despacio (gorra, sombrilla).
    /// </summary>
    /// <param name="multiplicador">0.5 = el calor sube la mitad de rápido.</param>
    /// <param name="segundos">Cuánto dura la protección.</param>
    public void Proteger(float multiplicador, float segundos)
    {
        multiplicadorProteccion = multiplicador;
        tiempoProteccion = segundos;
    }

    /// <summary>
    /// Marta muere: deja de moverse, reproduce la animación de daño y,
    /// pasados <see cref="segundosAntesDeDerrota"/>, avisa de la derrota.
    /// </summary>
    public void Morir()
    {
        if (muerto) return;
        muerto = true;

        controles.enabled = false;
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        animator.SetTrigger("Dano");

        Invoke("AvisarDerrota", segundosAntesDeDerrota);
    }

    /// <summary>
    /// Avisa al GameManager de que se ha perdido la partida.
    /// </summary>
    private void AvisarDerrota()
    {
        if (GameManager.instancia != null)
        {
            GameManager.instancia.Perder();
        }
    }

    /// <summary>
    /// Al entrar en una zona, mira su tag: sombra, fuente, lava o meta.
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Sombra"))
        {
            enSombra = true;
        }
        else if (other.CompareTag("Fuente"))
        {
            enFuente = true;
        }
        else if (other.CompareTag("Lava"))
        {
            Morir();
        }
        else if (other.CompareTag("Meta"))
        {
            if (GameManager.instancia != null)
            {
                GameManager.instancia.Ganar();
            }
        }
    }

    /// <summary>
    /// Al salir de una sombra o de una fuente, deja de aplicar su efecto.
    /// </summary>
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Sombra"))
        {
            enSombra = false;
        }
        else if (other.CompareTag("Fuente"))
        {
            enFuente = false;
        }
    }

    /// <summary>
    /// Por si la lava es un suelo sólido en vez de un trigger.
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Lava"))
        {
            Morir();
        }
    }
}
