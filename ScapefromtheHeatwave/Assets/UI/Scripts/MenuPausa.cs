using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Menú de pausa: Esc en PC, botón de pausa en Android (conectado a Alternar).</summary>
public class MenuPausa : MonoBehaviour
{
    public static bool EnPausa { get; private set; }

    public GameObject panel;
    public Slider sliderMusica;
    public Slider sliderEfectos;

    [Header("Audio (opcional): parámetros expuestos del AudioMixer")]
    public AudioMixer mixer;
    public string parametroMusica = "MusicVolume";
    public string parametroEfectos = "SfxVolume";

    public string escenaMenu = "MenuPrincipal";

    void Start()
    {
        EnPausa = false;
        panel.SetActive(false);

        sliderMusica.value = PlayerPrefs.GetFloat(parametroMusica, 1f);
        sliderEfectos.value = PlayerPrefs.GetFloat(parametroEfectos, 1f);
        sliderMusica.onValueChanged.AddListener(v => AplicarVolumen(parametroMusica, v));
        sliderEfectos.onValueChanged.AddListener(v => AplicarVolumen(parametroEfectos, v));
        AplicarVolumen(parametroMusica, sliderMusica.value);
        AplicarVolumen(parametroEfectos, sliderEfectos.value);
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        bool esc = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        bool esc = Input.GetKeyDown(KeyCode.Escape);
#endif
        if (esc) Alternar();
    }

    public void Alternar() { if (EnPausa) Continuar(); else Pausar(); }

    public void Pausar() { EnPausa = true; panel.SetActive(true); Time.timeScale = 0f; SFX.Play("pausa", .8f, 0f); }

    public void Continuar() { EnPausa = false; panel.SetActive(false); Time.timeScale = 1f; SFX.Play("continuar", .8f, 0f); }

    public void SalirAlMenu()
    {
        EnPausa = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenu);
    }

    void AplicarVolumen(string parametro, float lineal)
    {
        PlayerPrefs.SetFloat(parametro, lineal);
        if (mixer) mixer.SetFloat(parametro, Mathf.Log10(Mathf.Max(lineal, 0.0001f)) * 20f);
    }
}
