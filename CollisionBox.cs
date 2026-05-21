using OpenTK.Mathematics;

class CollisionBox : CollisionGeometry {

    public class Face {
	    public int[] corners {get; set;}
	    public Vector3 normal {get; set;}
	    public Face(int[] cs,Vector3 n)
	    {
		corners=cs;
		normal=n;
	    }

    }

    public Vector3[] corners {get;  init;} = new Vector3[8];
    public Tuple<int,int>[] edges {get; init;} = new Tuple<int,int>[12];
    public Face[] faces {get; init;} =  new Face[6];

    public CollisionBox() : base() {
	    
	    SetEdgesAndFaces();
	    UpdateBoxElements();

	    
    
    }
    public CollisionBox(Vector3 center, Vector3 dim) : base(center,dim) {
	    SetEdgesAndFaces();
	    UpdateBoxElements();
    
    }
    
    private void SetEdgesAndFaces()
    {
	//Edges
	edges[0]=Tuple.Create(0,1);
	edges[1]=Tuple.Create(0,2);
	edges[2]=Tuple.Create(0,4);
	edges[3]=Tuple.Create(1,3);
	edges[4]=Tuple.Create(1,5);
	edges[5]=Tuple.Create(2,3);
	edges[6]=Tuple.Create(2,6);
	edges[7]=Tuple.Create(3,7);
	edges[8]=Tuple.Create(4,5);
	edges[9]=Tuple.Create(4,6);
	edges[10]=Tuple.Create(5,7);
	edges[11]=Tuple.Create(6,7);
	
	//Faces
	faces[0]=new Face(new int[] {0,1,3,2},parameters.uZ);
	faces[1]=new Face(new int[] {0,4,5,1},parameters.uX);
	faces[2]=new Face(new int[] {0,2,6,4},parameters.uY);
	faces[3]=new Face(new int[] {4,6,7,5},-1*parameters.uZ);
	faces[4]=new Face(new int[] {2,3,7,6},-1*parameters.uX);
	faces[5]=new Face(new int[] {1,5,7,3},-1*parameters.uY);


    }
    public override void Transform(in Matrix4 Model)
    {
	    base.Transform(Model);
	    UpdateBoxElements();
    }

    public void UpdateBoxElements()
    {
        Vector3 dim=parameters.dimensions;
        Vector3 center = parameters.center;
	// Displacements
	Vector3 dimX=dim.X*parameters.uX;
	Vector3 dimY=dim.Y*parameters.uY;
	Vector3 dimZ=dim.Z*parameters.uZ;
	
	// Corners
        corners[0]=new Vector3(center+dimX+dimY+dimZ);
        corners[1]=new Vector3(center+dimX-dimY+dimZ);
        corners[2]=new Vector3(center-dimX+dimY+dimZ);
        corners[3]=new Vector3(center-dimX-dimY+dimZ);
        corners[4]=new Vector3(center+dimX+dimY-dimZ);
        corners[5]=new Vector3(center+dimX-dimY-dimZ);
        corners[6]=new Vector3(center-dimX+dimY-dimZ);
        corners[7]=new Vector3(center-dimX-dimY-dimZ);

	// Faces
	faces[0].normal=parameters.uZ;
	faces[1].normal=parameters.uX;
	faces[2].normal=parameters.uY;
	faces[3].normal=-1*parameters.uZ;
	faces[4].normal=-1*parameters.uX;
	faces[5].normal=-1*parameters.uY;
	

    }
}
