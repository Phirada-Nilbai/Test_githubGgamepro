using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private PlayerAnimatorController animatorController;
    [SerializeField] private PlayerAudioController audioController;

    [Header("Player Values")]
    [SerializeField] private float movementSpeed = 3f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float timeBetweenJumps = 0.1f;
    [SerializeField] private float coyoteTimeDuration = 0.5f;

    [Header("Ground Checks")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float extraGroundCheckDistance = 0.5f;

    // Input Values
    private float _moveInput;

    // Boolean flags. Booleans for checking conditions.
    private bool _isGrounded;
    private bool _canJump;
    private bool _canDoubleJump;

    // Private variables
    private float _coyoteTimeTimer;
    private float _lastJumpTimer;

    // Stored References
    private GameManager _gameManager;

    private void Update()
    {
        CheckGround();
        CheckCanJump();
        SetAnimatorParameters();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void FindGameManager()
    {
        if (_gameManager != null) return;

        _gameManager = FindObjectOfType<GameManager>();
    }

    #region Actions

    private void Move()
    {
        rb.velocity = new Vector2(_moveInput * movementSpeed, rb.velocity.y);
    }

    private void FlipPlayerSprite()
    {
        player.localScale = _moveInput switch
        {
            > 0f => new Vector3(1, 1, 1),
            < 0f => new Vector3(-1, 1, 1),
            _ => player.localScale
        };
    }

    private void TryJumping()
    {
        if (_lastJumpTimer <= timeBetweenJumps) return;

        if (!_canJump)
        {
            if (!_canDoubleJump) return; 
            _canDoubleJump = false; 
        }

        Jump(jumpForce);
        audioController.PlayJumpSound();
    }

    public void Jump(float force, float additionalTimeWait = 0f)
    {
        _canJump = false; 
        _lastJumpTimer = 0f - additionalTimeWait;
        rb.velocity = new Vector2(rb.velocity.x, 0f); 
        rb.AddForce(force * transform.up, ForceMode2D.Impulse);
    }

    private void CheckGround()
    {
        var playerColliderBounds = playerCollider.bounds;

        var raycastHit = Physics2D.BoxCast(
            playerColliderBounds.center,
            playerColliderBounds.size,
            0f,
            Vector2.down,
            extraGroundCheckDistance,
            groundLayers);

        _isGrounded = raycastHit.collider != null;
    }

    private void CheckCanJump()
    {
        _lastJumpTimer = Mathf.Min(_lastJumpTimer, timeBetweenJumps) + Time.deltaTime; 

        if (_isGrounded)
        {
            _canJump = true;
            _coyoteTimeTimer = 0f;
            return;
        }

        _coyoteTimeTimer = Mathf.Min(_coyoteTimeTimer, coyoteTimeDuration) + Time.deltaTime;

        if (_coyoteTimeTimer <= coyoteTimeDuration) return; 

        _canJump = false; 
    }

    private void SetAnimatorParameters()
    {
        animatorController.SetAnimatorParameter(rb.velocity, _isGrounded);
    }

    public void EnableDoubleJump()
    {
        _canDoubleJump = true;
    }

    public void TakeDamage()
    {
        FindGameManager();
        _gameManager.ProcessPlayerDeath();
        audioController.PlayDeadSound();

    }

    #endregion

    #region Input

    private void OnMove(InputValue value)
    {
        _moveInput = value.Get<float>();

        FlipPlayerSprite();
    }

    private void OnJump(InputValue value)
    {
        if (!value.isPressed) return;

        TryJumping();
    }

    private void OnQuit(InputValue value)
    {
        FindGameManager();
        _gameManager.ReturnToMainMenu();
    }

    #endregion

}