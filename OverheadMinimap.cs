using UnityEngine;
using ModTool.Interface;

// Enums must live outside the class so [Header] attributes land on fields, not enum declarations
public enum ScreenCorner { TopLeft, TopRight, BottomLeft, BottomRight, Custom }
public enum MinimapShape  { Rectangle, Circle }

/// <summary>
/// Overhead Terrain Minimap — Unity 2017 / C# 4.0 / ModTool compatible
///
/// NEW: Terrain boundary clamping - camera never shows areas outside terrain bounds
/// This prevents gray/black areas from appearing on the minimap
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
    //  VISUAL ENHANCEMENTS
    // -------------------------------------------------------------------------
    [Header("--- Visual Enhancements ---")]
    [Tooltip("Brightness multiplier. Try 3-5 for dark terrain.")]
    [Range(0.5f, 10.0f)]
    public float brightness = 3.0f;

    [Tooltip("Ambient brightness - lightens camera background.")]
    [Range(0f, 1f)]
    public float ambientBrightness = 0.3f;

    [Tooltip("Contrast adjustment.")]
    [Range(0.5f, 2.0f)]
    public float contrast = 1.2f;

    [Tooltip("Color saturation.")]
    [Range(0f, 2.0f)]
    public float saturation = 1.0f;

    [Tooltip("Tint color.")]
    public Color tintColor = Color.white;

    // -------------------------------------------------------------------------
    //  TERRAIN BOUNDARIES
    // -------------------------------------------------------------------------
    [Header("--- Terrain Boundaries ---")]
    [Tooltip("Clamp camera to terrain bounds to prevent showing gray areas outside terrain. " +
             "Automatically enabled if terrain is found.")]
    public bool clampToTerrainBounds = true;

    [Tooltip("Extra padding inside terrain edges (in world units). Increase if edges still show.")]
    [Range(0f, 100f)]
    public float boundaryPadding = 10f;

    // -------------------------------------------------------------------------
    //  DEBUG
    // -------------------------------------------------------------------------
    [Header("--- Debug ---")]
    [Tooltip("Enable detailed logging.")]
    public bool debugMode = false;

    [Tooltip("Manually position camera.")]
    public bool useManualPosition = false;

    [Tooltip("Manual camera position.")]
    public Vector3 manualCameraPosition = new Vector3(0f, 300f, 0f);

    // -------------------------------------------------------------------------
    //  CAMERA CONFIGURATION
    // -------------------------------------------------------------------------
    [Header("--- Camera Configuration ---")]
    [Tooltip("Which layers to render. -1 = Everything.")]
    public LayerMask minimapCullingMask = -1;

    [Tooltip("Near clip plane.")]
    public float cameraNearClip = 0.3f;

    [Tooltip("Far clip plane.")]
    public float cameraFarClip = 5000f;

    [Tooltip("Camera render depth.")]
    public float cameraDepth = -2f;

    // -------------------------------------------------------------------------
    //  PLAYER TRACKING
    // -------------------------------------------------------------------------
    [Header("--- Player Tracking ---")]
    [Tooltip("Player GameObject name.")]
    public string playerObjectName = "Player_Human";

    [Tooltip("Height above player.")]
    public float cameraHeight = 300f;

    [Tooltip("Orthographic size - how much terrain is visible. REDUCED from 1227!")]
    [Range(10f, 2000f)]
    public float orthographicSize = 150f;

    [Tooltip("Camera follow smoothing.")]
    [Range(0f, 20f)]
    public float cameraFollowSmoothing = 0f;

    [Tooltip("Lock camera Y to cameraHeight.")]
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
    private Transform _playerTransform;
    private bool      _visible           = true;
    private float     _currentOrthoSize;
    private bool      _cameraCreated     = false;

    // Terrain boundary data
    private bool      _terrainBoundsKnown = false;
    private Vector3   _terrainMin;
    private Vector3   _terrainMax;
    private Vector3   _terrainCenter;

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

        // Find and store terrain bounds
        FindTerrainBounds();

        // Position camera
        if (!useManualPosition)
            PositionCameraInitial();
        else
            minimapCameraHost.transform.position = manualCameraPosition;

        _cameraCreated    = true;
        _currentOrthoSize = orthographicSize;

        if (debugMode)
        {
            Debug.Log("========== MINIMAP DEBUG INFO ==========");
            Debug.Log("[OverheadMinimap] Camera Position: " + minimapCameraHost.transform.position);
            Debug.Log("[OverheadMinimap] Camera Rotation: " + minimapCameraHost.transform.rotation.eulerAngles);
            Debug.Log("[OverheadMinimap] Orthographic Size: " + _minimapCamera.orthographicSize);
            Debug.Log("[OverheadMinimap] Terrain Bounds: " + _terrainMin + " to " + _terrainMax);
            Debug.Log("[OverheadMinimap] Clamping Enabled: " + clampToTerrainBounds);
            Debug.Log("========================================");
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
    //  FIND TERRAIN BOUNDS
    // =========================================================================
    private void FindTerrainBounds()
    {
        Terrain terrain = Object.FindObjectOfType<Terrain>();

        if (terrain != null)
        {
            Vector3 terrainPos  = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;

            _terrainMin = terrainPos;
            _terrainMax = terrainPos + terrainSize;
            _terrainCenter = terrainPos + (terrainSize * 0.5f);
            
            _terrainBoundsKnown = true;

            if (debugMode)
            {
                Debug.Log("[OverheadMinimap] Terrain: " + terrain.name);
                Debug.Log("[OverheadMinimap] Terrain Position: " + terrainPos);
                Debug.Log("[OverheadMinimap] Terrain Size: " + terrainSize);
                Debug.Log("[OverheadMinimap] Terrain Bounds: Min=" + _terrainMin + ", Max=" + _terrainMax);
            }
        }
        else
        {
            _terrainBoundsKnown = false;
            Debug.LogWarning("[OverheadMinimap] No Terrain found - boundary clamping disabled.");
        }
    }

    // =========================================================================
    //  POSITION CAMERA INITIAL
    // =========================================================================
    private void PositionCameraInitial()
    {
        if (_terrainBoundsKnown)
        {
            // Start at terrain center
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
    //  Prevents showing gray areas outside terrain
    // =========================================================================
    private Vector3 ClampCameraPosition(Vector3 desiredPos)
    {
        if (!clampToTerrainBounds || !_terrainBoundsKnown)
            return desiredPos;

        // Calculate how much area the camera can see at current orthographic size
        float visibleRadius = _currentOrthoSize + boundaryPadding;

        // Clamp X and Z so the camera never shows areas outside terrain bounds
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

                // Arrow head
                if (y < -2f && Mathf.Abs(x) < (-y * 0.5f))
                    isArrow = true;

                // Arrow shaft
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

        // Move camera (with boundary clamping)
        if (!useManualPosition && _playerTransform != null)
        {
            float targetY = absoluteHeight
                ? cameraHeight
                : _playerTransform.position.y + cameraHeight;

            Vector3 desiredPos = new Vector3(
                _playerTransform.position.x,
                targetY,
                _playerTransform.position.z);

            // Clamp to terrain bounds
            Vector3 clampedPos = ClampCameraPosition(desiredPos);

            if (cameraFollowSmoothing <= 0f)
                minimapCameraHost.transform.position = clampedPos;
            else
                minimapCameraHost.transform.position = Vector3.Lerp(
                    minimapCameraHost.transform.position,
                    clampedPos,
                    Time.deltaTime * cameraFollowSmoothing);
        }

        // Enforce rotation
        minimapCameraHost.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    // =========================================================================
    //  OnGUI
    // =========================================================================
    private void OnGUI()
    {
        if (hideMap || !_visible || minimapTexture == null || _minimapCamera == null)
            return;

        _mapRect = ComputeMapRect();

        // Apply brightness
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
            
            Vector3 camPos = minimapCameraHost.transform.position;
            string debugText = "Cam: (" + camPos.x.ToString("F0") + ", " + camPos.z.ToString("F0") + ")" +
                               "\nOrtho: " + _currentOrthoSize.ToString("F0") +
                               "\nBounds: " + clampToTerrainBounds;
            
            if (_playerTransform != null)
            {
                Vector3 pPos = _playerTransform.position;
                debugText += "\nPlayer: (" + pPos.x.ToString("F0") + ", " + pPos.z.ToString("F0") + ")";
                
                // Show if player is near edge
                if (_terrainBoundsKnown && clampToTerrainBounds)
                {
                    float distToEdge = Mathf.Min(
                        pPos.x - _terrainMin.x,
                        _terrainMax.x - pPos.x,
                        pPos.z - _terrainMin.z,
                        _terrainMax.z - pPos.z);
                    
                    if (distToEdge < _currentOrthoSize)
                        debugText += "\n[NEAR EDGE]";
                }
            }
            
            GUI.Label(new Rect(_mapRect.x, _mapRect.yMax + 5f, 300f, 100f), debugText, debugStyle);
            GUI.color = prevColor;
        }
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

        GUI.DrawTexture(_mapRect, minimapTexture, ScaleMode.StretchToFill);

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

        GUI.DrawTexture(_mapRect, minimapTexture, ScaleMode.StretchToFill);

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

        if (_borderTex != null) Destroy(_borderTex);
        if (_arrowTex  != null) Destroy(_arrowTex);
        if (_maskTex   != null) Destroy(_maskTex);
    }
}
