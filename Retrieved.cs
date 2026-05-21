public class RetrievedMaterial {
    public string name {get; init;} = "Unknown";
    public float[] diffuse_color {get; init;} = {1.0f,1.0f,1.0f,1.0f};

}

//Collision
public class RetrievedCollision {
    public string type {get; init;} = "nocollision";
    public float[] location {get; init;} = {0.0f,0.0f,0.0f};
    public float[] scale {get; init;} = {0.0f,0.0f,0.0f};
    public float[] rotation {get; init;} = {0.0f,0.0f,0.0f};


}

public class RetrievedMesh{
    //Collision
    public RetrievedCollision collision {get; init;} = new RetrievedCollision();

    public RetrievedMaterial[] materials {get; init;} = new RetrievedMaterial[0];
    public int nvertex {get; set;}

    public float[] vertexdata {get; init;} = new float[0];
    public float[]  weightdata {get; init;}= new float[0];
    public float[] normaldata {get; init;} = new float[0];
    public float[] uvs {get; init;} = new float[0];
    public int[] nindex {get; init;}=new int[0];

    public int[][] indexdata {get; init;}= new int[0][];

}

public class RetrievedOrientation{
        public float[] axis {get; init;}= {0.0f,1.0f,0.0f};
        public float angle {get; set;}

    }


public class RetrievedActor{
        public string id {get; init;}="Unknown";
        public string sm {get; init;}="Unknown";
        //Collision
        public string collision {get; init;}="Unknown";
        public bool enabled {get; init;}=false;


        public float[] position {get;init;}={0.0f,0.0f,0.0f};


        public float[] scale {get; init;}={1.0f,1.0f,1.0f};

        public RetrievedOrientation orientation {get; init;}=new RetrievedOrientation();

        
    }

public class RetrievedMeshMeta {
        public string file {get; init;}="mesh.json";
        public string id{get; init;}="Unknown";
 

    }

public class RetrievedLevel{
            public RetrievedMeshMeta[] mesh_list {get; init;}= new RetrievedMeshMeta[0];

            public RetrievedActor[] actor_list {get; init;} = new RetrievedActor[0];   

            public float[] playerstartposition{get; init;}={0.0f,0.0f,0.0f};
            public float playerstartrotationangle{get;init;}
            public float[] playerstartrotationaxis{get;init;}={0.0f,1.0f,0.0f};


}
