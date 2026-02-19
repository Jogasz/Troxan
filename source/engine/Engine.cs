using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;

namespace Engine;

internal partial class Engine : GameWindow
{
    internal bool isSaveState = false;

    //Settings
    int FOV = Settings.Graphics.FOV;
    int rayCount { get; set; } = Settings.Graphics.RayCount;
    int renderDistance = Settings.Graphics.RenderDistance;
    float distanceShade = Settings.Graphics.DistanceShade;
    int tileSize = Settings.Gameplay.TileSize;
    readonly int[,] mapCeiling = Level.MapCeiling;
    readonly int[,] mapFloor = Level.MapFloor;
    int[,] mapWalls = Level.MapWalls;
    float playerMovementSpeed = Settings.Player.MovementSpeed;
    float mouseSensitivity = Settings.Player.MouseSensitivity;

    //DeltaTime
    float deltaTime { get; set; }
    float deltaLastTime { get; set; }

    //Player variables
    Vector2 playerPosition { get; set; } = new Vector2(250, 250);
    float playerAngle { get; set; } = 0f;
    const float playerCollisionRadius = 10f;
    float pitch { get; set; } = 0f;

    //Bools for menus
    internal bool isInMainMenu = true;
    internal bool isInPauseMenu = false;
    internal bool isInStatisticsMenu = false;
    internal bool isInSettingsMenu = false;

    //Engine
    Stopwatch stopwatch { get; set; } = new Stopwatch();
    float FOVStart { get; set; }
    float radBetweenRays { get; set; }
    float wallWidth { get; set; }
    float minimumScreenSize { get; set; }
    float screenVerticalOffset { get; set; }
    float screenHorizontalOffset { get; set; }

    // FPS display that updates once per second
    int displayedFPS = 0;
    float fpsSecondTimer = 0f;
    int fpsFrameCounter = 0;

    //=============================================================================================
    public Engine(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings()
    {
        ClientSize = (width, height),
        Title = title,

        WindowState = WindowState.Fullscreen,
        WindowBorder = WindowBorder.Resizable,
    })
    {
        VSync = VSyncMode.On;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        //Viewport
        Utils.SetViewport(ClientSize.X, ClientSize.Y);

        //Allowed screen's size (square game aspect ratio)
        minimumScreenSize = ClientSize.Y > ClientSize.X ? ClientSize.X : ClientSize.Y;

        //Offsets to center allowed screen
        screenHorizontalOffset = ClientSize.X > ClientSize.Y ? ((ClientSize.X - minimumScreenSize) / 2) : 0;
        screenVerticalOffset = ClientSize.Y > ClientSize.X ? ((ClientSize.Y - minimumScreenSize) / 2) : 0;

        //Render distance limiter
        renderDistance = Math.Min(renderDistance, Math.Max(mapWalls.GetLength(0), mapWalls.GetLength(1)));

        //Loading textures + Exception handling
        try
        {
            Texture.LoadAll(mapWalls, mapCeiling, mapFloor);
        }
        catch (FileNotFoundException noFileEx)
        {
            Console.WriteLine(noFileEx);
        }
        catch (InvalidOperationException invOpEx)
        {
            Console.WriteLine(invOpEx);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Textures: Something went wrong...\n - {e}");
        }

        //Loading shaders + Exception handling
        try
        {
            ShaderHandler.LoadAll(
                ClientSize,
                minimumScreenSize,
                new Vector2(screenHorizontalOffset, screenVerticalOffset));
            Console.WriteLine(" - SHADERS have been loaded!");
        }
        catch (FileNotFoundException noFileEx)
        {
            Console.WriteLine(noFileEx);
        }
        catch (InvalidOperationException invOpEx)
        {
            Console.WriteLine(invOpEx);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Shader: Something went wrong...\n - {e}");
        }

        //Stopwatch for delta time
        stopwatch.Start();
    }
    
    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);

        //Viewport
        Utils.SetViewport(ClientSize.X, ClientSize.Y);

        //Allowed screen's size (square game aspect ratio)
        minimumScreenSize = ClientSize.Y > ClientSize.X ? ClientSize.X : ClientSize.Y;

        //Offsets to center allowed screen
        screenHorizontalOffset = ClientSize.X > ClientSize.Y ? ((ClientSize.X - minimumScreenSize) / 2) : 0;
        screenVerticalOffset = ClientSize.Y > ClientSize.X ? ((ClientSize.Y - minimumScreenSize) / 2) : 0;
        
        //Updating ALL shader uniforms
        ShaderHandler.UpdateUniforms(
            ClientSize,
            minimumScreenSize,
            new Vector2(screenHorizontalOffset, screenVerticalOffset));
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        //DeltaTime
        //=========================================================================================
        float currentTime = (float)stopwatch.Elapsed.TotalSeconds;
        deltaTime = currentTime - deltaLastTime;
        deltaLastTime = currentTime;
        //=========================================================================================

        //Per-second FPS measurement for display
        fpsFrameCounter++;
        fpsSecondTimer += deltaTime;
        if (fpsSecondTimer >= 1.0f)
        {
            //captured frames in the last ~1s
            displayedFPS = fpsFrameCounter;
            fpsFrameCounter = 0;
            fpsSecondTimer -= 1.0f; //carry remainder
        }

        //Handling main menu
        if (isInMainMenu)
        {
            CursorState = CursorState.Normal;
            MainMenu();
            ShaderHandler.LoadBufferAndClearMenus();
            return;
        }

        //Handling statistics menu
        if (isInStatisticsMenu)
        {
            CursorState = CursorState.Normal;
            StatisticsMenu();

            ShaderHandler.LoadBufferAndClearMenus();
            ShaderHandler.LoadBufferAndClearTexts();
            return;
        }

        //Handling settings menu
        if (isInSettingsMenu)
        {
            CursorState = CursorState.Normal;
            SettingsMenu();
            ShaderHandler.LoadBufferAndClearMenus();
            return;
        }

        if (KeyboardState.IsKeyPressed(Keys.Escape)) isInPauseMenu = true;

        //Handling pause menu
        if (isInPauseMenu)
        {
            CursorState = CursorState.Normal;
            PauseMenu();
            ShaderHandler.LoadBufferAndClearMenus();
            return;
        }

        //Handling controls
        Controls(KeyboardState, MouseState);
        //=========================================================================================

        //!!!!!!!!!!!!!!!!!
        //Lehet nem kell!!!
        //!!!!!!!!!!!!!!!!!

        //Allowed screen's color
        ShaderHandler.WindowVertexAttribList.AddRange(new float[]
        {
            screenHorizontalOffset,
            screenHorizontalOffset + minimumScreenSize,
            screenVerticalOffset,
            screenVerticalOffset + minimumScreenSize,
            0.3f,
            0.5f,
            0.9f
        });
        //=========================================================================================

        //Raycount limiter
        rayCount = Math.Min(Settings.Graphics.RayCount, (int)minimumScreenSize);
        // recompute wallWidth here so Engine and RayCasting use same value
        wallWidth = minimumScreenSize / Math.Max(1, rayCount);
        //=============================================================================================
        
        //Raycasting:
        // 1. RayCasting logic engine
        // 2. Computing ceiling
        // 3. Computing floor
        // 4. Computing walls
        // +. At the end of every part, the vertex attributes also get uploaded
        RayCasting.Run(
            ClientSize,
            FOV,
            rayCount,
            tileSize,
            distanceShade,
            minimumScreenSize,
            screenHorizontalOffset,
            screenVerticalOffset,
            playerAngle,
            playerPosition,
            pitch,
            mapWalls,
            mapFloor,
            mapCeiling,
            renderDistance
        );

        //Sprites
        //Sprites are not in the RayCasting because they use3D matrix
        //ComputeSprites();
        //=============================================================================================

        float lineSpacing = minimumScreenSize / 100f;
        float lineHeight = minimumScreenSize / 50f;

        //Text overlay: update only once per second
        DrawText(
            $"FPS: {displayedFPS}",
            screenHorizontalOffset + (minimumScreenSize /80f),
            screenVerticalOffset + minimumScreenSize - (minimumScreenSize /80f),
            1f,
            new Vector3(1f,1f,1f)
        );

        DrawText(
            $"X: {playerPosition.X}",
            screenHorizontalOffset + (minimumScreenSize / 80f),
            screenVerticalOffset + minimumScreenSize - (minimumScreenSize / 80f) - lineHeight - lineSpacing,
            1f,
            new Vector3(1f, 1f, 1f)
        );

        DrawText(
            $"Y: {playerPosition.Y}",
            screenHorizontalOffset + (minimumScreenSize / 80f),
            screenVerticalOffset + minimumScreenSize - (minimumScreenSize / 80f) - (lineHeight * 2) - (lineSpacing * 2),
            1f,
            new Vector3(1f, 1f, 1f)
        );

        //Text scheme
        // - text
        // - offset x
        // - offset y
        // - font size
        // - vec3 rgb

        ShaderHandler.LoadBufferAndClear();
    }
    //=============================================================================================

    //Every frame's renderer (second-half)
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        //Clearing window
        GL.ClearColor(0.0f,0.0f,0.0f,1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        if (isInMainMenu)
        {
            ShaderHandler.DrawMenus();
            SwapBuffers();
            return;
        }

        if (isInStatisticsMenu)
        {
            ShaderHandler.DrawMenus();
            ShaderHandler.DrawTexts();
            SwapBuffers();
            return;
        }

        if (isInSettingsMenu)
        {
            ShaderHandler.DrawMenus();
            SwapBuffers();
            return;
        }

        ShaderHandler.DrawGame(
            wallWidth,
            playerPosition,
            playerAngle,
            pitch);

        if (isInPauseMenu)
        {
            ShaderHandler.DrawMenus();
        }

        SwapBuffers();
    }
    
    protected override void OnUnload()
    {
        base.OnUnload();

        ShaderHandler.DisposeAll();
    }
}