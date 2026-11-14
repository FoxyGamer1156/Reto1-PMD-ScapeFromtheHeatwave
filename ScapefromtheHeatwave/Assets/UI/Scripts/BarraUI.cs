using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra que se rellena de izquierda a derecha (valor 0..1) con suavizado y color por gradiente.
/// </summary>
public class BarraUI : MonoBehaviour
{
    public RectTransform relleno;
    public Image imagenRelleno;
    public Gradient colorSegunValor;
    [Range(0f, 1f)] public float valor = 1f;
    public float suavizado = 8f;

    float objetivo;

    /// <summary>
    /// Toma el valor inicial como objetivo y dibuja la barra.
    /// </summary>
    void Awake() { objetivo = valor; Aplicar(); }

    /// <summary>
    /// Cambia el valor al que se moverá la barra.
    /// </summary>
    /// <param name="normalizado">0 = vacía, 1 = llena.</param>
    public void SetValor(float normalizado) { objetivo = Mathf.Clamp01(normalizado); }

    /// <summary>
    /// Acerca el valor al objetivo de forma suave (también durante la pausa) y redibuja la barra.
    /// </summary>
    void Update()
    {
        valor = Mathf.Lerp(valor, objetivo, 1f - Mathf.Exp(-suavizado * Time.unscaledDeltaTime));
        Aplicar();
    }

    /// <summary>
    /// Ajusta el ancho del relleno y su color según el valor actual.
    /// </summary>
    void Aplicar()
    {
        if (relleno) relleno.anchorMax = new Vector2(valor, 1f);
        if (imagenRelleno && colorSegunValor != null) imagenRelleno.color = colorSegunValor.Evaluate(valor);
    }
}
