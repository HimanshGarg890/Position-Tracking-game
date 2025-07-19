using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class Player2 : MonoBehaviour
{

    private PlayerInputActions inputActions;
    public Rigidbody2D rb;
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
    private Vector2 knockbackVelocity2 = Vector2.zero;

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

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y) + knockbackVelocity2;


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
            bool attackerFacingLeft = transform.rotation.eulerAngles.y == 180f ? true : false;

            Vector2 knockbackVector = Vector2.zero;

            if (attackerFacingLeft)
            {
                knockbackVector = new Vector2(Vector2.left.x, 0.01f);
            }
            else
            {
                knockbackVector = new Vector2(Vector2.right.x, 0.01f);
            }

            player1.ApplyKnockback(knockbackVector.normalized, 20f, 1);
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

        if (isGrounded)
        {
            animator.Play("KickAnimation");
        }
        else
        {
            animator.Play("JumpKickAnimation");
            isJumping = false;
        }


            //see if we actually hit the person

        RaycastHit2D hitRight = Physics2D.Raycast(raycastOrigin.transform.position, Vector2.right, 3.5f);
        RaycastHit2D hitLeft = Physics2D.Raycast(raycastOrigin.transform.position, Vector2.left, 3.5f);

        if (hitLeft.collider.gameObject.TryGetComponent<Player>(out player1) || hitRight.collider.gameObject.TryGetComponent<Player>(out player1))
        {
            player1.OnKicked();
            bool attackerFacingLeft = transform.rotation.eulerAngles.y == 180f ? true : false;

            Vector2 knockbackVector = Vector2.zero;

            if (attackerFacingLeft)
            {
                knockbackVector = new Vector2(Vector2.left.x, 0.01f);
            }
            else
            { 
                 knockbackVector = new Vector2(Vector2.right.x, 0.01f);
            }

            player1.ApplyKnockback(knockbackVector.normalized, 20f, 1);
        }
    }
    public void OnKicked()
    {
        GetComponent<HealthSystem>().TakeDamage(5);
        actionHappening = true;
        animator.SetBool("actionHappening", true);
        animator.Play("HurtAnimation");
        //Debug.Log($"player 1 health is {health}");

    }
    public void OnPunched()
    {
        GetComponent<HealthSystem>().TakeDamage(3);
        actionHappening = true;
        animator.SetBool("actionHappening", true);
        animator.Play("HurtAnimation");
        //Debug.Log($"player 1 health is {health}");

       
    }
    public float GetHealth()
    {
        return health;
    }
    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        knockbackVelocity2 = direction.normalized * force;
        StartCoroutine(ResetKnockbackAfter(duration));
    }

    private IEnumerator ResetKnockbackAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        knockbackVelocity2 = Vector2.zero;
    }

    public void GameOver()
    {
        // Disable input actions
        inputActions.Disable();


        Debug.Log($"{gameObject.name} has been disabled for game over.");
    }
}
