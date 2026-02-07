using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AgentAnimated : Agent
{
    [Header("Animation Settings")]
    public Animator _animator;
    public string _walkingParameterName = "IsWalking";
    void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }
    }
    protected override void OnMoveStart()
    {
        if(_animator != null && !string.IsNullOrEmpty(_walkingParameterName))
        {
            _animator.SetBool(_walkingParameterName, true);
        }
    }


    protected override void OnMoveStop()
    {
        if(_animator != null && !string.IsNullOrEmpty(_walkingParameterName))
        {
            _animator.SetBool(_walkingParameterName, false);
        }
    }
}
