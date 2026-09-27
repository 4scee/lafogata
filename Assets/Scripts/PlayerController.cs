using UnityEngine;

/// Actividad 4/5 - Movimiento del personaje + estados de animación (Idle, Walk, Jump).
/// Requiere Rigidbody2D (Dynamic, Gravity Scale 3, Freeze Rotation Z) y un Collider2D.
/// Animator: parámetros  Speed (float)  y  Grounded (bool).
/// Compatible con Unity 6 (Rigidbody2D.linearVelocity). Teclado: flechas / A D / Espacio,
/// funciona con el Input System nuevo, con el antiguo o con ambos.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 10f;
    public Animator animator;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Vector3 startPos;

    int uiMove;          // botones en pantalla (-1, 0, 1)
    float keyMove;       // teclado
    float move;
    bool jumpQueued;
    bool grounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        rb.freezeRotation = true;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;   // si duerme, OnCollisionStay2D deja de llamarse y no podría saltar
        startPos = transform.position;
    }

    // Enlazados desde los botones de la UI (MoveButton / OnClick)
    public void SetMove(int dir) => uiMove = dir;
    public void Jump() => jumpQueued = true;

    public void Respawn()
    {
        transform.position = startPos;
        rb.linearVelocity = Vector2.zero;
    }

    void Update()
    {
        bool active = GameManager.Instance == null || GameManager.Instance.State == GameState.Playing;

        keyMove = 0f;
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && active)
        {
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) keyMove -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) keyMove += 1f;
            if (kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
                jumpQueued = true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (active)
        {
            float k = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(k) > 0.01f) keyMove = k;
            if (Input.GetButtonDown("Jump")) jumpQueued = true;
        }
#endif
        if (!active) jumpQueued = false;

        move = !active ? 0f : (Mathf.Abs(keyMove) > 0.01f ? Mathf.Sign(keyMove) : uiMove);

        if (move != 0f && sr) sr.flipX = move < 0f;

        if (animator)
        {
            animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
            animator.SetBool("Grounded", grounded);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(move * speed, rb.linearVelocity.y);

        if (jumpQueued && grounded)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpQueued = false;

        grounded = false;   // OnCollisionStay2D lo vuelve a poner en true si hay piso
    }

    void OnCollisionStay2D(Collision2D c)
    {
        foreach (var p in c.contacts)
            if (p.normal.y > 0.5f) { grounded = true; return; }
    }
}
