using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EntityAnimator : MonoBehaviour
{
    private static readonly int MovementSpeed = Animator.StringToHash("MovementSpeed");
    public Animator animator;
    private Vector3 _direction;

    public void SetDirection(Vector3 direction)
    {
        _direction = direction;
    }
    
    void Start()
    {
        if(!animator) animator = GetComponent<Animator>();
    }
    
    void Update()
    {
        animator.SetFloat(MovementSpeed, _direction.magnitude);
    }
}
