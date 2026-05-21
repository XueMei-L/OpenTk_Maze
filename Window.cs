using System;
using System.Text.Json;
using LearnOpenTK.Common;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Optional;
using Optional.Unsafe;


public class Window : GameWindow
{
public RetrievedMaterial[] ?matData;

public Dictionary<string,Mesh> AssetCollection {get; set;}

public string levelFilePath {get; set;}

private Level _level=new Level();

private bool bDrawCollision=false;
private Vector3 _pawnPosition = Vector3.Zero;
private Vector3 _pawnScale = new Vector3(0.2f);
private float _pawnYaw = 0.0f;
private float _pawnCameraDistance = 1.0f;
private float _pawnCameraHeight = 0.7f;
// inicialmente esta frente de la entrada del laberinto
private float _pawnModelYawOffset = 270.0f; // visual rotation offset for the pawn model (90 + 180)

// 15x15 的迷宫矩阵映射 (根据你的迷宫图片完全一比一像素化还原)
// 1 = 墙壁 Cube，0 = 可以行走的通道
private int[,] mazeGrid = new int[15, 15]
{
    {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}, // 顶层外墙
    {1,0,0,0,0,0,1,0,0,0,0,0,0,0,1}, 
    {1,0,1,1,1,0,1,0,1,1,1,1,1,0,1}, 
    {1,0,1,0,0,0,1,0,1,0,0,0,1,0,1}, 
    {1,0,1,0,1,1,1,0,1,0,1,0,1,1,1}, 
    {0,0,1,0,0,0,0,0,1,0,1,0,0,0,0}, // 行索引5：左侧入口 A 和右侧出口 B
    {1,0,1,1,1,1,1,0,1,0,1,1,1,1,1}, 
    {1,0,0,0,1,0,0,0,1,0,0,0,0,0,1}, 
    {1,1,1,0,1,0,1,0,1,1,1,1,1,0,1}, 
    {1,0,0,0,1,0,1,0,0,0,0,0,1,0,1}, 
    {1,0,1,1,1,0,1,1,1,1,1,0,1,0,1}, 
    {1,0,1,0,0,0,1,0,1,0,0,0,1,0,1}, 
    {1,0,1,1,1,1,1,0,1,0,1,1,1,0,1}, 
    {1,0,0,0,0,0,0,0,1,0,0,0,0,0,1}, 
    {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}  // 底层外墙
};

    public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
    : base(gameWindowSettings,nativeWindowSettings)
    {
                levelFilePath="assets/level.json";
        AssetCollection=new Dictionary<string,Mesh>();

        // Monitor and resolution
        MonitorInfo minfo = Monitors.GetMonitorFromWindow(this);
        _horizontalResolution=minfo.HorizontalResolution;
        _verticalResolution=minfo.VerticalResolution;

        //_camera=new Camera(Vector3.UnitZ*3,Size.X / (float)Size.Y); 
        Console.WriteLine($"Hor {_horizontalResolution} Vert {_verticalResolution}");
        _camera=new Camera(new Vector3(0.0f, 15.0f, 35.0f), _horizontalResolution / (float)_verticalResolution);
        _camera.Yaw = -90.0f;
        _camera.Pitch = -20.0f;

        _controller=new Controller(_horizontalResolution,_verticalResolution);
        _controller.Speed=5.0f;
  
    }

float cubeSize = 2.0f; // 每个方块的基准大小

private Vector3 MazeOrigin => new Vector3(-cubeSize * 7.0f, 0.0f, -cubeSize * 7.0f);

private Actor CreateMazeCubeWall(Vector3 position)
{
    Actor wall = new Actor();
    wall.Enabled = true;
    wall.StaticMeshId = "cube";
    wall.CollisionMeshId = "col_cube";
    wall.SetTransform(position, new Vector3(0.0f, 1.0f, 0.0f), 0.0f, new Vector3(1.0f, 1.0f, 1.0f));
    wall.SetCollisionGeometry(AssetCollection);
    wall.UpdateCollisionModel();
    return wall;
}

private void BuildMazeFromGrid()
{
    if (!AssetCollection.ContainsKey("cube") || !AssetCollection.ContainsKey("col_cube"))
        throw new Exception("Maze requires cube and col_cube assets loaded in level.json.");

    for (int row = 0; row < mazeGrid.GetLength(0); row++)
    {
        for (int col = 0; col < mazeGrid.GetLength(1); col++)
        {
            if (mazeGrid[row, col] != 1)
                continue;

            string wallId = $"wall_{row}_{col}";
            Vector3 position = new Vector3(
                MazeOrigin.X + col * cubeSize,
                0.0f,
                MazeOrigin.Z + row * cubeSize);
            _level.ActorCollection.Add(wallId, CreateMazeCubeWall(position));
        }
    }

        if (_level.ActorCollection.TryGetValue("apawn", out Actor? pawn))
    {
        // Spawn outside the left entrance at row 5, facing into the maze
        _pawnPosition = new Vector3(MazeOrigin.X - 3.0f * cubeSize, 0.0f, MazeOrigin.Z + 5 * cubeSize);
        _pawnYaw = 0.0f;
            pawn.SetTransform(_pawnPosition, new Vector3(0.0f, 1.0f, 0.0f), _pawnYaw + _pawnModelYawOffset, _pawnScale);
        pawn.UpdateCollisionModel();
    }
}

protected void UpdateGameState(float deltaTime){
    Vector3 movement=_controller.GetMovement();
    Angles2D deltaAngles=_controller.GetArmOrientation();

    if (_level.ActorCollection.TryGetValue("apawn", out Actor? pawn))
    {
        _pawnYaw += (float)deltaAngles.Yaw;
        _pawnYaw = MathF.IEEERemainder(_pawnYaw, 360.0f);

        float yawRad = MathHelper.DegreesToRadians(_pawnYaw);
        Vector3 pawnForward = new Vector3(MathF.Cos(yawRad), 0.0f, MathF.Sin(yawRad));
        Vector3 pawnRight = new Vector3(MathF.Cos(yawRad + MathHelper.PiOver2), 0.0f, MathF.Sin(yawRad + MathHelper.PiOver2));

        Vector3 previousPawnPosition = _pawnPosition;
        float previousPawnYaw = _pawnYaw;

        _pawnPosition += pawnForward * movement.X;
        _pawnPosition += pawnRight * movement.Y;

        pawn.SaveModel();
        pawn.SetTransform(_pawnPosition, new Vector3(0.0f, 1.0f, 0.0f), _pawnYaw + _pawnModelYawOffset, _pawnScale);
        pawn.UpdateCollisionModel();

        foreach(string actorid in _level.ActorCollection.Keys)
        {
            if(actorid == "apawn")
                continue;

            Actor actor = _level.ActorCollection[actorid];
            if(!actor.Enabled)
                continue;

            if (Collision.CheckEB(pawn, actor))
            {
                pawn.RestoreModel();
                pawn.UpdateCollisionModel();
                _pawnPosition = previousPawnPosition;
                _pawnYaw = previousPawnYaw;
                break;
            }
        }

        // Position the camera behind-and-above the pawn (local back offset), and orient it to look at the pawn
        Vector3 camPos = _pawnPosition - pawnForward * _pawnCameraDistance + new Vector3(0.0f, _pawnCameraHeight, 0.0f);
        _camera.Position = camPos;

        Vector3 toPawn = _pawnPosition - camPos;
        float distXZ = MathF.Sqrt(toPawn.X * toPawn.X + toPawn.Z * toPawn.Z);
        float pitchDeg = MathHelper.RadiansToDegrees(MathF.Atan2(toPawn.Y, distXZ));
        float yawDeg = MathHelper.RadiansToDegrees(MathF.Atan2(toPawn.Z, toPawn.X));

        _camera.Yaw = yawDeg;
        _camera.Pitch = pitchDeg;
    }
}

protected void InitializeLevel()
{
        _level=new Level(levelFilePath);
        _level.LoadLevel(AssetCollection);
        BuildMazeFromGrid();
}


protected override void OnLoad()
 {
    base.OnLoad();
    InitializeLevel();

    GL.ClearColor(0.2f,0.2f,0.2f,1.0f); // Color de borrado
    GL.Enable(EnableCap.CullFace);  // Elimina las caras traseras 
    GL.Enable(EnableCap.DepthTest);  

    _shader=new Shader("Shaders/shader.vert","Shaders/shader.frag");
    _shader.Use();

    List<string> activeMeshes = _level.GetActiveMeshes(AssetCollection);

foreach(string meshid in activeMeshes){
    if(AssetCollection[meshid] is null )
               throw new Exception("Mesh with empty data"); 
    if(AssetCollection[meshid].vertexData is null )
               throw new Exception("Mesh with empty data"); 
           
    int _vertexBuffer=GL.GenBuffer();
    AssetCollection[meshid].vertexBuffer=_vertexBuffer;
    GL.BindBuffer(BufferTarget.ArrayBuffer,_vertexBuffer);
    GL.BufferData(BufferTarget.ArrayBuffer,
        AssetCollection[meshid].vertexData.Length*sizeof(float),
        AssetCollection[meshid].vertexData,
        BufferUsageHint.StaticDraw);
        
    int _vertexArray=GL.GenVertexArray();
    AssetCollection[meshid].vertexArray=_vertexArray;
    GL.BindVertexArray(_vertexArray);

     int _indexBuffer=GL.GenBuffer();
    AssetCollection[meshid].indexBuffer=_indexBuffer;
    GL.BindBuffer(BufferTarget.ElementArrayBuffer,_indexBuffer);
    GL.BufferData(BufferTarget.ElementArrayBuffer,
    AssetCollection[meshid].indexData.Length*sizeof(int),
    AssetCollection[meshid].indexData,BufferUsageHint.StaticDraw);

    //
    // Paso 14. Creamos el VAO para el atributo aPosition del shader
    var posLocation = _shader.GetAttribLocation("aPosition");
    if(posLocation!=(-1))
    {
    GL.EnableVertexAttribArray(posLocation);
    GL.VertexAttribPointer(posLocation,3,VertexAttribPointerType.Float,false,9*sizeof(float),0);
    }

    // Paso 15. Creamos el VAO para el atributo aWeight del shader
    var weightLocation = _shader.GetAttribLocation("aWeight");
    if(weightLocation!=(-1))
    {
    GL.EnableVertexAttribArray(weightLocation);
    GL.VertexAttribPointer(weightLocation,1,VertexAttribPointerType.Float,false,9*sizeof(float),3*sizeof(float));
    }

    var uvLocation = _shader.GetAttribLocation("aTexCoord");
    if(uvLocation!=(-1))
    {

    GL.EnableVertexAttribArray(uvLocation);
    GL.VertexAttribPointer(uvLocation,2,VertexAttribPointerType.Float,false,9*sizeof(float),4*sizeof(float));
    }

    var normalLocation = _shader.GetAttribLocation("aNormal");
    if(normalLocation!=(-1))
    {
    GL.EnableVertexAttribArray(normalLocation);
    GL.VertexAttribPointer(normalLocation,3,VertexAttribPointerType.Float,false,9*sizeof(float),6*sizeof(float));
    }

    // Unbind VBO, EBO and VAO
            GL.BindBuffer(BufferTarget.ElementArrayBuffer,0);
            GL.BindBuffer(BufferTarget.ArrayBuffer,0);
            GL.BindVertexArray(0);

        }


}

float time=0.0f;
 protected override void OnUpdateFrame(FrameEventArgs e)
{
   time+=(float)e.Time;
   
    base.OnUpdateFrame(e);
    if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            // If it is, close the window.
            Close();
        }
    if (KeyboardState.IsKeyDown(Keys.C) && time>0.5f)
    {
        bDrawCollision=!bDrawCollision;
	time=0.0f;
    }
	//Matrix4.CreateFromAxisAngle(_Axis,_RotAngle,out _Model); 
    //_RotAngle+=_RotSpeed*(float)e.Time;
    //if(_RotAngle>=MathHelper.TwoPi)
    // Controller Update
     _controller.UpdateState(this.KeyboardState,this.MouseState,e);

    // Update GameState
    UpdateGameState((float)e.Time);


}
	
	
protected override void OnRenderFrame(FrameEventArgs args)
{
    // Sin _shader or _mesh no podemos hacer nada
    if(_shader==null)
    {
        return;
    }
    base.OnRenderFrame(args);
    GL.Enable(EnableCap.DepthTest);  
    GL.Enable(EnableCap.StencilTest);  

    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit );

List<string> activeMeshes = _level.GetActiveMeshes(AssetCollection);

    foreach(string actorid in _level.ActorCollection.Keys){
        Actor actor=_level.ActorCollection[actorid];
        if( !actor.Enabled)
            continue;
        
        //Collisions
        Mesh ?mesh;
        if(bDrawCollision)
        {
            if(! AssetCollection.ContainsKey(actor.CollisionMeshId))
                continue;
            else{
                mesh=AssetCollection[actor.CollisionMeshId];
            }
        }
        else
        {
            if(! AssetCollection.ContainsKey(actor.StaticMeshId))
                continue;
            else
                mesh=AssetCollection[actor.StaticMeshId];

        }

        if(mesh is null)
            throw new Exception("Trying to render an actor without mesh");
        
        // Binding mesh VAO
        GL.BindVertexArray(mesh.vertexArray);
            Matrix4 model;
            if (bDrawCollision)
            {
                model = actor.CollisionModel;
            }
            else
            {
                model = actor.Model;
            }

        _shader.SetMatrix4("model", model);
        _shader.SetMatrix4("view",_camera.GetViewMatrix());
        _shader.SetMatrix4("projection",_camera.GetProjectionMatrix());

       _shader.SetMatrix4("normalTransformMatrix",actor.NormalTransform);
       _shader.SetVector3("AmbientLight",new Vector3(0.1f,0.1f,0.1f));
       _shader.SetVector3("DirLight0Diffuse",new Vector3(0.6f,0.6f,0.6f));
       _shader.SetVector3("DirLight0Direction",Vector3.Normalize(new Vector3(1.0f,1.0f,1.0f)));


        // Paso 20. Lanzamos la orden Draw
        GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);  
        GL.StencilFunc(StencilFunction.Always,1,0xFF);
        GL.StencilMask(0xFF);
 
        mesh.Draw(_shader,Option.None<Vector3>());
    
        GL.StencilFunc(StencilFunction.Notequal,1,0xFF);
        GL.StencilMask(0x00);
    
        actor.SaveModel();
            //actor.Scale(new Vector3(1.02f,1.02f,1.02f));
            //    actor.UpdateCollisionModel();
            model = Matrix4.CreateScale(1.02f, 1.02f, 1.02f) * model;

        _shader.SetMatrix4("model",model);

        mesh.Draw(_shader,Option.Some(new Vector3(0.0f,0.0f,0.0f)));
        //GL.StencilFunc(StencilFunction.Always,1,0xFF);
        //GL.StencilMask(0xFF);
        //actor.RestoreModel();
        //    actor.UpdateCollisionModel();


        GL.BindVertexArray(0);

  } // Loop sobre los actores
        GL.StencilMask(0xFF);
    
// Paso 21. Hacemos el swap del doble buffer.
SwapBuffers();


}

protected override void OnResize(ResizeEventArgs e)
{
    base.OnResize(e);
    GL.Viewport(0,0,Size.X,Size.Y);
}
	
 protected override void OnUnload()
{
	
        GL.BindBuffer(BufferTarget.ArrayBuffer,0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer,0);
        GL.BindVertexArray(0);

        base.OnUnload();
}
    private Shader? _shader ;
    
	private Camera _camera;
    
    private Controller _controller;
    private int _horizontalResolution;
    private int _verticalResolution;

}
