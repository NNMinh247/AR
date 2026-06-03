using UnityEngine;

public class ARBodyTracker : MonoBehaviour
{
    [Header("Nguồn dữ liệu (BẮT BUỘC dùng 2D)")]
    [Tooltip("Để model chạy theo người trên màn hình, BẮT BUỘC dùng Point List Annotation")]
    public string annotationParentName = "Point List Annotation";

    [Header("Xương 3D của Model (Dùng để Auto-Fit)")]
    public string boneNamePrefix = "mixamorig:";
    public Transform armatureRoot;

    [Header("Tự động Căn chỉnh (Auto-Fit)")]
    [Tooltip("Tự động đo chiều dài lưng của model 3D để phóng to cho vừa")]
    public bool autoCalculateScale = true;
    [Tooltip("Tự động đo khoảng cách từ gốc chân lên hông để bù trừ vị trí Y")]
    public bool autoCalculateOffset = true;

    [Header("Cấu hình Thủ công (Dùng khi tắt Auto)")]
    public float manualReferenceTorsoHeight = 0.5f;
    public Vector3 manualPositionOffset = Vector3.zero;
    [Range(0.1f, 3f)] public float scaleMultiplier = 1f;

    [Header("Chung")]
    public bool mirrorLeftRight = true;
    public bool flipZ = false;
    [Range(0f, 1f)] public float smoothing = 0.3f;
    public bool showDebugLog = true;

    // Xương Model nội bộ để đo đạc
    private Transform boneHips;
    private Transform boneLeftArm;
    private Transform boneRightArm;

    // Landmark 2D
    private Transform mpLeftShoulder, mpRightShoulder, mpLeftHip, mpRightHip;

    private Vector3 initialScale;
    private bool landmarksFound = false;
    private bool bonesFound = false;
    private float searchTimer = 0f;
    private float autoReferenceTorsoHeight = 0.5f;

    void Start()
    {
        initialScale = transform.localScale;
        TryFindBones();
    }

    void LateUpdate()
    {
        TryFindLandmarks();
        if (!landmarksFound) return;
        if (!bonesFound) TryFindBones();

        if (bonesFound)
        {
            UpdateScale(); // Bắt buộc Scale trước
            UpdatePosition(); // Tính toán vị trí dựa trên model đã được Scale
        }
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
            mpLeftShoulder = parentObj.transform.GetChild(11);
            mpRightShoulder = parentObj.transform.GetChild(12);
            mpLeftHip = parentObj.transform.GetChild(23);
            mpRightHip = parentObj.transform.GetChild(24);
        }
        else
        {
            mpLeftShoulder = parentObj.transform.GetChild(12);
            mpRightShoulder = parentObj.transform.GetChild(11);
            mpLeftHip = parentObj.transform.GetChild(24);
            mpRightHip = parentObj.transform.GetChild(23);
        }

        landmarksFound = true;
        if (showDebugLog) Debug.Log("🟢 [AR Tracker] Đã tìm thấy Landmark 2D.");
    }

    void TryFindBones()
    {
        if (armatureRoot == null)
        {
            Transform hips = FindInScene(boneNamePrefix + "Hips");
            if (hips != null) { armatureRoot = hips; while (armatureRoot.parent != null) armatureRoot = armatureRoot.parent; }
        }

        Transform searchRoot = armatureRoot != null ? armatureRoot : transform;
        boneHips = FindBoneRecursive(searchRoot, boneNamePrefix + "Hips");
        boneLeftArm = FindBoneRecursive(searchRoot, boneNamePrefix + "LeftArm");
        boneRightArm = FindBoneRecursive(searchRoot, boneNamePrefix + "RightArm");

        if (boneHips != null && boneLeftArm != null && boneRightArm != null)
        {
            bonesFound = true;
            if (autoCalculateScale)
            {
                // Tự động đo khoảng cách vật lý của 3D model
                Vector3 modelShoulderCenter = (boneLeftArm.position + boneRightArm.position) / 2f;
                autoReferenceTorsoHeight = Vector3.Distance(modelShoulderCenter, boneHips.position);
                if (showDebugLog) Debug.Log("🟢 [AR Tracker] Tự động đo lưng Model: " + autoReferenceTorsoHeight);
            }
        }
    }

    Transform FindInScene(string targetName)
    {
        var scene = gameObject.scene;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform result = FindBoneRecursive(root.transform, targetName);
            if (result != null) return result;
        }
        return null;
    }

    Transform FindBoneRecursive(Transform parent, string targetName)
    {
        if (parent.name == targetName) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindBoneRecursive(parent.GetChild(i), targetName);
            if (result != null) return result;
        }
        return null;
    }

    void UpdateScale()
    {
        if (mpLeftShoulder == null || mpRightShoulder == null || mpLeftHip == null || mpRightHip == null) return;

        Vector3 mpShoulderCenter = (mpLeftShoulder.position + mpRightShoulder.position) / 2f;
        Vector3 mpHipCenter = (mpLeftHip.position + mpRightHip.position) / 2f;

        float currentTorsoHeight = Vector3.Distance(mpShoulderCenter, mpHipCenter);
        if (currentTorsoHeight < 0.001f) return;

        // Dùng kích thước tự đo làm chuẩn
        float refHeight = autoCalculateScale ? autoReferenceTorsoHeight : manualReferenceTorsoHeight;
        float scaleFactor = (currentTorsoHeight / refHeight) * scaleMultiplier;

        Vector3 targetScale = initialScale * scaleFactor;

        if (smoothing > 0f) transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 1f - smoothing);
        else transform.localScale = targetScale;
    }

    void UpdatePosition()
    {
        if (mpLeftShoulder == null || mpRightShoulder == null || mpLeftHip == null || mpRightHip == null) return;

        Vector3 anchorPos = (mpLeftHip.position + mpRightHip.position) / 2f;
        if (flipZ) anchorPos.z = -anchorPos.z;

        Vector3 targetPos = anchorPos;

        if (autoCalculateOffset && boneHips != null)
        {
            // TỰ ĐỘNG BÙ TRỪ VỊ TRÍ
            // Đo khoảng cách từ Bàn chân (Transform) lên Hông (boneHips) sau khi đã scale
            Vector3 rootToHipOffset = boneHips.position - transform.position;

            // Trừ đi khoảng cách đó để Hông model dán chặt vào Hông MediaPipe
            targetPos = anchorPos - rootToHipOffset;
        }
        else
        {
            // Trở về thủ công nếu tắt Auto
            targetPos += manualPositionOffset;
        }

        if (smoothing > 0f) transform.position = Vector3.Lerp(transform.position, targetPos, 1f - smoothing);
        else transform.position = targetPos;
    }
}