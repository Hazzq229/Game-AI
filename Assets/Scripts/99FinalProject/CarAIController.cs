using UnityEngine;
using System.Collections.Generic;
using UnityHFSM;

[RequireComponent(typeof(Rigidbody))]
public class CarAIController : MonoBehaviour
{
    [Header("AI & Sensors")]
    public TrafficLightController trafficLight; 
    public Transform stopLineTrigger; 
    public float detectionRange = 6f; 
    public LayerMask obstacleLayer;   
    
    [Header("Sensor Adjustment")]
    public float sensorRadius = 0.5f; // Diperkecil sedikit agar tidak terlalu overlap
    public float sensorAngle = 30f;   // Sudut penyebaran sensor (Cone)
    public Vector3 sensorOffset = new Vector3(0, 0.5f, 0); 

    [Header("Physics and Path Setting")]
    public Vector3 CenterOfMass;
    public Transform PathGroup; 
    [Range(0f, 45f)] public float MaxSteer = 15f;
    [Range(0f, 10f)] public float DistFromPath = 5f;

    [Header("Wheel Colliders")]
    public WheelCollider colWheelFL;
    public WheelCollider colWheelFR;
    public WheelCollider colWheelRL;
    public WheelCollider colWheelRR;

    [Header("Wheel Transforms")]
    public Transform trWheelFL;
    public Transform trWheelFR;
    public Transform trWheelRL;
    public Transform trWheelRR;

    [Header("Motor & Speed Settings")]
    [Range(0f, 1000f)] public float MaxTorque = 100f;
    [Range(0f, 2000f)] public float MaxBrakeTorque = 500f; 
    [Range(0f, 400f)] public float TopSpeed = 150f;
    [Range(0f, 100f)] public float DecelerationSpeed = 10f; 

    [Header("Read-Only Data")]
    public int CurrentPathIndex;
    public float CurrentSpeed;
    public string CurrentStateName; 

    private List<Transform> path;
    private Rigidbody rb;
    private StateMachine fsm; 

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = CenterOfMass;

        path = new List<Transform>();
        GetPath();

        // --- INISIALISASI FSM ---
        fsm = new StateMachine();

        fsm.AddState("Drive",
            onLogic: (state) => {
                GetSteer(); 
                Drive();    
                CurrentStateName = "Drive";
            }
        );

        fsm.AddState("Stop",
            onLogic: (state) => {
                GetSteer(); 
                Brake();    
                CurrentStateName = "Stop";
            }
        );

        fsm.AddTransition("Drive", "Stop", 
            (transition) => ShouldStop()
        );

        fsm.AddTransition("Stop", "Drive", 
            (transition) => !ShouldStop()
        );

        fsm.Init();
    }

    void FixedUpdate()
    {
        fsm.OnLogic();

        CurrentSpeed = rb.velocity.magnitude * 3.6f;
        CurrentSpeed = Mathf.Round(CurrentSpeed);

        UpdateWheelVisuals();
    }

    // --- LOGIKA UTAMA ---

    void GetPath()
    {
        if (PathGroup == null) return;
        
        Transform[] childObjects = PathGroup.GetComponentsInChildren<Transform>();
        for (int i = 0; i < childObjects.Length; i++)
        {
            if (childObjects[i] != PathGroup.transform)
            {
                path.Add(childObjects[i]);
            }
        }
    }

    void GetSteer()
    {
        if (path.Count == 0) return;

        Vector3 steerVector = transform.InverseTransformPoint(path[CurrentPathIndex].position);
        float newSteer = MaxSteer * (steerVector.x / steerVector.magnitude);
        
        colWheelFL.steerAngle = newSteer;
        colWheelFR.steerAngle = newSteer;

        if (steerVector.magnitude <= DistFromPath)
        {
            CurrentPathIndex++;
            if (CurrentPathIndex >= path.Count)
            {
                CurrentPathIndex = 0; 
            }
        }
    }

    void Drive()
    {
        colWheelRL.brakeTorque = 0;
        colWheelRR.brakeTorque = 0;

        if (CurrentSpeed <= TopSpeed)
        {
            colWheelRL.motorTorque = MaxTorque;
            colWheelRR.motorTorque = MaxTorque;
        }
        else
        {
            colWheelRL.motorTorque = 0;
            colWheelRR.motorTorque = 0;
            colWheelRL.brakeTorque = DecelerationSpeed;
            colWheelRR.brakeTorque = DecelerationSpeed;
        }
    }

    void Brake()
    {
        colWheelRL.motorTorque = 0;
        colWheelRR.motorTorque = 0;

        colWheelRL.brakeTorque = MaxBrakeTorque;
        colWheelRR.brakeTorque = MaxBrakeTorque;
    }

    // --- SENSOR (Decision Logic) ---

    bool ShouldStop()
    {
        // 1. Cek Lampu Merah
        if (trafficLight != null && stopLineTrigger != null)
        {
            float distToStopLine = Vector3.Distance(transform.position, stopLineTrigger.position);
            bool isRedLight = trafficLight.CurrentState != LightColor.Green; 

            if (isRedLight && distToStopLine < 10f)
            {
                Vector3 dirToLine = (stopLineTrigger.position - transform.position).normalized;
                if (Vector3.Dot(transform.forward, dirToLine) > 0)
                {
                    return true;
                }
            }
        }

        // 2. Cek Obstacle (Cone / Whiskers detection)
        // Kita tembakkan 3 sensor: Tengah, Kiri, Kanan
        Vector3 sensorStartPos = transform.position + sensorOffset;
        Vector3 forward = transform.forward;
        Vector3 leftDir = Quaternion.Euler(0, -sensorAngle, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, sensorAngle, 0) * forward;

        RaycastHit hit;

        // Cek Tengah
        if (Physics.SphereCast(sensorStartPos, sensorRadius, forward, out hit, detectionRange, obstacleLayer)) return true;
        
        // Cek Kiri (Sudut)
        if (Physics.SphereCast(sensorStartPos, sensorRadius, leftDir, out hit, detectionRange, obstacleLayer)) return true;

        // Cek Kanan (Sudut)
        if (Physics.SphereCast(sensorStartPos, sensorRadius, rightDir, out hit, detectionRange, obstacleLayer)) return true;

        return false;
    }

    // --- VISUALIZATION ---

    void UpdateWheelVisuals()
    {
        UpdateSingleWheel(colWheelFL, trWheelFL);
        UpdateSingleWheel(colWheelFR, trWheelFR);
        UpdateSingleWheel(colWheelRL, trWheelRL);
        UpdateSingleWheel(colWheelRR, trWheelRR);
    }

    void UpdateSingleWheel(WheelCollider collider, Transform wheelTransform)
    {
        collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }

    // Visualisasi Cone Whiskers
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 sensorStartPos = transform.position + sensorOffset;
        
        // Fungsi helper lokal untuk menggambar satu "kumis"
        void DrawSensor(Vector3 direction)
        {
            Vector3 endPos = sensorStartPos + direction * detectionRange;
            Gizmos.DrawWireSphere(sensorStartPos, sensorRadius);
            Gizmos.DrawLine(sensorStartPos, endPos);
            Gizmos.DrawWireSphere(endPos, sensorRadius);
        }

        Vector3 forward = transform.forward;
        Vector3 leftDir = Quaternion.Euler(0, -sensorAngle, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, sensorAngle, 0) * forward;

        DrawSensor(forward);   // Tengah
        DrawSensor(leftDir);   // Kiri
        DrawSensor(rightDir);  // Kanan
    }
}