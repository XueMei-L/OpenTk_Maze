using LearnOpenTK.Common;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Optional;

public class Window : GameWindow
{
    public RetrievedMaterial[]? matData;
    public Dictionary<string, Mesh> AssetCollection { get; set; }
    public string levelFilePath { get; set; }

    private Level _level = new Level();
    private bool _foundExit = false;
    private bool bDrawCollision = false;

    // 主角（Pawn）控制变量
    private Vector3 _pawnPosition = Vector3.Zero;
    private Vector3 _pawnScale = new Vector3(0.12f);
    private float _pawnYaw = 0.0f;
    private float _pawnCameraHeight = 0.7f;
    private float _pawnModelYawOffset = 270.0f; // 初始偏置角度 (90 + 180)，让模型面朝迷宫内

    // 缓存出口的 Actor 引用，避免在 Update 中每帧进行高能耗的循环比对
    private Actor? _exitActor = null; 

    // 15x15 的迷宫矩阵映射 (根据你的迷宫图片完全一比一像素化还原)
    // 1 = 墙壁 Cube，0 = 可以行走的通道
    // private int[,] mazeGrid = new int[15, 15]
    // {
    //     {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}, // 顶层外墙
    //     {1,0,0,0,0,0,1,0,0,0,0,0,0,0,1}, 
    //     {1,0,1,1,1,0,1,0,1,1,1,1,1,0,1}, 
    //     {1,0,1,0,0,0,1,0,1,0,0,0,1,0,1}, 
    //     {1,0,1,0,1,1,1,0,1,0,1,0,1,1,1}, 
    //     {0,0,1,0,0,0,0,0,1,0,1,0,0,0,2}, // 行索引5：左侧入口 A 和右侧出口 B
    //     {1,0,1,1,1,1,1,0,1,0,1,1,1,1,1}, 
    //     {1,0,0,0,1,0,0,0,1,0,0,0,0,0,1}, 
    //     {1,1,1,0,1,0,1,0,1,1,1,1,1,0,1}, 
    //     {1,0,0,0,1,0,1,0,0,0,0,0,1,0,1}, 
    //     {1,0,1,1,1,0,1,1,1,1,1,0,1,0,1}, 
    //     {1,0,1,0,0,0,1,0,1,0,0,0,1,0,1}, 
    //     {1,0,1,1,1,1,1,0,1,0,1,1,1,0,1}, 
    //     {1,0,0,0,0,0,0,0,1,0,0,0,0,0,1}, 
    //     {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}  // 底层外墙
    // };
    // 15x15 迷宫矩阵：1 = 墙壁 Cube，0 = 通道，2 = 绿色出口 B
    private int[,] mazeGrid = new int[15, 15]
    {
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}, // 顶层外墙
        {1,0,0,0,0,0,1,0,0,0,0,0,0,0,1}, 
        {1,0,1,1,1,0,1,0,1,1,1,1,1,0,1}, 
        {1,0,1,0,0,0,1,0,1,0,0,0,1,0,1}, 
        {1,0,1,0,1,1,1,0,1,0,1,0,1,1,1}, 
        {0,0,0,0,0,0,0,0,0,0,0,0,0,0,2}, // 行索引5：左侧入口 A 和右侧出口 B 
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

    float cubeSize = 2.0f; // 每个方块的基准大小
    private Vector3 MazeOrigin => new Vector3(-cubeSize * 7.0f, 0.0f, -cubeSize * 7.0f);

    // 内部类成员（原代码底部移上来的私有变量）
    private Shader? _shader;
    private Camera _camera;
    private Controller _controller;
    private int _horizontalResolution;
    private int _verticalResolution;

    public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        levelFilePath = "assets/level.json";
        AssetCollection = new Dictionary<string, Mesh>();

        // 获取屏幕分辨率与初始化相机
        MonitorInfo minfo = Monitors.GetMonitorFromWindow(this);
        _horizontalResolution = minfo.HorizontalResolution;
        _verticalResolution = minfo.VerticalResolution;

        Console.WriteLine($"Hor {_horizontalResolution} Vert {_verticalResolution}");
        _camera = new Camera(new Vector3(0.0f, 15.0f, 35.0f), _horizontalResolution / (float)_verticalResolution);
        _camera.Yaw = -90.0f;
        _camera.Pitch = -20.0f;

        _controller = new Controller(_horizontalResolution, _verticalResolution);
        _controller.Speed = 5.0f;
        _controller.CameraDistance = 1.0f; // 让第三人称相机更靠近主角
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

    private void BuildMazeFromGrid()
    {
        if (!AssetCollection.ContainsKey("cube") || !AssetCollection.ContainsKey("col_cube"))
            throw new Exception("Maze requires cube and col_cube assets loaded in level.json.");

        for (int row = 0; row < mazeGrid.GetLength(0); row++)
        {
            for (int col = 0; col < mazeGrid.GetLength(1); col++)
            {
                int cellType = mazeGrid[row, col];
                if (cellType != 1 && cellType != 2)
                    continue;

                Vector3 position = new Vector3(
                    MazeOrigin.X + col * cubeSize,
                    0.0f,
                    MazeOrigin.Z + row * cubeSize);

                if (cellType == 1)
                {
                    string wallId = $"wall_{row}_{col}";
                    _level.ActorCollection.Add(wallId, CreateMazeCubeWall(position));
                }
                else if (cellType == 2)
                {
                    // 优化：直接生成唯一的出口块并建立缓存，不走先加后删的逻辑
                    string exitId = $"exit_{row}_{col}";
                    _exitActor = CreateMazeCubeWall(position);
                    _level.ActorCollection.Add(exitId, _exitActor);
                }
            }
        }

        // 把主角精准传送到左侧入口外侧安全区
        if (_level.ActorCollection.TryGetValue("apawn", out Actor? pawn))
        {
            _pawnPosition = new Vector3(MazeOrigin.X - 3.0f * cubeSize, 0.0f, MazeOrigin.Z + 5 * cubeSize);
            _pawnYaw = 0.0f;
            pawn.SetTransform(_pawnPosition, new Vector3(0.0f, 1.0f, 0.0f), _pawnYaw + _pawnModelYawOffset, _pawnScale);
            pawn.UpdateCollisionModel();

            // 设置相机的初始跟随位置与朝向
            float radius = _controller.CameraDistance;
            float yawRad = MathHelper.DegreesToRadians(_pawnYaw);
            Vector3 pawnForward = new Vector3(MathF.Cos(yawRad), 0.0f, MathF.Sin(yawRad));
            _camera.Position = _pawnPosition - pawnForward * radius + new Vector3(0.0f, _pawnCameraHeight, 0.0f);

            Vector3 toPawn = _pawnPosition - _camera.Position;
            float distXZ = MathF.Sqrt(toPawn.X * toPawn.X + toPawn.Z * toPawn.Z);
            _camera.Yaw = MathHelper.RadiansToDegrees(MathF.Atan2(toPawn.Z, toPawn.X));
            _camera.Pitch = MathHelper.RadiansToDegrees(MathF.Atan2(toPawn.Y, distXZ));
        }
    }

    protected void UpdateGameState(float deltaTime)
    {
        // 获取用户 WASD 轴向输入
        Vector3 movement = _controller.GetMovement();

        // 获取鼠标旋转增量，仅允许绕 Y 轴旋转（Yaw），保持 Pitch 固定
        Angles2D deltaAngles = _controller.GetArmOrientation();
        _camera.Yaw = _camera.Yaw + (float)deltaAngles.Yaw;

        if (!_level.ActorCollection.TryGetValue("apawn", out Actor? pawn))
            return;

        // 计算基于当前相机视角的平面移动向量 (XZ平面)
        Vector3 forward = new Vector3(_camera.Front.X, 0, _camera.Front.Z);
        Vector3 right = new Vector3(_camera.Right.X, 0, _camera.Right.Z);
        Vector3 translation = forward * movement.X + right * movement.Y;

        pawn.SaveModel();

        Vector3 pos = pawn.Model.ExtractTranslation();
        Vector3 scale = pawn.Model.ExtractScale();

        if (translation.Length > 0.0001f)
        {
            // 动态计算主角朝向：让模型的正前方与当前运动方向完美对齐
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

        // 优化点 1：利用缓存的句柄单次检测出口碰撞，彻底消除上百次无脑字符串循环
        if (!_foundExit && _exitActor != null && _exitActor.Enabled)
        {
            if (Collision.CheckEB(pawn, _exitActor))
            {
                _foundExit = true;
                _exitActor.Enabled = false; // 隐藏绿色方块
                _exitActor.UpdateCollisionModel();

                this.Title = "Has llegado a la salida";
                var prev = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Has llegado a la salida");
                Console.ForegroundColor = prev;
            }
        }

        // 优化点 2：普通墙面碰撞循环中过滤掉带有 exit 标识的 Actor，防止胜利时玩家被弹飞
        foreach (string actorid in _level.ActorCollection.Keys)
        {
            if (actorid == "apawn" || actorid.StartsWith("exit_"))
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

        // 第三人称 Orbit 环绕相机跟随
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
        GL.Enable(EnableCap.CullFace);  // 开启背面剔除
        GL.Enable(EnableCap.DepthTest); // 开启深度测试  

        _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
        _shader.Use();

        List<string> activeMeshes = _level.GetActiveMeshes(AssetCollection);

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

            // VAO 绑定顶点着色器属性
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

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
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
            _shader.SetVector3("AmbientLight", new Vector3(0.1f, 0.1f, 0.1f));
            _shader.SetVector3("DirLight0Diffuse", new Vector3(0.6f, 0.6f, 0.6f));
            _shader.SetVector3("DirLight0Direction", Vector3.Normalize(new Vector3(1.0f, 1.0f, 1.0f)));

            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);  
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
            GL.StencilMask(0xFF);
     
            // 出口块(B)特殊渲染为绿色
            if (actorid.StartsWith("exit_"))
            {
                mesh.Draw(_shader, Option.Some(new Vector3(0.0f, 1.0f, 0.0f)));
            }
            else
            {
                mesh.Draw(_shader, Option.None<Vector3>());
            }
        }

        GL.StencilMask(0xFF);
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
}