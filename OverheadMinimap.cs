using UnityEngine;
using ModTool.Interface;

// Enums must live outside the class so [Header] attributes land on fields, not enum declarations
public enum ScreenCorner { TopLeft, TopRight, BottomLeft, BottomRight, Custom }
public enum MinimapShape  { Rectangle, Circle }

/// <summary>
/// Overhead Terrain Minimap — Unity 2017 / C# 4.0 / ModTool compatible
///
/// POST-PROCESSING SETUP:
/// 1. Place MinimapPostProcess.shader in your Assets folder
/// 2. Create a Material: Right-click in Assets > Create > Material
/// 3. Name it "MinimapPostProcessMat"
/// 4. In the Material Inspector, set Shader dropdown to: Custom/MinimapPostProcess
/// 5. Drag this Material into the "postProcessMaterial" field on this script
/// 6. Enable usePostProcessing and adjust exposure/gamma
/// </summary>
public class OverheadMinimap : ModBehaviour
{
    // -------------------------------------------------------------------------
    //  REQUIRED REFERENCES
    // -------------------------------------------------------------------------
    [Header("--- Required References ---")]
    [Tooltip("The RenderTexture asset the minimap camera will render to.")]
    public RenderTexture minimapTexture;

    [Tooltip("Pre-placed EMPTY GameObject in the scene. Camera added via ModBehaviour.AddComponent.")]
    public GameObject minimapCameraHost;

    // -------------------------------------------------------------------------
    //  POST-PROCESSING (Recommended for dark terrains)
    // -------------------------------------------------------------------------
    [Header("--- Post-Processing (Brightens minimap only) ---")]
    [Tooltip("Enable post-processing to brighten the minimap without affecting the main scene.")]
    public bool usePostProcessing = true;

    [Tooltip("REQUIRED: Material with MinimapPostProcess shader. Create Material > Set shader to Custom/MinimapPostProcess.")]
    public Material postProcessMaterial;

    [Tooltip("Exposure multiplier. Higher = brighter minimap. Try 2-4 for dark terrain.")]
    [Range(0.5f, 8.0f)]
    public float exposure = 2.5f;

    [Tooltip("Gamma correction. Lower = brighter shadows. Try 0.6-0.8.")]
    [Range(0.3f, 2.0f)]
    public float gamma = 0.7f;

    // -------------------------------------------------------------------------
    //  MINIMAP LIGHTING (Optional)
    // -------------------------------------------------------------------------
    [Header("--- Minimap Lighting (Optional) ---")]
    public bool useMinimapLight = false;

    [Range(0f, 5.0f)]
    public float lightIntensity = 2.0f;

    public Color lightColor = Color.white;
    public bool useLayerIsolation = false;

    [Range(0, 31)]
    public int minimapLightLayer = 31;

    // -------------------------------------------------------------------------
    //  TOPOGRAPHIC LINES
    // -------------------------------------------------------------------------
    [Header("--- Topographic Contours ---")]
    public bool showTopographicLines = false;

    [Range(5f, 100f)]
    public float contourInterval = 25f;

    public Color contourColor = new Color(0f, 0f, 0f, 0.5f);

    [Range(1f, 5f)]
    public float contourThickness = 1.5f;

    [Range(256, 2048)]
    public int contourTextureResolution = 512;

    // -------------------------------------------------------------------------
    //  VISUAL ENHANCEMENTS
    // -------------------------------------------------------------------------
    [Header("--- Visual Enhancements ---")]
    [Range(0.5f, 10.0f)]
    public float brightness = 1.5f;

    [Range(0f, 1f)]
    public float ambientBrightness = 0.3f;

    [Range(0.5f, 2.0f)]
    public float contrast = 1.2f;

    [Range(0f, 2.0f)]
    public float saturation = 1.0f;

    public Color tintColor = Color.white;

    // -------------------------------------------------------------------------
    //  TERRAIN BOUNDARIES
    // -------------------------------------------------------------------------
    [Header("--- Terrain Boundaries ---")]
    public bool clampToTerrainBounds = true;

    [Range(0f, 100f)]
    public float boundaryPadding = 10f;

    // -------------------------------------------------------------------------
    //  DEBUG
    // -------------------------------------------------------------------------
    [Header("--- Debug ---")]
    public bool debugMode = true;
    public bool useManualPosition = false;
    public Vector3 manualCameraPosition = new Vector3(0f, 300f, 0f);

    // -------------------------------------------------------------------------
    //  CAMERA CONFIGURATION
    // -------------------------------------------------------------------------
    [Header("--- Camera Configuration ---")]
    public LayerMask minimapCullingMask = -1;
    public float cameraNearClip = 0.3f;
    public float cameraFarClip = 5000f;
    public float cameraDepth = -2f;

    // -------------------------------------------------------------------------
    //  PLAYER TRACKING
    // -------------------------------------------------------------------------
    [Header("--- Player Tracking ---")]
    public string playerObjectName = "Player_Human";
    public float cameraHeight = 300f;

    [Range(10f, 2000f)]
    public float orthographicSize = 150f;

    [Range(0f, 20f)]
    public float cameraFollowSmoothing = 0f;

    public bool absoluteHeight = true;

    // -------------------------------------------------------------------------
    //  SCREEN POSITION & SIZE
    // -------------------------------------------------------------------------
    [Header("--- Screen Position and Size ---")]
    public ScreenCorner anchor = ScreenCorner.TopRight;
    public Vector2 margin = new Vector2(10f, 10f);
    public Vector2 customPosition = new Vector2(10f, 10f);

    [Range(64f, 512f)]
    public float minimapWidth = 200f;

    [Range(64f, 512f)]
    public float minimapHeight = 200f;

    // -------------------------------------------------------------------------
    //  SHAPE
    // -------------------------------------------------------------------------
    [Header("--- Shape ---")]
    public MinimapShape shape = MinimapShape.Circle;

    // -------------------------------------------------------------------------
    //  BORDER
    // -------------------------------------------------------------------------
    [Header("--- Border ---")]
    public bool showBorder = true;

    [Range(1f, 20f)]
    public float borderThickness = 3f;

    public Color borderColor = Color.white;

    // -------------------------------------------------------------------------
    //  PLAYER INDICATOR
    // -------------------------------------------------------------------------
    [Header("--- Player Indicator ---")]
    public bool showPlayerIndicator = true;
    public Color playerIndicatorColor = Color.cyan;

    [Range(8f, 32f)]
    public float playerIndicatorSize = 16f;

    // -------------------------------------------------------------------------
    //  VISIBILITY & ALPHA
    // -------------------------------------------------------------------------
    [Header("--- Visibility and Alpha ---")]
    [Range(0f, 1f)]
    public float minimapAlpha = 0.85f;

    public bool hideMap = false;
    public KeyCode toggleKey = KeyCode.M;

    // -------------------------------------------------------------------------
    //  ZOOM
    // -------------------------------------------------------------------------
    [Header("--- Runtime Zoom ---")]
    public bool allowZoom = true;
    public KeyCode zoomInKey = KeyCode.Equals;
    public KeyCode zoomOutKey = KeyCode.Minus;
    public float zoomStep = 20f;

    [Range(10f, 2000f)]
    public float minZoom = 30f;

    [Range(10f, 2000f)]
    public float maxZoom = 800f;

    // -------------------------------------------------------------------------
    //  MAP ROTATION
    // -------------------------------------------------------------------------
    [Header("--- Map Rotation ---")]
    public bool rotateMapWithPlayer = false;

    // -------------------------------------------------------------------------
    //  COMPASS LABEL
    // -------------------------------------------------------------------------
    [Header("--- Compass Label ---")]
    public bool showCompassLabel = true;
    public string compassLabelText = "N";
    public Color compassLabelColor = Color.white;

    // -------------------------------------------------------------------------
    //  PRIVATE STATE
    // -------------------------------------------------------------------------
    private Camera    _minimapCamera;
    private Light     _minimapLight;
    private Transform _playerTransform;
    private bool      _visible           = true;
    private float     _currentOrthoSize;
    private bool      _cameraCreated     = false;

    // Post-processing
    private RenderTexture _processedTexture;
    private bool          _postProcessingReady = false;

    // Terrain boundary data
    private bool      _terrainBoundsKnown = false;
    private Vector3   _terrainMin;
    private Vector3   _terrainMax;
    private Vector3   _terrainCenter;
    private Terrain   _terrain;

    // Topographic contour data
    private Texture2D _contourTexture;
    private bool      _contourTextureReady = false;

    private Texture2D _borderTex;
    private Texture2D _arrowTex;
    private Texture2D _maskTex;
    private Rect      _mapRect;

    // =========================================================================
    //  START
    // =========================================================================
    private void Start()
    {
        if (minimapTexture == null)
        {
            Debug.LogError("[OverheadMinimap] minimapTexture is not assigned.");
            return;
        }

        if (minimapCameraHost == null)
        {
            Debug.LogError("[OverheadMinimap] minimapCameraHost is not assigned.");
            return;
        }

        // Add Camera component
        _minimapCamera = AddComponent<Camera>(minimapCameraHost);

        if (_minimapCamera == null)
        {
            Debug.LogError("[OverheadMinimap] Failed to add Camera component.");
            return;
        }

        // Configure camera
        _minimapCamera.enabled          = true;
        _minimapCamera.clearFlags       = CameraClearFlags.SolidColor;
        
        float ambientValue = ambientBrightness;
        _minimapCamera.backgroundColor  = new Color(ambientValue, ambientValue, ambientValue);
        
        _minimapCamera.cullingMask      = minimapCullingMask;
        _minimapCamera.orthographic     = true;
        _minimapCamera.orthographicSize = orthographicSize;
        _minimapCamera.nearClipPlane    = cameraNearClip;
        _minimapCamera.farClipPlane     = cameraFarClip;
        _minimapCamera.depth            = cameraDepth;
        _minimapCamera.targetTexture    = minimapTexture;
        _minimapCamera.rect             = new Rect(0, 0, 1, 1);
        _minimapCamera.aspect           = 1.0f;

        // Point straight down
        minimapCameraHost.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Setup post-processing
        if (usePostProcessing)
        {
            SetupPostProcessing();
        }

        // Add minimap light if enabled
        if (useMinimapLight)
        {
            CreateMinimapLight();
        }

        // Find and store terrain bounds
        FindTerrainBounds();

        // Generate contour lines if enabled
        if (showTopographicLines && _terrain != null)
        {
            GenerateContourTexture();
        }

        // Position camera
        if (!useManualPosition)
            PositionCameraInitial();
        else
            minimapCameraHost.transform.position = manualCameraPosition;

        _cameraCreated    = true;
        _currentOrthoSize = orthographicSize;

        if (debugMode)
        {
            Debug.Log("========== MINIMAP DEBUG ==========");
            Debug.Log("[OverheadMinimap] Post-Processing: " + usePostProcessing + 
                      " | Ready: " + _postProcessingReady);
            if (usePostProcessing && postProcessMaterial != null)
            {
                Debug.Log("[OverheadMinimap] Material: " + postProcessMaterial.name);
                Debug.Log("[OverheadMinimap] Shader: " + (postProcessMaterial.shader != null ? postProcessMaterial.shader.name : "NULL"));
                Debug.Log("[OverheadMinimap] Shader valid: " + (postProcessMaterial.shader != null && postProcessMaterial.shader.isSupported));
            }
            Debug.Log("[OverheadMinimap] Processed RT: " + (_processedTexture != null ? "Created" : "NULL"));
            Debug.Log("===================================");
        }

        // Find player
        GameObject playerGO = GameObject.Find(playerObjectName);
        if (playerGO != null)
            _playerTransform = playerGO.transform;

        // Build textures
        _borderTex = new Texture2D(1, 1);
        _borderTex.SetPixel(0, 0, Color.white);
        _borderTex.Apply();

        BuildArrowTexture();
        BuildCircleMask();
    }

    // =========================================================================
    //  SETUP POST-PROCESSING
    // =========================================================================
    private void SetupPostProcessing()
    {
        if (postProcessMaterial == null)
        {
            Debug.LogError("[OverheadMinimap] postProcessMaterial is not assigned! Post-processing disabled.");
            usePostProcessing = false;
            return;
        }

        // Check if shader is valid
        if (postProcessMaterial.shader == null)
        {
            Debug.LogError("[OverheadMinimap] Material has no shader assigned! Post-processing disabled.");
            usePostProcessing = false;
            return;
        }

        if (!postProcessMaterial.shader.isSupported)
        {
            Debug.LogError("[OverheadMinimap] Shader '" + postProcessMaterial.shader.name + 
                           "' is not supported on this platform! Post-processing disabled.");
            usePostProcessing = false;
            return;
        }

        // Create processed texture (same size as input)
        _processedTexture = new RenderTexture(
            minimapTexture.width,
            minimapTexture.height,
            0,
            RenderTextureFormat.ARGB32);

        _processedTexture.Create();

        _postProcessingReady = true;

        Debug.Log("[OverheadMinimap] Post-processing initialized successfully with shader: " + 
                  postProcessMaterial.shader.name);
    }

    // =========================================================================
    //  CREATE MINIMAP LIGHT
    // =========================================================================
    private void CreateMinimapLight()
    {
        _minimapLight = AddComponent<Light>(minimapCameraHost);

        if (_minimapLight == null)
        {
            Debug.LogError("[OverheadMinimap] Failed to create light.");
            return;
        }

        _minimapLight.type      = LightType.Directional;
        _minimapLight.intensity = lightIntensity;
        _minimapLight.color     = lightColor;
        _minimapLight.shadows   = LightShadows.None;

        if (useLayerIsolation)
            _minimapLight.cullingMask = 1 << minimapLightLayer;
        else
            _minimapLight.cullingMask = minimapCullingMask;
    }

    // =========================================================================
    //  APPLY POST-PROCESSING
    // =========================================================================
    private void ApplyPostProcessing()
    {
        if (!_postProcessingReady || postProcessMaterial == null || _processedTexture == null)
            return;

        try
        {
            // Set shader properties
            postProcessMaterial.SetFloat("_Exposure", exposure);
            postProcessMaterial.SetFloat("_Gamma", gamma);

            // Save current RenderTexture
            RenderTexture previous = RenderTexture.active;

            // Blit from raw minimap texture to processed texture with shader
            Graphics.Blit(minimapTexture, _processedTexture, postProcessMaterial);

            // Restore previous RenderTexture
            RenderTexture.active = previous;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[OverheadMinimap] Post-processing blit failed: " + e.Message);
            _postProcessingReady = false;
            usePostProcessing = false;
        }
    }

    // =========================================================================
    //  FIND TERRAIN BOUNDS
    // =========================================================================
    private void FindTerrainBounds()
    {
        _terrain = Object.FindObjectOfType<Terrain>();

        if (_terrain != null)
        {
            Vector3 terrainPos  = _terrain.transform.position;
            Vector3 terrainSize = _terrain.terrainData.size;

            _terrainMin = terrainPos;
            _terrainMax = terrainPos + terrainSize;
            _terrainCenter = terrainPos + (terrainSize * 0.5f);
            
            _terrainBoundsKnown = true;
        }
        else
        {
            _terrainBoundsKnown = false;
        }
    }

    // =========================================================================
    //  GENERATE CONTOUR TEXTURE
    // =========================================================================
    private void GenerateContourTexture()
    {
        if (_terrain == null) return;

        int resolution = contourTextureResolution;
        _contourTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);

        TerrainData terrainData = _terrain.terrainData;

        for (int py = 0; py < resolution; py++)
        {
            for (int px = 0; px < resolution; px++)
            {
                float u = (float)px / (resolution - 1);
                float v = (float)py / (resolution - 1);

                float height = terrainData.GetInterpolatedHeight(u, v);

                bool isContourLine = false;

                int checkRadius = 1;
                for (int dy = -checkRadius; dy <= checkRadius && !isContourLine; dy++)
                {
                    for (int dx = -checkRadius; dx <= checkRadius && !isContourLine; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = px + dx;
                        int ny = py + dy;

                        if (nx >= 0 && nx < resolution && ny >= 0 && ny < resolution)
                        {
                            float nu = (float)nx / (resolution - 1);
                            float nv = (float)ny / (resolution - 1);
                            float neighborHeight = terrainData.GetInterpolatedHeight(nu, nv);

                            float minH = Mathf.Min(height, neighborHeight);
                            float maxH = Mathf.Max(height, neighborHeight);

                            int minContour = Mathf.FloorToInt(minH / contourInterval);
                            int maxContour = Mathf.FloorToInt(maxH / contourInterval);

                            if (maxContour > minContour)
                            {
                                isContourLine = true;
                            }
                        }
                    }
                }

                if (isContourLine)
                    _contourTexture.SetPixel(px, py, contourColor);
                else
                    _contourTexture.SetPixel(px, py, Color.clear);
            }
        }

        _contourTexture.Apply();
        _contourTextureReady = true;
    }

    // =========================================================================
    //  POSITION CAMERA INITIAL
    // =========================================================================
    private void PositionCameraInitial()
    {
        if (_terrainBoundsKnown)
        {
            minimapCameraHost.transform.position = new Vector3(
                _terrainCenter.x,
                _terrainMax.y + cameraHeight,
                _terrainCenter.z);
        }
        else
        {
            minimapCameraHost.transform.position = new Vector3(0f, cameraHeight, 0f);
        }
    }

    // =========================================================================
    //  CLAMP CAMERA TO TERRAIN BOUNDS
    // =========================================================================
    private Vector3 ClampCameraPosition(Vector3 desiredPos)
    {
        if (!clampToTerrainBounds || !_terrainBoundsKnown)
            return desiredPos;

        float visibleRadius = _currentOrthoSize + boundaryPadding;

        float clampedX = Mathf.Clamp(
            desiredPos.x,
            _terrainMin.x + visibleRadius,
            _terrainMax.x - visibleRadius);

        float clampedZ = Mathf.Clamp(
            desiredPos.z,
            _terrainMin.z + visibleRadius,
            _terrainMax.z - visibleRadius);

        return new Vector3(clampedX, desiredPos.y, clampedZ);
    }

    // =========================================================================
    //  BUILD ARROW TEXTURE
    // =========================================================================
    private void BuildArrowTexture()
    {
        int size = 32;
        _arrowTex = new Texture2D(size, size, TextureFormat.ARGB32, false);

        float cx = size * 0.5f;
        float cy = size * 0.5f;

        for (int py = 0; py < size; py++)
        {
            for (int px = 0; px < size; px++)
            {
                float x = px - cx;
                float y = py - cy;

                bool isArrow = false;

                if (y < -2f && Mathf.Abs(x) < (-y * 0.5f))
                    isArrow = true;

                if (y >= -2f && y < 8f && Mathf.Abs(x) < 3f)
                    isArrow = true;

                _arrowTex.SetPixel(px, py, isArrow ? Color.white : Color.clear);
            }
        }

        _arrowTex.Apply();
    }

    // =========================================================================
    //  UPDATE
    // =========================================================================
    private void Update()
    {
        if (!_cameraCreated || _minimapCamera == null) return;

        // Update ambient brightness
        float ambientValue = ambientBrightness;
        _minimapCamera.backgroundColor = new Color(ambientValue, ambientValue, ambientValue);

        // Update light if it exists
        if (_minimapLight != null)
        {
            _minimapLight.intensity = lightIntensity;
            _minimapLight.color     = lightColor;
        }

        // Regenerate contours if needed
        if (showTopographicLines && !_contourTextureReady && _terrain != null)
        {
            GenerateContourTexture();
        }

        // Find player
        if (_playerTransform == null)
        {
            GameObject playerGO = GameObject.Find(playerObjectName);
            if (playerGO != null)
                _playerTransform = playerGO.transform;
        }

        // Toggle visibility
        if (Input.GetKeyDown(toggleKey))
            _visible = !_visible;

        // Zoom
        if (allowZoom)
        {
            if (Input.GetKeyDown(zoomInKey))
                _currentOrthoSize = Mathf.Clamp(_currentOrthoSize - zoomStep, minZoom, maxZoom);

            if (Input.GetKeyDown(zoomOutKey))
                _currentOrthoSize = Mathf.Clamp(_currentOrthoSize + zoomStep, minZoom, maxZoom);

            _minimapCamera.orthographicSize = _currentOrthoSize;
        }

        // Move camera
        if (!useManualPosition && _playerTransform != null)
        {
            float targetY = absoluteHeight
                ? cameraHeight
                : _playerTransform.position.y + cameraHeight;

            Vector3 desiredPos = new Vector3(
                _playerTransform.position.x,
                targetY,
                _playerTransform.position.z);

            Vector3 clampedPos = ClampCameraPosition(desiredPos);

            if (cameraFollowSmoothing <= 0f)
                minimapCameraHost.transform.position = clampedPos;
            else
                minimapCameraHost.transform.position = Vector3.Lerp(
                    minimapCameraHost.transform.position,
                    clampedPos,
                    Time.deltaTime * cameraFollowSmoothing);
        }

        minimapCameraHost.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    // =========================================================================
    //  OnGUI
    // =========================================================================
    private void OnGUI()
    {
        if (hideMap || !_visible || minimapTexture == null || _minimapCamera == null)
            return;

        // Apply post-processing if enabled
        if (usePostProcessing && _postProcessingReady)
            ApplyPostProcessing();

        _mapRect = ComputeMapRect();

        Color enhancedColor = new Color(
            tintColor.r * brightness,
            tintColor.g * brightness,
            tintColor.b * brightness,
            minimapAlpha);

        Color prevColor = GUI.color;
        GUI.color = enhancedColor;

        if (shape == MinimapShape.Circle)
            DrawCircleMinimap();
        else
            DrawRectMinimap();

        GUI.color = prevColor;

        // Debug overlay
        if (debugMode)
        {
            GUI.color = Color.yellow;
            GUIStyle debugStyle = new GUIStyle(GUI.skin.label);
            debugStyle.fontSize = 10;
            
            string debugText = "PostProcess: " + usePostProcessing + " Ready: " + _postProcessingReady +
                               "\nMaterial: " + (postProcessMaterial != null ? "OK" : "NULL") +
                               "\nExposure: " + exposure.ToString("F1") + 
                               " | Gamma: " + gamma.ToString("F2") +
                               "\nDisplay: " + (GetDisplayTexture() == _processedTexture ? "Processed" : "Raw");
            
            GUI.Label(new Rect(_mapRect.x, _mapRect.yMax + 5f, 300f, 80f), debugText, debugStyle);
            GUI.color = prevColor;
        }
    }

    // =========================================================================
    //  GET DISPLAY TEXTURE
    // =========================================================================
    private RenderTexture GetDisplayTexture()
    {
        if (usePostProcessing && _postProcessingReady && _processedTexture != null)
            return _processedTexture;
        else
            return minimapTexture;
    }

    // =========================================================================
    //  DRAWING
    // =========================================================================
    private void DrawRectMinimap()
    {
        float cx = _mapRect.x + _mapRect.width  * 0.5f;
        float cy = _mapRect.y + _mapRect.height * 0.5f;

        Matrix4x4 prevMatrix = GUI.matrix;

        if (rotateMapWithPlayer && _playerTransform != null)
            GUIUtility.RotateAroundPivot(-_playerTransform.eulerAngles.y, new Vector2(cx, cy));

        RenderTexture displayTex = GetDisplayTexture();
        GUI.DrawTexture(_mapRect, displayTex, ScaleMode.StretchToFill);

        if (showTopographicLines && _contourTextureReady)
            DrawContourOverlay();

        if (showBorder)          DrawBorderRect(_mapRect);
        if (showPlayerIndicator) DrawPlayerArrow();
        if (showCompassLabel)    DrawCompassLabel();

        GUI.matrix = prevMatrix;
    }

    private void DrawCircleMinimap()
    {
        float cx = _mapRect.x + _mapRect.width  * 0.5f;
        float cy = _mapRect.y + _mapRect.height * 0.5f;

        Matrix4x4 prevMatrix = GUI.matrix;

        if (rotateMapWithPlayer && _playerTransform != null)
            GUIUtility.RotateAroundPivot(-_playerTransform.eulerAngles.y, new Vector2(cx, cy));

        RenderTexture displayTex = GetDisplayTexture();
        GUI.DrawTexture(_mapRect, displayTex, ScaleMode.StretchToFill);

        if (showTopographicLines && _contourTextureReady)
            DrawContourOverlay();

        if (_maskTex != null)
        {
            Color c = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 1f);
            GUI.DrawTexture(_mapRect, _maskTex, ScaleMode.StretchToFill, true);
            GUI.color = c;
        }

        if (showBorder)          DrawBorderRect(_mapRect);
        if (showPlayerIndicator) DrawPlayerArrow();
        if (showCompassLabel)    DrawCompassLabel();

        GUI.matrix = prevMatrix;
    }

    // =========================================================================
    //  DRAW CONTOUR OVERLAY
    // =========================================================================
    private void DrawContourOverlay()
    {
        if (_contourTexture == null || !_terrainBoundsKnown) return;

        Vector3 camPos = minimapCameraHost.transform.position;
        float visibleSize = _currentOrthoSize * 2f;
        
        float terrainWidth  = _terrainMax.x - _terrainMin.x;
        float terrainDepth  = _terrainMax.z - _terrainMin.z;
        
        float centerU = (camPos.x - _terrainMin.x) / terrainWidth;
        float centerV = (camPos.z - _terrainMin.z) / terrainDepth;
        
        float sizeU = visibleSize / terrainWidth;
        float sizeV = visibleSize / terrainDepth;
        
        Rect sourceRect = new Rect(
            centerU - sizeU * 0.5f,
            centerV - sizeV * 0.5f,
            sizeU,
            sizeV);

        Color prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, contourColor.a * minimapAlpha);
        
        GUI.DrawTextureWithTexCoords(_mapRect, _contourTexture, sourceRect);
        
        GUI.color = prev;
    }

    private void DrawBorderRect(Rect r)
    {
        Color prev = GUI.color;
        GUI.color  = new Color(
            borderColor.r,
            borderColor.g,
            borderColor.b,
            borderColor.a * minimapAlpha);

        float t = borderThickness;
        GUI.DrawTexture(new Rect(r.x,        r.y,        r.width, t),        _borderTex);
        GUI.DrawTexture(new Rect(r.x,        r.yMax - t, r.width, t),        _borderTex);
        GUI.DrawTexture(new Rect(r.x,        r.y,        t,       r.height), _borderTex);
        GUI.DrawTexture(new Rect(r.xMax - t, r.y,        t,       r.height), _borderTex);

        GUI.color = prev;
    }

    private void DrawPlayerArrow()
    {
        if (_playerTransform == null || _arrowTex == null) return;

        float half = playerIndicatorSize * 0.5f;
        float cx   = _mapRect.x + _mapRect.width  * 0.5f;
        float cy   = _mapRect.y + _mapRect.height * 0.5f;
        Rect  arrow = new Rect(cx - half, cy - half, playerIndicatorSize, playerIndicatorSize);

        Color prev = GUI.color;
        GUI.color  = new Color(
            playerIndicatorColor.r,
            playerIndicatorColor.g,
            playerIndicatorColor.b,
            playerIndicatorColor.a * minimapAlpha);

        Matrix4x4 prevMatrix = GUI.matrix;

        if (!rotateMapWithPlayer)
            GUIUtility.RotateAroundPivot(_playerTransform.eulerAngles.y, new Vector2(cx, cy));

        GUI.DrawTexture(arrow, _arrowTex);

        GUI.matrix = prevMatrix;
        GUI.color = prev;
    }

    private void DrawCompassLabel()
    {
        GUIStyle style         = new GUIStyle(GUI.skin.label);
        style.normal.textColor = compassLabelColor;
        style.alignment        = TextAnchor.UpperCenter;
        style.fontStyle        = FontStyle.Bold;

        GUI.Label(
            new Rect(_mapRect.x, _mapRect.y + 2f, _mapRect.width, 20f),
            compassLabelText,
            style);
    }

    // =========================================================================
    //  LAYOUT
    // =========================================================================
    private Rect ComputeMapRect()
    {
        float sw = Screen.width;
        float sh = Screen.height;
        float mw = minimapWidth;
        float mh = minimapHeight;
        float x, y;

        switch (anchor)
        {
            case ScreenCorner.TopLeft:
                x = margin.x;
                y = margin.y;
                break;
            case ScreenCorner.TopRight:
                x = sw - mw - margin.x;
                y = margin.y;
                break;
            case ScreenCorner.BottomLeft:
                x = margin.x;
                y = sh - mh - margin.y;
                break;
            case ScreenCorner.BottomRight:
                x = sw - mw - margin.x;
                y = sh - mh - margin.y;
                break;
            default:
                x = customPosition.x;
                y = customPosition.y;
                break;
        }

        return new Rect(x, y, mw, mh);
    }

    // =========================================================================
    //  CIRCLE MASK
    // =========================================================================
    private void BuildCircleMask()
    {
        int   res    = 128;
        float half   = res * 0.5f;
        float radius = half - 1f;

        _maskTex = new Texture2D(res, res, TextureFormat.ARGB32, false);

        for (int py = 0; py < res; py++)
        {
            for (int px = 0; px < res; px++)
            {
                float dx   = px - half + 0.5f;
                float dy   = py - half + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                _maskTex.SetPixel(px, py, dist > radius
                    ? new Color(0f, 0f, 0f, 1f)
                    : Color.clear);
            }
        }

        _maskTex.Apply();
    }

    // =========================================================================
    //  CLEANUP
    // =========================================================================
    private void OnDestroy()
    {
        if (_minimapCamera != null)
            Destroy(_minimapCamera);

        if (_minimapLight != null)
            Destroy(_minimapLight);

        if (_processedTexture != null)
            Destroy(_processedTexture);

        if (_contourTexture != null)
            Destroy(_contourTexture);

        if (_borderTex != null) Destroy(_borderTex);
        if (_arrowTex  != null) Destroy(_arrowTex);
        if (_maskTex   != null) Destroy(_maskTex);
    }
}
