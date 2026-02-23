using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;

using Root;
using Sources;
using Shaders;

namespace Engine;

internal partial class Engine : GameWindow
{
    internal bool isSaveState = false;

    //Settings
    int FOV = Settings.Graphics.FOV;
    int rayCount { get; set; } = Settings.Graphics.RayCount;
    int renderDistance = Settings.Graphics.RenderDistance;
    float distanceShade = Settings.Graphics.DistanceShade / 10f;
    int tileSize = Settings.Gameplay.TileSize;
    int[,] mapCeiling = Level.MapCeiling;
    int[,] mapFloor = Level.MapFloor;
    int[,] mapWalls = Level.MapWalls;
    float playerMovementSpeed = Settings.Player.MovementSpeed * 10f;
    float mouseSensitivity = Settings.Player.MouseSensitivity / 1000f;

    //DeltaTime
    float deltaTime { get; set; }
    float deltaLastTime { get; set; }

    //Player variables
    Vector2 playerPosition { get; set; }
    float playerAngle { get; set; }
    const float playerCollisionRadius = 10f;
    float pitch { get; set; } = 0f;

    //Bools for menus
    enum MenuId
    {
        None,
        Main,
        Campaign,
        Customs,
        Statistics,
        Settings,
        Pause,
        LvlCompleted
    }

    MenuId currentMenu = MenuId.Main;

    internal bool isInGame => currentMenu == MenuId.None;

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

        //WindowState = WindowState.Fullscreen,
        WindowBorder = WindowBorder.Resizable,
    })
    {
        VSync = VSyncMode.On;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        InitMenuHandlers();
        playerPosition = (
            Level.PlayerStarterPosition.X * tileSize + (tileSize / 2f),
            Level.PlayerStarterPosition.Y * tileSize + (tileSize / 2f));
        playerAngle = MathHelper.DegreesToRadians(Level.PlayerStarterAngle);
        distanceShade = Level.DistanceShade;
        mapCeiling = Level.MapCeiling;
        mapWalls = Level.MapWalls;
        mapFloor = Level.MapFloor;

        //Viewport
        Utils.SetViewport(ClientSize.X, ClientSize.Y);

        //Allowed screen's size (square game aspect ratio)
        minimumScreenSize = ClientSize.Y > ClientSize.X ? ClientSize.X : ClientSize.Y;

        //Offsets to center allowed screen
        screenHorizontalOffset = ClientSize.X > ClientSize.Y ? ((ClientSize.X - minimumScreenSize) / 2) : 0;
        screenVerticalOffset = ClientSize.Y > ClientSize.X ? ((ClientSize.Y - minimumScreenSize) / 2) : 0;

        //Render distance limiter
        renderDistance = Math.Min(renderDistance, Math.Max(mapWalls.GetLength(0), mapWalls.GetLength(1)));

        //Loading textures
        try
        {
            Textures.LoadAll(mapWalls, mapCeiling, mapFloor);
            Console.WriteLine("Textures loaded!");
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

        //Loading shaders
        try
        {
            ShaderHandler.LoadAll(
                ClientSize,
                minimumScreenSize,
                new Vector2(screenHorizontalOffset, screenVerticalOffset));
            Console.WriteLine("Shaders loaded!");
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

        //Menus
        //=========================================================================================
        //If player opens pause menu in game
        if (isInGame && KeyboardState.IsKeyPressed(Keys.Escape))
        {
            currentMenu = MenuId.Pause;
        }

        //Handling menus
        if (!isInGame)
        {
            CursorState = CursorState.Normal;
            HandleAllMenus();
            ShaderHandler.LoadBufferAndClear();
            return;
        }
        //=========================================================================================

        //Game
        //=========================================================================================
        CursorState = CursorState.Grabbed;
        
        //Per - second FPS measurement for display
        fpsFrameCounter++;
        fpsSecondTimer += deltaTime;
        if (fpsSecondTimer >= 1.0f)
        {
            //captured frames in the last ~1s
            displayedFPS = fpsFrameCounter;
            fpsFrameCounter = 0;
            fpsSecondTimer -= 1.0f; //carry remainder
        }

        //Handling controls
        Controls(KeyboardState, MouseState);
        
        //Allowed screen's color
        LoadWindowAttribs();
        
        //Raycount limiter
        rayCount = Math.Min(Settings.Graphics.RayCount, (int)minimumScreenSize);
        // recompute wallWidth here so Engine and RayCasting use same value
        wallWidth = minimumScreenSize / Math.Max(1, rayCount);
        
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
        LoadSpriteAttribs();
        
        float lineSpacing = minimumScreenSize / 100f;
        float lineHeight = minimumScreenSize / 50f;

        //Text overlay: update only once per second
        LoadTextAttribs(
            $"FPS: {displayedFPS}",
            screenHorizontalOffset + (minimumScreenSize / 80f),
            screenVerticalOffset + minimumScreenSize - (minimumScreenSize / 80f),
            1f,
            new Vector3(1f, 1f, 1f)
        );

        LoadTextAttribs(
            $"X: {playerPosition.X}",
            screenHorizontalOffset + (minimumScreenSize / 80f),
            screenVerticalOffset + minimumScreenSize - (minimumScreenSize / 80f) - lineHeight - lineSpacing,
            1f,
            new Vector3(1f, 1f, 1f)
        );

        LoadTextAttribs(
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

        //Allowed screen's color
        ShaderHandler.DrawWindow();
        
        if (!isInGame)
        {
            ShaderHandler.DrawMenus();
            ShaderHandler.DrawTexts();
            SwapBuffers();
            return;
        }
        else
        {
            ShaderHandler.DrawGame(
                wallWidth,
                playerPosition,
                playerAngle,
                pitch);
        }

        SwapBuffers();
    }
    
    protected override void OnUnload()
    {
        base.OnUnload();

        ShaderHandler.DisposeAll();
    }
}