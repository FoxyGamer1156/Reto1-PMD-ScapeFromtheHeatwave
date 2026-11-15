using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [Tooltip("Nombre de la escena del juego (debe estar en Build Settings)")]
    public string escenaJuego = "PruebaPersonaje";

    void Start() { Time.timeScale = 1f; }

    public void Jugar() { SceneManager.LoadScene(escenaJuego); }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
