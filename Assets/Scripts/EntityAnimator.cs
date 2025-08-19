using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EntityAnimator : MonoBehaviour
{
    public Animator animator;
    
    private static readonly int MovementSpeed = Animator.StringToHash("MovementSpeed");
    private static readonly int StrafeSpeed = Animator.StringToHash("StrafeSpeed");
    private static readonly int ForwardSpeed = Animator.StringToHash("ForwardSpeed");
    private const float MovementDamping = 0.05f;
    private Vector3 _direction;
    private float _currentStrafe = 0f;
    private float _currentForward = 0f;
    private Vector2 _relativeDirection;
    
    public void SetMovement(Vector3 direction, Vector2 relativeDirection)
    {
        _direction = direction;
        _relativeDirection = relativeDirection;
        animator.SetFloat(MovementSpeed, _direction.magnitude);
    }

    private void Update()
    {
        _currentStrafe = Mathf.Lerp(_currentStrafe, _relativeDirection.x, Time.deltaTime / MovementDamping);
        _currentForward = Mathf.Lerp(_currentForward, _relativeDirection.y, Time.deltaTime / MovementDamping);
        animator.SetFloat(StrafeSpeed, _currentStrafe);
        animator.SetFloat(ForwardSpeed, _currentForward);
    }

    void Start()
    {
        if(!animator) animator = GetComponent<Animator>();
    }
}
