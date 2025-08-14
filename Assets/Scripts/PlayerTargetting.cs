using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTargetting : MonoBehaviour
{
    private static readonly int TargetPosition = Shader.PropertyToID("_TargetPosition");
    private static readonly int Alpha = Shader.PropertyToID("_Alpha");
    private static readonly int Color1 = Shader.PropertyToID("_Color");
    private static readonly int Radius = Shader.PropertyToID("_Radius");

    [Header("Reticle")]
    public Material circleMaterial;
    public GameObject circleQuad;
    public float circleRadius = 0.6f;
    public Color circleColor = Color.green;
    
    [Header("Animation")]
    public bool animateCircle = true;
    public float pulseSpeed = 4.5f;
    public float minAlpha = 0.4f;
    public float maxAlpha = 0.7f;

    [Header("Target")]
    public LayerMask targetLayerMask = 0;
    public GameObject currentTarget;
    
    private readonly Collider[] _colliders = new Collider[5];
    private Material _materialInstance;

    private void Start()
    {
        targetLayerMask = LayerMask.GetMask("Default");
        
        if (circleMaterial)
        {
            _materialInstance = new Material(circleMaterial);
            _materialInstance.SetColor(Color1, circleColor);
            _materialInstance.SetFloat(Radius, circleRadius);
        }
        
        if (!circleQuad)
        {
            CreateCircleQuad();
        }
    }

    [UsedImplicitly]
    private void OnSwitchTarget(InputValue value)
    {
        var size = Physics.OverlapSphereNonAlloc(transform.position, 25f, _colliders, targetLayerMask);
        size = Math.Min(size, _colliders.Length);
        
        for (int i = 0; i < size; i++)
        {
            var dirToTarget = (_colliders[i].transform.position - transform.position).normalized;
            var playerForward = transform.forward;
            var dotProduct = Vector3.Dot(playerForward, dirToTarget);
            var isInFront = dotProduct > 0.5f;
            if (!isInFront) continue;
            currentTarget = _colliders[i].gameObject;
            break;
        }

        if (currentTarget)
        {
            circleQuad.SetActive(true);
        }
    }
    
    void Update()
    {
        if (!currentTarget || !_materialInstance) return;
        Vector3 targetPos = currentTarget.transform.position;
        _materialInstance.SetVector(TargetPosition, new Vector4(targetPos.x, targetPos.y, targetPos.z, 0));
        circleQuad.transform.position = new Vector3(targetPos.x, targetPos.y + 0.01f, targetPos.z);
        if (!animateCircle) return;
        var alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
        _materialInstance.SetFloat(Alpha, alpha);
    }

    private void CreateCircleQuad()
    {
        circleQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        circleQuad.name = "TargetCircle";
        DestroyImmediate(circleQuad.GetComponent<Collider>());
        circleQuad.transform.rotation = Quaternion.Euler(90, 0, 0);
        float scale = circleRadius * 2.5f;
        circleQuad.transform.localScale = new Vector3(scale, scale, 1);
        if (_materialInstance)
        {
            circleQuad.GetComponent<Renderer>().material = _materialInstance;
        }
    }

    private void OnDestroy()
    {
        if (_materialInstance)
        {
            DestroyImmediate(_materialInstance);
        }
    }
}
