using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;

namespace Engine;

internal class Level
{
    internal static bool[] levelFinished = Array.Empty<bool>();
    internal static void FirstLoad(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($" - File not found: '{filePath}'");

        var lines = File.ReadAllLines(filePath);

        levelFinished = new bool[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            levelFinished[i] = line == "1";

            string temp = levelFinished[i] ? "Finished" : "Not Finished";

            Console.WriteLine($"{i + 1}. Level: {temp}, '{Convert.ToString(levelFinished[i])}'");
        }
    }

    internal static int[,] MapWalls { get; private set; } = new int[0, 0];
    internal static int[,] MapCeiling { get; private set; } = new int[0, 0];
    internal static int[,] MapFloor { get; private set; } = new int[0, 0];
    internal static Vector2 PlayerStarterPosition { get; private set; }
    internal static int PlayerStarterAngle { get; private set; }
    internal static List<Sprite> Sprites { get; private set; } = new();

    internal class Sprite
    {
        //Needed properties
        public int Type { get; set; }
        public int Id { get; set; }
        public bool State { get; internal set; }
        public Vector2 Position { get; set; }

        //Optional properties
        public bool? Interacted { get; internal set; }
        public int? Health { get; internal set; }
    }

    internal class Datas
    {
        internal static string Author { get; private set; } = "Unknown";
        internal static string Name { get; private set; } = "Unknown";
        internal static string Created { get; private set; } = "Unknown";
    }

    //typeId: Story=0, Custom=1
    //mapId: choosen map's number
    internal static void Load(int typeId, int mapId)
    {
        if (typeId != 0) return;

        string filePath = $"assets/maps/story/{mapId}.json";

        if (!File.Exists(filePath))
            throw new FileNotFoundException($" - File not found: '{filePath}'");

        var dto = JsonConvert.DeserializeObject<LevelDto>(File.ReadAllText(filePath)) ??
            throw new InvalidOperationException($" - Failed to deserialize: '{filePath}'");

        PlayerStarterPosition = new Vector2(dto.PlayerStarterPosition.X, dto.PlayerStarterPosition.Y);
        PlayerStarterAngle = dto.PlayerStarterAngle;
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
    }

    private sealed class LevelDto
    {
        public int[,] MapWalls { get; set; } = new int[0, 0];
        public int[,] MapCeiling { get; set; } = new int[0, 0];
        public int[,] MapFloor { get; set; } = new int[0, 0];
        public Vector2 PlayerStarterPosition { get; set; }
        public int PlayerStarterAngle { get; set; }
        public List<Sprite> Sprites { get; set; } = new();
    }
}