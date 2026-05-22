#version 450 

layout(location=0) in vec3 aPosition;
layout(location=1) in float aWeight;
layout(location=2) in vec2 aTexCoord;
layout(location=3) in vec3 aNormal;

uniform mat4 view;
uniform mat4 model;
uniform mat4 projection;

uniform mat4 normalTransformMatrix;
uniform mat4 lightSpaceMatrix;

out vec4 lightSpacePosition;
out vec2 TexCoord;
out vec4 VertexNormal; 

void main(){

    // Coordinates in Light Space
    vec3 worldPosition=vec3(vec4(aPosition,1.0f) * model);
    lightSpacePosition=vec4(worldPosition,1.0f)*lightSpaceMatrix;

    // Coordinates in Camera Space and the projection
    vec4 position=vec4(worldPosition,1.0f) * view * projection;
    VertexNormal=vec4(aNormal,0.0f)*normalTransformMatrix;
    TexCoord=aTexCoord;
    gl_Position=position;

}