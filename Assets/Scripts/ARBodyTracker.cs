using UnityEngine;

public class ARBodyTracker : MonoBehaviour
{
    [Header("Landmark")]
    public string annotationParentName = "Point List Annotation";

    [Header("MediaPipe Landmarks")]
    public Transform mpLeftShoulder;
    public Transform mpRightShoulder;
    public Transform mpLeftHip;
    public Transform mpRightHip;

    [Header("Vị trí (Position Tracking)")]
    public bool trackPosition = true;
    public AnchorPoint anchorPoint = AnchorPoint.CenterHip;
    public Vector3 positionOffset = Vector3.zero;

    [Header("Kích thước (Scale Tracking)")]
    public bool trackScale = true;

    [Tooltip("Chiều cao thân tham chiếu (khoảng cách vai-hông). Dùng Calibrate để tự cập nhật.")]
    public float referenceTorsoHeight = 0.5f;

    [Range(0.1f, 3f)]
    public float scaleMultiplier = 1f;

    [Header("Mirror & Flip")]
    public bool mirrorLeftRight = false;
    public bool flipZ = true;

    [Header("Làm mượt")]
    [Range(0f, 1f)]
    public float smoothing = 0.3f;

    [Header("Debug")]
    public bool showDebugLog = true;

    [Header("Tinh chỉnh nhanh (bấm trong Play Mode)")]
    [Tooltip("Tick ô này khi đang T-pose để calibrate referenceTorsoHeight.")]
    public bool calibrateTorsoHeight = false;
    private float searchTimer = 0f;

    public enum AnchorPoint
    {
        CenterHip,
        CenterShoulder,
        CenterBody
    }

    private Vector3 initialScale;
    private bool landmarksFound = false;

    void Start()
    {
        initialScale = transform.localScale;
    }

    void LateUpdate()
    {
        TryFindLandmarks();
        if (!landmarksFound) return;

        if (trackPosition) UpdatePosition();
        if (trackScale) UpdateScale();
    }

    void TryFindLandmarks()
    {
        if (landmarksFound) return;

        searchTimer += Time.deltaTime;
        if (searchTimer < 0.5f) return;
        searchTimer = 0f;

        GameObject parentObj = GameObject.Find(annotationParentName);
        if (parentObj == null || parentObj.transform.childCount < 33) return;

        if (mirrorLeftRight)
        {
            mpLeftShoulder = parentObj.transform.GetChild(12);
            mpRightShoulder = parentObj.transform.GetChild(11);
            mpLeftHip = parentObj.transform.GetChild(24);
            mpRightHip = parentObj.transform.GetChild(23);
        }
        else
        {
            mpLeftShoulder = parentObj.transform.GetChild(11);
            mpRightShoulder = parentObj.transform.GetChild(12);
            mpLeftHip = parentObj.transform.GetChild(23);
            mpRightHip = parentObj.transform.GetChild(24);
        }

        if (!landmarksFound)
        {
            landmarksFound = true;
            if (showDebugLog)
                Debug.Log("🟢 [AR] Đã tìm thấy Landmark.");
        }
    }

    void UpdatePosition()
    {
        if (mpLeftShoulder == null || mpRightShoulder == null ||
            mpLeftHip == null || mpRightHip == null) return;

        Vector3 anchor = Vector3.zero;
        switch (anchorPoint)
        {
            case AnchorPoint.CenterHip:
                anchor = (mpLeftHip.position + mpRightHip.position) / 2f;
                break;
            case AnchorPoint.CenterShoulder:
                anchor = (mpLeftShoulder.position + mpRightShoulder.position) / 2f;
                break;
            case AnchorPoint.CenterBody:
                Vector3 sc = (mpLeftShoulder.position + mpRightShoulder.position) / 2f;
                Vector3 hc = (mpLeftHip.position + mpRightHip.position) / 2f;
                anchor = (sc + hc) / 2f;
                break;
        }

        Vector3 targetPos = anchor + positionOffset;
        if (flipZ) targetPos.z = -targetPos.z;

        if (smoothing > 0f)
            transform.position = Vector3.Lerp(transform.position, targetPos, 1f - smoothing);
        else
            transform.position = targetPos;
    }

    void UpdateScale()
    {
        if (mpLeftShoulder == null || mpRightShoulder == null ||
            mpLeftHip == null || mpRightHip == null) return;

        Vector3 shoulderCenter = (mpLeftShoulder.position + mpRightShoulder.position) / 2f;
        Vector3 hipCenter = (mpLeftHip.position + mpRightHip.position) / 2f;
        float currentTorsoHeight = Vector3.Distance(shoulderCenter, hipCenter);
        if (currentTorsoHeight < 0.001f) return;

        float scaleFactor = (currentTorsoHeight / referenceTorsoHeight) * scaleMultiplier;
        Vector3 targetScale = initialScale * scaleFactor;

        if (smoothing > 0f)
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 1f - smoothing);
        else
            transform.localScale = targetScale;
    }

    void OnValidate()
    {
        if (calibrateTorsoHeight && Application.isPlaying &&
            mpLeftShoulder != null && mpRightShoulder != null &&
            mpLeftHip != null && mpRightHip != null)
        {
            Vector3 sc = (mpLeftShoulder.position + mpRightShoulder.position) / 2f;
            Vector3 hc = (mpLeftHip.position + mpRightHip.position) / 2f;
            referenceTorsoHeight = Vector3.Distance(sc, hc);
            Debug.Log("🟢 [AR] Calibrated torso height: " + referenceTorsoHeight);
            calibrateTorsoHeight = false;
        }
    }
}