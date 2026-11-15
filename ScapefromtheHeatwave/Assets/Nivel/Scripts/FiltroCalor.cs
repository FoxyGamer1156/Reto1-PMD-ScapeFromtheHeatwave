using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tinte naranja/rojo sobre toda la pantalla. Ligero siempre, más fuerte con el calor;
/// por encima del 70 % vira a rojo y late como un pulso.
/// </summary>
public class FiltroCalor : MonoBehaviour
{
    public Image imagen;
    public Color colorNaranja = new Color(1f, .55f, .15f);
    public Color colorRojo = new Color(.9f, .08f, .04f);
    public float alfaBase = .05f;
    public float alfaMax = .30f;
    public float umbralEstres = .7f;

    void Update()
    {
        var e = EstadoJugador.Instancia;
        if (!imagen || !e) return;

        float c = e.Calor;
        float estres = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(umbralEstres, 1f, c));
        Color col = Color.Lerp(colorNaranja, colorRojo, estres);
        float alfa = Mathf.Lerp(alfaBase, alfaMax, c);
        if (estres > 0f)
            alfa += estres * .08f * (.5f + .5f * Mathf.Sin(Time.unscaledTime * (6f + 6f * estres)));
        col.a = alfa;
        imagen.color = col;
    }
}
