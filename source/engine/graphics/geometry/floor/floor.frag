//Fragment shader is calculating the color output for the pixels

//Declaration of shader version and core profile functionality
//==============================================================
#version 330 core
//==============================================================

//Incoming and outgoing variables, uniforms
//==============================================================
    //Strip quad Y1, Y2 (in)
    // x: Y1
    // y: Y2
in vec2 vStripQuadY;
    //Ray's angle (in)
in float rayAngle;
    //Wall's height (in)
in float vWallHeight;

    //Final outgoing RGBA of the quad (out)
out vec4 FragColor;

//OnRenderFrame uniforms
//======================
    //Textures array (uIn)
uniform sampler2D uTextures[5];
    //Map Ceiling array (uIn)
uniform isampler2D uMapFloor;
    //Map's size (uIn)
uniform vec2 uMapSize;
    //Y step value for quads in strip quad (uIn)
uniform float uStepSize;
    //Player's position (uIn)
uniform vec2 uPlayerPos;
    //Player's angle (uIn)
uniform float uPlayerAngle;
    //Player's pitch (uIn)
uniform float uPitch;

//OnLoad / OnFramebufferResize uniforms
//=====================================
    //Window's size
uniform vec2 uClientSize;
    //Minimum window's size (uIn)
uniform float uMinimumScreenSize;
    //TileSize (uIn)
uniform float uTileSize;
    //Distance shade value (uIn)
uniform float uDistanceShade;
//==============================================================

//Entry point
//==============================================================
void main()
{
    //Screen positions
    //====================================================================================
        //Strip quad Y1 - top
    float stripY1 = vStripQuadY.x;
        //Strip quad Y2 - bottom
    float stripY2 = vStripQuadY.y;
        //Strip quad's height
    float stripQuadHeight = max(0.0, stripY1 - stripY2);
        //Current pixel's Y from strip quad's top (Clamp to avoid weird values on borders)
    float pixelYInStrip = clamp(stripY1 - gl_FragCoord.y,0.0, stripQuadHeight);
        //Nth mini quad from the top in strip quad (starting from zero)
    float YStepIndex = floor(pixelYInStrip / uStepSize);
    //====================================================================================

    //World positions
    //=======================================================================================================================
        //Height of the player
    float cameraZ = uMinimumScreenSize / 2;
        //Y of the miniQuad's middle
    float rowY = (uClientSize.y / 2 - (gl_FragCoord.y + uPitch));
        //Distance of the miniQuad from the player
    float ceilingPixelDistance = ((cameraZ / rowY) * uTileSize) / cos(uPlayerAngle - rayAngle);
        //World X position of the pixel
    float ceilingPixelX = uPlayerPos.x + (cos(rayAngle) * ceilingPixelDistance);
        //World Y position of the pixel
    float ceilingPixelY = uPlayerPos.y + (sin(rayAngle) * ceilingPixelDistance);
    //=======================================================================================================================

    //Coloring
    //=====================================================================================================================
        //Strength of shading by distance
    float distanceShade = uDistanceShade / 255;
        //Current miniQuad's shade value based on it's distance from the player
    float shade = ceilingPixelDistance * distanceShade;
        //Selecting texture based on map array
    int texIndex = texelFetch(uMapFloor, ivec2(floor(ceilingPixelX / uTileSize), floor(ceilingPixelY / uTileSize)), 0).r;
        //If index is zero, empty tile
    if (texIndex == 0) discard;
        //Corresponding color's position in the selected texture
    vec2 uv = fract(vec2(ceilingPixelX, ceilingPixelY) / uTileSize);
        //Taking out the color from the selected texture
    vec4 tex = texture(uTextures[texIndex - 1], uv);
    if (distance(tex.rgb, vec3(255,0,220) /255) < 0.1) discard;
        //Returning the correct color
    FragColor = tex - shade;
    //=====================================================================================================================
}
//==============================================================