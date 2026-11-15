using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Punto único para actualizar el HUD desde el resto del juego (p. ej. desde EstadoJugador):
///   HUDController.Instancia.SetCalor(calorActual / calorMaximo);
///   HUDController.Instancia.SetHidratacion(hidratacionActual / hidratacionMaxima);
///   HUDController.Instancia.SetObjeto(spriteDeLaGorra);   // null = hueco vacío
/// </summary>
public class HUDController : MonoBehaviour
{
    public static HUDController Instancia { get; private set; }

    public BarraUI barraCalor;
    public BarraUI barraHidratacion;
    public Image iconoObjeto;

    void Awake() { Instancia = this; SetObjeto(null); }

    public void SetCalor(float normalizado) { if (barraCalor) barraCalor.SetValor(normalizado); }
    public void SetHidratacion(float normalizado) { if (barraHidratacion) barraHidratacion.SetValor(normalizado); }

    public void SetObjeto(Sprite sprite)
    {
        if (!iconoObjeto) return;
        iconoObjeto.sprite = sprite;
        iconoObjeto.enabled = sprite != null;
    }
}
