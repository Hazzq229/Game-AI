using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshAgentController : MonoBehaviour
{
    private GameObject destination;
    private NavMeshAgent agent;

    void Start()
    {
        destination = GameObject.FindGameObjectWithTag("Destination");
        
        agent = GetComponent<NavMeshAgent>();
        agent.SetDestination(destination.transform.position);   
    }
}
