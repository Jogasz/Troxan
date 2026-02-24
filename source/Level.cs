using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;

namespace Sources;

internal class Level
{
    //First loading
    //Custom map metadatas / Story map's completed status
    //=========================================================================================
    internal static bool mapLoaded = false;
    internal static bool[] levelFinished = Array.Empty<bool>();

    //Custom level metadata
    internal sealed class CustomMetadatas
    {
        public required string FolderName { get; init; }
        public required string FolderPath { get; init; }
        internal string Author { get; init; } = "Unknown";
        internal string MapName { get; init; } = "Unknown";
        internal string CreatedAt { get; init; } = "Unknown";
        public required string InfosPath { get; init; }
        public required string MapJsonPath { get; init; }
        public required string MapPngPath { get; init; }
    }

    //Discovered custom maps
    internal static IReadOnlyList<CustomMetadatas> CustomMaps => _customMaps;
    static readonly List<CustomMetadatas> _customMaps = new();

    //Dto
    private sealed class CustomMetadatasDto
    {
        public string? Author { get; set; }
        public string? MapName { get; set; }
        public string? CreatedAt { get; set; }
    }

    internal static void FirstLoad(string storyFilePath, string customsRootDir)
    {
        //Story map's completed status
        //==========================================================================
        if (!File.Exists(storyFilePath))
            throw new FileNotFoundException($" - File not found: '{storyFilePath}'");

        var lines = File.ReadAllLines(storyFilePath);

        levelFinished = new bool[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            levelFinished[i] = line == "1";
        }

        //Custom map metadatas
        //==========================================================================
        if (!Directory.Exists(customsRootDir))
            throw new DirectoryNotFoundException($" - Directory not found: '{storyFilePath}'");

        _customMaps.Clear();

        //Each subdir is one custom map
        foreach (var mapDir in Directory.EnumerateDirectories(customsRootDir))
        {
            var folderName = Path.GetFileName(mapDir);

            var metaDataPath = Path.Combine(mapDir, "metadata.json");
            var mapJsonPath = Path.Combine(mapDir, "map.json");
            var mapPngPath = Path.Combine(mapDir, "map.png");

            //If a folder is incomplete, skip it
            if (!File.Exists(metaDataPath) ||
                !File.Exists(mapJsonPath) ||
                !File.Exists(mapPngPath))
                continue;

            CustomMetadatasDto? dto;
            try
            {
                dto = JsonConvert.DeserializeObject<CustomMetadatasDto>(File.ReadAllText(metaDataPath));
            }
            //Broken metadata.json
            catch
            {
                continue;
            }

            _customMaps.Add(new CustomMetadatas
            {
                FolderName = folderName,
                FolderPath = mapDir,
                InfosPath = metaDataPath,
                MapJsonPath = mapJsonPath,
                MapPngPath = mapPngPath,
                Author = dto?.Author ?? "Unknown",
                MapName = dto?.MapName ?? $"'{folderName}'",
                CreatedAt = dto?.CreatedAt ?? "Unknown"
            });
        }

        //Order maps
        _customMaps.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.MapName, b.MapName));
        //==========================================================================
    }
    //=========================================================================================

    //Level loader
    //=========================================================================================
    //Class properties
    internal static Vector2 PlayerStarterPosition { get; private set; }
    internal static int PlayerStarterAngle { get; private set; }
    internal static int DistanceShade { get; private set; }
    internal static int[,] MapWalls { get; private set; } = new int[0, 0];
    internal static int[,] MapCeiling { get; private set; } = new int[0, 0];
    internal static int[,] MapFloor { get; private set; } = new int[0, 0];
    internal static List<Sprite> Sprites { get; private set; } = new();

    //DTO - Data Transfer Object
    private sealed class LevelDto
    {
        public Vector2 PlayerStarterPosition { get; set; }
        public int PlayerStarterAngle { get; set; }
        public int DistanceShade { get; set; }
        public int[,] MapWalls { get; set; } = new int[0, 0];
        public int[,] MapCeiling { get; set; } = new int[0, 0];
        public int[,] MapFloor { get; set; } = new int[0, 0];
        public List<Sprite> Sprites { get; set; } = new();
    }

    //Sprite object
    internal class Sprite
    {
        //Needed properties
        public int Type { get; set; }
        public int Id { get; set; }
        public bool State { get; internal set; }
        public Vector2 Position { get; set; }

        //Optional properties
        public bool? Interacted { get; set; }
        public int? Health { get; set; }
    }

    //JSON reader and deserializer
    internal static void Load(int typeId, int mapId)
    {
        //Story map / type = 0
        //==========================================================================
        if (typeId == 0) return;
        //==========================================================================

        //Custom map / type = 1
        //==========================================================================
        else if (typeId == 1) LoadCustomMap(mapId);
        //==========================================================================

        //Console.WriteLine($"Choosen map type: {(typeId == 0 ? "Story" : "Custom")}");
        //Console.WriteLine($"Choosen map: {Level.CustomMaps[mapId].MapName}");

        //var m = Level.CustomMaps[mapId];
        //Console.WriteLine("================================");
        //Console.WriteLine($"Folder name: '{m.FolderName}'");
        //Console.WriteLine($"Folder path: '{m.FolderPath}'");
        //Console.WriteLine($"Metadata path: '{m.InfosPath}'");
        //Console.WriteLine($"Map Json path: '{m.MapJsonPath}'");
        //Console.WriteLine($"Map Png path: '{m.MapPngPath}'");
        //Console.WriteLine($"Author: '{m.Author}'");
        //Console.WriteLine($"Map name: '{m.MapName}'");
        //Console.WriteLine($"CreatedAt: '{m.CreatedAt}'");
        //Console.WriteLine("================================");
        //Console.WriteLine($"Player Starter Position: {Level.PlayerStarterPosition}");
        //Console.WriteLine($"Player Starter Angle: {Level.PlayerStarterAngle}");
        //Console.WriteLine($"Distance Shade: {Level.DistanceShade}");
        //Console.WriteLine("Map Ceiling Array:");
        //for (int i = 0; i < Level.MapCeiling.GetLength(1); i++)
        //{
        //    Console.Write("[");
        //    for (int j = 0; j < Level.MapCeiling.GetLength(0); j++)
        //    {
        //        Console.Write(Level.MapCeiling[i, j]);
        //    }
        //    Console.Write("]\n");
        //}
        //Console.WriteLine("Map Walls Array:");
        //for (int i = 0; i < Level.MapWalls.GetLength(1); i++)
        //{
        //    Console.Write("[");
        //    for (int j = 0; j < Level.MapWalls.GetLength(0); j++)
        //    {
        //        Console.Write(Level.MapWalls[i, j]);
        //    }
        //    Console.Write("]\n");
        //}
        //Console.WriteLine("Map Floor Array:");
        //for (int i = 0; i < Level.MapFloor.GetLength(1); i++)
        //{
        //    Console.Write("[");
        //    for (int j = 0; j < Level.MapFloor.GetLength(0); j++)
        //    {
        //        Console.Write(Level.MapFloor[i, j]);
        //    }
        //    Console.Write("]\n");
        //}
        //Console.WriteLine("================================");
        //Console.WriteLine("Sprites: ");
        //foreach (var sp in Level.Sprites)
        //{
        //    Console.WriteLine();
        //    Console.WriteLine($"Type: {sp.Type}");
        //    Console.WriteLine($"Id: {sp.Id}");
        //    Console.WriteLine($"State: {sp.State}");
        //    Console.WriteLine($"Position: {sp.Position}");
        //    if (sp.Interacted is not null) Console.WriteLine($"Interacted: {sp.Interacted}");
        //    if (sp.Health is not null) Console.WriteLine($"Health: {sp.Health}");
        //}
        //Console.WriteLine("================================");
    }

    private static void LoadCustomMap(int mapId)
    {
        //If mapId is invalid
        if (mapId < 0 || mapId > (_customMaps.Count - 1)) return;

        string filePath = _customMaps[mapId].MapJsonPath;

        if (!File.Exists(filePath))
            throw new FileNotFoundException($" - File not found: '{filePath}'");

        var dto = JsonConvert.DeserializeObject<LevelDto>(File.ReadAllText(filePath)) ??
            throw new InvalidOperationException($" - Failed to deserialize: '{filePath}'");

        PlayerStarterPosition = new Vector2(dto.PlayerStarterPosition.X, dto.PlayerStarterPosition.Y);
        PlayerStarterAngle = dto.PlayerStarterAngle;
        DistanceShade = dto.DistanceShade;
        MapCeiling = dto.MapCeiling;
        MapWalls = dto.MapWalls;
        MapFloor = dto.MapFloor;

        Sprites.Clear();
        Sprites.Capacity = dto.Sprites.Count;

        for (int i = 0; i < dto.Sprites.Count; i++)
        {
            var src = dto.Sprites[i];

            var dst = new Sprite
            {
                Type = src.Type,
                Id = src.Id,
                Position = src.Position,
                State = true
            };

            switch (dst.Type)
            {
                //Object / Item
                case 0:
                case 1:
                    dst.Interacted = false;
                    break;
                //Enemy
                case 2:
                    dst.Health = dst.Id switch
                    {
                        //Jiggler
                        0 => 50,
                        //Korvax
                        1 => 70,
                        //Default
                        _ => null
                    };
                    break;
            }

            Sprites.Add(dst);
        }
        mapLoaded = true;
    }

    public static void ClearLevelDatas()
    {
        mapLoaded = false;
    }

    //=========================================================================================
}