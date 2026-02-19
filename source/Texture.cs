using System;
using System.IO;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;

internal sealed class Texture : IDisposable
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

    };

    static readonly string[] HUDPaths =
    {

    };

    static readonly string[] buttonsPath =
    {

    };

    static readonly string[] fontPath =
    {

    };

    public static List<Texture?> textures = new();
    public static List<Texture?> images = new();
    public static List<Texture?> sprites = new();

    // Map textures (R32i)
    public static int mapCeilingTex { get; private set; }
    public static int mapFloorTex { get; private set; }
    public static int mapWallsTex { get; private set; }
    public static Vector2i mapSize { get; private set; }

    public static void LoadAll(int[,] mapWalls, int[,] mapCeiling, int[,] mapFloor)
    {
        // If LoadAll can be called more than once, make sure we release the old GPU resources.
        if (mapCeilingTex !=0) GL.DeleteTexture(mapCeilingTex);
        if (mapFloorTex !=0) GL.DeleteTexture(mapFloorTex);
        if (mapWallsTex !=0) GL.DeleteTexture(mapWallsTex);

        textures.Clear();
        images.Clear();
        sprites.Clear();

        try
        {
            mapCeilingTex = CreateMapTexture(mapCeiling);
            mapFloorTex = CreateMapTexture(mapFloor);
            mapWallsTex = CreateMapTexture(mapWalls);

            mapSize = (mapWalls.GetLength(1), mapWalls.GetLength(0));

            //LoadInto(textures, texturePaths);
            //LoadInto(images, imagePaths);
            //LoadInto(sprites, spritePaths);

            // Ensure the font atlas (last texture in textures list) does not repeat UVs.
            // The font atlas is appended as the last entry in texturePaths above.
            if (textures.Count >0)
            {
                int fontIdx = textures.Count -1;
                Texture? fontTex = textures[fontIdx];
                if (fontTex != null)
                {
                    GL.BindTexture(TextureTarget.Texture2D, fontTex.Handle);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                    GL.BindTexture(TextureTarget.Texture2D,0);
                }
            }

            Console.WriteLine(" - TEXTURES have been loaded!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" - Something went wrong while loading TEXTURES...\n{ex}");
        }
    }

    static void LoadInto(List<Texture?> target, IReadOnlyList<string> paths)
    {
        for (int i =0; i < paths.Count; i++)
            target.Add(new Texture(paths[i]));
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

    static void BindFrom(List<Texture?> list, int index, TextureUnit unit)
    {
        if (index <0 || index >= list.Count)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D,0);
            return;
        }

        Texture? texture = list[index];
        if (texture is null)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D,0);
            return;
        }

        texture.Use(unit);
    }

    public static void BindTexture(int textureIndex, TextureUnit unit = TextureUnit.Texture0) => BindFrom(textures, textureIndex, unit);

    public static void BindImage(int imageIndex, TextureUnit unit = TextureUnit.Texture0) => BindFrom(images, imageIndex, unit);

    public static void BindSprite(int spriteIndex, TextureUnit unit = TextureUnit.Texture0) => BindFrom(sprites, spriteIndex, unit);

    public static void Bind(int textureIndex, TextureUnit unit = TextureUnit.Texture0) => BindTexture(textureIndex, unit);

    public Texture(string path)
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