using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Root;
using Shaders;
using Sources;
using StbImageSharp;
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
    int[,] mapCeiling = Level.MapCeiling;
    int[,] mapFloor = Level.MapFloor;
    int[,] mapWalls = Level.MapWalls;
    float playerMovementSpeed = Settings.Player.MovementSpeed;
    float mouseSensitivity = Settings.Player.MouseSensitivity;

    //DeltaTime
    float deltaTime { get; set; }
    float deltaLastTime { get; set; }

    //Player variables
    Vector2 playerPosition { get; set; }
    float playerAngle { get; set; }
    const float playerCollisionRadius =10f;
    float pitch { get; set; } =0f;

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

    // Do not enter game until a map is actually loaded
    internal bool isInGame => currentMenu == MenuId.None && Level.mapLoaded;

    //Engine
    Stopwatch stopwatch { get; set; } = new Stopwatch();
    float FOVStart { get; set; }
    float radBetweenRays { get; set; }
    float wallWidth { get; set; }
    float minimumScreenSize { get; set; }
    float screenVerticalOffset { get; set; }
    float screenHorizontalOffset { get; set; }

    // FPS display that updates once per second
    int displayedFPS =0;
    float fpsSecondTimer =0f;
    int fpsFrameCounter =0;

    //=============================================================================================
    public Engine(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings()
    {
        ClientSize = (width, height),
        Title = title,

        WindowState = WindowState.Fullscreen,
        WindowBorder = WindowBorder.Resizable,

        Icon = LoadWindowIcon("assets/icon.png"),
    })
    {
        VSync = VSyncMode.On;
    }

    private static WindowIcon? LoadWindowIcon(string path)
    {
        if (!File.Exists(path))
            return null;

        using var stream = File.OpenRead(path);
        var img = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        // OpenTK expects RGBA bytes
        var image = new OpenTK.Windowing.Common.Input.Image(img.Width, img.Height, img.Data);
        return new WindowIcon(image);
    }

    public void ApplyLevel()
    {
        // Copy level state into engine runtime
        playerPosition = (
            Level.PlayerStarterPosition.X * tileSize + (tileSize /2f),
            Level.PlayerStarterPosition.Y * tileSize + (tileSize /2f));

        playerAngle = MathHelper.DegreesToRadians(Level.PlayerStarterAngle);
        mapCeiling = Level.MapCeiling;
        mapWalls = Level.MapWalls;
        mapFloor = Level.MapFloor;

        pitch =0f;

        // Depends on map size
        //renderDistance = Level.DistanceShade;

        // Upload new integer maps to GPU
        Textures.LoadMapTextures(mapWalls, mapCeiling, mapFloor);
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        InitMenuHandlers();

        //Viewport
        Utils.SetViewport(ClientSize.X, ClientSize.Y);

        //Allowed screen's size (square game aspect ratio)
        minimumScreenSize = ClientSize.Y > ClientSize.X ? ClientSize.X : ClientSize.Y;

        //Offsets to center allowed screen
        screenHorizontalOffset = ClientSize.X > ClientSize.Y ? ((ClientSize.X - minimumScreenSize) /2) :0;
        screenVerticalOffset = ClientSize.Y > ClientSize.X ? ((ClientSize.Y - minimumScreenSize) /2) :0;

        //Loading textures (static atlases ONLY; map textures are created when user selects a map)
        try
        {
            Textures.LoadStatic();
            Console.WriteLine("Static textures loaded!");
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
        screenHorizontalOffset = ClientSize.X > ClientSize.Y ? ((ClientSize.X - minimumScreenSize) /2) :0;
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
        if (fpsSecondTimer >=1.0f)
        {
            //captured frames in the last ~1s
            displayedFPS = fpsFrameCounter;
            fpsFrameCounter =0;
            fpsSecondTimer -=1.0f; //carry remainder
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
        LoadSpriteAttribs();

        float lineSpacing = minimumScreenSize /95f;
        float lineHeight = minimumScreenSize /50f;

        //Text overlay: update only once per second
        LoadTextAttribs(
            $"FPS: {displayedFPS}",
            screenHorizontalOffset + (minimumScreenSize /80f),
            screenVerticalOffset + minimumScreenSize - (minimumScreenSize /80f),
            1f,
            new Vector3(1f,1f,1f)
        );

        LoadTextAttribs(
            $"X: {playerPosition.X}",
            screenHorizontalOffset + (minimumScreenSize /80f),
            screenVerticalOffset + minimumScreenSize - (minimumScreenSize /80f) - lineHeight - lineSpacing,
            1f,
            new Vector3(1f,1f,1f)
        );

        LoadTextAttribs(
            $"Y: {playerPosition.Y}",
            screenHorizontalOffset + (minimumScreenSize /80f),
            screenVerticalOffset + minimumScreenSize - (minimumScreenSize /80f) - (lineHeight *2) - (lineSpacing *2),
            1f,
            new Vector3(1f,1f,1f)
        );

        LoadTextAttribs(
            $"Player Angle: {playerAngle}",
            screenHorizontalOffset + (minimumScreenSize /80f),
            screenVerticalOffset + minimumScreenSize - (minimumScreenSize /80f) - (lineHeight *3) - (lineSpacing *3),
            1f,
            new Vector3(1f,1f,1f)
        );

        LoadTextAttribs(
            $"Runtime: {stopwatch.Elapsed:mm\\:ss}",
            screenHorizontalOffset + (minimumScreenSize / 80f),
            screenVerticalOffset + minimumScreenSize - (minimumScreenSize / 80f) - (lineHeight * 4) - (lineSpacing * 4),
            1f,
            new Vector3(1f, 1f, 1f)
        );

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