using UnityEngine;

/// <summary>Trigger bajo todo el nivel: si Marta cae, reaparece en el último checkpoint (o al inicio) con más calor.</summary>
public class ZonaMuerte : MonoBehaviour
{
    [Range(0f, 1f)] public float calorAlCaer = 0.10f;

    void Start()
    {
        PuntoControl.Reiniciar();
        var marta = FindFirstObjectByType<Controles>();
        if (marta) PuntoControl.EstablecerInicio(marta.transform.position);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var marta = other.GetComponentInParent<Controles>();
        if (!marta) return;
        SFX.Play("caida", .8f, 0f);
        PuntoControl.Reaparecer(marta, calorAlCaer);
    }
}
