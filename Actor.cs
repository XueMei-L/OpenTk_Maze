using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Optional;
using Optional.Unsafe;

public class Actor{

public bool Enabled {get; set;}


public string StaticMeshId {get; set;}

public Matrix4 Model=new Matrix4();
public Matrix4 NormalTransform = new Matrix4();


private Matrix4 _backModel=new Matrix4();
private Matrix4 _backNormalTransform=new Matrix4();


// Collision
public string CollisionMeshId {get; set;}
public Matrix4 CollisionModel=new Matrix4();
public Matrix4 StartCollisionModel = new Matrix4();
public Option<CollisionGeometry> CollisionGeometry=Option.None<CollisionGeometry>();

public Actor(){
        this.Enabled=false;
        this.StaticMeshId="";
        //Collision
        this.CollisionMeshId="";
    }

public void SetTransform(Vector3 positionVector, Vector3 axisVector, float angle, Vector3 scale){

        Model=Matrix4.CreateScale(scale)*Matrix4.CreateFromAxisAngle(axisVector,angle * (float) (Math.PI/180.0f) )*Matrix4.CreateTranslation(positionVector);
	UpdateNormalTransform();

        
}

// Nuevo m’etodo de la clas actor
public void UpdateNormalTransform(){
        NormalTransform=Matrix4.Transpose(Matrix4.Invert(Model));
}

public void SaveModel()
{
        _backModel=Model;
        _backNormalTransform=NormalTransform;
}

public void RestoreModel()
{
        Model=_backModel;
        NormalTransform=_backNormalTransform;
}



public void Scale(Vector3 scale)
{
        Model=Matrix4.CreateScale(scale)*Model;
}

//Collisions

public void SetCollisionGeometry(Dictionary<string,Mesh> AssetCollection)
{
        if(! AssetCollection.ContainsKey(CollisionMeshId))
                return;
        
        
        
        Mesh CollisionMesh = AssetCollection[CollisionMeshId];
        
        Vector3 defaultCenter=new Vector3(0.0f,0.0f,0.0f);
        Vector3 defaultDimensions=new Vector3(1.0f,1.0f,1.0f);
        // Initialize
        switch(CollisionMesh.collisionData.type)
        {
                case "nocollision":
                        CollisionGeometry=Option.None<CollisionGeometry>();
                        break;
                case "box":
                        CollisionGeometry=Option.Some<CollisionGeometry>(new CollisionBox(defaultCenter,defaultDimensions));
                        break;
                case "ellipsoid":
                        CollisionGeometry=Option.Some<CollisionGeometry>(new CollisionEllipsoid(defaultCenter,defaultDimensions));
                        break;
                default:
                        CollisionGeometry=Option.Some(new CollisionGeometry(defaultCenter,defaultDimensions));
                        break;        

        }

        if (CollisionGeometry.HasValue)
        {
                CollisionGeometry geometry = CollisionGeometry.ValueOrFailure("Unexpected access to non assigned collision geometry");
                float[] location = CollisionMesh.collisionData.location;
                float[] rotation = CollisionMesh.collisionData.rotation;
                float[] scale = CollisionMesh.collisionData.scale;
                geometry.initialParameters.dimensions = new Vector3(scale[0], scale[1], scale[2]);

                //Matrix4 ModelScale=Matrix4.CreateScale(scale[0],scale[1], scale[2]);
                // Rotation> Its imported in Euler system Pitch, Yaw, Roll
                Matrix4 ModelRotYaw = Matrix4.CreateRotationY(rotation[1]);
                Vector4 uX = Vector4.UnitX * ModelRotYaw;
                Vector4 uZ = Vector4.UnitZ * ModelRotYaw;

                Matrix4 ModelRotPitch = Matrix4.CreateFromAxisAngle(uX.Xyz, rotation[0]);
                Vector4 uY = Vector4.UnitY * ModelRotPitch;
                uZ = Vector4.UnitZ * ModelRotPitch;

                Matrix4 ModelRotRoll = Matrix4.CreateFromAxisAngle(uZ.Xyz, rotation[2]);
                uX = uX * ModelRotRoll;
                uY = uY * ModelRotRoll;
                geometry.initialParameters.uX = uX.Xyz;
                geometry.initialParameters.uY = uY.Xyz;
                geometry.initialParameters.uZ = uZ.Xyz;

                geometry.initialParameters.center = new Vector3(location[0], location[1], location[2]);

                StartCollisionModel = Matrix4.CreateScale(scale[0], scale[1], scale[2]) * ModelRotYaw * ModelRotPitch * ModelRotRoll * Matrix4.CreateTranslation(location[0], location[1], location[2]);
                CollisionModel = StartCollisionModel;

        }
}

        public void UpdateCollisionModel()
        {
                CollisionGeometry.ValueOrFailure("Unexpected empty CollisionGeometry").Transform(Model);
                CollisionModel = StartCollisionModel * Model;
        }

}
