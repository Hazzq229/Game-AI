using UnityEngine;
using UnityEngine.AI; // Wajib untuk NavMesh
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees; // Import namespace Fluid Behavior Tree

public class CompanionAIController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator; // Opsional jika ada animasi

    [Header("Movement Settings")]
    public float walkSpeed = 3.5f; // Kecepatan jalan normal
    public float runSpeed = 6.0f;  // Kecepatan lari (jika jauh)
    public float acceleration = 8.0f; // Agar tidak terlalu kaku saat mulai jalan

    [Header("Distance Settings")]
    public float followDistance = 3.0f; // Jarak trigger mulai mengikuti (dari diam)
    public float stopDistance = 1.5f;   // Jarak berhenti dekat slot
    public float runDistanceThreshold = 6.0f; // Jarak dimana anjing mulai lari karena tertinggal jauh
    
    [Header("Slot Offset")]
    // Posisi relatif terhadap pemain (x=1 artinya di kanan, z=-1 artinya di belakang)
    public Vector3 slotOffset = new Vector3(1.5f, 0, -1.0f); 

    [SerializeField] // Agar bisa dilihat di inspector untuk debug
    private BehaviorTree tree;

    private Vector3 currentSlotPosition;

    void Start()
    {
        // Validasi dan Setup Awal
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        
        // Set akselerasi agar gerak lebih natural
        agent.acceleration = acceleration;

        // --- MEMBANGUN BEHAVIOR TREE ---
        
        tree = new BehaviorTreeBuilder(gameObject)
            .Selector() 
                
                // CABANG 1: FOLLOW PLAYER
                .Sequence("Follow Player")
                    
                    // KONDISI: Apakah harus ikut? (Pake Hysteresis)
                    .Condition("Should Follow?", () => 
                    {
                        UpdateSlotPosition();
                        float dist = Vector3.Distance(transform.position, currentSlotPosition);
                        
                        // Hysteresis Logic:
                        // Jika sedang jalan, batasnya stopDistance (biar sampai tujuan).
                        // Jika sedang diam, batasnya followDistance (biar ga sensitif gerak dikit-dikit).
                        float currentThreshold = agent.hasPath ? stopDistance : followDistance;

                        // Tambah buffer dikit biar stabil
                        return dist > currentThreshold + 0.1f;
                    })
                    
                    // AKSI: Bergerak ke Slot
                    .Do("Move To Slot", () => 
                    {
                        UpdateSlotPosition();
                        float dist = Vector3.Distance(transform.position, currentSlotPosition);

                        // --- LOGIKA KECEPATAN ---
                        // Jika tertinggal sangat jauh (> runDistanceThreshold), lari.
                        // Jika tidak, jalan biasa.

                        agent.speed = walkSpeed;
                        if (dist > runDistanceThreshold) agent.speed = runSpeed;

                        if(animator) animator.SetBool("IsWalking", true);

                        // Perintahkan NavMesh Agent
                        agent.SetDestination(currentSlotPosition);
                        
                        // Cek apakah sudah sampai
                        if (!agent.pathPending && agent.remainingDistance <= stopDistance)
                        {
                            return TaskStatus.Success; // Sudah sampai
                        }
                        return TaskStatus.Continue; // Masih jalan
                    })
                .End()

                // CABANG 2: IDLE (Fallback jika pemain dekat)
                .Do("Idle", () =>
                {
                    agent.ResetPath(); // Stop NavMesh Agent
                    
                    // Reset Animasi
                    if(animator) 
                    {
                        animator.SetBool("IsWalking", false);
                    }
                    return TaskStatus.Success;
                })

            .End() // End Selector
            .Build();
    }

    void Update()
    {
        // Jalankan Tick tree setiap frame
        tree.Tick();
    }

    // Menghitung posisi "Slot" (Leader-Based Steering)
    void UpdateSlotPosition()
    {
        if (player == null) return;

        // TransformPoint mengubah local position menjadi world position
        // Ini otomatis menangani rotasi pemain.
        currentSlotPosition = player.TransformPoint(slotOffset);
    }

    // Visualisasi Debugging
    void OnDrawGizmos()
    {
        if (player != null)
        {
            Vector3 debugPos = player.TransformPoint(slotOffset);
            
            // Gambar posisi slot target
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(debugPos, 0.5f);
            Gizmos.DrawLine(transform.position, debugPos);

            // Gambar batas lari (opsional)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, runDistanceThreshold);
        }
    }
}