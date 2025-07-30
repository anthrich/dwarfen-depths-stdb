using UnityEngine;

public class EntityInterpolation : MonoBehaviour
{
    private float _lerpTime;
    private Vector3 _current;
    private Vector3 _previous;
    public float lerpDuration = 0.1f;

    public void SetDeltaTime(float deltaTime)
    {
        lerpDuration = deltaTime;
    }

    public void SetCanonicalPosition(Vector3 position)
    {
        _previous = _current;
        _current = position;
        _lerpTime = 0.0f;
    }

    public Vector3 GetCanonicalPosition()
    {
        return _current;
    }

    public void Update()
    {
        _lerpTime = Mathf.Min(_lerpTime + Time.deltaTime, lerpDuration);
        transform.position = Vector3.Lerp(_previous, _current, _lerpTime / lerpDuration);
    }
}