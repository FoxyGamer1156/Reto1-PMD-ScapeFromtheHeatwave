using UnityEngine;
using UnityEngine.InputSystem;

public enum TipoObjeto { Ninguno, Gorra, Botella }

/// <summary>
/// Un solo hueco (el cuadrado de arriba a la derecha). Si está ocupado no se puede recoger otro objeto.
/// Se usa con E (acción Player/Interact) o llamando a Usar() desde un botón táctil.
/// </summary>
[RequireComponent(typeof(EstadoJugador))]
public class Inventario : MonoBehaviour
{
    [Range(0f, 1f)] public float aguaPorBotella = 0.30f;

    public TipoObjeto Actual { get; private set; }
    public bool Vacio => Actual == TipoObjeto.Ninguno;

    EstadoJugador estado;
    InputAction accionUsar;

    void Awake()
    {
        estado = GetComponent<EstadoJugador>();
        if (InputSystem.actions != null) accionUsar = InputSystem.actions.FindAction("Player/Interact");
    }

    void Update()
    {
        if (Time.timeScale == 0f || Vacio) return;   // en pausa no se usa nada
        bool pulsada = (accionUsar != null && accionUsar.WasPressedThisFrame())
                    || (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);
        if (pulsada) Usar();
    }

    public bool TryRecoger(TipoObjeto tipo, Sprite icono)
    {
        if (!Vacio) return false;
        Actual = tipo;
        if (HUDController.Instancia) HUDController.Instancia.SetObjeto(icono);
        SFX.Play("recoger", .8f, .03f);
        return true;
    }

    public void Usar()
    {
        switch (Actual)
        {
            case TipoObjeto.Gorra: estado.ActivarGorra(); SFX.Play("gorra", .8f); break;
            case TipoObjeto.Botella: estado.Beber(aguaPorBotella); SFX.Play("beber", .9f); break;
            default: return;
        }
        Actual = TipoObjeto.Ninguno;
        if (HUDController.Instancia) HUDController.Instancia.SetObjeto(null);
    }
}
