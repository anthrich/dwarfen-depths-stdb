using System;
using UnityEngine;

public class EntityInterpolation : MonoBehaviour
{
    private float _lerpTime;
    private Vector3 _current;
    private Vector3 _previous;
    public float lerpDuration = 0.1f;

    private void Start()
    {
        lerpDuration = GameManager.Config.UpdateEntityInterval;
    }

    public void SetCanonicalPosition(Vector3 position)
    {
        if(Vector3.Distance(position, _current) < 0.001f) return;
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