using OpenTK.Mathematics;

public class CollisionGeometry {

    public struct CollisionParameters
    {
        public Vector3 uX { get; set; }
        public Vector3 uY { get; set; }
        public Vector3 uZ { get; set; }
        public Vector3 dimensions { get; set; }
        public Vector3 center { get; set; }

    }

    public CollisionParameters initialParameters;
    public CollisionParameters parameters;

    public CollisionGeometry()
    {
        initialParameters.uX = new Vector3(1.0f, 0.0f, 0.0f);
        initialParameters.uY = new Vector3(0.0f, 1.0f, 0.0f);
        initialParameters.uZ = new Vector3(0.0f, 0.0f, 1.0f);
        initialParameters.dimensions = new Vector3(1.0f, 1.0f, 1.0f);
        initialParameters.center = new Vector3(0.0f, 0.0f, 0.0f);
        parameters = initialParameters; 

    }

    public  CollisionGeometry(Vector3 center, Vector3 dim)
    {
        initialParameters.uX=new Vector3(1.0f,0.0f,0.0f);
        initialParameters.uY=new Vector3(0.0f,1.0f,0.0f);
        initialParameters.uZ=new Vector3(0.0f,0.0f,1.0f);
        initialParameters.dimensions=new Vector3(dim);
        initialParameters.center=new Vector3(center);
        parameters = initialParameters;
    }

    
    public virtual void Transform(in Matrix4 Model)
    {
        Vector3 Scale = Model.ExtractScale();
        //Console.WriteLine($"Scale {Scale}");
        parameters.dimensions = Vector3.Multiply(initialParameters.dimensions, Scale);
        //Console.WriteLine($"Dimensions {parameters.dimensions}");
        Matrix4 Rotation = Model.ClearScale().ClearTranslation();
        parameters.uX = (new Vector4(initialParameters.uX) * Rotation).Xyz;
        parameters.uY = (new Vector4(initialParameters.uY) * Rotation).Xyz;
        parameters.uZ = (new Vector4(initialParameters.uZ) * Rotation).Xyz;
        Vector3 Translation = Model.ExtractTranslation();
        parameters.center = Vector3.Multiply(initialParameters.center,Scale) + Translation;
    }

    public override string ToString()
    {
    return String.Format("Center:{0}\nDimensions:{1}\nuX:{2}\nuY:{3}\nuZ:{4}",parameters.center, parameters.dimensions, parameters.uX, parameters.uY,parameters.uZ);
        
    }
}
