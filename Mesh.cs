using LearnOpenTK.Common;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using Optional;

public class Mesh
{

    public class CollisionMeshInfo {
        public string type {get; set; }="Unknown";
        public float[] location {get; set; }= {0.0f,0.0f,0.0f};
        public float[] scale {get;  set; } = {1.0f,1.0f,1.0f};
        public float[] rotation {get;  set; } = {0.0f,0.0f,0.0f};
    }

    public float[] vertexData { get; private set; } = new float[0];
    public int[] indexData { get; private set; } = new int[0];

    public int[] slotData { get; private set; } = new int[0];

    public RetrievedMaterial[] matData { get; private set; } = new RetrievedMaterial[0];

    //Collision
    private RetrievedCollision _collisionData  = new RetrievedCollision();
    public CollisionMeshInfo collisionData {get; private set; }= new CollisionMeshInfo();


    private RetrievedMesh _retrievedMesh = new RetrievedMesh();

    public int vertexBuffer { get; set; }
    public int indexBuffer { get; set; }

    public int vertexArray { get; set; }


    public Mesh()
    {



    }

    public Mesh(RetrievedMesh retMesh)
    : this()
    {

        _retrievedMesh = retMesh;

    }

 public void Make()
    {


       var mesh = _retrievedMesh;

       collisionData.type=_retrievedMesh.collision.type;
       _retrievedMesh.collision.location.CopyTo(collisionData.location,0);
       _retrievedMesh.collision.scale.CopyTo(collisionData.scale,0);
       _retrievedMesh.collision.rotation.CopyTo(collisionData.rotation,0);

       matData = new RetrievedMaterial[mesh.materials.Length - 1];

       for (int i = 1; i < mesh.materials.Length; i++)
        {
            matData[i - 1] = mesh.materials[i]; // The first one is a default

        }
        if (mesh.vertexdata is null || mesh.weightdata is null || mesh.indexdata is null)
            throw new Exception("Error: mesh data is wrong or empty");
        int nvertices = mesh.vertexdata.Length;
        int nweight = mesh.weightdata.Length;

if ((nvertices / 3) != nweight)
            throw new Exception("Number of vertex weights is different of number of vertices");


        slotData = new int[mesh.materials.Length - 1];

        int nindex = 0;
        for (int i = 0; i < (mesh.materials.Length - 1); i++)
        {
            slotData[i] = nindex;
            nindex += mesh.indexdata[i].Length;

        }

        indexData = new int[nindex];
        int count = 0;
        for (int i = 0; i < (mesh.materials.Length - 1); i++)
        {
            for (int j = 0; j < mesh.indexdata[i].Length; j++)
                indexData[count++] = mesh.indexdata[i][j];
        }

        // Interleaving de los datos de vertices
         int nvalues = 9; // 3 components per vers, 1 per weight, 2 uvs, 3 normals
         vertexData = new float[indexData.Length*nvalues];
        for (int i = 0, l=0; i<indexData.Length;i++,l=l+nvalues)
        {
            vertexData[l]=mesh.vertexdata[3*indexData[i]];
            vertexData[l+1]=mesh.vertexdata[3*indexData[i]+1];
            vertexData[l+2]=mesh.vertexdata[3*indexData[i]+2];
            vertexData[l+3]=mesh.weightdata[indexData[i]];
            vertexData[l+4]=mesh.uvs[2*i];
            vertexData[l+5]=mesh.uvs[2*i+1];
            vertexData[l+6]=mesh.normaldata[3*indexData[i]];
            vertexData[l+7]=mesh.normaldata[3*indexData[i]+1];
            vertexData[l+8]=mesh.normaldata[3*indexData[i]+2];


        }


    }


 public void Draw(Shader _shader, Option<Vector3> dcolor)
    {
        Vector3 vcolor;

        if (indexData is not null && slotData is not null)
            {
                for (int i = 0; i < slotData.Length; i++)
                {
                    vcolor = dcolor.ValueOr(new Vector3(
                        matData[i].diffuse_color[0],
                        matData[i].diffuse_color[1],
                        matData[i].diffuse_color[2]));

                   _shader.SetVector3("diffuse_color", vcolor);
                    int nelements = 0;
                    if (i == (slotData.Length - 1))
                        nelements = indexData.Length - slotData[i];
		     else
                        nelements = slotData[i + 1] - slotData[i];


			//GL.DrawElements(PrimitiveType.Triangles, nelements, DrawElementsType.UnsignedInt, ref indexData[slotData[i]]);

			GL.DrawArrays(PrimitiveType.Triangles,slotData[i],nelements);


                }
            }
    }

}
