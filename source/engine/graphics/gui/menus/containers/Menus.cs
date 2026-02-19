using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics;

namespace Engine;

internal partial class Engine
{
    /* Menu background ID translator
     * 0. Main Menu
     * 1. Pause Menu
     * 2. Statistics Menu
     * 3. Settings Menu
     */

    /* Buttons ID Translator
     * 0. Contiune
     * 1. New Game
     * 2. Play
     * 3. Settings
     * 4. Statistics
     * 5. Exit
     * 6. Back to Game
     */

    //Handling main menu

    internal static int[] buttonIds;
    void MainMenu()
    {
        if (isSaveState) buttonIds = new int[] {0,1,3,4,5 };
        else buttonIds = new int[] {2,3,4,5 };

        UploadMenus(0);
        UploadButtons(buttonIds);
    }

    void PauseMenu()
    {
        buttonIds = new int[] {6,5 };

        UploadMenus(1);
        UploadButtons(buttonIds);
    }

    void StatisticsMenu()
    {
        buttonIds = new int[] {5 };

        UploadMenus(2);
        UploadButtons(buttonIds);

        Vector3 aliasFontColor = (
                232f / 255f,
                225f / 255f,
                218f / 255f);

        Vector3 statFontColor = (
            96f / 255f,
            89f / 255f,
            82f / 255f);

        //Statistics
        //Alias
        DrawText(
            $"Recskas",
            screenHorizontalOffset + (minimumScreenSize / 2.72f),
            screenVerticalOffset + (minimumScreenSize / 1.49f),
            2f,
            new Vector3(aliasFontColor)
        );

        //Joined
        DrawText(
            $"Joined:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 1.83f),
            1.5f,
            new Vector3(statFontColor)
        );

        //Joined value
        DrawText(
            $"26-03-17",
            screenHorizontalOffset + (minimumScreenSize / 1.65f),
            screenVerticalOffset + (minimumScreenSize / 1.87f),
            1f,
            new Vector3(statFontColor)
        );

        //Time played
        DrawText(
            $"Time played:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 2.1f),
            1.5f,
            new Vector3(statFontColor)
        );

        //Time played value
        DrawText(
            $"2h 34m",
            screenHorizontalOffset + (minimumScreenSize / 1.65f),
            screenVerticalOffset + (minimumScreenSize / 2.16f),
            1f,
            new Vector3(statFontColor)
        );

        //Story done
        DrawText(
            $"Story done:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 2.46f),
            1.5f,
            new Vector3(statFontColor)
        );

        //Story done value
        DrawText(
            $"True",
            screenHorizontalOffset + (minimumScreenSize / 1.65f),
            screenVerticalOffset + (minimumScreenSize / 2.55f),
            1f,
            new Vector3(statFontColor)
        );

        //Enemy kill
        DrawText(
            $"Enemy kill:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 2.97f),
            1.5f,
            new Vector3(statFontColor)
        );

        //Enemy kill value
        DrawText(
            $"120",
            screenHorizontalOffset + (minimumScreenSize / 1.65f),
            screenVerticalOffset + (minimumScreenSize / 3.1f),
            1f,
            new Vector3(statFontColor)
        );

        //Deaths
        DrawText(
            $"Deaths:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 3.75f),
            1.5f,
            new Vector3(statFontColor)
        );

        //Deaths value
        DrawText(
            $"2",
            screenHorizontalOffset + (minimumScreenSize / 1.65f),
            screenVerticalOffset + (minimumScreenSize / 3.9f),
            1f,
            new Vector3(statFontColor)
        );

        //Score
        DrawText(
            $"Score:",
            screenHorizontalOffset + (minimumScreenSize / 5f),
            screenVerticalOffset + (minimumScreenSize / 5.13f),
            1.5f,
            new Vector3(statFontColor)
        );

        //Score value
        DrawText(
            $"1230",
            screenHorizontalOffset + (minimumScreenSize / 1.65f),
            screenVerticalOffset + (minimumScreenSize / 5.45f),
            1f,
            new Vector3(statFontColor)
        );
    }

    void SettingsMenu()
    {
        buttonIds = new int[] { 5 };

        UploadMenus(3);
        UploadButtons(buttonIds);
    }

    void UploadMenus(int backgroundIndex)
    {
        // Provide aPos as x1, x2, yTop, yBottom (y1,y2) to match vertex shader
        ShaderHandler.MenusVertexAttribList.AddRange(new float[]
        {
            screenHorizontalOffset,
            screenHorizontalOffset + minimumScreenSize,
            screenVerticalOffset + minimumScreenSize, // top
            screenVerticalOffset, // bottom
            backgroundIndex
        });
    }
}
