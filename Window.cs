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
    private float _totalTime = 10.0f; // 总时间（比如 60 秒）
    private float _timeRemaining = 10.0f; // 剩余时间

    // ui 
    // 定义游戏状态：运行中、胜利、失败
    private enum GameState { Running, Won, Lost }
    private GameState _currentState = GameState.Running;

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
                    // ✨【保持原汁原味】：直接从 JSON 加载出来的字典里去抓取这个 aexit
                    if (_level.ActorCollection.TryGetValue("aexit", out Actor? jsonExit))
                    {
                        _exitActor = jsonExit;

                        // 将其精准挪动到迷宫矩阵第 5 行，第 14 列的物理出口坐标
                        _exitActor.SetTransform(
                            position, 
                            new Vector3(0.0f, 1.0f, 0.0f), 
                            0.0f, 
                            new Vector3(1.0f, 1.0f, 1.0f)
                        );
                        
                        // 刷新它的物理碰撞盒位置
                        _exitActor.UpdateCollisionModel();
                    }
                }
            }
        }

        // 把主角精准传送到左侧入口外侧安全区
        if (_level.ActorCollection.TryGetValue("apawn", out Actor? pawn))
        {
            // _pawnPosition = new Vector3(MazeOrigin.X - 3.0f * cubeSize, 0.0f, MazeOrigin.Z + 5 * cubeSize);
            // player start point
            _pawnPosition = new Vector3(
                MazeOrigin.X + 13 * cubeSize,
                0.0f,
                MazeOrigin.Z + 5 * cubeSize
            );
            _pawnYaw = 0.0f;
            pawn.SetTransform(_pawnPosition, new Vector3(0.0f, 1.0f, 0.0f), _pawnYaw + _pawnModelYawOffset, _pawnScale);
            pawn.UpdateCollisionModel();

            // ✨【修正初始位置】：考虑模型偏置角，并将相机往后拉 180 度
            float radius = _controller.CameraDistance;
            // 计算 Pawn 实际面朝方向的弧度（基础偏角 + 模型的九十度偏置）
            float pawnTrueYawRad = MathHelper.DegreesToRadians(_pawnYaw + _pawnModelYawOffset + 90.0f);
            Vector3 pawnForward = new Vector3(MathF.Cos(pawnTrueYawRad), 0.0f, MathF.Sin(pawnTrueYawRad));

            // 相机位置 = Pawn位置 - (Pawn的前方向量 * 半径) + 高度
            // 这样相机在出生时就会直接落在 Pawn 的屁股后面
            _camera.Position = _pawnPosition - pawnForward * radius + new Vector3(0.0f, _pawnCameraHeight, 0.0f);

            // 重新让相机算朝向，对准主角
            Vector3 toPawn = _pawnPosition - _camera.Position;
            _camera.Yaw = MathHelper.RadiansToDegrees(MathF.Atan2(toPawn.Z, toPawn.X));
        }
    }

    // protected void UpdateGameState(float deltaTime)
    // {
    //     // ✨【修改点 2】：在游戏更新时处理时间倒计时
    //     if (_timeRemaining > 0)
    //     {
    //         _timeRemaining -= deltaTime;
    //         if (_timeRemaining <= 0)
    //         {
    //             _timeRemaining = 0;
    //             _currentState = GameState.Lost; // 时间到，标记为失败
    //             this.Title = "¡Has perdido! Tiempo agotado. (Game Over)";
    //             Console.WriteLine("失败：时间已耗尽！");
    //             return; // 立即结束更新，Pawn 动弹不得
    //         }
    //     }
    //     // 获取用户 WASD 轴向输入
    //     Vector3 movement = _controller.GetMovement();

    //     // 获取鼠标旋转增量，仅允许绕 Y 轴旋转（Yaw），保持 Pitch 固定
    //     Angles2D deltaAngles = _controller.GetArmOrientation();
    //     _camera.Yaw = _camera.Yaw + (float)deltaAngles.Yaw;

    //     if (!_level.ActorCollection.TryGetValue("apawn", out Actor? pawn))
    //         return;

    //     // 计算基于当前相机视角的平面移动向量 (XZ平面)
    //     Vector3 forward = new Vector3(_camera.Front.X, 0, _camera.Front.Z);
    //     Vector3 right = new Vector3(_camera.Right.X, 0, _camera.Right.Z);
    //     Vector3 translation = forward * movement.X + right * movement.Y;

    //     pawn.SaveModel();

    //     Vector3 pos = pawn.Model.ExtractTranslation();
    //     Vector3 scale = pawn.Model.ExtractScale();

    //     if (translation.Length > 0.0001f)
    //     {
    //         // 动态计算主角朝向：让模型的正前方与当前运动方向完美对齐
    //         float targetAngleRad = MathF.Atan2(translation.Z, translation.X);
    //         pawn.Model =
    //             Matrix4.CreateScale(scale) *
    //             Matrix4.CreateRotationY(-targetAngleRad - MathF.PI / 2f) *
    //             Matrix4.CreateTranslation(pos + translation);
    //     }
    //     else
    //     {
    //         pawn.Model = pawn.Model * Matrix4.CreateTranslation(translation);
    //     }

    //     pawn.UpdateCollisionModel();

    //     // 碰撞判定：检测是否到达终点
    //     if (!_foundExit && _exitActor != null && _exitActor.Enabled)
    //     {
    //         if (Collision.CheckEB(pawn, _exitActor))
    //         {
    //             _foundExit = true;
    //             _exitActor.Enabled = false; // 隐藏终点模型
    //             _exitActor.UpdateCollisionModel();

    //             this.Title = "Has llegado a la salida";
    //             var prev = Console.ForegroundColor;
    //             Console.ForegroundColor = ConsoleColor.Red;
    //             Console.WriteLine("Has llegado a la salida");
    //             Console.ForegroundColor = prev;
    //         }
    //     }

    //     // 普通墙面碰撞循环
    //     foreach (string actorid in _level.ActorCollection.Keys)
    //     {
    //         // ✨【微调点 1】：过滤掉我们 JSON 里的终点 id "aexit"，防止碰撞导致飞船倒退卡死
    //         if (actorid == "apawn" || actorid == "aexit")
    //             continue;

    //         Actor actor = _level.ActorCollection[actorid];
    //         if (!actor.Enabled)
    //             continue;

    //         if (Collision.CheckEB(pawn, actor))
    //         {
    //             // 撞墙倒带
    //             pawn.RestoreModel();
    //             pawn.UpdateCollisionModel();
    //             translation = Vector3.Zero;
    //             break;
    //         }
    //     }

    //     // 第三人称 Orbit 环绕相机跟随
    //     Vector3 pawnNewPosition = pawn.Model.ExtractTranslation();
    //     float radius = _controller.CameraDistance;      
    //     Vector3 offset = -_camera.Front * radius;      
    //     _camera.Position = pawnNewPosition + offset;   
    // }

    protected void UpdateGameState(float deltaTime)
    {
        // 1. 在最开始处理时间倒计时
        if (_timeRemaining > 0)
        {
            _timeRemaining -= deltaTime;
            if (_timeRemaining <= 0)
            {
                _timeRemaining = 0;
                _currentState = GameState.Lost; // 时间到，标记为失败
                this.Title = "¡Has perdido! Tiempo agotado. (Game Over)";
                Console.WriteLine("失败：时间已耗尽！");
                return; // 立即结束更新，彻底锁死
            }
        }

        // 2. 如果游戏状态已经不是 Running（比如已经在上一帧赢了或输了），强行拦截，不让走后续任何逻辑
        if (_currentState != GameState.Running)
        {
            return; 
        }

        // 获取用户 WASD 轴向输入
        Vector3 movement = _controller.GetMovement();

        // 获取鼠标旋转增量
        Angles2D deltaAngles = _controller.GetArmOrientation();
        _camera.Yaw = _camera.Yaw + (float)deltaAngles.Yaw;

        if (!_level.ActorCollection.TryGetValue("apawn", out Actor? pawn))
            return;

        // 计算基于当前相机视角的平面移动向量 (XZ平面)
        Vector3 forward = new Vector3(_camera.Front.X, 0, _camera.Front.Z);
        Vector3 right = new Vector3(_camera.Right.X, 0, _camera.Right.Z);
        Vector3 translation = forward * movement.X + right * movement.Y;

        // ✨【核心修复点 1】：在真正应用移动之前，提前单独给“2号盒子（终点）”做碰撞预测判定
        if (!_foundExit && _exitActor != null && _exitActor.Enabled)
        {
            // 模拟 Pawn 移动之后的临时状态（这里为了安全，直接测当前是否已经重叠）
            if (Collision.CheckEB(pawn, _exitActor))
            {
                _foundExit = true;
                _exitActor.Enabled = false; // 隐藏终点模型
                _exitActor.UpdateCollisionModel();

                // 彻底锁死状态
                _currentState = GameState.Won; 
                
                this.Title = "¡Has ganado! Éxito. (Victory)";
                Console.WriteLine("成功：你找到了出口！");
                
                // 强行把当前帧和未来的移动向量归零，并直接跳出函数
                translation = Vector3.Zero; 
                return; 
            }
        }

        // 保存当前没有移动前的模型（用于撞墙倒带）
        pawn.SaveModel();

        Vector3 pos = pawn.Model.ExtractTranslation();
        Vector3 scale = pawn.Model.ExtractScale();

        if (translation.Length > 0.0001f)
        {
            // 动态计算主角朝向
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

        // ✨【核心修复点 2】：再次在移动应用后双重判定（防止由于速度太快穿过去）
        if (!_foundExit && _exitActor != null && _exitActor.Enabled)
        {
            if (Collision.CheckEB(pawn, _exitActor))
            {
                _foundExit = true;
                _exitActor.Enabled = false;
                _exitActor.UpdateCollisionModel();

                _currentState = GameState.Won;
                this.Title = "¡Has ganado! Éxito. (Victory)";
                Console.WriteLine("成功：移动后触碰出口！");
                return;
            }
        }

        // 普通墙面碰撞循环
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
        _dragonTexture = Texture.LoadFromFile("assets/greendragon.jpg");


        List<string> activeMeshes = _level.GetActiveMeshes(AssetCollection);

        // 否则GetActiveMeshes里找不到dragon，导致显卡没为它编译VAO，屏幕上就画不出任何东西。
        if (AssetCollection.ContainsKey("cube_dragon") && !activeMeshes.Contains("cube_dragon"))
        {
            activeMeshes.Add("cube_dragon");
        }

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

        // 1. 随时允许玩家按 Esc 键退出游戏
        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        // ✨【核心修改】：如果游戏已经胜利或失败，直接拦截，停止接收后续的任何控制和状态更新
        if (_currentState != GameState.Running)
        {
            return; // 绿龙动弹不得，相机停止环绕输入
        }

        // 2. 只有在游戏进行中，才允许按 C 键切换碰撞盒显示
        if (KeyboardState.IsKeyDown(Keys.C) && time > 0.5f)
        {
            bDrawCollision = !bDrawCollision;
            time = 0.0f;
        }

        // 3. 更新输入控制器的底层状态（获取键盘鼠标增量）
        _controller.UpdateState(this.KeyboardState, this.MouseState, e);

        // 4. 执行核心游戏状态更新（移动、时间倒计时、碰撞检测等）
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

        // ==================== 开始绘制 2D UI (计时条 + 结束状态) ====================
        if (_shader != null)
        {
            // ---- A. 顶部固定的纯红计时条 ----
            float timeRatio = _timeRemaining / _totalTime;
            float barMaxWidth = 400.0f;
            float barHeight = 25.0f;
            float barCurrentWidth = barMaxWidth * timeRatio;

            float posX = (Size.X / 2.0f) - (barMaxWidth / 2.0f);
            float posY = Size.Y - 50.0f - barHeight;

            // 关闭 3D 渲染状态，强制 2D 置顶
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.StencilTest);
            GL.Disable(EnableCap.CullFace);

            Matrix4 orthoProjection = Matrix4.CreateOrthographicOffCenter(0, Size.X, 0, Size.Y, -1, 1);
            _shader.Use();
            _shader.SetMatrix4("view", Matrix4.Identity);
            _shader.SetMatrix4("model", Matrix4.Identity);
            _shader.SetMatrix4("projection", orthoProjection); 
            _shader.SetInt("bTex", 0); // 禁用贴图，使用纯色

            // 绘制顶部的红色计时条
            _shader.SetVector3("diffuse_color", new Vector3(1.0f, 0.0f, 0.0f)); // 纯红
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


            // ---- B. 【游戏结束状态提示】失败显示纯红，成功显示纯绿 ----
            if (_currentState != GameState.Running)
            {
                // 定义屏幕中央大框的尺寸和像素位置
                float boxWidth = 500.0f;
                float boxHeight = 200.0f;
                float boxX = (Size.X / 2.0f) - (boxWidth / 2.0f);
                float boxY = (Size.Y / 2.0f) - (boxHeight / 2.0f);

                Vector3 endColor;
                if (_currentState == GameState.Won)
                {
                    // ✨ 成功：亮绿色 (R=0, G=1, B=0)
                    endColor = new Vector3(0.0f, 1.0f, 0.0f);
                }
                else
                {
                    // ✨ 失败：鲜红色 (R=1, G=0, B=0)
                    endColor = new Vector3(1.0f, 0.0f, 0.0f);
                }
                
                // 将颜色注入着色器
                _shader.SetVector3("diffuse_color", endColor);

                // 构建中央大框的 4 个顶点
                float[] endBoxVertices = new float[] {
                    boxX,            boxY,             0.0f,
                    boxX + boxWidth, boxY,             0.0f,
                    boxX + boxWidth, boxY + boxHeight, 0.0f,
                    boxX,            boxY + boxHeight, 0.0f
                };

                // 重新填充数据并一气呵成画出来
                GL.BufferData(BufferTarget.ArrayBuffer, endBoxVertices.Length * sizeof(float), endBoxVertices, BufferUsageHint.StreamDraw);
                GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
            }

            // 清理缓冲区垃圾，释放显存
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.DeleteVertexArray(tempVAO);
            GL.DeleteBuffer(tempVBO);
        }
        // ==================== 2D UI 绘制结束 ====================


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

    // 主角（Pawn）控制变量
    private Vector3 _pawnPosition = Vector3.Zero;
    private Vector3 _pawnScale = new Vector3(0.12f);
    private float _pawnYaw = 0.0f;
    private float _pawnCameraHeight = 0.7f;
    private float _pawnModelYawOffset = 90.0f; // 初始偏置角度 (90 + 180)，让模型面朝迷宫内

    // 缓存出口的 Actor 引用，避免在 Update 中每帧进行高能耗的循环比对
    private Actor? _exitActor = null; 

    // 15x15 迷宫矩阵：1 = 墙壁 Cube，0 = 通道，2 = 猴头出口 B
    private int[,] mazeGrid = new int[15, 15]
    {
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}, // 顶层外墙
        {1,0,0,0,0,0,1,0,0,0,0,0,0,0,1}, 
        {1,0,1,1,1,0,1,0,1,1,1,1,1,0,1}, 
        {1,0,1,0,0,0,1,0,1,0,0,0,1,0,1}, 
        {1,0,1,0,1,1,1,1,1,0,1,0,1,0,1}, 
        {2,0,0,0,0,0,0,0,0,0,0,0,0,0,1}, // 行索引5：左侧入口 A 和右侧出口 B 
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
    private Texture _dragonTexture = null!;
    private Camera _camera;
    private Controller _controller;
    private int _horizontalResolution;
    private int _verticalResolution;
}