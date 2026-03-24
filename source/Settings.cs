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
        // Runtime values that may arrive from server
        internal static int Coins { get; set; }
        internal static int Level { get; set; }
        internal static string? Username { get; set; }
        internal static string? JoinTime { get; set; }
        internal static string? TimePlayed { get; set; }
        internal static int NumOfStoryFinished { get; set; }
        internal static int NumOfEnemiesKilled { get; set; }
        internal static int NumOfDeaths { get; set; }
        internal static int Score { get; set; }
    }

    internal static class Graphics
    {
        internal static int FOV { get; set; }
        internal static int RayCount { get; set; }
        internal static int RenderDistance { get; set; }
        internal static float DistanceShade { get; set; }
    }

    internal static class Api
    {
        internal static string? BaseUrl { get; set; }
        internal static string? Token { get; set; }
    }

    internal static class Gameplay
    {
        internal static int TileSize { get; set; }
    }

    internal static void Save(string filePath)
    {
        var root = new
        {
            Player = new
            {
                Health = Player.Health,
                Stamina = Player.Stamina,
                // store values in the original units where reasonable
                MovementSpeed = (int)(Player.MovementSpeed / 10f),
                MouseSensitivity = (int)(Player.MouseSensitivity * 1000f)
            },
            Graphics = new
            {
                FOV = Graphics.FOV,
                RayCount = Graphics.RayCount,
                RenderDistance = Graphics.RenderDistance,
                DistanceShade = (int)(Graphics.DistanceShade * 100f)
            },
            Gameplay = new
            {
                TileSize = Gameplay.TileSize
            },
            Api = new
            {
                BaseUrl = Api.BaseUrl ?? string.Empty,
                Token = Api.Token ?? string.Empty
            }
            ,
            // Runtime player values that may be updated from server
            PlayerRuntime = new
            {
                Username = Player.Username ?? string.Empty,
                Coins = Player.Coins,
                Level = Player.Level,
                JoinTime = Player.JoinTime ?? string.Empty,
                TimePlayed = Player.TimePlayed ?? string.Empty,
                NumOfStoryFinished = Player.NumOfStoryFinished,
                NumOfEnemiesKilled = Player.NumOfEnemiesKilled,
                NumOfDeaths = Player.NumOfDeaths,
                Score = Player.Score
            }
        };

        var opts = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(root, opts);
        File.WriteAllText(filePath, json);
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
        Graphics.DistanceShade = graphicsObj.GetProperty("DistanceShade").GetInt32() / 100f;

        //Optional API base URL
        if (root.TryGetProperty("Api", out var apiObj) &&
            apiObj.ValueKind == JsonValueKind.Object &&
            apiObj.TryGetProperty("BaseUrl", out var baseUrlProp) &&
            baseUrlProp.ValueKind == JsonValueKind.String)
        {
            Api.BaseUrl = baseUrlProp.GetString();
        }
        else
        {
            Api.BaseUrl = null;
        }
        // Optional persisted token
        if (root.TryGetProperty("Api", out apiObj) && apiObj.ValueKind == JsonValueKind.Object &&
            apiObj.TryGetProperty("Token", out var tokenProp) && tokenProp.ValueKind == JsonValueKind.String)
        {
            Api.Token = tokenProp.GetString();
        }

        //Loading gameplay values
        Gameplay.TileSize = gameplayObj.GetProperty("TileSize").GetInt32();

        // Optional runtime player values (may be absent)
        if (root.TryGetProperty("PlayerRuntime", out var pr) && pr.ValueKind == JsonValueKind.Object)
        {
            try
            {
                if (pr.TryGetProperty("Username", out var u) && u.ValueKind == JsonValueKind.String)
                    Player.Username = u.GetString();
                if (pr.TryGetProperty("Coins", out var c) && c.ValueKind == JsonValueKind.Number)
                    Player.Coins = c.GetInt32();
                if (pr.TryGetProperty("Level", out var l) && l.ValueKind == JsonValueKind.Number)
                    Player.Level = l.GetInt32();
                if (pr.TryGetProperty("JoinTime", out var jt) && jt.ValueKind == JsonValueKind.String)
                    Player.JoinTime = jt.GetString();
                if (pr.TryGetProperty("TimePlayed", out var tp) && tp.ValueKind == JsonValueKind.String)
                    Player.TimePlayed = tp.GetString();
                if (pr.TryGetProperty("NumOfStoryFinished", out var ns) && ns.ValueKind == JsonValueKind.Number)
                    Player.NumOfStoryFinished = ns.GetInt32();
                if (pr.TryGetProperty("NumOfEnemiesKilled", out var ne) && ne.ValueKind == JsonValueKind.Number)
                    Player.NumOfEnemiesKilled = ne.GetInt32();
                if (pr.TryGetProperty("NumOfDeaths", out var nd) && nd.ValueKind == JsonValueKind.Number)
                    Player.NumOfDeaths = nd.GetInt32();
                if (pr.TryGetProperty("Score", out var sc) && sc.ValueKind == JsonValueKind.Number)
                    Player.Score = sc.GetInt32();
            }
            catch { }
        }
    }
}