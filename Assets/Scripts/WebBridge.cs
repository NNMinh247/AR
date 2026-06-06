using UnityEngine;

[System.Serializable]
public class MediaPipeLandmark
{
    public float x, y, z, visibility;
    public float wx, wy, wz;
}

[System.Serializable]
public class LandmarkWrapper
{
    public MediaPipeLandmark[] items;
}

public class WebBridge : MonoBehaviour
{
    [Header("Screen Landmarks - Point List Annotation")]
    public Transform annotationParent;
    public string annotationParentName = "Point List Annotation";

    [Header("WebGL Auto-Scaling (Giải pháp chống thu nhỏ)")]
    [Tooltip("BẬT ĐỂ TỰ ĐỘNG KHỚP TỌA ĐỘ VỚI CAMERA BẤT CHẤP KÍCH THƯỚC WEBGL")]
    public bool autoMatchCameraBounds = true;

    [Tooltip("Tỷ lệ khung hình thực tế của Webcam (Thường là 16:9 = 1.777 hoặc 4:3 = 1.333). Điều này giúp khung xương không bị méo!")]
    public float webcamAspectRatio = 1.777f;

    [Tooltip("Nếu TẮT Auto Match ở trên, nó sẽ dùng hệ số thủ công này")]
    public float fallbackScreenScale = 30f;

    [Header("Extra Settings")]
    public bool applyExtraMirrorX = false;
    public bool useDepthInScreenPoints = false;
    public float screenDepthScale = 1f;

    [Header("World Landmarks - World Point List Annotation")]
    public Transform worldAnnotationParent;
    public string worldAnnotationParentName = "World Point List";
    public float worldScale = 100f;

    [Header("Debug")]
    public bool showDebugLog = true;
    public int debugEveryNFrames = 120;

    private readonly Transform[] screenPoints = new Transform[33];
    private readonly Transform[] worldPoints = new Transform[33];

    private bool initialized = false;
    private int frameCounter = 0;

    void Start()
    {
        EnsureInitialized();
    }

    void EnsureInitialized()
    {
        if (initialized) return;

        if (annotationParent == null)
        {
            GameObject obj = GameObject.Find(annotationParentName);
            if (obj == null) obj = new GameObject(annotationParentName);
            annotationParent = obj.transform;
        }

        if (worldAnnotationParent == null)
        {
            GameObject obj = GameObject.Find(worldAnnotationParentName);
            if (obj == null) obj = new GameObject(worldAnnotationParentName);
            worldAnnotationParent = obj.transform;
        }

        InitPoints(annotationParent, screenPoints, "Point_");
        InitPoints(worldAnnotationParent, worldPoints, "WorldPoint_");

        initialized = true;
    }

    void InitPoints(Transform parent, Transform[] arr, string prefix)
    {
        if (parent == null) return;
        for (int i = 0; i < 33; i++)
        {
            if (i < parent.childCount) arr[i] = parent.GetChild(i);
            else
            {
                GameObject pt = new GameObject(prefix + i);
                pt.transform.SetParent(parent);
                arr[i] = pt.transform;
            }
        }
    }

    public void ReceiveLandmarks(string jsonString)
    {
        EnsureInitialized();

        LandmarkWrapper data = JsonUtility.FromJson<LandmarkWrapper>(jsonString);
        if (data == null || data.items == null || data.items.Length < 33) return;

        // --- BƯỚC QUAN TRỌNG NHẤT: TÍNH TOÁN KÍCH THƯỚC WORLD SPACE ---
        // --- BƯỚC QUAN TRỌNG NHẤT: TÍNH TOÁN KÍCH THƯỚC WORLD SPACE ---
        float worldHeight = fallbackScreenScale;
        float worldWidth = fallbackScreenScale * webcamAspectRatio;

        if (autoMatchCameraBounds && Camera.main != null)
        {
            // Lấy khoảng cách từ Camera đến Parent Object
            float distance = Mathf.Abs(Camera.main.transform.position.z - annotationParent.position.z);
            if (distance < 0.1f) distance = 10f; // Dự phòng an toàn

            // Tính chiều cao không gian 3D dựa vào FOV của Camera
            if (Camera.main.orthographic)
            {
                worldHeight = Camera.main.orthographicSize * 2f;
            }
            else
            {
                worldHeight = 2.0f * distance * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
            }

            // [ĐÃ SỬA] Đo tỷ lệ màn hình WebGL an toàn (Chống lỗi chia cho 0)
            float actualAspectRatio = 1.777f;
            if (Screen.height > 0)
            {
                actualAspectRatio = (float)Screen.width / Screen.height;
            }

            worldWidth = worldHeight * actualAspectRatio;
        }

        for (int i = 0; i < 33; i++)
        {
            MediaPipeLandmark lm = data.items[i];

            // Áp dụng tính toán Screen Points với World Width & Height chuẩn
            float sx = (lm.x - 0.5f) * worldWidth;
            if (applyExtraMirrorX) sx = -sx;

            float sy = (0.5f - lm.y) * worldHeight;
            float sz = useDepthInScreenPoints ? lm.z * screenDepthScale : 0f;

            if (screenPoints[i] != null)
                screenPoints[i].localPosition = new Vector3(sx, sy, sz);

            // World Points (Dữ liệu 3D thực từ thuật toán MediaPipe)
            float wx = -lm.wx * worldScale;
            float wy = -lm.wy * worldScale;
            float wz = lm.wz * worldScale;

            if (worldPoints[i] != null)
                worldPoints[i].localPosition = new Vector3(wx, wy, wz);
        }

        frameCounter++;
        if (showDebugLog && debugEveryNFrames > 0 && frameCounter % debugEveryNFrames == 0)
        {
            Debug.Log($"[WebBridge] Auto Bounds={autoMatchCameraBounds}, Width={worldWidth}, Height={worldHeight}");
        }
    }
}