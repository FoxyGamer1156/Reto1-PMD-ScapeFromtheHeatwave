using UnityEngine;
using UnityEngine.UI;

/// <summary>Barra que se rellena de izquierda a derecha (valor 0..1) con suavizado y color por gradiente.</summary>
public class BarraUI : MonoBehaviour
{
    public RectTransform relleno;
    public Image imagenRelleno;
    public Gradient colorSegunValor;
    [Range(0f, 1f)] public float valor = 1f;
    public float suavizado = 8f;

    float objetivo;

    void Awake() { objetivo = valor; Aplicar(); }

    /// <summary>Normalizado: 0 = vacía, 1 = llena.</summary>
    public void SetValor(float normalizado) { objetivo = Mathf.Clamp01(normalizado); }

    void Update()
    {
        // unscaledDeltaTime: la barra sigue animándose aunque el juego esté en pausa
        valor = Mathf.Lerp(valor, objetivo, 1f - Mathf.Exp(-suavizado * Time.unscaledDeltaTime));
        Aplicar();
    }

    void Aplicar()
    {
        if (relleno) relleno.anchorMax = new Vector2(valor, 1f);
        if (imagenRelleno && colorSegunValor != null) imagenRelleno.color = colorSegunValor.Evaluate(valor);
    }
}
