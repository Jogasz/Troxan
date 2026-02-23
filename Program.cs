using Engine;
using Sources;

namespace Root;

public class Program
{
    static void Main()
    {
        //Loading settings (More exception handling is needed)
        try
        {
            Settings.Load("settings.json");
            Console.WriteLine("Settings loaded!");

        }
        catch (FileNotFoundException NoFileEx)
        {
            Console.WriteLine(NoFileEx);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Settings:\n{ex}");
        }

        //Loading needed level infos, NOT LEVELS (More exception handling is needed)
        try
        {
            string storyFilePath = "assets/maps/story/info.txt";
            string customsRootDir = "assets/maps/customs";
            Level.FirstLoad(storyFilePath, customsRootDir);
        }
        catch (FileNotFoundException NoFileEx)
        {
            Console.WriteLine(NoFileEx);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Level:\n{ex}");
        }

        int levelTypeId = 1;
        int levelMapId = 0;

        Level.Load(levelTypeId, levelMapId);

        Console.WriteLine($"Choosen map type: {(levelTypeId == 0 ? "Story" : "Custom")}");
        Console.WriteLine($"Choosen map: {Level.CustomMaps[levelMapId].MapName}");

        var m = Level.CustomMaps[levelMapId];
        Console.WriteLine("================================");
        Console.WriteLine($"Folder name: '{m.FolderName}'");
        Console.WriteLine($"Folder path: '{m.FolderPath}'");
        Console.WriteLine($"Metadata path: '{m.InfosPath}'");
        Console.WriteLine($"Map Json path: '{m.MapJsonPath}'");
        Console.WriteLine($"Map Png path: '{m.MapPngPath}'");
        Console.WriteLine($"Author: '{m.Author}'");
        Console.WriteLine($"Map name: '{m.MapName}'");
        Console.WriteLine($"CreatedAt: '{m.CreatedAt}'");
        Console.WriteLine("================================");
        Console.WriteLine($"Player Starter Position: {Level.PlayerStarterPosition}");
        Console.WriteLine($"Player Starter Angle: {Level.PlayerStarterAngle}");
        Console.WriteLine($"Distance Shade: {Level.DistanceShade}");
        Console.WriteLine("Map Ceiling Array:");
        for (int i = 0; i < Level.MapCeiling.GetLength(1); i++)
        {
            Console.Write("[");
            for (int j = 0; j < Level.MapCeiling.GetLength(0); j++)
            {
                Console.Write(Level.MapCeiling[i, j]);
            }
            Console.Write("]\n");
        }
        Console.WriteLine("Map Walls Array:");
        for (int i = 0; i < Level.MapWalls.GetLength(1); i++)
        {
            Console.Write("[");
            for (int j = 0; j < Level.MapWalls.GetLength(0); j++)
            {
                Console.Write(Level.MapWalls[i, j]);
            }
            Console.Write("]\n");
        }
        Console.WriteLine("Map Floor Array:");
        for (int i = 0; i < Level.MapFloor.GetLength(1); i++)
        {
            Console.Write("[");
            for (int j = 0; j < Level.MapFloor.GetLength(0); j++)
            {
                Console.Write(Level.MapFloor[i, j]);
            }
            Console.Write("]\n");
        }
        Console.WriteLine("================================");
        Console.WriteLine("Sprites: ");
        foreach (var sp in Level.Sprites)
        {
            Console.WriteLine();
            Console.WriteLine($"Type: {sp.Type}");
            Console.WriteLine($"Id: {sp.Id}");
            Console.WriteLine($"State: {sp.State}");
            Console.WriteLine($"Position: {sp.Position}");
            if (sp.Interacted is not null) Console.WriteLine($"Interacted: {sp.Interacted}");
            if (sp.Health is not null) Console.WriteLine($"Health: {sp.Health}");
        }
        Console.WriteLine("================================");

        Engine.Engine engine = new Engine.Engine(800, 800, "ProjectRaycast");
        engine.Run();
    }
}