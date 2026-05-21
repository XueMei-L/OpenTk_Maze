#version 330 core

layout(location=0) in vec3 aPosition;
layout(location=1) in float aWeight;
layout(location=2) in vec2 aTexCoord;
layout(location=3) in vec3 aNormal;

uniform mat4 view;
uniform mat4 model;
uniform mat4 projection;

uniform mat4 normalTransformMatrix;

out vec2 TexCoord;
out vec4 VertexNormal; 

void main(){
    vec4 position=vec4(aPosition,1.0f) * model * view * projection;
    VertexNormal=vec4(aNormal,0.0)*normalTransformMatrix;
    TexCoord=aTexCoord;
    gl_Position=position;
}

