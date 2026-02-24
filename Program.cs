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

        Engine.Engine engine = new Engine.Engine(800, 800, "Troxan");
        engine.Run();
    }
}