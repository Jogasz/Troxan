using System;
using System.IO;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;

namespace Sources;

internal sealed class Textures : IDisposable
{
    public int Handle { get; }

    /* 
     * Minden textúrafélének meg kellene csinálni a saját maga tárolóját a könnyebb kezelés érdekében.
     * Ebből kiindulva valahogy egy teljesen univerzális bindot kellene csinálni.
     */

    static readonly string[] wallPaths =
    {
        //Map textures
        "assets/textures/map/stonebricks.png",
        "assets/textures/map/planks.png",
        "assets/textures/map/mossy_planks.png",
        "assets/textures/map/door.png",
        "assets/textures/map/window.png"
    };

    static readonly string[] spritePaths =
    {
        //Objects atlas
        "assets/textures/map/sprites/objects_atlas.png",
        //Ietms atlas
        "assets/textures/map/sprites/items_atlas.png",
        //Enemies atlas
        "assets/textures/map/sprites/enemies_atlas.png",
        //Projectiles
        "assets/textures/map/sprites/projectiles_atlas.png"
    };

    static readonly string[] containerPaths =
    {
        //Main menu
        "assets/textures/gui/containers/main.png",
        //Campaign menu
        "assets/textures/gui/containers/campaign.png",
        //Customs menu
        "assets/textures/gui/containers/customs.png",
        //Settings menu
        "assets/textures/gui/containers/settings.png",
        //Statistics menu
        "assets/textures/gui/containers/statistics.png",
        //Pause menu
        "assets/textures/gui/containers/pause.png",
        //Level completed menu
        //"assets/textures/gui/containers/mainmenu.png",
    };

    //static readonly string[] HUDPaths =
    //{

    //};

    static readonly string[] buttonPaths =
    {
        "assets/textures/gui/buttons_atlas.png"
    };

    static readonly string[] fontPaths =
    {
        "assets/textures/gui/web_ibm_mda_atlas.png"
    };

    //Texture lists
    public static List<Textures> Walls = new();
    public static List<Textures> Sprites = new();
    public static List<Textures> Containers = new();
    //public static List<Textures> HUD = new();
    public static List<Textures> Buttons = new();
    public static List<Textures> Fonts = new();

    //Map textures (R32i)
    public static int MapCeilingTex { get; private set; }
    public static int MapFloorTex { get; private set; }
    public static int MapWallsTex { get; private set; }
    public static Vector2i MapSize { get; private set; }

    /// <summary>
    /// Loads non-map textures (atlases, UI, wall/sprite sheets). Call this once on startup.
    /// </summary>
    public static void LoadStatic()
    {
        if (Walls.Count !=0) Walls.Clear();
        if (Sprites.Count !=0) Sprites.Clear();
        if (Containers.Count !=0) Containers.Clear();
        //if (HUD.Count !=0) HUD.Clear();
        if (Buttons.Count !=0) Buttons.Clear();
        if (Fonts.Count !=0) Fonts.Clear();

        LoadInto(Walls, wallPaths);
        LoadInto(Sprites, spritePaths);
        LoadInto(Containers, containerPaths);
        //LoadInto(HUD, hudPaths);
        LoadInto(Buttons, buttonPaths);
        LoadInto(Fonts, fontPaths);
    }

    /// <summary>
    /// Loads/updates the map integer textures (R32i). Call this after a map was selected.
    /// </summary>
    public static void LoadMapTextures(int[,] mapWalls, int[,] mapCeiling, int[,] mapFloor)
    {
        // Reject zero-sized maps to avoid undefined GL state.
        if (mapWalls.GetLength(0) ==0 || mapWalls.GetLength(1) ==0)
            throw new ArgumentException("mapWalls is empty");
        if (mapCeiling.GetLength(0) ==0 || mapCeiling.GetLength(1) ==0)
            throw new ArgumentException("mapCeiling is empty");
        if (mapFloor.GetLength(0) ==0 || mapFloor.GetLength(1) ==0)
            throw new ArgumentException("mapFloor is empty");

        if (MapCeilingTex !=0) GL.DeleteTexture(MapCeilingTex);
        if (MapFloorTex !=0) GL.DeleteTexture(MapFloorTex);
        if (MapWallsTex !=0) GL.DeleteTexture(MapWallsTex);

        MapCeilingTex = CreateMapTexture(mapCeiling);
        MapWallsTex = CreateMapTexture(mapWalls);
        MapFloorTex = CreateMapTexture(mapFloor);

        MapSize = (mapWalls.GetLength(1), mapWalls.GetLength(0));
    }

    // Backwards-compatible: keep old API but delegate.
    public static void LoadAll(int[,] mapWalls, int[,] mapCeiling, int[,] mapFloor)
    {
        LoadStatic();
        LoadMapTextures(mapWalls, mapCeiling, mapFloor);
    }

    static void LoadInto(List<Textures> texList, IReadOnlyList<string> paths)
    {
        for (int i =0; i < paths.Count; i++)
            texList.Add(new Textures(paths[i]));
    }

    static int CreateMapTexture(int[,] map)
    {
        Vector2i size = (map.GetLength(1), map.GetLength(0));
        int[] data = new int[size.X * size.Y];

        for (int y =0; y < size.Y; y++)
            for (int x =0; x < size.X; x++)
                data[y * size.X + x] = map[y, x];

        int handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, handle);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.TexImage2D(
            TextureTarget.Texture2D,
            level:0,
            internalformat: PixelInternalFormat.R32i,
            width: size.X,
            height: size.Y,
            border:0,
            format: PixelFormat.RedInteger,
            type: PixelType.Int,
            pixels: data);

        GL.BindTexture(TextureTarget.Texture2D,0);
        return handle;
    }

    public static void BindTex(List<Textures> texList, int texIndex, TextureUnit unit)
    {
        if (texIndex <0 || texIndex >= texList.Count)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D,0);
            return;
        }

        Textures texture = texList[texIndex];
        if (texture is null)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D,0);
            return;
        }

        texture.Use(unit);
    }

    public Textures(string path)
    {
        Handle = GL.GenTexture();
        Use();

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        StbImage.stbi_set_flip_vertically_on_load(1);

        using var stream = File.OpenRead(path);
        ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        GL.TexImage2D(
            TextureTarget.Texture2D,
            level:0,
            internalformat: PixelInternalFormat.Rgba,
            width: image.Width,
            height: image.Height,
            border:0,
            format: PixelFormat.Rgba,
            type: PixelType.UnsignedByte,
            pixels: image.Data);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }

    public void Use(TextureUnit unit = TextureUnit.Texture0)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, Handle);
    }

    public void Dispose()
    {
        if (Handle !=0)
            GL.DeleteTexture(Handle);

        GC.SuppressFinalize(this);
    }
}