using UnityEngine;

public class EntityRotationInterpolation : MonoBehaviour
{
    private float _lerpDuration = 0.05f;
    
    private float _lerpTime;
    private Quaternion _current;
    private Quaternion _previous;

    public void Init(float lerpDuration)
    {
        _lerpDuration = lerpDuration;
    }

    public void SetCanonicalRotation(Quaternion rotation)
    {
        _previous = transform.rotation;
        _current = rotation;
        _lerpTime = 0.0f;
    }

    public void Update()
    {
        _lerpTime = Mathf.Min(_lerpTime + Time.deltaTime, _lerpDuration);
        transform.rotation = Quaternion.Lerp(_previous, _current, _lerpTime / _lerpDuration);
    }
}