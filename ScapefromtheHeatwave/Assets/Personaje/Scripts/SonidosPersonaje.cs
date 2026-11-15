using UnityEngine;

/// <summary>Pasos, salto, aterrizaje y sprint de Marta. Solo lee Controles y el Rigidbody2D (no modifica nada).</summary>
[RequireComponent(typeof(Controles))]
public class SonidosPersonaje : MonoBehaviour
{
    public float velocidadParaPasos = 0.8f;

    Controles ctrl;
    Rigidbody2D rb;
    bool estabaEnSuelo, corriendoAntes;
    float tPaso, caidaMax;

    void Awake() { ctrl = GetComponent<Controles>(); rb = GetComponent<Rigidbody2D>(); }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        float dt = Time.deltaTime;
        bool enSuelo = ctrl.EnSuelo;
        float vx = Mathf.Abs(rb.linearVelocity.x);
        float vy = rb.linearVelocity.y;

        if (!enSuelo) caidaMax = Mathf.Min(caidaMax, vy);

        if (!enSuelo && estabaEnSuelo && vy > 2f) SFX.Play("salto", .8f);
        if (enSuelo && !estabaEnSuelo)
        {
            if (caidaMax < -4f) SFX.Play("aterrizaje", Mathf.Clamp01(-caidaMax / 20f) * .8f + .2f);
            caidaMax = 0f;
        }

        if (enSuelo && vx > velocidadParaPasos)
        {
            tPaso -= dt;
            if (tPaso <= 0f)
            {
                SFX.Play("paso", .5f, .12f);
                tPaso = Mathf.Lerp(.34f, .15f, Mathf.InverseLerp(3f, 20f, vx));
            }
        }
        else tPaso = 0f;

        bool corre = ctrl.EstaCorriendo && vx > 5f;
        if (corre && !corriendoAntes) SFX.Play("sprint", .7f);
        corriendoAntes = corre;
        estabaEnSuelo = enSuelo;
    }
}
