#version 450 

in vec2 TexCoord;
in vec4 VertexNormal; 
in vec3 worldPosition;
in vec4 lightSpacePosition;

layout(location=0) out vec4 OutFragColor;
layout(binding=0) uniform sampler2D uTexture;
layout(binding=1) uniform sampler2D uShadowMap;

uniform int bTex;
uniform vec3 diffuse_color;
uniform vec3 AmbientLight;
uniform vec3 DirLight0Diffuse;
uniform vec3 DirLight0Direction;

float ShadowCalculation(vec4 lightSpacePosition)
{
// Transform to clip-space normalized device coordinates [-1.0,1.0]

// because fragPosLightSpace is not divided automatically as

// gl_Position

vec3 projCoords = lightSpacePosition.xyz / lightSpacePosition.w;


// Now, to produce the depth in [0,1]:

projCoords=0.5f*projCoords+0.5;

float closestDepth = texture(uShadowMap, projCoords.xy).r;

float currentDepth = projCoords.z; 

float bias=0.002f;

float shadow = (currentDepth-bias)  > closestDepth  ? 1.0 : 0.0; 

return shadow;

}

void main()
{
    vec4 tSample=texture(uTexture,TexCoord);

    float cl=max(dot(DirLight0Direction,VertexNormal.xyz),0);

    vec3 color= bTex==1 ? tSample.rgb : diffuse_color;

    float shadow= ShadowCalculation(lightSpacePosition);

    vec4 newcolor=vec4(AmbientLight*color+(1.0f-shadow)*cl*DirLight0Diffuse.rgb*color,1.0);

    OutFragColor=newcolor;

}   