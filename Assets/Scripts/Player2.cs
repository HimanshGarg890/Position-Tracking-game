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
    [SerializeField] private Transform raycastOrigin;
    [SerializeField] private Player player1;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    private float moveInput;
    private bool isGrounded = true;
    public bool actionHappening = false;
    private bool isWalking = false;
    private bool isJumping = false;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;
    private float health = 50f;

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
        isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down, 3.5f);
        animator.SetBool("isGrounded", isGrounded);

        if (!actionHappening && !isWalking && !isJumping)
        {
            animator.Play("IdleAnimation");
        }

        //Debug.DrawRay(raycastOrigin.transform.position, Vector2.left * 100f, Color.red);
        //Debug.DrawRay(raycastOrigin.transform.position, Vector2.right * 100f, Color.red);
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

        if (isGrounded)
        {
           
            isJumping = true;
            //animator.SetBool("actionHappening", true);



            rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce);

            animator.Play("JumpAnimation");
        }
        else
        {
            isJumping = false;
        }
        
        
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

        //see if we actually hit the person

        RaycastHit2D hitRight = Physics2D.Raycast(raycastOrigin.transform.position, Vector2.right, 3.5f);
        RaycastHit2D hitLeft = Physics2D.Raycast(raycastOrigin.transform.position, Vector2.left, 3.5f);

        if (hitLeft.collider.gameObject.TryGetComponent<Player>(out player1) || hitRight.collider.gameObject.TryGetComponent<Player>(out player1))
        {
            player1.OnPunched();
        }

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

        //see if we actually hit the person

        RaycastHit2D hitRight = Physics2D.Raycast(raycastOrigin.transform.position, Vector2.right, 3.5f);
        RaycastHit2D hitLeft = Physics2D.Raycast(raycastOrigin.transform.position, Vector2.left, 3.5f);

        if (hitLeft.collider.gameObject.TryGetComponent<Player>(out player1) || hitRight.collider.gameObject.TryGetComponent<Player>(out player1))
        {
            player1.OnKicked();
        }
    }
    public void OnKicked()
    {
        GetComponent<HealthSystem>().TakeDamage(5);
        //Debug.Log($"player 1 health is {health}");
    }
    public void OnPunched()
    {
        GetComponent<HealthSystem>().TakeDamage(3);
        //Debug.Log($"player 1 health is {health}");
    }
    public float GetHealth()
    {
        return health;
    }
}
