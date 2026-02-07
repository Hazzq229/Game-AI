using UnityEngine;
using UnityHFSM; 

public enum LightColor
{
    Green,
    Yellow,
    Red
}

public class TrafficLightController : MonoBehaviour
{
    [Header("Settings")]
    public float greenDuration = 5f;
    public float yellowDuration = 2f;
    public float redDuration = 5f;

    [Header("Visualization")]
    public MeshRenderer lightRenderer; // Menggunakan MeshRenderer sesuai contohmu
    private Shader defShader, unlitShader;

    private StateMachine<LightColor> fsm;
    public LightColor CurrentState { get; private set; }

    private bool pedestrianRequest = false;
    
    // Timer manual agar kita punya kontrol penuh atas waktu per state
    private float timer;

    void Start()
    {
        // Setup Shader
        if (lightRenderer == null) lightRenderer = GetComponent<MeshRenderer>();
        defShader = Shader.Find("Standard");
        unlitShader = Shader.Find("Unlit/Color"); // Atau shader lain yang self-illuminated

        fsm = new StateMachine<LightColor>();

        // --- DEFINISI STATE ---
        // Kita reset timer = 0 setiap kali masuk state baru (onEnter)
        // Kita tambah timer += Time.deltaTime setiap frame (onLogic)

        fsm.AddState(LightColor.Green, 
            onEnter: (state) => {
                SetLight(LightColor.Green);
                CurrentState = LightColor.Green;
                timer = 0; 
            },
            onLogic: (state) => timer += Time.deltaTime
        );

        fsm.AddState(LightColor.Yellow, 
            onEnter: (state) => {
                SetLight(LightColor.Yellow);
                CurrentState = LightColor.Yellow;
                timer = 0;
            },
            onLogic: (state) => timer += Time.deltaTime
        );

        fsm.AddState(LightColor.Red, 
            onEnter: (state) => {
                SetLight(LightColor.Red);
                CurrentState = LightColor.Red;
                timer = 0;
                pedestrianRequest = false; // Reset tombol saat sudah merah
            },
            onLogic: (state) => timer += Time.deltaTime
        );

        // --- DEFINISI TRANSISI ---
        // Menggunakan lambda () => bool untuk kondisi

        // Green -> Yellow
        // Pindah jika waktu habis ATAU (ada request penyeberang DAN sudah jalan minimal 2 detik)
        fsm.AddTransition(LightColor.Green, LightColor.Yellow, 
            (transition) => timer >= greenDuration || (pedestrianRequest && timer >= 2f));

        // Yellow -> Red
        fsm.AddTransition(LightColor.Yellow, LightColor.Red, 
            (transition) => timer >= yellowDuration);

        // Red -> Green
        fsm.AddTransition(LightColor.Red, LightColor.Green, 
            (transition) => timer >= redDuration);

        fsm.Init();
    }

    void Update()
    {
        fsm.OnLogic();
    }

    // Fungsi Pengganti Material (Diadaptasi dari script TrafficLights kamu)
    // Asumsi: Material index 1 = Green, 2 = Yellow, 3 = Red
    // Index 0 biasanya adalah body/tiang lampu
    public void SetLight(LightColor color)
    {
        if (lightRenderer == null) return;

        int activeIndex = 0;
        switch (color)
        {
            case LightColor.Green:
                activeIndex = 1;
                break;
            case LightColor.Yellow:
                activeIndex = 2;
                break;
            case LightColor.Red:
                activeIndex = 3;
                break;
        }

        // Loop dari material index 1 sampai 3 (sesuai contohmu: 1=Hijau, 2=Kuning, 3=Merah)
        // Jika mesh renderer materials count berbeda, sesuaikan loopnya (misal i < lightRenderer.materials.Length)
        for(int i = 1; i < 4 && i < lightRenderer.materials.Length; i++) 
        {
            lightRenderer.materials[i].shader = (activeIndex == i) ? unlitShader : defShader;
        }
    }

    public void RequestCrossing()
    {
        // Hanya terima input jika lampu sedang Hijau
        if (CurrentState == LightColor.Green)
        {
            pedestrianRequest = true;
            Debug.Log("Pejalan kaki menekan tombol!");
        }
    }
}