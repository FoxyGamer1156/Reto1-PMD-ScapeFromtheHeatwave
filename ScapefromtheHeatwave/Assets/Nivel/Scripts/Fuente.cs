using UnityEngine;

/// <summary>Fuente o laguito: mientras Marta está dentro, bebe y recupera hidratación.</summary>
public class Fuente : MonoBehaviour
{
    [Tooltip("Hidratación que se recupera por segundo (0.35 = una botella en ~1 s)")]
    public float hidratacionPorSegundo = 0.35f;
    EstadoJugador dentro;
    float tSonido;

    void OnTriggerEnter2D(Collider2D other)
    {
        var e = other.GetComponentInParent<EstadoJugador>();
        if (e) { dentro = e; tSonido = 0f; }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<EstadoJugador>() == dentro) dentro = null;
    }

    void Update()
    {
        if (!dentro || dentro.Hidratacion >= 1f) return;
        dentro.Beber(hidratacionPorSegundo * Time.deltaTime);
        tSonido -= Time.deltaTime;
        if (tSonido <= 0f) { SFX.Play("beber", .5f, .08f); tSonido = .55f; }
    }
}
