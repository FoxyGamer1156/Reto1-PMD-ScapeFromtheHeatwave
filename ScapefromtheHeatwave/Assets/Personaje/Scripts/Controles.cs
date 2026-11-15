using UnityEngine;
using UnityEngine.InputSystem;

// Controla a Marta: andar, correr (sprint), saltar y avisar al Animator.
//
// [RequireComponent] hace que Unity añada estos componentes automáticamente
// si faltan, y no deja quitarlos mientras el script esté puesto. Así nunca
// tendremos un GetComponent que devuelva null.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class Controles : MonoBehaviour
{
    // ---------- Valores ajustables desde el Inspector ----------
    // [Header] solo sirve para ordenar el Inspector con títulos.
    [Header("Movimiento")]
    public float velocidad = 5f;          // Velocidad andando
    public float velocidadSprint = 8f;    // Velocidad corriendo (Shift)

    [Header("Salto")]
    public float fuerzaSalto = 12f;
    // Si sueltas el botón de salto mientras subes, la velocidad vertical se
    // multiplica por esto. Resultado: toque corto = salto bajo, mantener = salto alto.
    [Range(0f, 1f)] public float cortarSalto = 0.5f;
    // "Coyote time": segundos que seguimos considerando que está en el suelo
    // después de dejar de tocarlo. Sirve para dos cosas:
    //  1) Evita parpadeos de la animación de salto por perder el contacto un instante
    //     (juntas entre tiles, pequeños botes de la física).
    //  2) Deja saltar un pelín después de salir de un borde (se siente más justo).
    public float tiempoCoyote = 0.1f;

    // ---------- Estado que otros scripts pueden LEER ----------
    // { get; private set; } = cualquiera puede leerlo, pero solo este script lo cambia.
    // Lo usará EstadoJugador para gastar hidratación al moverse / correr.
    public bool EnSuelo { get; private set; }
    public bool SeMueve { get; private set; }
    public bool EstaCorriendo { get; private set; }

    // Esto lo pondrá EstadoJugador a false cuando la hidratación llegue a 0.
    [HideInInspector] public bool puedeCorrer = true;

    // Esto lo pone PlataformaMovil mientras Marta está encima, para que viaje con ella.
    [HideInInspector] public float velocidadPlataforma = 0f;

    // ---------- Componentes ----------
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sprite;

    // ---------- Input ----------
    // En vez de preguntar por teclas concretas (Keyboard.current.aKey...) usamos
    // ACCIONES. Una acción ("Move", "Jump") puede estar asociada a teclado, mando
    // o botones táctiles a la vez: el código no sabe ni le importa de dónde viene.
    private InputAction accionMover;
    private InputAction accionSaltar;
    private InputAction accionSprint;

    // ---------- Detección de suelo ----------
    // Un filtro que solo acepta contactos cuya "normal" apunte hacia arriba
    // (entre 45º y 135º). Es la misma idea que tenías con normal.y > 0.5,
    // pero se la preguntamos a la física cuando queremos, en vez de depender
    // de los eventos OnCollisionStay / OnCollisionExit.
    private ContactFilter2D filtroSuelo;
    private readonly ContactPoint2D[] contactos = new ContactPoint2D[8];

    private float mover;          // -1 izquierda, 0 quieto, 1 derecha
    private bool quiereSaltar;    // Se pidió un salto este frame (lo ejecuta FixedUpdate)
    private float contadorCoyote; // Cuenta atrás desde la última vez que tocó suelo

    // Awake se ejecuta antes que Start. Se suele usar para "auto-configurarse".
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();

        rb.freezeRotation = true;
        // Interpolate suaviza el movimiento visual entre pasos de física (evita "tirones").
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // InputSystem.actions = el archivo InputSystem_Actions (es "project-wide").
        // "Player/Move" = mapa Player, acción Move.
        accionMover = InputSystem.actions.FindAction("Player/Move");
        accionSaltar = InputSystem.actions.FindAction("Player/Jump");
        accionSprint = InputSystem.actions.FindAction("Player/Sprint");

        filtroSuelo.SetNormalAngle(45f, 135f);
        filtroSuelo.useTriggers = false;   // Las zonas de sombra (triggers) no son suelo
    }

    // Update: cada frame. Aquí LEEMOS el input y actualizamos lo visual.
    void Update()
    {
        LeerInput();
        Girar();
        ActualizarAnimator();
    }

    // FixedUpdate: a ritmo fijo, junto a la física. Aquí MOVEMOS el Rigidbody.
    void FixedUpdate()
    {
        ComprobarSuelo();
        Mover();
        Saltar();
    }

    // ================== Métodos privados ==================
    // Dividir en métodos pequeños con nombre claro hace que Update/FixedUpdate
    // se lean como un índice. La rúbrica valora esta modularización.

    private void LeerInput()
    {
        // Move devuelve un Vector2 (x, y). Solo nos interesa la x.
        mover = accionMover.ReadValue<Vector2>().x;

        SeMueve = Mathf.Abs(mover) > 0.1f;
        EstaCorriendo = SeMueve && puedeCorrer && accionSprint.IsPressed();

        // WasPressedThisFrame solo es true el frame exacto en que se pulsa.
        // Lo guardamos y lo ejecuta FixedUpdate: si saltáramos aquí y FixedUpdate
        // no se ejecutara ese frame, se podría perder el salto.
        if (accionSaltar.WasPressedThisFrame() && EnSuelo)
        {
            quiereSaltar = true;
        }

        // Salto variable: si suelta el botón mientras sube, frenamos la subida.
        if (accionSaltar.WasReleasedThisFrame() && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * cortarSalto);
        }
    }

    private void ComprobarSuelo()
    {
        // GetContacts rellena el array con los contactos que pasan el filtro
        // y devuelve cuántos hay. Si hay al menos uno, estamos tocando suelo.
        bool tocandoSuelo = rb.GetContacts(filtroSuelo, contactos) > 0;

        // Esto es un TEMPORIZADOR sencillo (la rúbrica lo pide):
        // - Si toca suelo, "recargamos" el contador.
        // - Si no, le vamos restando el tiempo que pasa.
        // Solo cuando llega a 0 consideramos que de verdad está en el aire.
        if (tocandoSuelo)
        {
            contadorCoyote = tiempoCoyote;
        }
        else
        {
            contadorCoyote -= Time.fixedDeltaTime;
        }

        EnSuelo = contadorCoyote > 0f;
    }

    private void Mover()
    {
        // Operador ternario: condicion ? siEsTrue : siEsFalse
        float vel = EstaCorriendo ? velocidadSprint : velocidad;
        // Solo cambiamos la X. La Y la controla la gravedad / el salto.
        // Si está sobre una plataforma móvil, se le suma la velocidad de la plataforma.
        rb.linearVelocity = new Vector2(mover * vel + velocidadPlataforma, rb.linearVelocity.y);
    }

    private void Saltar()
    {
        if (!quiereSaltar) return;

        quiereSaltar = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);

        // Al saltar vaciamos el contador: si no, durante esos 0.1 s seguiría
        // "en el suelo" y podría hacer un doble salto, y la animación tardaría en salir.
        contadorCoyote = 0f;
        EnSuelo = false;
    }

    private void Girar()
    {
        if (mover < 0) sprite.flipX = true;
        else if (mover > 0) sprite.flipX = false;
        // Si mover es 0 se queda mirando hacia donde miraba.
    }

    private void ActualizarAnimator()
    {
        // Los nombres tienen que coincidir EXACTAMENTE con los parámetros del Animator.
        animator.SetBool("Running", SeMueve);
        animator.SetBool("EnSuelo", EnSuelo);
        animator.SetFloat("VelY", rb.linearVelocity.y);

        // Al hacer sprint la animación de correr va más rápida.
        animator.speed = (EstaCorriendo && EnSuelo) ? 1.5f : 1f;
    }
}
