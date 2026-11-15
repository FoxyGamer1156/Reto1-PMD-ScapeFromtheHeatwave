using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Final del nivel (el refugio climático). De momento vuelve al menú.</summary>
public class Meta : MonoBehaviour
{
    public string escenaSiguiente = "MenuPrincipal";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<Controles>() == null) return;
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaSiguiente);
    }
}
