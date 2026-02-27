using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using static Sources.Level;

namespace Sources;

internal class Level
{
    //First loading:
    // - Which story levels are finished
    // - Rooms for the story maps
    // - Custom map metadatas for pre-load info display
    //=========================================================================================
        //Sprite template
    internal sealed class SpriteTemplate
    {
        //Required properties
        public int Type { get; set; }
        public int Id { get; set; }
        public bool State { get; internal set; }
        public Vector2 Position { get; set; }

        //Optional properties
        public bool? Interacted { get; set; }
        public int? Health { get; set; }
    }

        //Story rooms template
    internal class StoryRoomTemplate
    {
        public required string FilePath { get; init; }
        public required int[,] RoomWalls { get; set; } = new int[0, 0];
        public List<SpriteTemplate>? Sprites { get; set; } = new();
    }

        //Custom map metadatas template
    internal sealed class CustomMetaDataTemplate
    {
        public required string FolderName { get; init; }
        public required string FolderPath { get; init; }
        public required string Author { get; init; }
        public required string MapName { get; init; }
        public required string CreatedAt { get; init; }
        public required string InfosPath { get; init; }
        public required string MapJsonPath { get; init; }
        public required string MapPngPath { get; init; }
    }

        //Bool to determine if the map has been loaded, if yes, it can be drawn
    internal static bool mapLoaded = false;
        //Array of bools to check which story maps has been completed
    internal static bool[] levelFinished = Array.Empty<bool>();
        
        //List of StoryRooms to contain loaded rooms
    internal static readonly List<StoryRoomTemplate> StoryRooms = new();
        //Directory where the room files can be found
    internal static readonly string roomsDir = "assets/maps/story/rooms";
        //To set the ID's to rooms, each path is given in sequence
    internal static readonly string[] roomPaths =
    {
        $"{roomsDir}/spawn.json"
        //$"{roomsDir}/enemy_easy_1.json",
        //$"{roomsDir}/enemy_easy_2.json",
        //$"{roomsDir}/enemy_easy_3.json",
        //$"{roomsDir}/enemy_medium_1.json",
        //$"{roomsDir}/enemy_medium_2.json",
        //$"{roomsDir}/enemy_medium_3.json",
        //$"{roomsDir}/enemy_hard_1.json",
        //$"{roomsDir}/enemy_hard_2.json",
        //$"{roomsDir}/enemy_hard_3.json",
        //$"{roomsDir}/heal.json",
        //$"{roomsDir}/tresaure_1.json",
        //$"{roomsDir}/tresaure_2.json",
        //$"{roomsDir}/tresaure_3.json",
        //$"{roomsDir}/boss.json",
        //$"{roomsDir}/portal.json"
    };

        //List of custom map metadatas to contain pre-loaded infos for display of custom maps before loading actual map
    internal static readonly List<CustomMetaDataTemplate> CustomMetaDatas = new();

        //Method for the first essential loading
    internal static void FirstLoad(string storyFilePath, string customsRootDir)
    {
        FirstLoadStory(storyFilePath);
        FirstLoadCustoms(customsRootDir);
    }

    static void FirstLoadStory(string storyFilePath)
    {
        //Story maps completed status
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
        //==========================================================================

        //Story map rooms
        //==========================================================================
        for (int i = 0; i < roomPaths.Count(); i++)
        {
            string filePath = roomPaths[i];

            if (!File.Exists(filePath))
                throw new FileNotFoundException($" - File not found: '{filePath}'");

            var obj = JsonConvert.DeserializeObject<StoryRoomTemplate>(File.ReadAllText(filePath)) ??
                throw new InvalidOperationException($" - Failed to deserialize: '{filePath}'");

            if (obj.RoomWalls.GetLength(0) != 11 || obj.RoomWalls.GetLength(1) != 11) throw new InvalidOperationException($" - Room must be a size of 11x11!: '{filePath}'");

            int[,] roomWalls = obj.RoomWalls ?? throw new InvalidOperationException($" - Deserialized RoomWalls is null in: '{filePath}'");

            List<SpriteTemplate> sprites = new();

            foreach (var sprite in obj.Sprites ?? Enumerable.Empty<SpriteTemplate>())
            {
                    //Temporary destination variable to later load it into Sprites
                var dst = new SpriteTemplate
                {
                    Type = sprite.Type,
                    Id = sprite.Id,
                    Position = sprite.Position,
                    State = true
                };

                    //Adding optional properties
                switch (sprite.Type)
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
                sprites.Add(dst);
            }

            StoryRooms.Add(new StoryRoomTemplate
            {
                FilePath = filePath,
                RoomWalls = roomWalls,
                Sprites = sprites
            });

            Console.WriteLine($" - {filePath}:");
            Console.WriteLine("RoomWalls");
            for (int y = 0; y < StoryRooms[i].RoomWalls.GetLength(0); y++)
            {
                for (int x = 0; x < StoryRooms[i].RoomWalls.GetLength(1); x++)
                {
                    Console.Write(StoryRooms[i].RoomWalls[y, x]);
                }
                Console.WriteLine();
            }
            Console.WriteLine("Sprites");
            foreach (var sprite in StoryRooms[i].Sprites ?? Enumerable.Empty<SpriteTemplate>())
            {
                Console.WriteLine("=============");
                Console.WriteLine($"Type: {sprite.Type}");
                Console.WriteLine($"Id: {sprite.Id}");
                Console.WriteLine($"Position: {sprite.Position}");
                Console.WriteLine($"State: {sprite.State}");
                if (sprite.Interacted is not null) Console.WriteLine($"Interacted: {sprite.Interacted}");
                if (sprite.Health is not null) Console.WriteLine($"Health: {sprite.Health}");
                Console.WriteLine("=============");
            }

        }
        //==========================================================================
    }

    static void FirstLoadCustoms(string customsRootDir)
    {
        //Custom map metadatas
        //==========================================================================
        if (!Directory.Exists(customsRootDir))
            throw new DirectoryNotFoundException($" - Directory not found: '{customsRootDir}'");

        CustomMetaDatas.Clear();

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

            CustomMetaDataTemplate? obj;
            try
            {
                obj = JsonConvert.DeserializeObject<CustomMetaDataTemplate>(File.ReadAllText(metaDataPath));
            }
            //Broken metadata.json
            catch
            {
                continue;
            }

            CustomMetaDatas.Add(new CustomMetaDataTemplate
            {
                FolderName = folderName,
                FolderPath = mapDir,
                InfosPath = metaDataPath,
                MapJsonPath = mapJsonPath,
                MapPngPath = mapPngPath,
                Author = obj?.Author ?? "Unknown",
                MapName = obj?.MapName ?? $"'{folderName}'",
                CreatedAt = obj?.CreatedAt ?? "Unknown"
            });
        }

        //Order maps
        CustomMetaDatas.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.MapName, b.MapName));
        //==========================================================================
    }
    //=========================================================================================

    //Level loading:
    //Takes two arguments:
    // - typeId: 0 = story | 1 = custom
    // - mapId
    //Loading:
    // - Story map: Loads the given story map, then constructs a usable map for Engine from the given room Ids
    // - Custom map: Loads the given custom map
    //=========================================================================================
        //Data Transfer Object for loaded custom map
    private sealed class CustomMapDto
    {
        public Vector2 PlayerStarterPosition { get; set; }
        public int PlayerStarterAngle { get; set; }
        public int DistanceShade { get; set; }
        public int[,] MapWalls { get; set; } = new int[0, 0];
        public int[,] MapCeiling { get; set; } = new int[0, 0];
        public int[,] MapFloor { get; set; } = new int[0, 0];
        public List<SpriteTemplate> Sprites { get; set; } = new();
    }

    internal static Vector2 PlayerStarterPosition { get; private set; }
    internal static int PlayerStarterAngle { get; private set; }
    internal static int DistanceShade { get; private set; }
    internal static int[,] MapWalls { get; private set; } = new int[0, 0];
    internal static int[,] MapCeiling { get; private set; } = new int[0, 0];
    internal static int[,] MapFloor { get; private set; } = new int[0, 0];
        //A list of Sprites based on SpiteTemplates that will contain the current loaded map's sprites
    internal static List<SpriteTemplate> Sprites { get; private set; } = new();

        //Level loader
    internal static void Load(int typeId, int mapId)
    {
        //Story map
        //==========================================================================
        if (typeId == 0) LoadStoryMap(mapId);
        //==========================================================================

        //Custom map
        //==========================================================================
        else if (typeId == 1) LoadCustomMap(mapId);
        //==========================================================================
    }

    static void LoadStoryMap(int mapId)
    {
        string filePath = $"assets/maps/story/{mapId}.json";

            //If file doesn't exist
        if (!File.Exists(filePath))
            throw new FileNotFoundException($" - File not found: '{filePath}'");

            //Deserialize
        int[,] roomsIdArray = JsonConvert.DeserializeObject<int[,]>(File.ReadAllText(filePath)) ??
            throw new InvalidOperationException($" - Failed to deserialize: {filePath}");
    }

    static void LoadCustomMap(int mapId)
    {
            //If mapId is invalid
        if (mapId < 0 || mapId > (CustomMetaDatas.Count - 1)) return;

        string filePath = CustomMetaDatas[mapId].MapJsonPath;

            //If map file doesn't exist
        if (!File.Exists(filePath))
            throw new FileNotFoundException($" - File not found: '{filePath}'");

            //Deserializing JSON to a Data Transfer Object
        var dto = JsonConvert.DeserializeObject<CustomMapDto>(File.ReadAllText(filePath)) ??
            throw new InvalidOperationException($" - Failed to deserialize: '{filePath}'");

            //Connecting the runtime properties to DTO properties
        PlayerStarterPosition = new Vector2(dto.PlayerStarterPosition.X, dto.PlayerStarterPosition.Y);
        PlayerStarterAngle = dto.PlayerStarterAngle;
        DistanceShade = dto.DistanceShade;
        MapCeiling = dto.MapCeiling;
        MapWalls = dto.MapWalls;
        MapFloor = dto.MapFloor;

            //Clearing sprite list before using
        Sprites.Clear();

            //Declaring sprite list's size
        Sprites.Capacity = dto.Sprites.Count;

        foreach (var sprite in dto.Sprites)
        {
                //Temporary destination variable to later load it into Sprites
            var dst = new SpriteTemplate
            {
                Type = sprite.Type,
                Id = sprite.Id,
                Position = sprite.Position,
                State = true
            };

                //Adding optional properties
            switch (sprite.Type)
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
            //If there was no problem loading map, we set the bool to true
        mapLoaded = true;
    }
    //=========================================================================================

    //WE DONT NEED IT
    public static void ClearLevelDatas()
    {
        mapLoaded = false;
    }
}