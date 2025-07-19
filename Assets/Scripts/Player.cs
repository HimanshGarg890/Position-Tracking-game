using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class Player : MonoBehaviour
{
    private PlayerInputActions inputActions;
    private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visual;


    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    private float moveInput;
    private bool isGrounded = true;
    public bool actionHappening = false;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    void Awake()
    {
        inputActions = new PlayerInputActions();

        rb = GetComponent<Rigidbody2D>();
        

        // Hook up input callbacks
        inputActions.Player1.Jump.performed += ctx => OnJump();
        inputActions.Player1.Punch.performed += ctx => OnPunch();
        inputActions.Player1.Kick.performed += ctx => OnKick();
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    void Update()
    {
        moveInput = inputActions.Player1.Move.ReadValue<float>();
        animator.SetFloat("Speed", Mathf.Abs(moveInput));

        // Ground check
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("isGrounded", isGrounded);
    }

    void FixedUpdate()
    {
        if (moveInput < 0)
        {
            visual.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
        else
        {
            visual.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    void OnJump()
    {
        Debug.Log(isGrounded);

        if (actionHappening)
        {
            return;
        }
        actionHappening = true;

        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce);

            animator.SetTrigger("Jump");
        }
    }

    void OnPunch()
    {
        if (actionHappening)
        {
            return;
        }
        actionHappening = true;
        animator.SetTrigger("Punch");
    }

    void OnKick()
    {
        if (actionHappening)
        {
            return;
        }
        actionHappening = true;
        animator.SetTrigger("Kick");
    }

    
}
