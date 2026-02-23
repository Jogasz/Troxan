using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using Sources;

namespace Shaders;

internal partial class ShaderHandler
{
    public int Handle;

    public ShaderHandler(string vertexPath, string fragmentPath)
    {
        //Handlers for the induvidual shaders
            //Vertex Shader
        int VertexShader;
            //Fragment Shader
        int FragmentShader;

        //If file is not found
        if (!File.Exists(vertexPath))
            throw new FileNotFoundException($"Vertex shader file not found:\n - '{vertexPath}'");

        if (!File.Exists(fragmentPath))
            throw new FileNotFoundException($"Fragment shader file not found:\n - '{fragmentPath}'");

        //Loading shader files
        string VertexShaderSource = File.ReadAllText(vertexPath);
        string FragmentShaderSource = File.ReadAllText(fragmentPath);

        //Generating the shaders and binding the source code to the shaders
        VertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(VertexShader, VertexShaderSource);

        FragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(FragmentShader, FragmentShaderSource);

        //Compiling the Vertex Shader and checking for errors
        GL.CompileShader(VertexShader);

        //Compiling the Fragment Shader and checking for errors
        GL.CompileShader(FragmentShader);

        int VertexCompileSucces;
        int FragmentCompileSucces;

        //Getting the status of the compiler
        GL.GetShader(VertexShader, ShaderParameter.CompileStatus, out VertexCompileSucces);

        //Vertex compiling error
        if (VertexCompileSucces == 0)
        {
            string infoLog = string.IsNullOrWhiteSpace(GL.GetShaderInfoLog(VertexShader)) ? "Unknown" : GL.GetShaderInfoLog(VertexShader);
            throw new InvalidOperationException($"Compiling vertex shader has failed:\n - '{vertexPath}'\n - Reason: '{infoLog}'");
        }

        //Getting the status of the compiler
        GL.GetShader(FragmentShader, ShaderParameter.CompileStatus, out FragmentCompileSucces);

        //Fragment compiling error
        if (FragmentCompileSucces == 0)
        {
            string infoLog = string.IsNullOrWhiteSpace(GL.GetShaderInfoLog(FragmentShader)) ? "Unknown" : GL.GetShaderInfoLog(FragmentShader);
            throw new InvalidOperationException($"Compiling fragment shader has failed:\n - '{fragmentPath}'\n - Reason: '{infoLog}'");
        }

        //Linking shaders into a program that can be run on the GPU
        Handle = GL.CreateProgram();

        GL.AttachShader(Handle, VertexShader);
        GL.AttachShader(Handle, FragmentShader);

        GL.LinkProgram(Handle);

        //Writing out error if there is one
        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int linkingSuccess);

        //Program linking error
        if (linkingSuccess == 0)
        {
            string infoLog = string.IsNullOrWhiteSpace(GL.GetProgramInfoLog(Handle)) ? "Unknown" : GL.GetProgramInfoLog(Handle);
            throw new InvalidOperationException($"Linking shader program has failed:\n - Vertex: '{vertexPath}'\n - Fragment: '{fragmentPath}'\n - Reason: '{infoLog}'");
        }

        GL.DetachShader(Handle, VertexShader);
        GL.DetachShader(Handle, FragmentShader);
        GL.DeleteShader(FragmentShader);
        GL.DeleteShader(VertexShader);
    }
    //Method to be able to use the Shader handler program
    public void Use()
    {
        GL.UseProgram(Handle);
    }

    //Cleaning up the handle after this class dies
    private bool disposedValue = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            GL.DeleteProgram(Handle);

            disposedValue = true;
        }
    }

    ~ShaderHandler()
    {
        if (disposedValue == false)
        {
            Console.WriteLine("GPU Resource leak!");
        }
    }

    public void SetMatrix4(string name, Matrix4 matrix)
    {
        int location = GL.GetUniformLocation(Handle, name);
        if (location == -1) return;
        
        GL.UniformMatrix4(location, false, ref matrix);
    }

    //Float uniform setter
    public void SetFloat(string name, float value)
    {
        int location = GL.GetUniformLocation(Handle, name);
        if (location == -1) return;
        GL.Uniform1(location, value);
    }

    //Int uniform setter
    public void SetInt(string name, int value)
    {
        int location = GL.GetUniformLocation(Handle, name);
        if (location == -1) return;
        GL.Uniform1(location, value);
    }

    //Vec2 uniform setter
    public void SetVector2(string name, Vector2 value)
    {
        int location = GL.GetUniformLocation(Handle, name);
        if (location == -1) return;
        GL.Uniform2(location, value);
    }

    //Vec3 uniform setter
    public void SetVector3(string name, Vector3 value)
    {
        int location = GL.GetUniformLocation(Handle, name);
        if (location == -1) return;
        GL.Uniform3(location, value);
    }

    //Vec4 uniform setter
    public void SetVector4(string name, Vector4 value)
    {
        int location = GL.GetUniformLocation(Handle, name);
        if (location == -1) return;
        GL.Uniform4(location, value);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    static Matrix4 projection { get; set; }

    //OnLoad
    public static void LoadAll(Vector2i ClientSize, float minimumScreenSize, Vector2 screenOffset)
    {
        //Viewport and projection (bottom-left origin)
        projection = Matrix4.CreateOrthographicOffCenter(0f, ClientSize.X,0f, ClientSize.Y, -1f,1f);

        LoadWindowShader(
            "source/engine/graphics/window/window.vert",
            "source/engine/graphics/window/window.frag",
            projection);

        LoadCeilingShader(
            "source/engine/graphics/geometry/ceiling/ceiling.vert",
            "source/engine/graphics/geometry/ceiling/ceiling.frag",
            projection,
            minimumScreenSize);

        LoadFloorShader(
            "source/engine/graphics/geometry/floor/floor.vert",
            "source/engine/graphics/geometry/floor/floor.frag",
            projection,
            ClientSize,
            minimumScreenSize);

        LoadWallShader(
            "source/engine/graphics/geometry/wall/wall.vert",
            "source/engine/graphics/geometry/wall/wall.frag",
            projection,
            minimumScreenSize,
            screenOffset);

        LoadSpriteShader(
            "source/engine/graphics/geometry/sprites/sprites.vert",
            "source/engine/graphics/geometry/sprites/sprites.frag",
            projection,
            minimumScreenSize,
            screenOffset);

        LoadMenusShader(
            "source/engine/graphics/gui/menus/containers/menus.vert",
            "source/engine/graphics/gui/menus/containers/menus.frag",
            projection);

        LoadButtonsShader(
            "source/engine/graphics/gui/menus/buttons/buttons.vert",
            "source/engine/graphics/gui/menus/buttons/buttons.frag",
            projection);

        LoadTextsShader(
            "source/engine/graphics/gui/texts/texts.vert",
            "source/engine/graphics/gui/texts/texts.frag",
            projection);
    }

    //OnFramebufferResize
    public static void UpdateUniforms(
        Vector2i ClientSize,
        float minimumScreenSize,
        Vector2 screenOffset)
    {
        //Updating projection matrix by the current window's size
        projection = Matrix4.CreateOrthographicOffCenter(0f, ClientSize.X, 0f, ClientSize.Y, -1f, 1f);

        UpdateWindowUniforms();
        UpdateCeilingUniforms(minimumScreenSize);
        UpdateWallUniforms(minimumScreenSize, screenOffset);
        UpdateFloorUniforms(ClientSize, minimumScreenSize);
        UpdateSpriteUniforms(minimumScreenSize, screenOffset);
        UpdateMenusUniforms();
        UpdateButtonsUniforms();
        UpdateButtonsUniforms();
        UpdateTextsUniforms();
    }

    //OnUpdateFrame
    public static void LoadBufferAndClear()
    {
        LoadBufferAndClearWindow();
        LoadBufferAndClearWall();
        LoadBufferAndClearCeiling();
        LoadBufferAndClearFloor();
        LoadBufferAndClearSprite();
        LoadBufferAndClearMenus();
        LoadBufferAndClearButtons();
        LoadBufferAndClearTexts();
    }

    //OnRenderFrame
    public static void DrawGame(
        float wallWidth,
        Vector2 playerPosition,
        float playerAngle,
        float pitch)
    {
        int walltexCount = Textures.Walls.Count;

        DrawCeiling(walltexCount, wallWidth, playerPosition, playerAngle, pitch);
        DrawWalls(walltexCount, pitch);
        DrawFloor(walltexCount, wallWidth, playerPosition, playerAngle, pitch);
        DrawSprite();
        DrawTexts();
    }

    //OnUnload
    public static void DisposeAll()
    {
        //Dispose shaders
        WindowShader?.Dispose();
        CeilingShader?.Dispose();
        FloorShader?.Dispose();
        WallShader?.Dispose();
        SpriteShader?.Dispose();
        MenusShader?.Dispose();
        ButtonsShader?.Dispose();
        TextsShader?.Dispose();
    }
}