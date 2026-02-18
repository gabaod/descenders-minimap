Shader "Custom/MinimapPostProcess" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Exposure ("Exposure", Float) = 1.0
        _Gamma ("Gamma", Float) = 1.0
    }
    SubShader {
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            float _Exposure;
            float _Gamma;
            
            float4 frag(v2f_img i) : COLOR {
                float4 col = tex2D(_MainTex, i.uv);
                
                // Apply exposure (brightness multiplier)
                col.rgb *= _Exposure;
                
                // Apply gamma correction (brightens dark areas more than bright areas)
                col.rgb = pow(col.rgb, 1.0 / _Gamma);
                
                // Clamp to prevent over-bright
                col.rgb = saturate(col.rgb);
                
                return col;
            }
            ENDCG
        }
    }
}
