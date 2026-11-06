using UnityEngine;
using UnityEngine.InputSystem;

// Controla a Marta: moverse a izquierda/derecha, saltar y avisar al Animator
// de lo que está haciendo para que ponga la animación correcta.
//
// El GameObject necesita: Rigidbody2D, un Collider2D, SpriteRenderer y Animator.
public class Controles : MonoBehaviour
{
    // ---------- Valores que puedes cambiar desde el Inspector ----------
    public float velocidad = 5f;      // Qué tan rápido camina
    public float fuerzaSalto = 12f;   // Qué tan alto salta

    // ---------- Componentes del personaje ----------
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sprite;

    // ---------- Estado ----------
    private float mover;      // -1 = izquierda, 0 = quieto, 1 = derecha
    private bool enSuelo;     // ¿Está tocando el suelo?

    // Start se ejecuta UNA vez, al empezar el juego.
    void Start()
    {
        // Guardamos los componentes para no tener que buscarlos cada vez
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();

        // Evita que el personaje se caiga rodando al chocar con algo
        rb.freezeRotation = true;
    }

    // Update se ejecuta CADA FRAME. Aquí leemos el teclado.
    void Update()
    {
        // 1) Leer las teclas de movimiento
        mover = 0;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            mover -= 1;
        }
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            mover += 1;
        }
        // (Si pulsas las dos a la vez, se anulan y mover vale 0)

        // 2) Saltar con la barra espaciadora, solo si está en el suelo
        if (Keyboard.current.spaceKey.wasPressedThisFrame && enSuelo)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
            enSuelo = false;
        }

        // 3) Mirar hacia donde camina (girar el dibujo)
        if (mover < 0)
        {
            sprite.flipX = true;   // mira a la izquierda
        }
        else if (mover > 0)
        {
            sprite.flipX = false;  // mira a la derecha
        }
        // Si mover es 0 no tocamos nada: se queda mirando a donde miraba

        // 4) Contarle al Animator qué está pasando.
        //    Los nombres tienen que ser IGUALES a los parámetros del Animator.
        animator.SetBool("Running", mover != 0);

        // Cuando añadas la animación de salto, crea en el Animator un parámetro
        // Bool llamado "EnSuelo" y descomenta esta línea:
        // animator.SetBool("EnSuelo", enSuelo);
    }

    // FixedUpdate se ejecuta a ritmo fijo, junto con la física.
    // Todo lo que toque el Rigidbody (velocidad, fuerzas) va aquí.
    void FixedUpdate()
    {
        // Cambiamos solo la velocidad horizontal.
        // La vertical (y) la dejamos como está para que la gravedad y el salto funcionen.
        rb.linearVelocity = new Vector2(mover * velocidad, rb.linearVelocity.y);
    }

    // Se ejecuta mientras el personaje está tocando algo.
    void OnCollisionStay2D(Collision2D choque)
    {
        // Miramos cada punto de contacto. Si el "normal" apunta hacia arriba (y > 0.5)
        // significa que estamos ENCIMA de eso, es decir, es suelo.
        // Si apunta hacia los lados, es una pared, y no cuenta como suelo.
        foreach (ContactPoint2D contacto in choque.contacts)
        {
            if (contacto.normal.y > 0.5f)
            {
                enSuelo = true;
                return;
            }
        }
    }

    // Se ejecuta cuando el personaje deja de tocar algo (por ejemplo, al saltar
    // o al caerse por un borde).
    void OnCollisionExit2D(Collision2D choque)
    {
        enSuelo = false;
    }
}
