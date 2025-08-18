using UnityEngine;

public class EntityInterpolation : MonoBehaviour
{
    public float lerpDuration = 0.1f;
    public float rotationPerSecond = 33f;
    
    private float _lerpTime;
    private Vector3 _current;
    private Vector3 _previous;
    private Vector3 _movementDirection;

    private void Start()
    {
        lerpDuration = GameManager.Config.UpdateEntityInterval;
    }

    public void SetCanonicalPosition(Vector3 position)
    {
        if(Vector3.Distance(position, _current) < 0.001f) return;
        _previous = transform.position;
        _current = position;
        _lerpTime = 0.0f;
    }
    
    public void SetMovementDirection(Vector3 movementDirection)
    {
        _movementDirection = movementDirection.normalized;
    }

    public void Update()
    {
        _lerpTime = Mathf.Min(_lerpTime + Time.deltaTime, lerpDuration);
        transform.position = Vector3.Lerp(_previous, _current, _lerpTime / lerpDuration);
        /*if (_movementDirection.magnitude < 0.001f) return;
        var newDirection = Vector3.RotateTowards(
            transform.forward, _movementDirection, rotationPerSecond * Time.deltaTime, 0.0f
        );
        transform.rotation = Quaternion.LookRotation(newDirection);*/
    }
}