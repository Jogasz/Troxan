//using OpenTK.Mathematics;
//using OpenTK.Windowing.GraphicsLibraryFramework;
//using System;
//using System.Security.Cryptography;

//namespace Engine;

//internal partial class Engine
//{
//    //Texture atlas size's
//    Vector2 objectAtlasSize = (360, 252);
//    Vector2 itemAtlasSize = (288, 72);
//    Vector2 enemyAtlasSize = (288, 432);

//    float objectSpriteCellSize = 36;
//    float itemSpriteCellSize = 36;
//    float enemySpriteCellSize = 72;

//    /* Sprite translator
//     * Types: Objects, Pick-up items, enemies
//     * Type 0 (Objects)
//     * - ID 0: Torch
//     * - ID 1: Purple torch
//     * - ID 2: Bronze Chest
//     * - ID 3: Silver Chest
//     * - ID 4: Gold Chest
//     * - ID 5: Diamond Chest
//     * - ID 6: Void Chest
//     * Type 1 (Pick-up items)
//     * - ID 4: Heal
//     * - ID 5: Ammo (No guns in game right now)
//     * Type 2 (Enemies)
//     * - ID 6: Void Sentinel - Initiate (Level 1)
//     * - ID 7: Void Sentinel - Enforcer (Level 2)
//     * - ID 8: Void Sentinel - Reaver   (Level 3)
//     * - ID 9: Sentinel Prime - K'hrovan
//     * - ID 10: Jiggler
//     * - ID 11: Korvax
//     */

//    //Types are used to seperate the functionality of the sprites
//    //ID's are used to configure the sprites one-by-one and give them unique textures

//    //Animation running time accumulator
//    static float _spriteAnimTime;

//    struct SpriteAnimConfig
//    {
//        public int FrameCount;
//        public float Fps;
//        public SpriteAnimConfig(int frameCount, float fps)
//        {
//            FrameCount = frameCount;
//            Fps = fps;
//        }
//    }

//    //Sprite animation config (Number of Frames, Frames per Second)
//    static readonly SpriteAnimConfig[] SpriteAnimTable =
//    [
//        //Type 0 (Objects)
//        new SpriteAnimConfig(
//            frameCount: 10,
//            fps: 8f),
//        //Type 1 (Items)
//        new SpriteAnimConfig(
//            frameCount: 8,
//            fps: 10f),
//        //Type 2 (Enemies)
//        // - Idle
//        new SpriteAnimConfig(
//            frameCount: 2,
//            fps: 2f),
//        // - Walk & Attack
//        new SpriteAnimConfig(
//            frameCount: 4,
//            fps: 4)
//    ];

//    void ComputeSprites()
//    {
//        // Advance sprite animation time (seconds)
//        _spriteAnimTime += deltaTime;

//        //Number of sprites on the map
//        int spritesCount = Level.Sprites.SpritesNum;

//        //Player's directon vectors in radians
//        Vector2 playerDirection = (MathF.Cos(playerAngle), MathF.Sin(playerAngle));

//        //Convert FOV degrees to radians
//        float FOVrad = FOV * (MathF.PI / 180f);

//        //Scaling the plane with FOV
//        float planeScale = MathF.Tan(FOVrad / 2f);

//        //Perpendicular distance of the plane
//        Vector2 perpPlaneDist = (-playerDirection.Y * planeScale, playerDirection.X * planeScale);

//        //Center of the square viewport in pixels.
//        Vector2 viewportCenter = (
//            screenHorizontalOffset + (minimumScreenSize / 2f),
//            screenVerticalOffset + (minimumScreenSize / 2f) - pitch);

//        float halfScreen = minimumScreenSize / 2f;

//        //Inverting the 2x2 matrix direction plane
//        float invDetBase = (perpPlaneDist.X * playerDirection.Y - playerDirection.X * perpPlaneDist.Y);

//        //If determinant is ~0, the projection is invalid (Might never happen if we have a correct FOV value)
//        if (MathF.Abs(invDetBase) < 1e-6f)
//            return;

//        //Inverting the determinant
//        float invDet = 1.0f / invDetBase;

//        //Building draw order to simulate distance
//        var drawOrder = new int[spritesCount];
//        var spriteDist = new float[spritesCount];

//        for (int i = 0; i < spritesCount; i++)
//        {
//            drawOrder[i] = i;
//            float sx = (Level.Sprites[i].Position.X + 0.5f) * tileSize;
//            float sy = (Level.Sprites[i].Position.Y + 0.5f) * tileSize;
//            float dx = sx - playerPosition.X;
//            float dy = sy - playerPosition.Y;
//            spriteDist[i] = dx * dx + dy * dy;
//        }

//        Array.Sort(drawOrder, (a, b) => spriteDist[b].CompareTo(spriteDist[a]));

//        //Loop through the sorted (ordered) sprites
//        for (int oi = 0; oi < drawOrder.Length; oi++)
//        {
//            int i = drawOrder[oi];

//            //If sprite is turned off, skip
//            if (Level.Sprites[i].State == false) continue;

//            // World position
//            Vector2 spriteWorldPos = (
//                (Level.Sprites[i].Position.X + 0.5f) * tileSize,
//                (Level.Sprites[i].Position.Y + 0.5f) * tileSize);

//            //Sprite's distance from player
//            Vector2 relSpriteDist = (
//                spriteWorldPos.X - playerPosition.X,
//                spriteWorldPos.Y - playerPosition.Y);

//            //Transforming to camera-space
//            Vector2 transCamera = (
//                invDet * (playerDirection.Y * relSpriteDist.X - playerDirection.X * relSpriteDist.Y),
//                invDet * (-perpPlaneDist.Y * relSpriteDist.X + perpPlaneDist.X * relSpriteDist.Y));

//            //If sprite is too close or behind the camera, skip
//            if (transCamera.Y <= 5f)
//                continue;

//            //Camera space to screen coordinates
//            float screenXCenter = viewportCenter.X + (transCamera.X / transCamera.Y) * halfScreen;

//            //Sprite's size on the screen
//            float spriteSize = (tileSize / transCamera.Y) * halfScreen;
            
//            //Screen coordinates
//            float quadX1 = screenXCenter - spriteSize;
//            float quadX2 = screenXCenter + spriteSize;
//            float quadY2 = viewportCenter.Y - spriteSize;
//            float quadY1 = quadY2 + spriteSize * 2;

//            //If quad is outside the limit, skip sprite
//            if (quadX2 < screenHorizontalOffset ||
//                quadX1 > screenHorizontalOffset + minimumScreenSize ||
//                quadY1 < screenVerticalOffset ||
//                quadY2 > screenVerticalOffset + minimumScreenSize)
//                continue;

//            int sType = Level.Sprites[i].Type;
//            int sId = Level.Sprites[i].Id;
//            Vector2 sPos = Level.Sprites[i].Position;

//            //=======================
//            //If sprite is an object
//            //=======================
//            if (sType == 0)
//            {
//                HandleObjects(
//                    sType,
//                    sId,
//                    i,
//                    spriteWorldPos,
//                    quadX1,
//                    quadX2,
//                    quadY1,
//                    quadY2);
//            }

//            //=======================
//            //If sprite is an item
//            //=======================
//            else if (sType == 1)
//            {
//                HandleItems(
//                    sType,
//                    sId,
//                    i,
//                    spriteWorldPos,
//                    quadX1,
//                    quadX2,
//                    quadY1,
//                    quadY2);
//            }

//            //=======================
//            //If sprite is an enemy
//            //=======================
//            else if (sType == 2)
//            {
//                HandleEnemies(
//                    sType,
//                    sId,
//                    i,
//                    spriteWorldPos,
//                    quadX1,
//                    quadX2,
//                    quadY1,
//                    quadY2);
//            }
//        }
//    }

//    void HandleObjects(
//        int sType,
//        int sId,
//        int i,
//        Vector2 spriteWorldPos,
//        float quadX1,
//        float quadX2,
//        float quadY1,
//        float quadY2
//        )
//    {
//        //Default texture UV rect's width
//        float u0 =0f;
//        float u1 =1f;

//        // Simple animation for torches (ID0,1)
//        if (sId ==0 || sId ==1)
//        {
//            var cfg = SpriteAnimTable[sType];
//            if (cfg.FrameCount >1 && cfg.Fps >0f)
//            {
//                int frame = (int)(_spriteAnimTime * cfg.Fps) % cfg.FrameCount;
//                u0 = (frame * objectSpriteCellSize) / objectAtlasSize.X;
//                u1 = ((frame +1) * objectSpriteCellSize) / objectAtlasSize.X;
//            }
//            else
//            {
//                u0 =0f;
//                u1 = objectSpriteCellSize / objectAtlasSize.X;
//            }
//        }
//        else if (sId >=2)
//        {
//            // Only allow chest interaction when player is close enough
//            float interactDist =0.75f * tileSize;
//            float dx = spriteWorldPos.X - playerPosition.X;
//            float dy = spriteWorldPos.Y - playerPosition.Y;
//            bool isNear = (dx * dx + dy * dy) <= (interactDist * interactDist);

//            //First time chest opening
//            if (!Level.Sprites[i].isInteracted && isNear && KeyboardState.IsKeyPressed(Keys.E))
//                Level.Sprites[i].isInteracted = true;

//            if (!Level.Sprites[i].isInteracted)
//            {
//                u0 =0f;
//                u1 = objectSpriteCellSize / objectAtlasSize.X;
//            }

//            else
//            {
//                u0 = objectSpriteCellSize / objectAtlasSize.X;
//                u1 =2 * objectSpriteCellSize / objectAtlasSize.X;
//            }
//        }

//        //Texture UV rect's height (Vertical texture stride is based on ID)
//        float v0 =1 - ((sId +1) * objectSpriteCellSize / objectAtlasSize.Y);
//        float v1 =1 - (sId * objectSpriteCellSize / objectAtlasSize.Y);

//        UploadSprite(
//        quadX1,
//        quadX2,
//        quadY1,
//        quadY2,
//        u0,
//        v0,
//        u1,
//        v1,
//        sType,
//        sId);
//    }

//    void HandleItems(
//        int sType,
//        int sId,
//        int i,
//        Vector2 spriteWorldPos,
//        float quadX1,
//        float quadX2,
//        float quadY1,
//        float quadY2)
//    {
//        // Horizontal animation based on sprite type config (one row per type)
//        float u0 = 0f;
//        float u1 = itemSpriteCellSize / itemAtlasSize.X;

//        var cfg = SpriteAnimTable[sType];
//        if (cfg.FrameCount > 1 && cfg.Fps > 0f)
//        {
//            int frame = (int)(_spriteAnimTime * cfg.Fps) % cfg.FrameCount;
//            u0 = (frame * itemSpriteCellSize) / itemAtlasSize.X;
//            u1 = ((frame + 1) * itemSpriteCellSize) / itemAtlasSize.X;
//        }

//        // Vertical stride based on sprite ID (top-down in atlas)
//        float v0 = 1 - ((sId + 1) * itemSpriteCellSize / itemAtlasSize.Y);
//        float v1 = 1 - (sId * itemSpriteCellSize / itemAtlasSize.Y);

//        UploadSprite(
//        quadX1,
//        quadX2,
//        quadY1,
//        quadY2,
//        u0,
//        v0,
//        u1,
//        v1,
//        sType,
//        sId);
//    }

//    //Enemy settings
//    //=========================================================================================
//    //Follow distance (in tiles)
//    float enemyNoticeDistance = 2f;
//    //Stop distance (in tiles)
//    float enemyStopDistance = 0.75f;
//    //Attack start distance (in tiles)
//    float enemyAttackStartDistance = 0.75f;
//    //Attack stop distance (in tiles)
//    float enemyAttackStopDistance = 1f;
//    //Enemy speed (pixels / sec)
//    float enemyMovementSpeed = 30f;
//    //=========================================================================================

//    //Enemy runtime state
//    //0: idle,1: walk,2: attack
//    static readonly Dictionary<int, int> enemyAnimState = new();

//    void HandleEnemies(
//        int sType,
//        int sId,
//        int i,
//        Vector2 spriteWorldPos,
//        float quadX1,
//        float quadX2,
//        float quadY1,
//        float quadY2)
//    {
//        //Enemy -> player distance (pixel)
//        float dx = playerPosition.X - spriteWorldPos.X;
//        float dy = playerPosition.Y - spriteWorldPos.Y;
//        float dist = MathF.Sqrt(dx * dx + dy * dy);

//        //Distance thresholds (pixel)
//        float followDist = enemyNoticeDistance * tileSize;
//        float stopDist = enemyStopDistance * tileSize;
//        float attackStartDist = enemyAttackStartDistance * tileSize;
//        float attackStopDist = enemyAttackStopDistance * tileSize;

//        //Getting enemy state (default: idle)
//        if (!enemyAnimState.TryGetValue(i, out int state))
//            state =0;

//        //Attack hysteresis
//        // - Start attack when close enough
//        // - Keep attacking until player is far enough
//        if (state !=2)
//        {
//            if (dist <= attackStartDist)
//                state =2;
//        }
//        else
//        {
//            if (dist > attackStopDist)
//                state =1;
//        }

//        //Follow logic
//        // - Only try to follow inside the follow distance
//        // - Stop if too close
//        bool canFollow = dist <= followDist;
//        bool isTooClose = dist <= stopDist;

//        if (state !=2)
//        {
//            if (!canFollow)
//                state =0;
//            else if (isTooClose)
//                state =0;
//            else
//                state =1;
//        }

//        //Movement
//        if (state ==1)
//        {
//            Vector2 dir = (dx, dy);
//            if (dir.LengthSquared >1e-6f)
//                dir.Normalize();

//            float step = enemyMovementSpeed * deltaTime;

//            //Move in tile-space units (Level sprites store tile coords)
//            Vector2 enemyPosPx = spriteWorldPos;
//            enemyPosPx += dir * step;

//            Level.Sprites[i].Position = (enemyPosPx.X / tileSize -0.5f, enemyPosPx.Y / tileSize -0.5f);
//        }

//        enemyAnimState[i] = state;

//        //Animation
//        //=====================================================================================
//        int idleRow = sId *3;
//        int row = idleRow;

//        //0: idle (2 frame)
//        if (state ==0)
//            row = idleRow;

//        //1: walk (4 frame)
//        else if (state ==1)
//            row = idleRow +1;

//        //2: attack (4 frame)
//        else if (state ==2)
//            row = idleRow +2;

//        int frameCount = state ==0 ? SpriteAnimTable[2].FrameCount : SpriteAnimTable[3].FrameCount;
//        float fps = state ==0 ? SpriteAnimTable[2].Fps : SpriteAnimTable[3].Fps;

//        frameCount = Math.Clamp(frameCount,1,4);

//        int frame =0;
//        if (frameCount >1 && fps >0f)
//            frame = (int)(_spriteAnimTime * fps) % frameCount;

//        float u0 = (frame * enemySpriteCellSize) / enemyAtlasSize.X;
//        float u1 = ((frame +1) * enemySpriteCellSize) / enemyAtlasSize.X;

//        float v0 =1 - ((row +1) * enemySpriteCellSize / enemyAtlasSize.Y);
//        float v1 =1 - (row * enemySpriteCellSize / enemyAtlasSize.Y);
//        //=====================================================================================

//        UploadSprite(
//        quadX1,
//        quadX2,
//        quadY1,
//        quadY2,
//        u0,
//        v0,
//        u1,
//        v1,
//        sType,
//        sId);
//    }

//    //Universal vertex attribute uploader
//    static void UploadSprite(
//        float quadX1,
//        float quadX2,
//        float quadY1,
//        float quadY2,
//        float u0,
//        float v0,
//        float u1,
//        float v1,
//        int sType,
//        int sId)
//    {
//        ShaderHandler.SpriteVertexAttribList.AddRange(new float[]
//        {
//            quadX1,
//            quadX2,
//            quadY1,
//            quadY2,
//            u0,
//            v0,
//            u1,
//            v1,
//            sType,
//            sId
//        });
//    }
//}
