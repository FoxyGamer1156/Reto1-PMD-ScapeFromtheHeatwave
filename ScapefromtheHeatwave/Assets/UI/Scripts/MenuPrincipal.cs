using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Botones del menú principal: empezar la partida o salir del juego.
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    [Tooltip("Nombre de la escena del juego (debe estar en Build Settings)")]
    public string escenaJuego = "PruebaPersonaje";

    /// <summary>
    /// Asegura que el tiempo corre normal (por si se llega desde la pausa).
    /// </summary>
    void Start() { Time.timeScale = 1f; }

    /// <summary>
    /// Carga la escena del juego.
    /// </summary>
    public void Jugar() { SceneManager.LoadScene(escenaJuego); }

    /// <summary>
    /// Cierra el juego (en el editor, detiene el modo Play).
    /// </summary>
    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
