using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Experimental.GraphView.GraphView;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class Player : MonoBehaviour
{

    private PlayerInputActions inputActions;
    public Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visual;
    [SerializeField] private Transform raycastOrigin;
    [SerializeField] private Player2 player2;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    private float moveInput;
    private bool isGrounded = true;
    public bool actionHappening = false;
    private bool isWalking = false;
    private bool isJumping = false;
    public bool knockedBack = false;
    private Vector2 knockbackVelocity = Vector2.zero;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;
    private float health = 50f;
    PoseDataReceiver inputs = PoseDataReceiver.Instance;

    void Awake()
    {

 

        inputActions = new PlayerInputActions();

        rb = GetComponent<Rigidbody2D>();

        

        // Hook up input callbacks
        //inputActions.Player1.Jump.performed += ctx => OnJump();
        //inputActions.Player1.Punch.performed += ctx => OnPunch();
        //inputActions.Player1.Kick.performed += ctx => OnKick();
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    private void Start()
    {
        StartCoroutine(WaitForPoseData());
    }

    void Update()
    {

        try
        {
            moveInput = inputs.CurrentInputs.move;
            animator.SetFloat("Speed", Mathf.Abs(moveInput));
        



            // Poll pose input from WebSocket
            if (PoseDataReceiver.Instance != null)
            {
            

                // Handle jump input
                if (inputs.CurrentInputs.jump && isGrounded && !isJumping)
                {
                    Debug.Log("Jump called!");
                    OnJump();
                }

                // Handle punch input
                if (inputs.CurrentInputs.punch != 0 && !actionHappening)
                {
                    Debug.Log("Punch called!");
                    OnPunch(inputs.CurrentInputs.punch);
                }

                // Handle kick input
                if (inputs.CurrentInputs.kick != 0 && !actionHappening)
                {
                    Debug.Log("Kick called!");
                    OnKick(inputs.CurrentInputs.kick);
                }



            }

        }
        catch (NullReferenceException)
        {

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

        
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y) + knockbackVelocity;
        
        


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

    void OnPunch(int dir)
    {
        if (actionHappening)
        {
            return;
        }

        if (dir == 1)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        actionHappening = true;
        animator.SetBool("actionHappening", true);
        animator.Play("JabAnimation");

        //see if we actually hit the person

        RaycastHit2D hitRight = Physics2D.Raycast(raycastOrigin.transform.position, Vector2.right, 3.5f);
        RaycastHit2D hitLeft = Physics2D.Raycast(raycastOrigin.transform.position, Vector2.left, 3.5f);



        if (hitLeft.collider.gameObject.TryGetComponent<Player2>(out player2) || hitRight.collider.gameObject.TryGetComponent<Player2>(out player2))
        {
            player2.OnPunched();
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

            player2.ApplyKnockback(knockbackVector.normalized, 20f, 1);
        }
    }

    void OnKick(int dir)
    {
        if (actionHappening)
        {
            return;
        }

        if (dir == 1)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
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

        if (hitLeft.collider.gameObject.TryGetComponent<Player2>(out player2) || hitRight.collider.gameObject.TryGetComponent<Player2>(out player2))
        {
            player2.OnKicked();
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
            Debug.Log($"player 2 knockback {knockbackVector}");
            player2.ApplyKnockback(knockbackVector.normalized, 20f, 1);
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
        knockbackVelocity = direction.normalized * force;
        StartCoroutine(ResetKnockbackAfter(duration));
    }

    private IEnumerator ResetKnockbackAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        knockbackVelocity = Vector2.zero;
    }

    public void GameOver()
    {
        // Disable input actions
        inputActions.Disable();


        Debug.Log($"{gameObject.name} has been disabled for game over.");
    }

    private IEnumerator WaitForPoseData()
    {
        while (PoseDataReceiver.Instance == null || !PoseDataReceiver.Instance.IsReady)
        {
            yield return null;
        }

        // Now it’s safe to access input data
        var inputs = PoseDataReceiver.Instance.CurrentInputs;

        // You could use inputs here or periodically in Update
    }

}
