using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Sources;
internal class Settings
{
    internal static class Player
    {
        internal static int Health { get; set; }
        internal static int Stamina { get; set; }
        internal static float MovementSpeed { get; set; }
        internal static float MouseSensitivity { get; set; }
    }

    internal static class Graphics
    {
        internal static int FOV { get; set; }
        internal static int RayCount { get; set; }
        internal static int RenderDistance { get; set; }
        internal static float DistanceShade { get; set; }
    }

    internal static class Gameplay
    {
        internal static int TileSize { get; set; }
    }

    internal static void Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($" - File Not found: '{filePath}'");

        //Reading JSON file's text and parsing it into a JSON document
        //JsonDocument is IDisposable, so "using" will auto-dispose it at the end of this method
        using var doc = JsonDocument.Parse(File.ReadAllText(filePath));

        //Outer object of the JSON document
        var root = doc.RootElement;

        //Extracting objects from root
        var playerObj = root.GetProperty("Player");
        var graphicsObj = root.GetProperty("Graphics");
        var gameplayObj = root.GetProperty("Gameplay");

        //Loading player values
        Player.Health = playerObj.GetProperty("Health").GetInt32();
        Player.Stamina = playerObj.GetProperty("Stamina").GetInt32();
        Player.MovementSpeed = playerObj.GetProperty("MovementSpeed").GetInt32() * 10f;
        Player.MouseSensitivity = playerObj.GetProperty("MouseSensitivity").GetInt32() / 1000f;

        //Loading graphics values
        Graphics.FOV = graphicsObj.GetProperty("FOV").GetInt32();
        Graphics.RayCount = graphicsObj.GetProperty("RayCount").GetInt32();
        Graphics.RenderDistance = graphicsObj.GetProperty("RenderDistance").GetInt32();
        Graphics.DistanceShade = graphicsObj.GetProperty("DistanceShade").GetInt32() / 10f;

        //Loading gameplay values
        Gameplay.TileSize = gameplayObj.GetProperty("TileSize").GetInt32();
    }
}