using LearnOpenTK.Common;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Optional;
using System;
using System.Collections.Generic;

public class Window : GameWindow
{
    public RetrievedMaterial[]? matData;
    public Dictionary<string, Mesh> AssetCollection { get; set; }
    public string levelFilePath { get; set; }

    private Level _level = new Level();
    private bool _foundExit = false;
    private bool bDrawCollision = false;

    // timer counter
    private float _totalTime = 90.0f;
    private float _timeRemaining = 90.0f;

    // ui 
    private enum GameState { Running, Won, Lost }
    private GameState _currentState = GameState.Running;

    public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        levelFilePath = "assets/level.json";
        AssetCollection = new Dictionary<string, Mesh>();

        MonitorInfo minfo = Monitors.GetMonitorFromWindow(this);
        _horizontalResolution = minfo.HorizontalResolution;
        _verticalResolution = minfo.VerticalResolution;

        Console.WriteLine($"Hor {_horizontalResolution} Vert {_verticalResolution}");
        _camera = new Camera(new Vector3(0.0f, 15.0f, 35.0f), _horizontalResolution / (float)_verticalResolution);
        _camera.Yaw = -90.0f;
        _camera.Pitch = -20.0f;

        _controller = new Controller(_horizontalResolution, _verticalResolution);
        _controller.Speed = 5.0f;
        _controller.CameraDistance = 1.0f;
    }

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

    // funcion para construir el laberinto
    private void BuildMazeFromGrid()
    {
        // recorrer la matriz
        for (int row = 0; row < mazeGrid.GetLength(0); row++)
        {
            for (int col = 0; col < mazeGrid.GetLength(1); col++)
            {
                int cellType = mazeGrid[row, col];
                // 0 es espacio vacio
                if (cellType != 1 && cellType != 2)
                    continue;

                Vector3 position = new Vector3(
                    col * cubeSize,
                    0.0f,
                    row * cubeSize);

                // tipo 1 es cubo normal
                if (cellType == 1)
                {
                    string wallId = $"wall_{row}_{col}";
                    _level.ActorCollection.Add(wallId, CreateMazeCubeWall(position));
                }
                // tipo 2 es la salida
                else if (cellType == 2)
                {
                    if (_level.ActorCollection.TryGetValue("aexit", out Actor? jsonExit))
                    {
                        _exitActor = jsonExit;

                        // Colocar el objeto de salida exactamente en la posición definida por la matriz del laberinto
                        _exitActor.SetTransform(
                            position, 
                            new Vector3(0.0f, 1.0f, 0.0f), 
                            0.0f, 
                            new Vector3(1.0f, 1.0f, 1.0f)
                        );
                        
                        // Actualizar el modelo de colisión
                        _exitActor.UpdateCollisionModel();
                    }
                }
            }
        }
        // posicion y camara
        if (_level.ActorCollection.TryGetValue("apawn", out Actor? pawn))
        {
            // colocar el usario en la posicion de entrada (fila [5] columna [13])
            // Punto inicial del jugador
            _pawnPosition = new Vector3(
                13 * cubeSize,
                0.0f,
                5 * cubeSize
            );

            _pawnYaw = 0.0f;

            pawn.SetTransform(
                _pawnPosition, 
                new Vector3(0.0f, 1.0f, 0.0f), 
                _pawnYaw + _pawnModelYawOffset, 
                _pawnScale
            );

            // colision
            pawn.UpdateCollisionModel();

            // camara inicial
            float radius = _controller.CameraDistance;

            float pawnYawRad = MathHelper.DegreesToRadians(_pawnYaw + _pawnModelYawOffset + 90.0f);
            Vector3 pawnForward = new Vector3(
                MathF.Cos(pawnYawRad), 
                0.0f, 
                MathF.Sin(pawnYawRad)
            );

            // posición de la cámara: detrás del jugador + altura
            _camera.Position = _pawnPosition - pawnForward * radius + new Vector3(0.0f, _pawnCameraHeight, 0.0f);

            // la cámara mira hacia el jugador al inicio
            Vector3 toPawn = _pawnPosition - _camera.Position;
            _camera.Yaw = MathHelper.RadiansToDegrees(MathF.Atan2(toPawn.Z, toPawn.X));
        }
    }
    
    
    protected void UpdateGameState(float deltaTime)
    {
        if (_timeRemaining > 0)
        {
            _timeRemaining -= deltaTime;
            if (_timeRemaining <= 0)
            {
                _timeRemaining = 0;
                _currentState = GameState.Lost; 
                this.Title = "¡Has perdido! Tiempo agotado. (Game Over)";
                Console.WriteLine(this.Title);
                return;
            }
        }

        if (_currentState != GameState.Running)
        {
            return; 
        }

        // player controller
        Vector3 movement = _controller.GetMovement();

        // raton
        Angles2D deltaAngles = _controller.GetArmOrientation();
        _camera.Yaw = _camera.Yaw + (float)deltaAngles.Yaw;

        if (!_level.ActorCollection.TryGetValue("apawn", out Actor? pawn))
            return;

        Vector3 forward = new Vector3(_camera.Front.X, 0, _camera.Front.Z);
        Vector3 right = new Vector3(_camera.Right.X, 0, _camera.Right.Z);
        Vector3 translation = forward * movement.X + right * movement.Y;

        if (!_foundExit && _exitActor != null && _exitActor.Enabled)
        {
            if (Collision.CheckEB(pawn, _exitActor))
            {
                _foundExit = true;
                _exitActor.Enabled = false; // ocultar el objeto "la salida"
                _exitActor.UpdateCollisionModel();

                _currentState = GameState.Won; 
                
                this.Title = "¡Has ganado! Éxito. (Victory)";
                Console.WriteLine(this.Title);
                
                translation = Vector3.Zero; 
                return; 
            }
        }

        pawn.SaveModel();

        Vector3 pos = pawn.Model.ExtractTranslation();
        Vector3 scale = pawn.Model.ExtractScale();

        if (translation.Length > 0.0001f)
        {
            float targetAngleRad = MathF.Atan2(translation.Z, translation.X);
            pawn.Model =
                Matrix4.CreateScale(scale) *
                Matrix4.CreateRotationY(-targetAngleRad - MathF.PI / 2f) *
                Matrix4.CreateTranslation(pos + translation);
        }
        else
        {
            pawn.Model = pawn.Model * Matrix4.CreateTranslation(translation);
        }

        pawn.UpdateCollisionModel();

        if (!_foundExit && _exitActor != null && _exitActor.Enabled)
        {
            if (Collision.CheckEB(pawn, _exitActor))
            {
                _foundExit = true;
                _exitActor.Enabled = false;
                _exitActor.UpdateCollisionModel();

                _currentState = GameState.Won;
                this.Title = "¡Has ganado! Éxito. (Victory)";
                Console.WriteLine(this.Title);
                return;
            }
        }

        foreach (string actorid in _level.ActorCollection.Keys)
        {
            if (actorid == "apawn" || actorid == "aexit")
                continue;

            Actor actor = _level.ActorCollection[actorid];
            if (!actor.Enabled)
                continue;

            if (Collision.CheckEB(pawn, actor))
            {
                // 撞墙倒带
                pawn.RestoreModel();
                pawn.UpdateCollisionModel();
                translation = Vector3.Zero;
                break;
            }
        }

        // orbitar la camra
        Vector3 pawnNewPosition = pawn.Model.ExtractTranslation();
        float radius = _controller.CameraDistance;      
        Vector3 offset = -_camera.Front * radius;      
        _camera.Position = pawnNewPosition + offset;   
    }
    protected void InitializeLevel()
    {
        _level = new Level(levelFilePath);
        _level.LoadLevel(AssetCollection);
        BuildMazeFromGrid();
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        InitializeLevel();

        GL.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);

        _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
        _shader.Use();
        _dragonTexture = Texture.LoadFromFile("assets/greendragon.jpg");

        List<string> activeMeshes = _level.GetActiveMeshes(AssetCollection);

        activeMeshes.Add("cube_dragon");

        foreach (string meshid in activeMeshes)
        {
            if (AssetCollection[meshid] is null || AssetCollection[meshid].vertexData is null)
                throw new Exception("Mesh with empty data"); 

            int _vertexBuffer = GL.GenBuffer();
            AssetCollection[meshid].vertexBuffer = _vertexBuffer;
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer,
                AssetCollection[meshid].vertexData.Length * sizeof(float),
                AssetCollection[meshid].vertexData,
                BufferUsageHint.StaticDraw);
            
            int _vertexArray = GL.GenVertexArray();
            AssetCollection[meshid].vertexArray = _vertexArray;
            GL.BindVertexArray(_vertexArray);

            int _indexBuffer = GL.GenBuffer();
            AssetCollection[meshid].indexBuffer = _indexBuffer;
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                AssetCollection[meshid].indexData.Length * sizeof(int),
                AssetCollection[meshid].indexData, BufferUsageHint.StaticDraw);

            var posLocation = _shader.GetAttribLocation("aPosition");
            if (posLocation != -1)
            {
                GL.EnableVertexAttribArray(posLocation);
                GL.VertexAttribPointer(posLocation, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 0);
            }

            var weightLocation = _shader.GetAttribLocation("aWeight");
            if (weightLocation != -1)
            {
                GL.EnableVertexAttribArray(weightLocation);
                GL.VertexAttribPointer(weightLocation, 1, VertexAttribPointerType.Float, false, 9 * sizeof(float), 3 * sizeof(float));
            }

            var uvLocation = _shader.GetAttribLocation("aTexCoord");
            if (uvLocation != -1)
            {
                GL.EnableVertexAttribArray(uvLocation);
                GL.VertexAttribPointer(uvLocation, 2, VertexAttribPointerType.Float, false, 9 * sizeof(float), 4 * sizeof(float));
            }

            var normalLocation = _shader.GetAttribLocation("aNormal");
            if (normalLocation != -1)
            {
                GL.EnableVertexAttribArray(normalLocation);
                GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 6 * sizeof(float));
            }

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }
    }

    float time = 0.0f;
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        time += (float)e.Time;
        base.OnUpdateFrame(e);

        // salir
        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        if (_currentState != GameState.Running)
        {
            return; 
        }

        if (KeyboardState.IsKeyDown(Keys.C) && time > 0.5f)
        {
            bDrawCollision = !bDrawCollision;
            time = 0.0f;
        }

        _controller.UpdateState(this.KeyboardState, this.MouseState, e);

        UpdateGameState((float)e.Time);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        if (_shader == null) return;

        base.OnRenderFrame(args);
        GL.Enable(EnableCap.DepthTest);  
        GL.Enable(EnableCap.StencilTest);  

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

        foreach (string actorid in _level.ActorCollection.Keys)
        {
            Actor actor = _level.ActorCollection[actorid];
            if (!actor.Enabled) continue;
            
            Mesh? mesh;
            if (bDrawCollision)
            {
                if (!AssetCollection.ContainsKey(actor.CollisionMeshId)) continue;
                mesh = AssetCollection[actor.CollisionMeshId];
            }
            else
            {
                if (!AssetCollection.ContainsKey(actor.StaticMeshId)) continue;
                mesh = AssetCollection[actor.StaticMeshId];
            }

            if (mesh is null)
                throw new Exception("Trying to render an actor without mesh");
            
            GL.BindVertexArray(mesh.vertexArray);
            Matrix4 model = bDrawCollision ? actor.CollisionModel : actor.Model;

            _shader.SetMatrix4("model", model);
            _shader.SetMatrix4("view", _camera.GetViewMatrix());
            _shader.SetMatrix4("projection", _camera.GetProjectionMatrix());

            _shader.SetMatrix4("normalTransformMatrix", actor.NormalTransform);
            _shader.SetVector3("AmbientLight", new Vector3(0.35f, 0.35f, 0.35f));
            _shader.SetVector3("DirLight0Diffuse", new Vector3(1.0f, 1.0f, 1.0f));
            _shader.SetVector3("DirLight0Direction", Vector3.Normalize(new Vector3(1.0f, 1.0f, 1.0f)));

            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);  
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
            GL.StencilMask(0xFF);
     
            if(actor.StaticMeshId == "cube_dragon")
            {
                _dragonTexture.Use(TextureUnit.Texture0);

                _shader.SetInt("bTex", 1);

                mesh.Draw(_shader, Option.None<Vector3>());
            }
            else
            {
                _shader.SetInt("bTex", 0);

                mesh.Draw(_shader, Option.None<Vector3>());
            }
                
        }

        GL.StencilMask(0xFF);

        // Timer Counter
        if (_shader != null)
        {
            float timeRatio = _timeRemaining / _totalTime;
            float barMaxWidth = 400.0f;
            float barHeight = 25.0f;
            float barCurrentWidth = barMaxWidth * timeRatio;

            float posX = (Size.X / 2.0f) - (barMaxWidth / 2.0f);
            float posY = Size.Y - 50.0f - barHeight;

            // 2D
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.StencilTest);
            GL.Disable(EnableCap.CullFace);

            Matrix4 orthoProjection = Matrix4.CreateOrthographicOffCenter(0, Size.X, 0, Size.Y, -1, 1);
            _shader.Use();
            _shader.SetMatrix4("view", Matrix4.Identity);
            _shader.SetMatrix4("model", Matrix4.Identity);
            _shader.SetMatrix4("projection", orthoProjection); 
            _shader.SetInt("bTex", 0); // para controlar que si usa textura imagen

            _shader.SetVector3("diffuse_color", new Vector3(1.0f, 0.0f, 0.0f)); // ROJO
            float[] uiVertices = new float[] {
                posX,                   posY,             0.0f,
                posX + barCurrentWidth, posY,             0.0f,
                posX + barCurrentWidth, posY + barHeight, 0.0f,
                posX,                   posY + barHeight, 0.0f
            };

            int tempVBO = GL.GenBuffer();
            int tempVAO = GL.GenVertexArray();
            GL.BindVertexArray(tempVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, tempVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, uiVertices.Length * sizeof(float), uiVertices, BufferUsageHint.StreamDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);


            if (_currentState != GameState.Running)
            {
                float boxWidth = 500.0f;
                float boxHeight = 200.0f;
                float boxX = (Size.X / 2.0f) - (boxWidth / 2.0f);
                float boxY = (Size.Y / 2.0f) - (boxHeight / 2.0f);

                Vector3 endColor;
                if (_currentState == GameState.Won)
                {
                    // VERDE
                    endColor = new Vector3(0.0f, 1.0f, 0.0f);
                }
                else
                {
                    // ROJO 
                    endColor = new Vector3(1.0f, 0.0f, 0.0f);
                }
                
                _shader.SetVector3("diffuse_color", endColor);

                float[] endBoxVertices = new float[] {
                    boxX,            boxY,             0.0f,
                    boxX + boxWidth, boxY,             0.0f,
                    boxX + boxWidth, boxY + boxHeight, 0.0f,
                    boxX,            boxY + boxHeight, 0.0f
                };

                GL.BufferData(BufferTarget.ArrayBuffer, endBoxVertices.Length * sizeof(float), endBoxVertices, BufferUsageHint.StreamDraw);
                GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.DeleteVertexArray(tempVAO);
            GL.DeleteBuffer(tempVBO);
        }


        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, Size.X, Size.Y);
    }
        
    protected override void OnUnload()
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        GL.BindVertexArray(0);
        base.OnUnload();
    }

    private Vector3 _pawnPosition = Vector3.Zero;
    private Vector3 _pawnScale = new Vector3(0.12f);
    private float _pawnYaw = 0.0f;
    private float _pawnCameraHeight = 0.7f;
    private float _pawnModelYawOffset = 90.0f; 

    // acto de salida del laberinto
    private Actor? _exitActor = null; 

    // 15x15 laberinto：1 = cubo, 0 = vacio，2 = salida con imagen de dragon
    private int[,] mazeGrid = new int[15, 15]
    {
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
        {1,0,0,0,0,0,1,0,0,0,0,0,0,0,1}, 
        {1,0,1,1,1,0,1,0,1,1,1,1,1,0,1}, 
        {1,0,1,0,0,0,1,0,1,0,0,0,1,0,1}, 
        {2,0,1,0,1,0,0,1,1,0,1,0,1,0,1}, // Salida numero 2
        {1,0,0,0,1,1,0,0,1,0,1,0,0,0,1}, // Entrada en la posicion fila [5] columna [13]
        {1,1,1,1,1,1,1,0,1,0,1,1,1,0,1}, 
        {1,0,0,0,1,1,1,0,1,0,0,0,1,0,1}, 
        {1,0,1,0,1,0,1,0,1,1,1,1,1,0,1}, 
        {1,0,1,0,1,0,1,0,0,0,0,0,1,0,1}, 
        {1,0,1,0,1,0,1,1,1,1,1,0,1,0,1}, 
        {1,0,1,0,0,0,1,0,0,0,0,0,1,0,1}, 
        {1,0,1,1,1,1,1,0,1,0,1,1,1,0,1}, 
        {1,0,0,0,0,0,0,0,1,0,0,0,0,0,1}, 
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}
    };

    float cubeSize = 2.0f;
    private Shader? _shader;
    private Texture _dragonTexture = null!;
    private Camera _camera;
    private Controller _controller;
    private int _horizontalResolution;
    private int _verticalResolution;
}