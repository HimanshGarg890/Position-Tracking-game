using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class Player2 : MonoBehaviour
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
    private bool isWalking = false;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    void Awake()
    {
        inputActions = new PlayerInputActions();

        rb = GetComponent<Rigidbody2D>();


        // Hook up input callbacks
        inputActions.Player2.Jump.performed += ctx => OnJump();
        inputActions.Player2.Punch.performed += ctx => OnPunch();
        inputActions.Player2.Kick.performed += ctx => OnKick();
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    void Update()
    {
        moveInput = inputActions.Player2.Move.ReadValue<float>();
        animator.SetFloat("Speed", Mathf.Abs(moveInput));

        // Ground check
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("isGrounded", isGrounded);

        if (!actionHappening)
        {
            animator.Play("IdleAnimation");
        }
    }

    void FixedUpdate()
    {
        if (moveInput < 0)
        {
            isWalking = true;
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
        else if (moveInput > 0)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            isWalking = true;
        }
        else
        {
            isWalking = false;
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
        animator.SetBool("actionHappening", true);



        rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce);

        animator.Play("JumpAnimation");

    }

    void OnPunch()
    {
        if (actionHappening)
        {
            return;
        }
        actionHappening = true;
        animator.SetBool("actionHappening", true);
        animator.Play("JabAnimation");

    }

    void OnKick()
    {
        if (actionHappening)
        {
            return;
        }
        actionHappening = true;
        animator.SetBool("actionHappening", true);
        animator.Play("KickAnimation");
    }


}
