using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Punto único para actualizar el HUD desde el resto del juego (p. ej. desde EstadoJugador):
///   HUDController.Instancia.SetCalor(calorActual / calorMaximo);
///   HUDController.Instancia.SetHidratacion(hidratacionActual / hidratacionMaxima);
///   HUDController.Instancia.SetObjeto(spriteDeLaGorra);
/// </summary>
public class HUDController : MonoBehaviour
{
    public static HUDController Instancia { get; private set; }

    public BarraUI barraCalor;
    public BarraUI barraHidratacion;
    public Image iconoObjeto;

    /// <summary>
    /// Registra esta instancia como la global y deja vacío el hueco de inventario.
    /// </summary>
    void Awake() { Instancia = this; SetObjeto(null); }

    /// <summary>
    /// Actualiza la barra de calor.
    /// </summary>
    /// <param name="normalizado">0 = sin calor, 1 = calor máximo.</param>
    public void SetCalor(float normalizado) { if (barraCalor) barraCalor.SetValor(normalizado); }

    /// <summary>
    /// Actualiza la barra de hidratación.
    /// </summary>
    /// <param name="normalizado">0 = deshidratado, 1 = hidratación máxima.</param>
    public void SetHidratacion(float normalizado) { if (barraHidratacion) barraHidratacion.SetValor(normalizado); }

    /// <summary>
    /// Muestra un objeto en el hueco de inventario.
    /// </summary>
    /// <param name="sprite">Icono del objeto, o null para dejar el hueco vacío.</param>
    public void SetObjeto(Sprite sprite)
    {
        if (!iconoObjeto) return;
        iconoObjeto.sprite = sprite;
        iconoObjeto.enabled = sprite != null;
    }
}
