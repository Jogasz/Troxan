using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using System.Text;
using System.Threading.Tasks;

using Sources;

namespace Engine;

internal partial class RayCasting
{
    public static void Run(
        Vector2i ClientSize,
        int FOV,
        int rayCount,
        int tileSize,
        float distanceShade,
        float minimumScreenSize,
        float screenHorizontalOffset,
        float screenVerticalOffset,
        float playerAngle,
        Vector2 playerPosition,
        float pitch,
        int[,] mapWalls,
        int[,] mapFloor,
        int[,] mapCeiling,
        int renderDistance)
    {
        float debugBorder = 0f;

        //FOV calculation
        float FOVStart = -((float)(FOV * (Math.PI / 180f)) / 2);
        float radBetweenRays = ((float)(FOV * (Math.PI / 180f)) / (rayCount - 1));
        float wallWidth = (float)minimumScreenSize / rayCount;

        float rayAngle = Utils.NormalizeAngle(playerAngle + FOVStart);

        for (int i = 0; i < rayCount; i++)
        {
            //Vertical wall check variables
            float offsetX = 0f,
                  offsetY = 0f,
                  verticalRayHitX = 0f,
                  verticalRayHitY = 0f,
                  verticalHypotenuse = 0f;

            int verticalRayCheckingCol,
                verticalRayCheckingRow;

            bool verticalWallFound = false;

            //Pre-defining variables for vertical wall checking
            //If player is looking right
            if (rayAngle > MathX.Quadrant3 || rayAngle < MathX.Quadrant1)
            {
                verticalRayHitX =
                    MathX.Clamp
                    (
                        (float)Math.Floor(playerPosition.X / tileSize) * tileSize + tileSize,
                        0,
                        (float)(mapWalls.GetLength(1) * tileSize - 0.0001f)
                    );
                verticalRayHitY =
                    MathX.Clamp
                    (
                        playerPosition.Y - ((tileSize - (playerPosition.X % tileSize)) * (float)Math.Tan(MathX.Quadrant4 - rayAngle)),
                        0,
                        (float)(mapWalls.GetLength(0) * tileSize - 0.0001f)
                    );
                offsetX = tileSize;
                offsetY = -(offsetX * (float)Math.Tan(MathX.Quadrant4 - rayAngle));
            }
            //If player is looking left
            else if (rayAngle < MathX.Quadrant3 && rayAngle > MathX.Quadrant1)
            {
                verticalRayHitX =
                    MathX.Clamp
                    (
                        (float)Math.Floor(playerPosition.X / tileSize) * tileSize - 0.0001f,
                        0,
                        (float)(mapWalls.GetLength(1) * tileSize - 0.0001f)
                    );
                verticalRayHitY =
                    MathX.Clamp
                    (
                        playerPosition.Y - ((playerPosition.X % tileSize) * (float)Math.Tan(rayAngle)),
                        0,
                        (float)(mapWalls.GetLength(0) * tileSize - 0.0001f)
                    );
                offsetX = -tileSize;
                offsetY = offsetX * (float)Math.Tan(rayAngle);
            }

            //Grid checker
            verticalRayCheckingCol = (int)Math.Floor(verticalRayHitX / tileSize);
            verticalRayCheckingRow = (int)Math.Floor(verticalRayHitY / tileSize);

            //Setting iterator
            int renderDistanceIterator = 0;

            //Cycle to find a wall
            while
            (
                renderDistanceIterator < renderDistance &&
                !verticalWallFound &&
                verticalRayHitX != (float)(mapWalls.GetLength(1) * tileSize - 0.0001f) &&
                verticalRayHitX != 0 &&
                verticalRayHitY != (float)(mapWalls.GetLength(0) * tileSize - 0.0001f) &&
                verticalRayHitY != 0
            )
            {
                //If wall is found
                if (mapWalls[verticalRayCheckingRow, verticalRayCheckingCol] > 0)
                {
                    renderDistanceIterator = renderDistance;
                    verticalWallFound = true;
                }
                //If wall isn't found
                else
                {
                    verticalRayHitX =
                        MathX.Clamp
                        (
                            verticalRayHitX + offsetX,
                            0,
                            (float)(mapWalls.GetLength(1) * tileSize - 0.0001f)
                        );
                    verticalRayHitY =
                        MathX.Clamp
                        (
                            verticalRayHitY + offsetY,
                            0,
                            (float)(mapWalls.GetLength(0) * tileSize - 0.0001f)
                        );
                    renderDistanceIterator++;
                    verticalRayCheckingCol = (int)(verticalRayHitX / tileSize);
                    verticalRayCheckingRow = (int)(verticalRayHitY / tileSize);
                }
            }

            //Vertical hypotenuse for line's length
            verticalHypotenuse = (float)Math.Sqrt(Math.Pow(playerPosition.X - verticalRayHitX, 2) + Math.Pow(playerPosition.Y - verticalRayHitY, 2));

            //Horizontal wall check variables
            float horizontalRayHitX = 0f,
                  horizontalRayHitY = 0f,
                  horizontalHypotenuse = 0f;

            int horizontalRayCheckingCol = 0,
                horizontalRayCheckingRow = 0;

            bool horizontalWallFound = false;

            //Pre-defining variables for horizontal wall checking
            //If player is looking up
            if (rayAngle > MathX.Quadrant2)
            {
                horizontalRayHitY =
                    MathX.Clamp
                    (
                        (float)Math.Floor(playerPosition.Y / tileSize) * tileSize - 0.0001f,
                        0,
                        (float)(mapWalls.GetLength(0) * tileSize - 0.0001f)
                    );
                horizontalRayHitX =
                    MathX.Clamp
                    (
                        playerPosition.X + (playerPosition.Y % tileSize) / (float)Math.Tan(MathX.Quadrant4 - rayAngle),
                        0,
                        (float)(mapWalls.GetLength(1) * tileSize - 0.0001f)
                    );
                offsetY = -tileSize;
                offsetX = tileSize / (float)Math.Tan(MathX.Quadrant4 - rayAngle);
            }
            //If player is looking down
            if (rayAngle < MathX.Quadrant2)
            {
                horizontalRayHitY =
                    MathX.Clamp
                    (
                        (float)Math.Floor(playerPosition.Y / tileSize) * tileSize + tileSize,
                        0,
                        (float)(mapWalls.GetLength(0) * tileSize - 0.0001f)
                    );
                horizontalRayHitX =
                    MathX.Clamp
                    (
                        playerPosition.X - (tileSize - (playerPosition.Y % tileSize)) / (float)Math.Tan(MathX.Quadrant4 - rayAngle),
                        0,
                        (float)(mapWalls.GetLength(1) * tileSize - 0.0001f)
                    );
                offsetY = tileSize;
                offsetX = -offsetY / (float)Math.Tan(MathX.Quadrant4 - rayAngle);
            }

            //Grid checker
            horizontalRayCheckingCol = (int)Math.Floor(horizontalRayHitX / tileSize);
            horizontalRayCheckingRow = (int)Math.Floor(horizontalRayHitY / tileSize);

            //Resetting iterator variable
            renderDistanceIterator = 0;

            //Cycle to find a wall
            while
            (
                renderDistanceIterator < renderDistance &&
                !horizontalWallFound &&
                horizontalRayHitY != (float)(mapWalls.GetLength(0) * tileSize - 0.0001f) &&
                horizontalRayHitY != 0 &&
                horizontalRayHitX != (float)(mapWalls.GetLength(1) * tileSize - 0.0001f) &&
                horizontalRayHitX != 0
            )
            {
                //If wall is found
                if (mapWalls[horizontalRayCheckingRow, horizontalRayCheckingCol] > 0)
                {
                    renderDistanceIterator = renderDistance;
                    horizontalWallFound = true;
                }
                else
                {
                    //If wall isn't found
                    horizontalRayHitY =
                        MathX.Clamp
                        (
                            horizontalRayHitY + offsetY,
                            0,
                            (float)(mapWalls.GetLength(0) * tileSize - 0.0001f)
                        );
                    horizontalRayHitX =
                        MathX.Clamp
                        (
                            horizontalRayHitX + offsetX,
                            0,
                            (float)(mapWalls.GetLength(1) * tileSize - 0.0001f)
                        );
                    renderDistanceIterator++;
                    horizontalRayCheckingCol = (int)(horizontalRayHitX / tileSize);
                    horizontalRayCheckingRow = (int)(horizontalRayHitY / tileSize);
                }
            }

            //Horizontal hypotenuse for line's length
            horizontalHypotenuse = (float)Math.Sqrt(Math.Pow(playerPosition.X - horizontalRayHitX, 2) + Math.Pow(playerPosition.Y - horizontalRayHitY, 2));

            //==========
            //Line datas
            //==========
            float rayLength = Math.Min(verticalHypotenuse, horizontalHypotenuse);
            float rayTilePosition = 0;
            int wallSide = 0;
            int wallType = 0;
            //If wall is vertical
            if (rayLength == verticalHypotenuse && verticalWallFound)
            {
                //Ray's end's position relative to the tile
                rayTilePosition = verticalRayHitY % tileSize;

                //If wall side is left
                if (rayAngle > MathX.Quadrant1 && rayAngle < MathX.Quadrant3)
                {
                    wallSide = 1;
                }
                //If wall side is right
                else if (rayAngle > MathX.Quadrant3 || rayAngle < MathX.Quadrant1)
                {
                    wallSide = 2;
                }

                //What type of wall did the ray hit (if out of bounds, 0)
                wallType = mapWalls[verticalRayCheckingRow, verticalRayCheckingCol];
            }
            //If wall is horizontal
            else if (rayLength == horizontalHypotenuse && horizontalWallFound)
            {
                //Ray's end's position relative to the tile
                rayTilePosition = horizontalRayHitX % tileSize;

                //If wall side is top
                if (rayAngle > 0 && rayAngle < Math.PI)
                {
                    wallSide = 3;
                }
                //If wall side is bottom
                else if (rayAngle > Math.PI && rayAngle < MathX.Quadrant4)
                {
                    wallSide = 4;
                }

                //What type of wall did the ray hit (if out of bounds, 0)
                wallType = mapWalls[horizontalRayCheckingRow, horizontalRayCheckingCol];
            }
            //No wall hit
            else
            {
                wallType = 0;
            }

            float wallHeight = (float)((tileSize * minimumScreenSize) /
                (rayLength * (float)Math.Cos(playerAngle -
                (playerAngle + FOVStart + i * radBetweenRays))));

            LoadCeilingAttribs(
                distanceShade,
                minimumScreenSize,
                screenHorizontalOffset,
                screenVerticalOffset,
                i,
                rayAngle,
                wallHeight,
                wallWidth,
                pitch,
                debugBorder
            );

            LoadFloorAttribs(
                distanceShade,
                minimumScreenSize,
                screenHorizontalOffset,
                screenVerticalOffset,
                i,
                rayAngle,
                wallHeight,
                wallWidth,
                pitch,
                debugBorder
            );

            if (wallType != 0)
            {
                LoadWallAttribs(
                    ClientSize,
                    distanceShade,
                    minimumScreenSize,
                    screenHorizontalOffset,
                    screenVerticalOffset,
                    tileSize,
                    i,
                    rayLength,
                    rayTilePosition,
                    wallHeight,
                    wallWidth,
                    wallSide,
                    wallType,
                    pitch,
                    debugBorder
                );
            }

            //Incrementing rayAngle for next ray
            rayAngle = Utils.NormalizeAngle(rayAngle + radBetweenRays);
        }
    }
}