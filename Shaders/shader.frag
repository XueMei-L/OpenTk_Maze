#version 330 core 


in vec2 TexCoord;
in vec4 VertexNormal; 

out vec4 OutFragColor;

uniform vec3 diffuse_color;
uniform vec3 AmbientLight;
uniform vec3 DirLight0Diffuse;
uniform vec3 DirLight0Direction;

void main()
{
    float cl=max(dot(DirLight0Direction,VertexNormal.xyz),0);

    vec4 newcolor=vec4(AmbientLight*diffuse_color+cl*DirLight0Diffuse.rgb*diffuse_color.rgb,1.0);
    OutFragColor=newcolor;
}   


