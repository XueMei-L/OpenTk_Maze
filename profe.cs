// using System;
// using System.Text.Json;
// using LearnOpenTK.Common;
// using OpenTK.Mathematics;
// using OpenTK.Graphics.OpenGL4;
// using OpenTK.Windowing.Common;
// using OpenTK.Windowing.Desktop;
// using OpenTK.Windowing.GraphicsLibraryFramework;
// using Optional;
// using Optional.Unsafe;


// public class Window : GameWindow
// {
// public RetrievedMaterial[] ?matData;

// public Dictionary<string,Mesh> AssetCollection {get; set;}

// public string levelFilePath {get; set;}

// private Level _level=new Level();

// private bool bDrawCollision=false;



//     public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
//     : base(gameWindowSettings,nativeWindowSettings)
//     {
//                 levelFilePath="assets/level.json";
//         AssetCollection=new Dictionary<string,Mesh>();

//         // Monitor and resolution
//         MonitorInfo minfo = Monitors.GetMonitorFromWindow(this);
//         _horizontalResolution=minfo.HorizontalResolution;
//         _verticalResolution=minfo.VerticalResolution;
//     //_camera=new Camera(Vector3.UnitZ*3,Size.X / (float)Size.Y); 
//     Console.WriteLine($"Hor {_horizontalResolution} Vert {_verticalResolution}");
//     _camera=new Camera(Vector3.UnitZ*3,_horizontalResolution / (float)_verticalResolution); 


//         _controller=new Controller(_horizontalResolution,_verticalResolution);
//         _controller.Speed=2.0f;

//                

//     }

// protected void UpdateGameState(float deltaTime){
//     
//     // Camera
//     Vector3 movement=_controller.GetMovement();
//    
//     //_camera.Position += _camera.Front*movement.X;
//     //_camera.Position += _camera.Right*movement.Y;
//     //_camera.Position += _camera.Up*movement.Z;

//     Angles2D deltaAngles=_controller.GetArmOrientation();
//     Angles2D cameraAngles = new Angles2D(_camera.Yaw,_camera.Pitch);
//     cameraAngles += deltaAngles;
//     _camera.Yaw = (float)cameraAngles.Yaw;
//     _camera.Pitch = (float)cameraAngles.Pitch;

//     // Buscamos el pawn
//     Actor? pawn;
//     if( _level.ActorCollection.TryGetValue("apawn", out pawn))
//     {
//         Matrix4 pawnModel=pawn.Model;
//         Vector3 translation = (movement.Y,movement.Z,-movement.X);
//     pawn.SaveModel();
//         pawn.Model = pawn.Model*Matrix4.CreateTranslation(translation);

//         //pawn.CollisionModel = pawn.CollisionModel * Matrix4.CreateTranslation(translation);

//         // Check for collisions.
//         //pawn.CollisionGeometry.ValueOrFailure("Pawn without collision geometry").Transform(pawn.Model);
//         pawn.UpdateCollisionModel();

//         foreach(string actorid in _level.ActorCollection.Keys){
//             if(actorid=="apawn")
//                 continue;
//             Actor actor=_level.ActorCollection[actorid];
//             if( !actor.Enabled)
//                 continue;
//             if (Collision.CheckEB(pawn, actor))
//                 {
//         // Restore previous
//             pawn.RestoreModel();
//             pawn.UpdateCollisionModel();
//             translation=new Vector3(0.0f,0.0f,0.0f);


//                 }
//         }
//  

//         

//         Vector3 pawnNewPosition = pawn.Model.ExtractTranslation();


//         
//         _camera.Position= new Vector3(_camera.Position.X,pawnNewPosition.Y+_controller.CameraDistance,pawnNewPosition.Z+2*_controller.CameraDistance);
//         _camera.Position += new Vector3(translation.X,0.0f,translation.Z);
//     } else
//     {
//     
//     _camera.Position += _camera.Front*movement.X;
//     _camera.Position += _camera.Right*movement.Y;
//     _camera.Position += _camera.Up*movement.Z;

//     //TODO: First person collision
//     
//     
//     }

//     
// }

// protected void InitializeLevel()
// {
//         _level=new Level(levelFilePath);
//         _level.LoadLevel(AssetCollection);
// }


// protected override void OnLoad()
//  {
//     base.OnLoad();
//     InitializeLevel();

//     GL.ClearColor(0.2f,0.2f,0.2f,1.0f); // Color de borrado
//     GL.Enable(EnableCap.CullFace);  // Elimina las caras traseras 
//     GL.Enable(EnableCap.DepthTest);  

//     _shader=new Shader("Shaders/shader.vert","Shaders/shader.frag");
//     _shader.Use();

//     List<string> activeMeshes = _level.GetActiveMeshes(AssetCollection);

// foreach(string meshid in activeMeshes){
//     if(AssetCollection[meshid] is null )
//                throw new Exception("Mesh with empty data"); 
//     if(AssetCollection[meshid].vertexData is null )
//                throw new Exception("Mesh with empty data"); 
//            
//     int _vertexBuffer=GL.GenBuffer();
//     AssetCollection[meshid].vertexBuffer=_vertexBuffer;
//     GL.BindBuffer(BufferTarget.ArrayBuffer,_vertexBuffer);
//     GL.BufferData(BufferTarget.ArrayBuffer,
//         AssetCollection[meshid].vertexData.Length*sizeof(float),
//         AssetCollection[meshid].vertexData,
//         BufferUsageHint.StaticDraw);
//         
//     int _vertexArray=GL.GenVertexArray();
//     AssetCollection[meshid].vertexArray=_vertexArray;
//     GL.BindVertexArray(_vertexArray);

//      int _indexBuffer=GL.GenBuffer();
//     AssetCollection[meshid].indexBuffer=_indexBuffer;
//     GL.BindBuffer(BufferTarget.ElementArrayBuffer,_indexBuffer);
//     GL.BufferData(BufferTarget.ElementArrayBuffer,
//     AssetCollection[meshid].indexData.Length*sizeof(int),
//     AssetCollection[meshid].indexData,BufferUsageHint.StaticDraw);

//     //
//     // Paso 14. Creamos el VAO para el atributo aPosition del shader
//     var posLocation = _shader.GetAttribLocation("aPosition");
//     if(posLocation!=(-1))
//     {
//     GL.EnableVertexAttribArray(posLocation);
//     GL.VertexAttribPointer(posLocation,3,VertexAttribPointerType.Float,false,9*sizeof(float),0);
//     }

//     // Paso 15. Creamos el VAO para el atributo aWeight del shader
//     var weightLocation = _shader.GetAttribLocation("aWeight");
//     if(weightLocation!=(-1))
//     {
//     GL.EnableVertexAttribArray(weightLocation);
//     GL.VertexAttribPointer(weightLocation,1,VertexAttribPointerType.Float,false,9*sizeof(float),3*sizeof(float));
//     }

//     var uvLocation = _shader.GetAttribLocation("aTexCoord");
//     if(uvLocation!=(-1))
//     {

//     GL.EnableVertexAttribArray(uvLocation);
//     GL.VertexAttribPointer(uvLocation,2,VertexAttribPointerType.Float,false,9*sizeof(float),4*sizeof(float));
//     }

//     var normalLocation = _shader.GetAttribLocation("aNormal");
//     if(normalLocation!=(-1))
//     {
//     GL.EnableVertexAttribArray(normalLocation);
//     GL.VertexAttribPointer(normalLocation,3,VertexAttribPointerType.Float,false,9*sizeof(float),6*sizeof(float));
//     }

//     // Unbind VBO, EBO and VAO
//             GL.BindBuffer(BufferTarget.ElementArrayBuffer,0);
//             GL.BindBuffer(BufferTarget.ArrayBuffer,0);
//             GL.BindVertexArray(0);

//         }


// }

// float time=0.0f;
//  protected override void OnUpdateFrame(FrameEventArgs e)
// {
//    time+=(float)e.Time;
//    
//     base.OnUpdateFrame(e);
//     if (KeyboardState.IsKeyDown(Keys.Escape))
//         {
//             // If it is, close the window.
//             Close();
//         }
//     if (KeyboardState.IsKeyDown(Keys.C) && time>0.5f)
//     {
//         bDrawCollision=!bDrawCollision;
//     time=0.0f;
//     }
//     //Matrix4.CreateFromAxisAngle(_Axis,_RotAngle,out _Model); 
//     //_RotAngle+=_RotSpeed*(float)e.Time;
//     //if(_RotAngle>=MathHelper.TwoPi)
//     // Controller Update
//      _controller.UpdateState(this.KeyboardState,this.MouseState,e);

//     // Update GameState
//     UpdateGameState((float)e.Time);


// }
//     
//     
// protected override void OnRenderFrame(FrameEventArgs args)
// {
//     // Sin _shader or _mesh no podemos hacer nada
//     if(_shader==null)
//     {
//         return;
//     }
//     base.OnRenderFrame(args);
//     GL.Enable(EnableCap.DepthTest);  
//     GL.Enable(EnableCap.StencilTest);  

//     GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit );

// List<string> activeMeshes = _level.GetActiveMeshes(AssetCollection);

//     foreach(string actorid in _level.ActorCollection.Keys){
//         Actor actor=_level.ActorCollection[actorid];
//         if( !actor.Enabled)
//             continue;
//         
//         //Collisions
//         Mesh ?mesh;
//         if(bDrawCollision)
//         {
//             if(! AssetCollection.ContainsKey(actor.CollisionMeshId))
//                 continue;
//             else{
//                 mesh=AssetCollection[actor.CollisionMeshId];
//             }
//         }
//         else
//         {
//             if(! AssetCollection.ContainsKey(actor.StaticMeshId))
//                 continue;
//             else
//                 mesh=AssetCollection[actor.StaticMeshId];

//         }

//         if(mesh is null)
//             throw new Exception("Trying to render an actor without mesh");
//         
//         // Binding mesh VAO
//         GL.BindVertexArray(mesh.vertexArray);
//             Matrix4 model;
//             if (bDrawCollision)
//             {
//                 model = actor.CollisionModel;
//             }
//             else
//             {
//                 model = actor.Model;
//             }

//         _shader.SetMatrix4("model", model);
//         _shader.SetMatrix4("view",_camera.GetViewMatrix());
//         _shader.SetMatrix4("projection",_camera.GetProjectionMatrix());

//        _shader.SetMatrix4("normalTransformMatrix",actor.NormalTransform);
//        _shader.SetVector3("AmbientLight",new Vector3(0.1f,0.1f,0.1f));
//        _shader.SetVector3("DirLight0Diffuse",new Vector3(0.6f,0.6f,0.6f));
//        _shader.SetVector3("DirLight0Direction",Vector3.Normalize(new Vector3(1.0f,1.0f,1.0f)));


//         // Paso 20. Lanzamos la orden Draw
//         GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);  
//         GL.StencilFunc(StencilFunction.Always,1,0xFF);
//         GL.StencilMask(0xFF);
//  
//         mesh.Draw(_shader,Option.None<Vector3>());
//     
//         GL.StencilFunc(StencilFunction.Notequal,1,0xFF);
//         GL.StencilMask(0x00);
//     
//         actor.SaveModel();
//             //actor.Scale(new Vector3(1.02f,1.02f,1.02f));
//             //    actor.UpdateCollisionModel();
//             model = Matrix4.CreateScale(1.02f, 1.02f, 1.02f) * model;

//         _shader.SetMatrix4("model",model);

//         mesh.Draw(_shader,Option.Some(new Vector3(0.0f,0.0f,0.0f)));
//         //GL.StencilFunc(StencilFunction.Always,1,0xFF);
//         //GL.StencilMask(0xFF);
//         //actor.RestoreModel();
//         //    actor.UpdateCollisionModel();


//         GL.BindVertexArray(0);

//   } // Loop sobre los actores
//         GL.StencilMask(0xFF);
//     
// // Paso 21. Hacemos el swap del doble buffer.
// SwapBuffers();


// }

// protected override void OnResize(ResizeEventArgs e)
// {
//     base.OnResize(e);
//     GL.Viewport(0,0,Size.X,Size.Y);
// }
//     
//  protected override void OnUnload()
// {
//     
//         GL.BindBuffer(BufferTarget.ArrayBuffer,0);
//         GL.BindBuffer(BufferTarget.ElementArrayBuffer,0);
//         GL.BindVertexArray(0);

//         base.OnUnload();
// }
// private Shader? _shader ;
//     
//     private Camera _camera;

//     
//     
//     private Controller _controller;
//     private int _horizontalResolution;
//     private int _verticalResolution;





// }
// using OpenTK.Mathematics;
// using System;

// namespace LearnOpenTK.Common
// {
//     // This is the camera class as it could be set up after the tutorials on the website.
//     // It is important to note there are a few ways you could have set up this camera.
//     // For example, you could have also managed the player input inside the camera class,
//     // and a lot of the properties could have been made into functions.

//     // TL;DR: This is just one of many ways in which we could have set up the camera.
//     // Check out the web version if you don't know why we are doing a specific thing or want to know more about the code.
//     public class Camera
//     {
//         // Those vectors are directions pointing outwards from the camera to define how it rotated.
//         private Vector3 _front = -Vector3.UnitZ;

//         private Vector3 _up = Vector3.UnitY;

//         private Vector3 _right = Vector3.UnitX;

//         // Rotation around the X axis (radians)
//         private float _pitch;

//         // Rotation around the Y axis (radians)
//         private float _yaw = -MathHelper.PiOver2; // Without this, you would be started rotated 90 degrees right.

//         // The field of view of the camera (radians)
//         private float _fov = MathHelper.PiOver2;

//         public Camera(Vector3 position, float aspectRatio)
//         {
//             Position = position;
//             AspectRatio = aspectRatio;
//         }

//         // The position of the camera
//         public Vector3 Position { get; set; }

//         // This is simply the aspect ratio of the viewport, used for the projection matrix.
//         public float AspectRatio { private get; set; }

//         public Vector3 Front => _front;

//         public Vector3 Up => _up;

//         public Vector3 Right => _right;

//         // We convert from degrees to radians as soon as the property is set to improve performance.
//         public float Pitch
//         {
//             get => MathHelper.RadiansToDegrees(_pitch);
//             set
//             {
//                 // We clamp the pitch value between -89 and 89 to prevent the camera from going upside down, and a bunch
//                 // of weird "bugs" when you are using euler angles for rotation.
//                 // If you want to read more about this you can try researching a topic called gimbal lock
//                 var angle = MathHelper.Clamp(value, -89f, 89f);
//                 _pitch = MathHelper.DegreesToRadians(angle);
//                 UpdateVectors();
//             }
//         }

//         // We convert from degrees to radians as soon as the property is set to improve performance.
//         public float Yaw
//         {
//             get => MathHelper.RadiansToDegrees(_yaw);
//             set
//             {
//                 _yaw = MathHelper.DegreesToRadians(value);
//                 UpdateVectors();
//             }
//         }

//         // The field of view (FOV) is the vertical angle of the camera view.
//         // This has been discussed more in depth in a previous tutorial,
//         // but in this tutorial, you have also learned how we can use this to simulate a zoom feature.
//         // We convert from degrees to radians as soon as the property is set to improve performance.
//         public float Fov
//         {
//             get => MathHelper.RadiansToDegrees(_fov);
//             set
//             {
//                 var angle = MathHelper.Clamp(value, 1f, 90f);
//                 _fov = MathHelper.DegreesToRadians(angle);
//             }
//         }

//         // Get the view matrix using the amazing LookAt function described more in depth on the web tutorials
//         public Matrix4 GetViewMatrix()
//         {
//             return Matrix4.LookAt(Position, Position + _front, _up);
//         }

//         // Get the projection matrix using the same method we have used up until this point
//         public Matrix4 GetProjectionMatrix()
//         {
//             return Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, 0.01f, 100f);
//         }

//         // This function is going to update the direction vertices using some of the math learned in the web tutorials.
//         private void UpdateVectors()
//         {
//             // First, the front matrix is calculated using some basic trigonometry.
//             _front.X = MathF.Cos(_pitch) * MathF.Cos(_yaw);
//             _front.Y = MathF.Sin(_pitch);
//             _front.Z = MathF.Cos(_pitch) * MathF.Sin(_yaw);

//             // We need to make sure the vectors are all normalized, as otherwise we would get some funky results.
//             _front = Vector3.Normalize(_front);

//             // Calculate both the right and the up vector using cross product.
//             // Note that we are calculating the right from the global up; this behaviour might
//             // not be what you need for all cameras so keep this in mind if you do not want a FPS camera.
//             _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
//             _up = Vector3.Normalize(Vector3.Cross(_right, _front));
//         }
//     }
// }