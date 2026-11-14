using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla a Marta: andar, correr (sprint), saltar y actualizar el Animator.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class Controles : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;
    public float velocidadSprint = 8f;

    [Header("Salto")]
    public float fuerzaSalto = 12f;
    [Range(0f, 1f)] public float cortarSalto = 0.5f;
    public float tiempoCoyote = 0.1f;

    public bool EnSuelo { get; private set; }
    public bool SeMueve { get; private set; }
    public bool EstaCorriendo { get; private set; }

    [HideInInspector] public bool puedeCorrer = true;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sprite;

    private InputAction accionMover;
    private InputAction accionSaltar;
    private InputAction accionSprint;

    private ContactFilter2D filtroSuelo;
    private readonly ContactPoint2D[] contactos = new ContactPoint2D[8];

    private float mover;
    private bool quiereSaltar;
    private float contadorCoyote;

    /// <summary>
    /// Obtiene los componentes, configura el Rigidbody, busca las acciones de input
    /// y prepara el filtro que detecta el suelo.
    /// </summary>
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        accionMover = InputSystem.actions.FindAction("Player/Move");
        accionSaltar = InputSystem.actions.FindAction("Player/Jump");
        accionSprint = InputSystem.actions.FindAction("Player/Sprint");

        filtroSuelo.SetNormalAngle(45f, 135f);
        filtroSuelo.useTriggers = false;
    }

    /// <summary>
    /// Cada frame: lee el input, gira el sprite y actualiza el Animator.
    /// </summary>
    void Update()
    {
        LeerInput();
        Girar();
        ActualizarAnimator();
    }

    /// <summary>
    /// Cada paso de física: comprueba el suelo, mueve al personaje y aplica el salto.
    /// </summary>
    void FixedUpdate()
    {
        ComprobarSuelo();
        Mover();
        Saltar();
    }

    /// <summary>
    /// Lee la dirección de movimiento y el sprint, guarda la petición de salto
    /// y corta la subida si se suelta el botón de salto (salto variable).
    /// </summary>
    private void LeerInput()
    {
        mover = accionMover.ReadValue<Vector2>().x;

        SeMueve = Mathf.Abs(mover) > 0.1f;
        EstaCorriendo = SeMueve && puedeCorrer && accionSprint.IsPressed();

        if (accionSaltar.WasPressedThisFrame() && EnSuelo)
        {
            quiereSaltar = true;
        }

        if (accionSaltar.WasReleasedThisFrame() && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * cortarSalto);
        }
    }

    /// <summary>
    /// Actualiza <see cref="EnSuelo"/> usando los contactos del Rigidbody y el
    /// temporizador de coyote time (sigue en el suelo un instante tras dejarlo).
    /// </summary>
    private void ComprobarSuelo()
    {
        bool tocandoSuelo = rb.GetContacts(filtroSuelo, contactos) > 0;

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

    /// <summary>
    /// Aplica la velocidad horizontal (andando o corriendo) sin tocar la vertical.
    /// </summary>
    private void Mover()
    {
        float vel = EstaCorriendo ? velocidadSprint : velocidad;
        rb.linearVelocity = new Vector2(mover * vel, rb.linearVelocity.y);
    }

    /// <summary>
    /// Ejecuta el salto pendiente y vacía el coyote time para evitar dobles saltos.
    /// </summary>
    private void Saltar()
    {
        if (!quiereSaltar) return;

        quiereSaltar = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);

        contadorCoyote = 0f;
        EnSuelo = false;
    }

    /// <summary>
    /// Voltea el sprite según la dirección; si está quieto mantiene la orientación.
    /// </summary>
    private void Girar()
    {
        if (mover < 0) sprite.flipX = true;
        else if (mover > 0) sprite.flipX = false;
    }

    /// <summary>
    /// Envía al Animator los parámetros Running, EnSuelo y VelY, y acelera
    /// la animación al hacer sprint.
    /// </summary>
    private void ActualizarAnimator()
    {
        animator.SetBool("Running", SeMueve);
        animator.SetBool("EnSuelo", EnSuelo);
        animator.SetFloat("VelY", rb.linearVelocity.y);

        animator.speed = (EstaCorriendo && EnSuelo) ? 1.5f : 1f;
    }
}
