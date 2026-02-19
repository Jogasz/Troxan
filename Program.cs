using System;
using System.Threading;

namespace Engine;

public class Program
{
    static void Main()
    {
        string filePath;

        //Loading settings (More exception handling is needed)
        try
        {
            filePath = "settings.json";
            Settings.Load(filePath);
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
            filePath = "assets/maps/story/info.txt";
            Level.FirstLoad(filePath);
        }
        catch (FileNotFoundException NoFileEx)
        {
            Console.WriteLine(NoFileEx);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Level:\n{ex}");
        }

        Level.Load(0, 1);

        //===========================================================================================================

        Console.WriteLine("===============");
        Console.WriteLine($"Player starter position: {Level.PlayerStarterPosition}");
        Console.WriteLine($"Player starter angle: {Level.PlayerStarterAngle}");
        Console.WriteLine("===============");

        Console.WriteLine();

        Console.WriteLine("===============");
        Console.WriteLine("MapCeiling:");
        for (int i = 0; i < Level.MapCeiling.GetLength(1); i++)
        {
            Console.Write("[");
            for (int j = 0; j < Level.MapCeiling.GetLength(0); j++)
            {
                Console.Write($"{Level.MapCeiling[i, j]}, ");
            }
            Console.WriteLine("],");
        }
        Console.WriteLine("===============");

        Console.WriteLine();

        Console.WriteLine("===============");
        Console.WriteLine("MapWalls:");
        for (int i = 0; i < Level.MapWalls.GetLength(1); i++)
        {
            Console.Write("[");
            for (int j = 0; j < Level.MapWalls.GetLength(0); j++)
            {
                Console.Write($"{Level.MapWalls[i, j]}, ");
            }
            Console.WriteLine("],");
        }
        Console.WriteLine("===============");

        Console.WriteLine();

        Console.WriteLine("===============");
        Console.WriteLine("MapFloor:");
        for (int i = 0; i < Level.MapFloor.GetLength(1); i++)
        {
            Console.Write("[");
            for (int j = 0; j < Level.MapFloor.GetLength(0); j++)
            {
                Console.Write($"{Level.MapFloor[i, j]}, ");
            }
            Console.WriteLine("],");
        }
        Console.WriteLine("===============");

        Console.WriteLine();

        if (Level.Sprites.Count > 0)
        {
            foreach (var sprite in Level.Sprites)
            {
                Console.WriteLine("===============");
                Console.WriteLine($"Type: {sprite.Type}");
                Console.WriteLine($"Id: {sprite.Id}");
                Console.WriteLine($"State: {sprite.State}");
                Console.WriteLine($"Position: {sprite.Position}");
                if (sprite.Interacted is not null) Console.WriteLine($"Interacted: {sprite.Interacted?.ToString()}");
                if (sprite.Health is not null) Console.WriteLine($"Health: {sprite.Health?.ToString()}");
                Console.WriteLine("===============");
            }
        }

        //===========================================================================================================

        //try
        //{
        //    Level map = new Level();
        //    map.Load();
        //    Console.WriteLine(" - MAP has been loaded!");
        //}
        //catch (FileNotFoundException noFileEx)
        //{
        //    Console.WriteLine(noFileEx);
        //}
        //catch (InvalidOperationException invOpEx)
        //{
        //    Console.WriteLine(invOpEx);
        //}
        //catch (Exception e)
        //{
        //    Console.WriteLine($"Map: Something went wrong...\n - {e}");
        //}

        //Engine.Engine engine = new Engine.Engine(800, 800, "ProjectRaycast");
        //engine.Run();
    }
}