using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Sources;
using Shaders;

namespace Engine;

internal partial class Engine
{
    //Texture atlas size's
    Vector2 objectAtlasSize = (360, 252);
    Vector2 itemAtlasSize = (288, 72);
    Vector2 enemyAtlasSize = (288, 432);

    float objectSpriteCellSize = 36;
    float itemSpriteCellSize = 36;
    float enemySpriteCellSize = 72;

    /* Sprite translator
     * Types: Objects, Pick-up items, enemies
     * Type 0 (Objects)
     * - ID 0: Torch
     * - ID 1: Purple torch
     * - ID 2: Bronze Chest
     * - ID 3: Silver Chest
     * - ID 4: Gold Chest
     * - ID 5: Diamond Chest
     * - ID 6: Void Chest
     * Type 1 (Pick-up items)
     * - ID 0: Heal
     * - ID 1: Ammo (No guns in game right now)
     * Type 2 (Enemies)
     * - ID 0: Jiggler
     * - ID 1: Korvax
     */

    //Types are used to seperate the functionality of the sprites
    //ID's are used to configure the sprites one-by-one and give them unique textures

    //Animation running time accumulator
    static float _spriteAnimTime;

    //Combat
    const int playerBaseDamage = 15;
    const int enemyBaseDamage = 10;
    const float enemyDamageOverlayDuration = 0.15f;
    const float enemyDamageOverlayMaxAlpha = 0.45f;
    const int enemyKillRewardCoins = 4;

    struct SpriteAnimConfig
    {
        public int FrameCount;
        public float Fps;
        public SpriteAnimConfig(int frameCount, float fps)
        {
            FrameCount = frameCount;
            Fps = fps;
        }
    }

    //Sprite animation config (Number of Frames, Frames per Second)
    static readonly SpriteAnimConfig[] SpriteAnimTable =
    [
        //Type 0 (Objects)
        new SpriteAnimConfig(
            frameCount: 10,
            fps: 8f),
        //Type 1 (Items)
        new SpriteAnimConfig(
            frameCount: 8,
            fps: 10f),
        //Type 2 (Enemies)
        // - Idle
        new SpriteAnimConfig(
            frameCount: 2,
            fps: 2f),
        // - Walk & Attack
        new SpriteAnimConfig(
            frameCount: 4,
            fps: 4)
    ];

    void LoadSpriteAttribs()
    {
        // Advance sprite animation time (seconds)
        _spriteAnimTime += deltaTime;

        //Enemy damage overlay timers
        var enemyOverlayKeys = new List<int>(enemyDamageOverlayTimers.Keys);
        foreach (int key in enemyOverlayKeys)
        {
            float nextTimer = enemyDamageOverlayTimers[key] - deltaTime;

            if (nextTimer <= 0f)
                enemyDamageOverlayTimers.Remove(key);
            else
                enemyDamageOverlayTimers[key] = nextTimer;
        }

        //Number of sprites on the map
        int spritesCount = Level.Sprites.Count;

        //Player's directon vectors in radians
        Vector2 playerDirection = (MathF.Cos(playerAngle), MathF.Sin(playerAngle));

        //Convert FOV degrees to radians
        float FOVrad = FOV * (MathF.PI / 180f);

        //Scaling the plane with FOV
        float planeScale = MathF.Tan(FOVrad / 2f);

        //Perpendicular distance of the plane
        Vector2 perpPlaneDist = (-playerDirection.Y * planeScale, playerDirection.X * planeScale);

        //Center of the square viewport in pixels.
        Vector2 viewportCenter = (
            screenHorizontalOffset + (minimumScreenSize / 2f),
            screenVerticalOffset + (minimumScreenSize / 2f) - pitch);

        float halfScreen = minimumScreenSize / 2f;

        //Inverting the 2x2 matrix direction plane
        float invDetBase = (perpPlaneDist.X * playerDirection.Y - playerDirection.X * perpPlaneDist.Y);

        //If determinant is ~0, the projection is invalid (Might never happen if we have a correct FOV value)
        if (MathF.Abs(invDetBase) < 1e-6f)
            return;

        //Inverting the determinant
        float invDet = 1.0f / invDetBase;

        //Building draw order to simulate distance
        var drawOrder = new int[spritesCount];
        var spriteDist = new float[spritesCount];
        float maxSpriteDistancePx = renderDistance * tileSize;
        float maxSpriteDistanceSq = maxSpriteDistancePx * maxSpriteDistancePx;

        for (int i = 0; i < spritesCount; i++)
        {
            drawOrder[i] = i;
            float sx = (Level.Sprites[i].Position.X + 0.5f) * tileSize;
            float sy = (Level.Sprites[i].Position.Y + 0.5f) * tileSize;
            float dx = sx - playerPosition.X;
            float dy = sy - playerPosition.Y;
            spriteDist[i] = dx * dx + dy * dy;
        }

        Array.Sort(drawOrder, (a, b) => spriteDist[b].CompareTo(spriteDist[a]));

        //Loop through the sorted (ordered) sprites
        for (int oi = 0; oi < drawOrder.Length; oi++)
        {
            int i = drawOrder[oi];

            //Skip sprites outside render distance
            if (spriteDist[i] > maxSpriteDistanceSq)
                continue;

            //If sprite is turned off, skip
            if (Level.Sprites[i].State == false) continue;

            // World position
            Vector2 spriteWorldPos = (
                (Level.Sprites[i].Position.X + 0.5f) * tileSize,
                (Level.Sprites[i].Position.Y + 0.5f) * tileSize);

            //Sprite's distance from player
            Vector2 relSpriteDist = (
                spriteWorldPos.X - playerPosition.X,
                spriteWorldPos.Y - playerPosition.Y);

            //Transforming to camera-space
            Vector2 transCamera = (
                invDet * (playerDirection.Y * relSpriteDist.X - playerDirection.X * relSpriteDist.Y),
                invDet * (-perpPlaneDist.Y * relSpriteDist.X + perpPlaneDist.X * relSpriteDist.Y));

            //If sprite is too close or behind the camera, skip
            if (transCamera.Y <= 5f)
                continue;

            //Camera space to screen coordinates
            float screenXCenter = viewportCenter.X + (transCamera.X / transCamera.Y) * halfScreen;

            //Sprite's size on the screen
            float spriteSize = (tileSize / transCamera.Y) * halfScreen;

            //Screen coordinates
            float quadX1 = screenXCenter - spriteSize;
            float quadX2 = screenXCenter + spriteSize;
            float quadY2 = viewportCenter.Y - spriteSize;
            float quadY1 = quadY2 + spriteSize * 2;

            //If quad is outside the limit, skip sprite
            if (quadX2 < screenHorizontalOffset ||
                quadX1 > screenHorizontalOffset + minimumScreenSize ||
                quadY1 < screenVerticalOffset ||
                quadY2 > screenVerticalOffset + minimumScreenSize)
                continue;

            // Occlusion test against per-ray wall depth buffer.
            // If all columns covered by this sprite have a nearer wall, skip sprite entirely.
            float clampedX1 = MathF.Max(quadX1, screenHorizontalOffset);
            float clampedX2 = MathF.Min(quadX2, screenHorizontalOffset + minimumScreenSize - 0.001f);

            int firstRay = (int)MathF.Floor((clampedX1 - screenHorizontalOffset) / wallWidth);
            int lastRay = (int)MathF.Floor((clampedX2 - screenHorizontalOffset) / wallWidth);

            firstRay = Math.Clamp(firstRay, 0, rayCount - 1);
            lastRay = Math.Clamp(lastRay, 0, rayCount - 1);

            float spriteDepth = transCamera.Y;
            float[] wallDepth = RayCasting.WallDepthBuffer;

            if (wallDepth.Length == rayCount)
            {
                bool visibleInAnyColumn = false;

                for (int ray = firstRay; ray <= lastRay; ray++)
                {
                    if (spriteDepth < wallDepth[ray])
                    {
                        visibleInAnyColumn = true;
                        break;
                    }
                }

                if (!visibleInAnyColumn)
                    continue;
            }

            int sType = Level.Sprites[i].Type;
            int sId = Level.Sprites[i].Id;

            //=======================
            //If sprite is an object
            //=======================
            if (sType == 0)
            {
                HandleObjects(
                    sType,
                    sId,
                    i,
                    spriteWorldPos,
                    quadX1,
                    quadX2,
                    quadY1,
                    quadY2,
                    spriteDepth);
            }

            //=======================
            //If sprite is an item
            //=======================
            else if (sType == 1)
            {
                HandleItems(
                    sType,
                    sId,
                    i,
                    spriteWorldPos,
                    quadX1,
                    quadX2,
                    quadY1,
                    quadY2,
                    spriteDepth);
            }

            //=======================
            //If sprite is an enemy
            //=======================
            else if (sType == 2)
            {
                HandleEnemies(
                    sType,
                    sId,
                    i,
                    spriteWorldPos,
                    quadX1,
                    quadX2,
                    quadY1,
                    quadY2,
                    spriteDepth);
            }
        }
    }

    void HandleObjects(
        int sType,
        int sId,
        int i,
        Vector2 spriteWorldPos,
        float quadX1,
        float quadX2,
        float quadY1,
        float quadY2,
        float spriteDepth
        )
    {
        //Default texture UV rect's width
        float u0 = 0f;
        float u1 = 1f;

        // Simple animation for torches (ID0,1)
        if (sId == 0 || sId == 1)
        {
            var cfg = SpriteAnimTable[sType];
            if (cfg.FrameCount > 1 && cfg.Fps > 0f)
            {
                int frame = (int)(_spriteAnimTime * cfg.Fps) % cfg.FrameCount;
                u0 = (frame * objectSpriteCellSize) / objectAtlasSize.X;
                u1 = ((frame + 1) * objectSpriteCellSize) / objectAtlasSize.X;
            }
            else
            {
                u0 = 0f;
                u1 = objectSpriteCellSize / objectAtlasSize.X;
            }
        }
        else if (sId >= 2)
        {
            // Only allow chest interaction when player is close enough
            float interactDist = 0.75f * tileSize;
            float dx = spriteWorldPos.X - playerPosition.X;
            float dy = spriteWorldPos.Y - playerPosition.Y;
            bool isNear = (dx * dx + dy * dy) <= (interactDist * interactDist);

            Vector2 toChest = (dx, dy);
            if (toChest.LengthSquared > 1e-6f)
                toChest.Normalize();

            Vector2 playerForward = (MathF.Cos(playerAngle), MathF.Sin(playerAngle));
            bool isFacing = Vector2.Dot(playerForward, toChest) > 0.45f;

            //First time chest opening
            if (Level.Sprites[i].Interacted == false && isNear && isFacing && KeyboardState.IsKeyPressed(Keys.E))
            {
                Level.Sprites[i].Interacted = true;

                int reward = sId switch
                {
                    //Bronze chest
                    2 => 8,
                    //Silver chest
                    3 => 14,
                    //Gold chest
                    4 => 22,
                    //Diamond chest
                    5 => 34,
                    //Void chest
                    6 => 50,
                    _ => 0
                };

                tempCurrentCoins += reward;
                // persist coins and update score to Settings so stats updater can read it
                Sources.Settings.Player.Coins = tempCurrentCoins;
                Sources.Settings.Player.Score += reward * 5;
                // persist to disk
                try { Sources.Settings.Save("settings.json"); } catch { }
                // fire-and-forget: push stats to server on change
                try
                {
                    var apiBase = Sources.Settings.Api.BaseUrl;
                    if (!string.IsNullOrEmpty(apiBase))
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                using var api = new Sources.JsonCrudApi(apiBase);
                                await api.UpdatePlayerStatsAsync();
                            }
                            catch { }
                        });
                    }
                }
                catch { }
            }

            if (Level.Sprites[i].Interacted == false)
            {
                u0 = 0f;
                u1 = objectSpriteCellSize / objectAtlasSize.X;
            }

            else
            {
                u0 = objectSpriteCellSize / objectAtlasSize.X;
                u1 = 2 * objectSpriteCellSize / objectAtlasSize.X;
            }
        }

        //Texture UV rect's height (Vertical texture stride is based on ID)
        float v0 = 1 - ((sId + 1) * objectSpriteCellSize / objectAtlasSize.Y);
        float v1 = 1 - (sId * objectSpriteCellSize / objectAtlasSize.Y);

        UploadSprite(
        quadX1,
        quadX2,
        quadY1,
        quadY2,
        u0,
        v0,
        u1,
        v1,
        sType,
        sId,
        spriteDepth,
        0f);
    }

    void HandleItems(
        int sType,
        int sId,
        int i,
        Vector2 spriteWorldPos,
        float quadX1,
        float quadX2,
        float quadY1,
        float quadY2,
        float spriteDepth)
    {
        float pickupDist = tileSize * 0.55f;
        float dx = spriteWorldPos.X - playerPosition.X;
        float dy = spriteWorldPos.Y - playerPosition.Y;

        if ((dx * dx + dy * dy) <= (pickupDist * pickupDist))
        {
            Level.Sprites[i].State = false;

            //Heal
            if (sId == 0)
                tempPlayerCurrentHealth = Math.Min(tempPlayerMaxHealth, tempPlayerCurrentHealth + 50);

            //Ammo -> TODO later

            TriggerPlayerPickupOverlay();
            return;
        }

        // Horizontal animation based on sprite type config (one row per type)
        float u0 = 0f;
        float u1 = itemSpriteCellSize / itemAtlasSize.X;

        var cfg = SpriteAnimTable[sType];
        if (cfg.FrameCount > 1 && cfg.Fps > 0f)
        {
            int frame = (int)(_spriteAnimTime * cfg.Fps) % cfg.FrameCount;
            u0 = (frame * itemSpriteCellSize) / itemAtlasSize.X;
            u1 = ((frame + 1) * itemSpriteCellSize) / itemAtlasSize.X;
        }

        // Vertical stride based on sprite ID (top-down in atlas)
        float v0 = 1 - ((sId + 1) * itemSpriteCellSize / itemAtlasSize.Y);
        float v1 = 1 - (sId * itemSpriteCellSize / itemAtlasSize.Y);

        UploadSprite(
        quadX1,
        quadX2,
        quadY1,
        quadY2,
        u0,
        v0,
        u1,
        v1,
        sType,
        sId,
        spriteDepth,
        0f);
    }

    //Enemy settings
    //=========================================================================================
    //Follow distance (in tiles)
    float enemyNoticeDistance = 2f;
    //Stop distance (in tiles)
    float enemyStopDistance = 0.75f;
    //Attack start distance (in tiles)
    float enemyAttackStartDistance = 0.75f;
    //Attack stop distance (in tiles)
    float enemyAttackStopDistance = 1f;
    //Enemy speed (pixels / sec)
    float enemyMovementSpeed = 30f;
    //Enemy collision radius (slightly smaller than full tile so 1-tile corridors are passable)
    float enemyCollisionRadius => tileSize * 0.35f;
    //=========================================================================================

    //Enemy runtime state
    //0: idle,1: walk,2: attack
    static readonly Dictionary<int, int> enemyAnimState = new();
    static readonly Dictionary<int, bool> enemyAttackDidDamage = new();
    static readonly Dictionary<int, int> enemyAttackLastFrame = new();
    static readonly Dictionary<int, float> enemyDamageOverlayTimers = new();

    void ResetCombatRuntimeStates()
    {
        enemyAnimState.Clear();
        enemyAttackDidDamage.Clear();
        enemyAttackLastFrame.Clear();
        enemyDamageOverlayTimers.Clear();
        _spriteAnimTime = 0f;
    }

    void TryDealPlayerSlashDamage()
    {
        float attackRange = tileSize * 1.15f;
        float attackRangeSq = attackRange * attackRange;
        Vector2 playerForward = (MathF.Cos(playerAngle), MathF.Sin(playerAngle));

        for (int i = 0; i < Level.Sprites.Count; i++)
        {
            var sprite = Level.Sprites[i];

            //Only active enemies can be damaged
            if (!sprite.State || sprite.Type != 2)
                continue;

            Vector2 enemyPosPx = (
                (sprite.Position.X + 0.5f) * tileSize,
                (sprite.Position.Y + 0.5f) * tileSize);

            Vector2 toEnemy = (enemyPosPx.X - playerPosition.X, enemyPosPx.Y - playerPosition.Y);
            float distSq = (toEnemy.X * toEnemy.X) + (toEnemy.Y * toEnemy.Y);

            if (distSq > attackRangeSq || distSq <= 1e-6f)
                continue;

            Vector2 toEnemyDir = toEnemy;
            toEnemyDir.Normalize();

            //Small frontal cone so the slash does not hit behind the player
            float facingDot = Vector2.Dot(playerForward, toEnemyDir);
            if (facingDot < 0.35f)
                continue;

            int enemyHp = sprite.Health ?? 0;
            enemyHp -= playerBaseDamage;
            Level.Sprites[i].Health = enemyHp;

            if (enemyHp <= 0)
            {
                Level.Sprites[i].State = false;
                enemyAnimState.Remove(i);
                enemyAttackDidDamage.Remove(i);
                enemyAttackLastFrame.Remove(i);
                enemyDamageOverlayTimers.Remove(i);
                tempCurrentCoins += enemyKillRewardCoins;
                // persist coins and increment enemy killed stat
                Sources.Settings.Player.Coins = tempCurrentCoins;
                Sources.Settings.Player.NumOfEnemiesKilled = Sources.Settings.Player.NumOfEnemiesKilled + 1;
                // fire-and-forget: push stats to server on change
                try
                {
                    var apiBase = Sources.Settings.Api.BaseUrl;
                    if (!string.IsNullOrEmpty(apiBase))
                    {
                        // persist to disk
                        try { Sources.Settings.Save("settings.json"); } catch { }
                        Task.Run(async () =>
                        {
                            try
                            {
                                using var api = new Sources.JsonCrudApi(apiBase);
                                await api.UpdatePlayerStatsAsync();
                            }
                            catch { }
                        });
                    }
                }
                catch { }
                continue;
            }

            enemyDamageOverlayTimers[i] = enemyDamageOverlayDuration;

            //Small knockback from player to enemy while still respecting collisions
            float knockBackDistance = tileSize * 0.25f;
            Vector2 enemyPosAfterHit = enemyPosPx;
            float moveX = toEnemyDir.X * knockBackDistance;
            float moveY = toEnemyDir.Y * knockBackDistance;

            Vector2 candidateX = (enemyPosAfterHit.X + moveX, enemyPosAfterHit.Y);
            if (!IsEnemyMoveBlockedX(enemyPosAfterHit, moveX) &&
                !IsEnemyOverlappingOtherEnemy(i, candidateX))
                enemyPosAfterHit.X += moveX;

            Vector2 candidateY = (enemyPosAfterHit.X, enemyPosAfterHit.Y + moveY);
            if (!IsEnemyMoveBlockedY(enemyPosAfterHit, moveY) &&
                !IsEnemyOverlappingOtherEnemy(i, candidateY))
                enemyPosAfterHit.Y += moveY;

            Level.Sprites[i].Position = (enemyPosAfterHit.X / tileSize - 0.5f, enemyPosAfterHit.Y / tileSize - 0.5f);
        }
    }

    void ApplyDamageToPlayer(int damage)
    {
        if (!isInGame || damage <= 0)
            return;

        tempPlayerCurrentHealth -= damage;
        if (tempPlayerCurrentHealth < 0)
            tempPlayerCurrentHealth = 0;

        TriggerPlayerDamageOverlay();

        if (tempPlayerCurrentHealth <= 0)
            HandlePlayerDeath();
    }

    bool IsEnemyMoveBlockedX(Vector2 posPx, float moveX)
    {
        float radius = enemyCollisionRadius;
        float nextXPlus = posPx.X + radius + moveX;
        float nextXMinus = posPx.X - radius + moveX;

        if (nextXMinus <= 0f || nextXPlus >= (mapWalls.GetLength(1) * tileSize))
            return true;

        int yPlus = (int)((posPx.Y + radius) / tileSize);
        int yMinus = (int)((posPx.Y - radius) / tileSize);
        int xPlus = (int)(nextXPlus / tileSize);
        int xMinus = (int)(nextXMinus / tileSize);

        return mapWalls[yPlus, xPlus] > 0 ||
               mapWalls[yPlus, xMinus] > 0 ||
               mapWalls[yMinus, xPlus] > 0 ||
               mapWalls[yMinus, xMinus] > 0;
    }

    bool IsEnemyMoveBlockedY(Vector2 posPx, float moveY)
    {
        float radius = enemyCollisionRadius;
        float nextYPlus = posPx.Y + radius + moveY;
        float nextYMinus = posPx.Y - radius + moveY;

        if (nextYMinus <= 0f || nextYPlus >= (mapWalls.GetLength(0) * tileSize))
            return true;

        int yPlus = (int)(nextYPlus / tileSize);
        int yMinus = (int)(nextYMinus / tileSize);
        int xPlus = (int)((posPx.X + radius) / tileSize);
        int xMinus = (int)((posPx.X - radius) / tileSize);

        return mapWalls[yPlus, xPlus] > 0 ||
               mapWalls[yMinus, xPlus] > 0 ||
               mapWalls[yPlus, xMinus] > 0 ||
               mapWalls[yMinus, xMinus] > 0;
    }

    bool IsEnemyOverlappingOtherEnemy(int selfIndex, Vector2 candidatePosPx)
    {
        float minDistance = enemyCollisionRadius * 2f;
        float minDistanceSq = minDistance * minDistance;

        for (int j = 0; j < Level.Sprites.Count; j++)
        {
            if (j == selfIndex) continue;

            var other = Level.Sprites[j];
            if (!other.State || other.Type != 2) continue;

            Vector2 otherPosPx = (
                (other.Position.X + 0.5f) * tileSize,
                (other.Position.Y + 0.5f) * tileSize);

            float dx = candidatePosPx.X - otherPosPx.X;
            float dy = candidatePosPx.Y - otherPosPx.Y;
            float distSq = (dx * dx) + (dy * dy);

            if (distSq < minDistanceSq)
                return true;
        }

        return false;
    }

    void HandleEnemies(
        int sType,
        int sId,
        int i,
        Vector2 spriteWorldPos,
        float quadX1,
        float quadX2,
        float quadY1,
        float quadY2,
        float spriteDepth)
    {
        //Enemy -> player distance (pixel)
        float dx = playerPosition.X - spriteWorldPos.X;
        float dy = playerPosition.Y - spriteWorldPos.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        //Distance thresholds (pixel)
        float followDist = enemyNoticeDistance * tileSize;
        float stopDist = enemyStopDistance * tileSize;
        float attackStartDist = enemyAttackStartDistance * tileSize;
        float attackStopDist = enemyAttackStopDistance * tileSize;

        //Getting enemy state (default: idle)
        if (!enemyAnimState.TryGetValue(i, out int state))
            state = 0;

        //Attack hysteresis
        // - Start attack when close enough
        // - Keep attacking until player is far enough
        if (state != 2)
        {
            if (dist <= attackStartDist)
                state = 2;
        }
        else
        {
            if (dist > attackStopDist)
                state = 1;
        }

        //Follow logic
        // - Only try to follow inside the follow distance
        // - Stop if too close
        bool canFollow = dist <= followDist;
        bool isTooClose = dist <= stopDist;

        if (state != 2)
        {
            if (!canFollow)
                state = 0;
            else if (isTooClose)
                state = 0;
            else
                state = 1;
        }

        //Movement
        if (state == 1)
        {
            Vector2 dir = (dx, dy);
            if (dir.LengthSquared > 1e-6f)
                dir.Normalize();

            float step = enemyMovementSpeed * deltaTime;
            float moveX = dir.X * step;
            float moveY = dir.Y * step;

            //Move in tile-space units (Level sprites store tile coords)
            Vector2 enemyPosPx = spriteWorldPos;

            Vector2 candidateX = (enemyPosPx.X + moveX, enemyPosPx.Y);

            if (!IsEnemyMoveBlockedX(enemyPosPx, moveX) &&
                !IsEnemyOverlappingOtherEnemy(i, candidateX))
                enemyPosPx.X += moveX;

            Vector2 candidateY = (enemyPosPx.X, enemyPosPx.Y + moveY);

            if (!IsEnemyMoveBlockedY(enemyPosPx, moveY) &&
                !IsEnemyOverlappingOtherEnemy(i, candidateY))
                enemyPosPx.Y += moveY;

            Level.Sprites[i].Position = (enemyPosPx.X / tileSize - 0.5f, enemyPosPx.Y / tileSize - 0.5f);
        }

        enemyAnimState[i] = state;

        //Animation
        //=====================================================================================
        int idleRow = sId * 3;
        int row = idleRow;

        //0: idle (2 frame)
        if (state == 0)
            row = idleRow;

        //1: walk (4 frame)
        else if (state == 1)
            row = idleRow + 1;

        //2: attack (4 frame)
        else if (state == 2)
            row = idleRow + 2;

        int frameCount = state == 0 ? SpriteAnimTable[2].FrameCount : SpriteAnimTable[3].FrameCount;
        float fps = state == 0 ? SpriteAnimTable[2].Fps : SpriteAnimTable[3].Fps;

        frameCount = Math.Clamp(frameCount, 1, 4);

        int frame = 0;
        if (frameCount > 1 && fps > 0f)
            frame = (int)(_spriteAnimTime * fps) % frameCount;

        if (state == 2)
        {
            if (!enemyAttackDidDamage.TryGetValue(i, out bool didDamage))
                didDamage = false;

            if (!enemyAttackLastFrame.TryGetValue(i, out int lastFrame))
                lastFrame = frame;

            //A new attack cycle has started
            if (frame < lastFrame)
                didDamage = false;

            //Middle of enemy attack animation -> apply hit once
            if (frame >= 2 && !didDamage)
            {
                ApplyDamageToPlayer(enemyBaseDamage);
                didDamage = true;
            }

            enemyAttackDidDamage[i] = didDamage;
            enemyAttackLastFrame[i] = frame;
        }
        else
        {
            enemyAttackDidDamage.Remove(i);
            enemyAttackLastFrame.Remove(i);
        }

        float u0 = (frame * enemySpriteCellSize) / enemyAtlasSize.X;
        float u1 = ((frame + 1) * enemySpriteCellSize) / enemyAtlasSize.X;

        float v0 = 1 - ((row + 1) * enemySpriteCellSize / enemyAtlasSize.Y);
        float v1 = 1 - (row * enemySpriteCellSize / enemyAtlasSize.Y);

        float damageOverlayAlpha = 0f;
        if (enemyDamageOverlayTimers.TryGetValue(i, out float damageOverlayTimer) && damageOverlayTimer > 0f)
            damageOverlayAlpha = (damageOverlayTimer / enemyDamageOverlayDuration) * enemyDamageOverlayMaxAlpha;
        //=====================================================================================

        UploadSprite(
        quadX1,
        quadX2,
        quadY1,
        quadY2,
        u0,
        v0,
        u1,
        v1,
        sType,
        sId,
        spriteDepth,
        damageOverlayAlpha);
    }

    //Universal vertex attribute uploader
    static void UploadSprite(
        float quadX1,
        float quadX2,
        float quadY1,
        float quadY2,
        float u0,
        float v0,
        float u1,
        float v1,
        int sType,
        int sId,
        float spriteDepth,
        float damageOverlayAlpha)
    {
        ShaderHandler.SpriteVertexAttribList.AddRange(new float[]
        {
            quadX1,
            quadX2,
            quadY1,
            quadY2,
            u0,
            v0,
            u1,
            v1,
            sType,
            sId,
            spriteDepth,
            damageOverlayAlpha
        });
    }
}
