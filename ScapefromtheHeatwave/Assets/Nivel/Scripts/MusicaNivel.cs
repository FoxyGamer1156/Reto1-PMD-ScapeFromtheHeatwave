using UnityEngine;

/// <summary>
/// Música del nivel con dos efectos que se mezclan suavemente:
///  - En sombra: efecto lofi/radio (paso bajo + paso alto + un poco de distorsión).
///  - Con calor > 70 %: más estresante (sube el tono y se ensucia con distorsión).
/// Los filtros se añaden solos al mismo objeto que el AudioSource.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicaNivel : MonoBehaviour
{
    public AudioClip cancion;
    [Range(0f, 1f)] public float volumen = 0.7f;

    [Header("Sombra (lofi / radio)")]
    public float pasoBajoSombra = 1400f;
    public float pasoAltoSombra = 350f;
    public float segundosTransicion = 0.6f;

    [Header("Calor alto (estrés)")]
    public float umbralCalor = 0.7f;
    public float subidaTonoMax = 0.08f;
    public float distorsionMax = 0.35f;

    AudioSource fuente;
    AudioLowPassFilter pasoBajo;
    AudioHighPassFilter pasoAlto;
    AudioDistortionFilter distorsion;
    float sombra;

    void Awake()
    {
        fuente = GetComponent<AudioSource>();
        pasoBajo = ObtenerOAnadir<AudioLowPassFilter>();
        pasoAlto = ObtenerOAnadir<AudioHighPassFilter>();
        distorsion = ObtenerOAnadir<AudioDistortionFilter>();

        fuente.clip = cancion ? cancion : fuente.clip;
        fuente.loop = true;
        fuente.volume = volumen;
        fuente.spatialBlend = 0f;
        pasoBajo.cutoffFrequency = 22000f;
        pasoAlto.cutoffFrequency = 10f;
        distorsion.distortionLevel = 0f;
        if (fuente.clip) fuente.Play();
    }

    T ObtenerOAnadir<T>() where T : Component
    {
        var c = GetComponent<T>();
        return c ? c : gameObject.AddComponent<T>();
    }

    void Update()
    {
        var e = EstadoJugador.Instancia;
        float objetivoSombra = (e && e.EnSombra) ? 1f : 0f;
        sombra = Mathf.MoveTowards(sombra, objetivoSombra, Time.unscaledDeltaTime / segundosTransicion);
        float s = Mathf.SmoothStep(0f, 1f, sombra);

        float estres = e ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(umbralCalor, 1f, e.Calor)) : 0f;

        pasoBajo.cutoffFrequency = Mathf.Lerp(22000f, pasoBajoSombra, s);
        pasoAlto.cutoffFrequency = Mathf.Lerp(10f, pasoAltoSombra, s);
        distorsion.distortionLevel = Mathf.Clamp01(0.08f * s + distorsionMax * estres);
        fuente.pitch = 1f + subidaTonoMax * estres;
        fuente.volume = Mathf.Lerp(volumen, volumen * 0.85f, s) * PlayerPrefs.GetFloat("MusicVolume", 1f);
    }
}
