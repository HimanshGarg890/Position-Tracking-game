using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class Player : MonoBehaviour
{
    private PlayerInputActions inputActions;
    private Rigidbody2D rb;
    private Animator animator;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    private float moveInput;
    private bool isGrounded = true;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    void Awake()
    {
        inputActions = new PlayerInputActions();

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

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
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    void OnJump()
    {
        Debug.Log(isGrounded);

        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce);
            
            animator.SetTrigger("Jump");
        }
    }

    void OnPunch()
    {
        animator.SetTrigger("Punch");
    }

    void OnKick()
    {
        animator.SetTrigger("Kick");
    }
}
