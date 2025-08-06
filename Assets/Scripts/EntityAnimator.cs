using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EntityAnimator : MonoBehaviour
{
    private static readonly int MovementSpeed = Animator.StringToHash("MovementSpeed");
    public Animator animator;
    private Vector3 _lastPosition;
    
    void Start()
    {
        if(!animator) animator = GetComponent<Animator>();
    }
    
    void Update()
    {
        animator.SetFloat(MovementSpeed, (transform.position - _lastPosition).magnitude);
        _lastPosition = transform.position;
    }
}
