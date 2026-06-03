using UnityEngine;

[System.Serializable]
public class MediaPipeLandmark
{
    // x/y/z: screen landmarks đã mirror theo hình video trong index.html.
    // z mặc định = 0 để không làm tay/chân bị xoắn hoặc model bị xa camera.
    public float x, y, z, visibility;

    // Full world landmarks, không bỏ world.
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

    [Tooltip("Tăng nếu model nhỏ, giảm nếu model lớn. Gợi ý: 20 - 40.")]
    public float screenScale = 30f;

    [Tooltip("ĐỂ FALSE với index_v3, vì HTML đã mirror x trước khi gửi sang Unity.")]
    public bool applyExtraMirrorX = false;

    [Tooltip("Nên để false. Nếu true sẽ đưa z vào Point List Annotation và có thể làm sai tay/model nhỏ.")]
    public bool useDepthInScreenPoints = false;

    public float screenDepthScale = 1f;

    [Header("World Landmarks - World Point List Annotation")]
    public Transform worldAnnotationParent;
    public string worldAnnotationParentName = "World Point List Annotation";

    [Tooltip("Scale world giống ARWorldPoseBridge cũ.")]
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
            if (i < parent.childCount)
            {
                arr[i] = parent.GetChild(i);
            }
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
        if (data == null || data.items == null || data.items.Length < 33)
            return;

        for (int i = 0; i < 33; i++)
        {
            MediaPipeLandmark lm = data.items[i];

            /*
              Point List Annotation:
              - dùng x/y screen đã mirror theo video.
              - z mặc định = 0.
              - Không dùng world x/y ở đây.
            */
            float sx = (lm.x - 0.5f) * screenScale;
            if (applyExtraMirrorX) sx = -sx;

            float sy = (0.5f - lm.y) * screenScale;
            float sz = useDepthInScreenPoints ? lm.z * screenDepthScale : 0f;

            if (screenPoints[i] != null)
                screenPoints[i].localPosition = new Vector3(sx, sy, sz);

            /*
              World Point List Annotation:
              Giữ đúng quy đổi của ARWorldPoseBridge cũ:
              new Vector3(-lm.x, -lm.y, lm.z) * scale
            */
            float wx = -lm.wx * worldScale;
            float wy = -lm.wy * worldScale;
            float wz = lm.wz * worldScale;

            if (worldPoints[i] != null)
                worldPoints[i].localPosition = new Vector3(wx, wy, wz);
        }

        frameCounter++;

        if (showDebugLog && debugEveryNFrames > 0 && frameCounter % debugEveryNFrames == 0)
        {
            Vector3 hip = (screenPoints[23].position + screenPoints[24].position) * 0.5f;
            Debug.Log("[WebBridge] Screen hip center = " + hip);
        }
    }
}
