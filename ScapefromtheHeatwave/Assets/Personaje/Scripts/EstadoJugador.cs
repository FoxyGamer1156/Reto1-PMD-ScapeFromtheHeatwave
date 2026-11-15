using UnityEngine;

/// <summary>
/// Calor e hidratación de Marta (ambos 0..1) y sus reglas. Va en el mismo objeto que Controles.
///  - El calor sube solo, poco a poco. La gorra lo reduce a la mitad durante 15 s. La sombra lo enfría.
///  - Esprintar gasta hidratación. La hidratación SOLO sube bebiendo.
///  - Calor > 70 %  → la hidratación baja sola.
///  - Hidratación < 20 % → se genera un 20 % más de calor.
///  - Hidratación 0 → no se puede esprintar (Controles.puedeCorrer).
/// Actualiza el HUD a través de HUDController.
/// </summary>
[RequireComponent(typeof(Controles))]
public class EstadoJugador : MonoBehaviour
{
    public static EstadoJugador Instancia { get; private set; }

    [Header("Calor (0 = fresco, 1 = golpe de calor)")]
    [Range(0f, 1f)] public float calor = 0f;
    [Tooltip("Calor que sube por segundo (0.01 = tarda 100 s en llenarse)")]
    public float calorPorSegundo = 0.01f;
    [Tooltip("Calor que baja por segundo dentro de una sombra")]
    public float enfriamientoSombra = 0.05f;
    public float umbralCalorAlto = 0.7f;

    [Header("Hidratación (1 = llena)")]
    [Range(0f, 1f)] public float hidratacion = 1f;
    [Tooltip("Pérdida por segundo al esprintar (0.015 = ~67 s de sprint continuo; antes 0.012)")]
    public float perdidaSprint = 0.015f;
    [Tooltip("Pérdida por segundo mientras el calor está por encima del umbral")]
    public float perdidaPorCalorAlto = 0.01f;
    public float umbralHidratacionBaja = 0.2f;
    public float multiplicadorHidratacionBaja = 1.2f;

    [Header("Gorra")]
    public float duracionGorra = 15f;
    [Range(0f, 1f)] public float reduccionGorra = 0.5f;

    [Header("Golpe de calor (calor al 100 %)")]
    public bool reaparecerConCalorMaximo = true;
    [Range(0f, 1f)] public float calorTrasReaparecer = 0.5f;

    public float Calor => calor;
    public float Hidratacion => hidratacion;
    public bool EnSombra => zonasSombra.Count > 0;
    public float GorraRestante { get; private set; }

    readonly System.Collections.Generic.HashSet<ZonaSombra> zonasSombra = new System.Collections.Generic.HashSet<ZonaSombra>();
    Controles controles;

    void Awake() { Instancia = this; controles = GetComponent<Controles>(); }

    void Start()
    {
        PuntoControl.EstablecerInicio(transform.position);
        ActualizarHUD();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;   // en pausa no pasa nada

        // ---- Calor ----
        float generacion = calorPorSegundo;
        if (hidratacion < umbralHidratacionBaja) generacion *= multiplicadorHidratacionBaja;
        if (GorraRestante > 0f) { generacion *= 1f - reduccionGorra; GorraRestante = Mathf.Max(0f, GorraRestante - dt); }
        // Sombra que enfría (farola, palmera, árbol grande): baja. Sombra que solo "frena" (árboles): deja de subir.
        float cambio = EnfriaSombra() ? generacion - enfriamientoSombra : (EnSombra ? 0f : generacion);
        calor = Mathf.Clamp01(calor + cambio * dt);

        // ---- Hidratación (nunca sube sola) ----
        float perdida = 0f;
        if (controles.EstaCorriendo) perdida += perdidaSprint;
        if (calor > umbralCalorAlto) perdida += perdidaPorCalorAlto;
        hidratacion = Mathf.Clamp01(hidratacion - perdida * dt);
        controles.puedeCorrer = hidratacion > 0f;

        ActualizarHUD();

        if (calor >= 1f && reaparecerConCalorMaximo)
        {
            PuntoControl.Reaparecer(controles);
            calor = calorTrasReaparecer;
        }
    }

    void ActualizarHUD()
    {
        if (!HUDController.Instancia) return;
        HUDController.Instancia.SetCalor(calor);
        HUDController.Instancia.SetHidratacion(hidratacion);
    }

    // ---- Los llaman Inventario y ZonaSombra ----
    bool EnfriaSombra()
    {
        foreach (var z in zonasSombra) if (z && z.reduceCalor) return true;
        return false;
    }

    public void Beber(float cantidad) { hidratacion = Mathf.Clamp01(hidratacion + cantidad); }
    public void AnadirCalor(float cantidad) { calor = Mathf.Clamp01(calor + cantidad); }
    public void ActivarGorra() { GorraRestante = duracionGorra; }
    public void EntrarSombra(ZonaSombra z) { zonasSombra.Add(z); }
    public void SalirSombra(ZonaSombra z) { zonasSombra.Remove(z); }
}
