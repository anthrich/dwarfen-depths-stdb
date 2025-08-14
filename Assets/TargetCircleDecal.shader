Shader "Custom/TargetCircleDecal"
{
    Properties
    {
        _Color ("Circle Color", Color) = (1, 0, 0, 1)
        _Radius ("Circle Radius", Float) = 2.0
        _LineThickness ("Line Thickness (pixels)", Float) = 2.0
        _TargetPosition ("Target Position", Vector) = (0, 0, 0, 0)
        _Alpha ("Alpha", Range(0, 1)) = 1.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };
            
            fixed4 _Color;
            float _Radius;
            float _LineThickness;
            float4 _TargetPosition;
            float _Alpha;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Calculate distance from fragment to target position (XZ plane)
                float2 worldPosXZ = i.worldPos.xz;
                float2 targetPosXZ = _TargetPosition.xz;
                float distance = length(worldPosXZ - targetPosXZ);
                
                // Calculate pixel size in world units
                float pixelSize = length(float2(ddx(worldPosXZ.x), ddy(worldPosXZ.x)));
                float worldLineThickness = _LineThickness * pixelSize;
                
                // Create ring with pixel-based thickness
                float outerRadius = _Radius;
                float innerRadius = _Radius - worldLineThickness;
                float ring = smoothstep(innerRadius, innerRadius, distance) - 
                           smoothstep(outerRadius, outerRadius, distance);
                
                // Create dotted pattern
                float2 direction = normalize(worldPosXZ - targetPosXZ);
                float angle = atan2(direction.y, direction.x);
                float normalizedAngle = (angle + 3.14159) / (2.0 * 3.14159); // 0 to 1
                
                float dotCount = 24.0; // Number of dots around the ring
                float dotPattern = sin(normalizedAngle * dotCount * 3.14159);
                float dots = smoothstep(0.2, 0.8, dotPattern); // Controls dot vs gap ratio
                
                fixed4 col = _Color;
                col.a *= ring * dots * _Alpha;
                
                return col;
            }
            ENDCG
        }
    }
}