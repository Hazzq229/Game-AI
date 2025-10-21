using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarController : MonoBehaviour
{
    // [Header("Loop")]
    // public bool isLooping = true;
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
    [Range(0f, 400f)] public float TopSpeed = 150f;
    [Range(0f, 100f)] public float DecelerationSpeed = 10f;

    [Header("Read-Only Data")]
    public int CurrentPathIndex;
    public float CurrentSpeed;

    private List<Transform> path;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = CenterOfMass;

        path = new List<Transform>();
        GetPath();
    }
    void Update()
    {
        GetSteer();
        Move();
        UpdateWheelVisuals();
    }
    void GetPath()
    {
        Transform[] childObjects = PathGroup.GetComponentsInChildren<Transform>();
        for (int i = 0; i < childObjects.Length; i++)
        {
            Transform temp = childObjects[i];
            if (temp != PathGroup.transform)
            {
                path.Add(temp);
            }
        }
    }
    void GetSteer()
    {
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
    void Move()
    {
        CurrentSpeed = rb.velocity.magnitude * 3.6f;
        CurrentSpeed = Mathf.Round(CurrentSpeed);

        if (CurrentSpeed <= TopSpeed)
        {
            colWheelRL.motorTorque = MaxTorque;
            colWheelRR.motorTorque = MaxTorque;
            colWheelRL.brakeTorque = 0;
            colWheelRR.brakeTorque = 0;
        }
        else
        {
            colWheelRL.motorTorque = 0;
            colWheelRR.motorTorque = 0;
            colWheelRL.brakeTorque = DecelerationSpeed;
            colWheelRR.brakeTorque = DecelerationSpeed;
        }
    }
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
}