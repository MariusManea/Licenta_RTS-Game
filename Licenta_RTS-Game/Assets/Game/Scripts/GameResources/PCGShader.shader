Shader "PCGShader" {
    Properties{
       _MainTex("textureImage", 2D) = "white" {}
       _Size("size", int) = 1000
    }
    SubShader{
        Pass {
            GLSLPROGRAM

            uniform sampler2D _MainTex;
            uniform int _Size;

            #ifdef VERTEX
            out vec4 textureCoordinates;
            void main()
            {
                textureCoordinates = gl_MultiTexCoord0;
                gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
            }

            #endif

            #ifdef FRAGMENT
            in vec4 textureCoordinates;
            void main()
            {
                
                gl_FragColor =
                    texture2D(_MainTex, vec2(textureCoordinates));
            }

            #endif

            ENDGLSL
        }
    }
}