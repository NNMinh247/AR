using UnityEngine;

public class ARBodyTracker : MonoBehaviour
{
    [Header("Nguồn dữ liệu (BẮT BUỘC dùng 2D)")]
    public string annotationParentName = "Point List Annotation";

    [Header("Xương 3D của Model (Dùng để Auto-Fit)")]
    public string boneNamePrefix = "mixamorig:";
    public Transform armatureRoot;

    [Header("Tự động Căn chỉnh (Auto-Fit)")]
    public bool autoCalculateScale = true;
    public bool autoCalculateOffset = true;

    [Header("Giới hạn Kích thước (Chống sập Scale WebGL)")]
    [Tooltip("Chặn tính toán nếu Landmark bị chụm lại lỗi ở đầu frame (khoảng cách < 5cm)")]
    public float minimumValidTorsoHeight = 0.05f;
    [Tooltip("Kích thước tối thiểu không để model biến mất (Ví dụ: 0.5)")]
    public float minScaleLimit = 0.5f;
    [Tooltip("Kích thước tối đa không để model phình to quá màn hình (Ví dụ: 2.5)")]
    public float maxScaleLimit = 3000f;

    [Header("Cấu hình Thủ công (Dùng khi tắt Auto)")]
    public float manualReferenceTorsoHeight = 0.5f;
    public Vector3 manualPositionOffset = Vector3.zero;
    [Range(0.1f, 3f)] public float scaleMultiplier = 1f;

    [Header("Chung")]
    public bool mirrorLeftRight = true;
    public bool flipZ = false;
    [Range(0f, 1f)] public float smoothing = 0.3f;
    public bool showDebugLog = true;

    private Transform boneHips, boneLeftArm, boneRightArm;
    private Transform mpLeftShoulder, mpRightShoulder, mpLeftHip, mpRightHip;
    private Vector3 initialScale;
    private bool landmarksFound = false, bonesFound = false;
    private float searchTimer = 0f, autoReferenceTorsoHeight = 0.5f;

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
            UpdateScale();
            UpdatePosition();
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
                Vector3 modelShoulderCenter = (boneLeftArm.position + boneRightArm.position) / 2f;
                autoReferenceTorsoHeight = Vector3.Distance(modelShoulderCenter, boneHips.position);
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

        // [QUAN TRỌNG] Chặn Frame rác làm sập Scale
        if (currentTorsoHeight < minimumValidTorsoHeight) return;

        float refHeight = autoCalculateScale ? autoReferenceTorsoHeight : manualReferenceTorsoHeight;
        if (refHeight < 0.001f) refHeight = 0.5f; // Tránh chia cho 0

        float scaleFactor = (currentTorsoHeight / refHeight) * scaleMultiplier;

        // [QUAN TRỌNG] Kẹp giới hạn kích thước an toàn tuyệt đối
        scaleFactor = Mathf.Clamp(scaleFactor, minScaleLimit, maxScaleLimit);

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
            Vector3 rootToHipOffset = boneHips.position - transform.position;
            targetPos = anchorPos - rootToHipOffset;
        }
        else
        {
            targetPos += manualPositionOffset;
        }

        if (smoothing > 0f) transform.position = Vector3.Lerp(transform.position, targetPos, 1f - smoothing);
        else transform.position = targetPos;
    }
}