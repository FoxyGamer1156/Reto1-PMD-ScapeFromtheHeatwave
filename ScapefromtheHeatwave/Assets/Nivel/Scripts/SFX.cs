using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Efectos de sonido globales: SFX.Play("salto"). Los clips viven en Assets/Resources/Sonidos/.
/// El volumen sigue al slider "Efectos" del menú de pausa (PlayerPrefs "SfxVolume").
/// Además conecta solo el sonido de clic/hover a TODOS los botones de cualquier escena.
/// </summary>
public static class SFX
{
    const int Voces = 8;
    static AudioSource[] fuentes;
    static int siguiente;
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    public static void Play(string nombre, float volumen = 1f, float variacionTono = 0.05f)
    {
        if (!Preparar()) return;
        if (!cache.TryGetValue(nombre, out var clip) || !clip)
        {
            clip = Resources.Load<AudioClip>("Sonidos/" + nombre);
            cache[nombre] = clip;
        }
        if (!clip) return;

        var f = fuentes[siguiente];
        siguiente = (siguiente + 1) % Voces;
        f.pitch = 1f + Random.Range(-variacionTono, variacionTono);
        f.volume = Mathf.Clamp01(volumen * PlayerPrefs.GetFloat("SfxVolume", 1f));
        f.clip = clip;
        f.Play();
    }

    static bool Preparar()
    {
        if (fuentes != null && fuentes[0]) return true;
        var go = new GameObject("SFX");
        Object.DontDestroyOnLoad(go);
        fuentes = new AudioSource[Voces];
        for (int i = 0; i < Voces; i++)
        {
            var a = go.AddComponent<AudioSource>();
            a.playOnAwake = false; a.spatialBlend = 0f; a.ignoreListenerPause = true;
            fuentes[i] = a;
        }
        return true;
    }

    // ---- Conectar automáticamente todos los botones ----
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        ConectarBotones();
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo) { ConectarBotones(); }

    static void ConectarBotones()
    {
        foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (!b.GetComponent<SonidoBoton>()) b.gameObject.AddComponent<SonidoBoton>();
    }
}

/// <summary>Se añade solo a cada Button: clic y hover.</summary>
[RequireComponent(typeof(Button))]
public class SonidoBoton : MonoBehaviour, IPointerEnterHandler
{
    Button boton;

    void Awake()
    {
        boton = GetComponent<Button>();
        boton.onClick.AddListener(() => SFX.Play("click", 1f, 0.02f));
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (boton && boton.interactable) SFX.Play("hover", 1f, 0.02f);
    }
}
