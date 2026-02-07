using UnityEngine;
using UnityHFSM; // Import FSM
using UnityEngine.InputSystem; // Wajib untuk New Input System

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;

    // --- References ---
    private PlayerControls playerControls; // Class generated dari Input Actions
    private CharacterController characterController;
    private Animator animator;
    private StateMachine fsm;
    
    // --- Logic Variables ---
    private TrafficLightController currentTrafficLight;
    private bool canInteract = false;
    private Vector3 velocity;
    private Vector3 moveDir;
    private float currentSpeed;
    private float interactTimer;

    // 1. Inisialisasi Input System di Awake
    void Awake()
    {
        playerControls = new PlayerControls();
    }

    // 2. Wajib Enable/Disable Input System
    void OnEnable()
    {
        playerControls.Enable();
    }

    void OnDisable()
    {
        playerControls.Disable();
    }

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // --- INISIALISASI FSM ---
        fsm = new StateMachine();

        // --- STATE: IDLE ---
        fsm.AddState("Idle",
            onEnter: (state) => {
                if(animator) {
                    animator.SetBool("IsWalking", false);
                    animator.SetBool("IsRunning", false);
                }
            },
            onLogic: (state) => {
                ApplyGravity();
                HandleRotation();
                
                // INTERAKSI: Menggunakan .triggered (setara GetButtonDown)
                if (canInteract && playerControls.PlayerMovement.Interact.triggered)
                {
                    fsm.RequestStateChange("Interact");
                }
            }
        );

        // --- STATE: MOVE ---
        fsm.AddState("Move",
            onEnter: (state) => {
                if (animator) animator.SetBool("IsWalking", true);
            },
            onLogic: (state) => {
                // RUN: Menggunakan .IsPressed() (setara GetButton Hold)
                bool isRunning = playerControls.PlayerMovement.Run.IsPressed();
                currentSpeed = isRunning ? runSpeed : walkSpeed;

                if(animator) {
                    animator.SetBool("IsWalking", !isRunning);
                    animator.SetBool("IsRunning", isRunning);
                }

                HandleMovement();
                HandleRotation();
                ApplyGravity();

                // INTERAKSI saat bergerak
                if (canInteract && playerControls.PlayerMovement.Interact.triggered)
                {
                    fsm.RequestStateChange("Interact");
                }
            }
        );

        // --- STATE: INTERACT ---
        fsm.AddState("Interact",
            onEnter: (state) => {
                Debug.Log("Interaksi dimulai...");
                if (currentTrafficLight != null)
                {
                    currentTrafficLight.RequestCrossing();
                }
                
                if(animator) animator.SetTrigger("Interact");
                interactTimer = 0f;
            },
            onLogic: (state) => {
                moveDir = Vector3.zero; // Stop gerak
                ApplyGravity();

                interactTimer += Time.deltaTime;

                // Kembali ke Idle setelah durasi animasi
                if (interactTimer > 1.5f)
                {
                    fsm.RequestStateChange("Idle");
                }
            }
        );

        // --- TRANSISI ---
        // Pindah ke Move jika Input Vector > 0
        fsm.AddTransition("Idle", "Move", 
            (transition) => GetInputVector().magnitude > 0.1f
        );

        // Pindah ke Idle jika Input Vector berhenti
        fsm.AddTransition("Move", "Idle", 
            (transition) => GetInputVector().magnitude < 0.1f
        );

        fsm.Init();
    }

    void Update()
    {
        fsm.OnLogic();
    }

    // --- INPUT HELPER (New Input System) ---

    Vector3 GetInputVector()
    {
        // Membaca Value Vector2 dari Action Map "Movement"
        Vector2 input = playerControls.PlayerMovement.Movement.ReadValue<Vector2>();
        return new Vector3(input.x, 0, input.y).normalized;
    }

    // --- MOVEMENT LOGIC ---

    void HandleMovement()
    {
        Vector3 input = GetInputVector();
        moveDir = input;

        if (moveDir.magnitude >= 0.1f)
        {
            characterController.Move(moveDir * currentSpeed * Time.deltaTime);
        }
    }

    void HandleRotation()
    {
        Vector3 input = GetInputVector();
        if (input.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationSpeed, 0.1f);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }
    }

    void ApplyGravity()
    {
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    // --- TRIGGERS ---

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Button"))
        {
            canInteract = true;
            currentTrafficLight = other.GetComponentInParent<TrafficLightController>();
            // Debug.Log("Tekan tombol Interaksi (Keyboard: E / Gamepad: South) untuk menyeberang");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Button"))
        {
            canInteract = false;
            currentTrafficLight = null;
        }
    }
}