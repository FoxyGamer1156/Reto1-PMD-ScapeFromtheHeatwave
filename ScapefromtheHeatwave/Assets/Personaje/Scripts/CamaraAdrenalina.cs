using UnityEngine;
using Unity.Cinemachine;

// Cámara "de adrenalina" para MartaCamera (CinemachineCamera dentro del prefab Personaje).
// Cuanto más tiempo corre Marta a toda velocidad:
//   - la cámara se ALEJA poco a poco hasta zoomMaximo (y vuelve si frena),
//   - se SACUDE más,
//   - se ADELANTA hacia donde corre (ves más nivel por delante),
//   - se inclina un poco (efecto de velocidad).
// Además tiene Sacudir() para golpes/daño: GetComponent<CamaraAdrenalina>().Sacudir(0.5f, 0.3f);
[RequireComponent(typeof(CinemachineCamera))]
public class CamaraAdrenalina : MonoBehaviour
{
    [Header("Objetivo (si está vacío, busca el Rigidbody2D del padre)")]
    public Rigidbody2D objetivo;

    [Header("Velocidad horizontal que cuenta")]
    public float velocidadMinima = 6f;   // por debajo no hay efecto
    public float velocidadMaxima = 20f;  // a esta velocidad el efecto va al 100 %

    [Header("Zoom out (tamaño ortográfico)")]
    public float zoomMaximo = 8.5f;          // el tamaño inicial lo lee de la cámara
    public float segundosHastaMaximo = 6f;   // cuánto tarda en alejarse del todo
    public float segundosParaVolver = 1.5f;  // cuánto tarda en volver al frenar

    [Header("Sacudida")]
    public float sacudidaMaxima = 0.12f;     // en unidades de mundo
    public float frecuencia = 28f;
    public float inclinacionMaxima = 1f;     // grados

    [Header("Adelanto hacia donde corre")]
    public float adelantoMaximo = 2.5f;

    CinemachineCamera cam;
    CinemachinePositionComposer composer;
    Vector3 offsetBase;
    float tamanoBase;
    float adrenalina;      // 0..1
    float direccion = 1f;
    float adelantoActual;
    float impulsoFuerza, impulsoTiempo, impulsoDuracion;

    void Awake()
    {
        cam = GetComponent<CinemachineCamera>();
        composer = GetComponent<CinemachinePositionComposer>();
        if (composer) offsetBase = composer.TargetOffset;
        tamanoBase = cam.Lens.OrthographicSize;
        if (!objetivo) objetivo = GetComponentInParent<Rigidbody2D>();
    }

    /// <summary>Sacudida puntual (golpe, caída...). fuerza en unidades de mundo.</summary>
    public void Sacudir(float fuerza, float duracion)
    {
        impulsoFuerza = fuerza;
        impulsoTiempo = impulsoDuracion = Mathf.Max(0.01f, duracion);
    }

    void LateUpdate()
    {
        if (!objetivo) return;
        float dt = Time.deltaTime;

        // 1) Adrenalina: sube despacio mientras corre rápido, baja al frenar
        float vx = objetivo.linearVelocity.x;
        float f = Mathf.InverseLerp(velocidadMinima, velocidadMaxima, Mathf.Abs(vx));
        if (f > 0.01f) adrenalina += f * dt / segundosHastaMaximo;
        else adrenalina -= dt / segundosParaVolver;
        adrenalina = Mathf.Clamp01(adrenalina);
        float a = Mathf.SmoothStep(0f, 1f, adrenalina);

        // 2) Sacudida continua + impulsos puntuales
        float amplitud = sacudidaMaxima * a;
        if (impulsoTiempo > 0f)
        {
            impulsoTiempo -= dt;
            amplitud += impulsoFuerza * Mathf.Clamp01(impulsoTiempo / impulsoDuracion);
        }
        float t = Time.time * frecuencia;
        float sx = (Mathf.PerlinNoise(t, 0.0f) - 0.5f) * 2f * amplitud;
        float sy = (Mathf.PerlinNoise(0.0f, t + 17f) - 0.5f) * 2f * amplitud;
        float inclinacion = (Mathf.PerlinNoise(t * 0.5f, 31f) - 0.5f) * 2f * inclinacionMaxima * a;

        // 3) Adelanto hacia donde mira/corre
        if (Mathf.Abs(vx) > 0.5f) direccion = Mathf.Sign(vx);
        adelantoActual = Mathf.Lerp(adelantoActual, direccion * adelantoMaximo * a, 2f * dt);

        if (composer) composer.TargetOffset = offsetBase + new Vector3(adelantoActual + sx, sy, 0f);

        // 4) Zoom out + inclinación
        var lente = cam.Lens;
        lente.OrthographicSize = Mathf.Lerp(tamanoBase, zoomMaximo, a);
        lente.Dutch = inclinacion;
        cam.Lens = lente;
    }
}
