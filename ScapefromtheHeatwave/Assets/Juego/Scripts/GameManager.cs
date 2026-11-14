using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla la partida: pasa el calor y la hidratación del jugador al HUD,
/// lleva la cuenta atrás y muestra el panel de victoria o de derrota.
/// Desde otros scripts se usa con GameManager.instancia.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instancia;

    [Header("Referencias")]
    public EstadoJugador jugador;
    public GameObject panelDerrota;
    public GameObject panelVictoria;

    [Header("Cuenta atrás")]
    public bool usarCuentaAtras = true;
    public float tiempoRestante = 180f;

    [Header("Escenas")]
    public string escenaMenu = "MenuPrincipal";

    [HideInInspector] public bool partidaTerminada = false;

    /// <summary>
    /// Se guarda en la variable estática para que otros scripts lo encuentren.
    /// </summary>
    void Awake()
    {
        instancia = this;
    }

    /// <summary>
    /// Pone el tiempo del juego en marcha, busca al jugador por su tag si no está asignado
    /// y oculta los paneles de final de partida.
    /// </summary>
    void Start()
    {
        Time.timeScale = 1f;

        if (jugador == null)
        {
            GameObject objetoJugador = GameObject.FindWithTag("Player");
            if (objetoJugador != null) jugador = objetoJugador.GetComponent<EstadoJugador>();
        }

        if (panelDerrota != null) panelDerrota.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);
    }

    /// <summary>
    /// Mientras se juega, actualiza el HUD y la cuenta atrás.
    /// </summary>
    void Update()
    {
        if (partidaTerminada) return;

        ActualizarHUD();
        ActualizarCuentaAtras();
    }

    /// <summary>
    /// Pasa el calor y la hidratación del jugador (de 0 a 1) a las barras del HUD.
    /// </summary>
    private void ActualizarHUD()
    {
        if (jugador == null || HUDController.Instancia == null) return;

        HUDController.Instancia.SetCalor(jugador.calor / jugador.calorMaximo);
        HUDController.Instancia.SetHidratacion(jugador.hidratacion / jugador.hidratacionMaxima);
    }

    /// <summary>
    /// Resta el tiempo que pasa; si llega a 0, se pierde la partida.
    /// </summary>
    private void ActualizarCuentaAtras()
    {
        if (!usarCuentaAtras) return;

        tiempoRestante = tiempoRestante - Time.deltaTime;
        if (tiempoRestante <= 0f)
        {
            tiempoRestante = 0f;
            Perder();
        }
    }

    /// <summary>
    /// Suma segundos a la cuenta atrás (por ejemplo, al usar un objeto).
    /// </summary>
    public void AnadirTiempo(float segundos)
    {
        tiempoRestante = tiempoRestante + segundos;
    }

    /// <summary>
    /// Termina la partida, para el juego y muestra el panel de derrota.
    /// </summary>
    public void Perder()
    {
        if (partidaTerminada) return;
        partidaTerminada = true;

        Time.timeScale = 0f;
        if (panelDerrota != null) panelDerrota.SetActive(true);
        Debug.Log("Derrota");
    }

    /// <summary>
    /// Termina la partida, para el juego y muestra el panel de victoria.
    /// </summary>
    public void Ganar()
    {
        if (partidaTerminada) return;
        partidaTerminada = true;

        Time.timeScale = 0f;
        if (panelVictoria != null) panelVictoria.SetActive(true);
        Debug.Log("Victoria");
    }

    /// <summary>
    /// Vuelve a cargar el nivel actual. Para el botón "Reintentar".
    /// </summary>
    public void Reintentar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Vuelve al menú principal. Para el botón "Volver al menú".
    /// </summary>
    public void IrAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenu);
    }
}
