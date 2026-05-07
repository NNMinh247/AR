using UnityEngine;

public class ShirtController : MonoBehaviour
{
    [Header("Landmark")]
    public string annotationParentName = "Point List Annotation";

    [Header("Prefix xương Mixamo")]
    public string boneNamePrefix = "mixamorig:";

    [Header("Root xương (để trống = tự tìm)")]
    public Transform armatureRoot;

    [Header("MediaPipe Landmarks")]
    public Transform mpLeftShoulder;
    public Transform mpRightShoulder;
    public Transform mpLeftElbow;
    public Transform mpRightElbow;
    public Transform mpLeftWrist;
    public Transform mpRightWrist;

    [Header("Xương 3D")]
    public Transform boneHips;
    public Transform boneLeftArm;
    public Transform boneRightArm;
    public Transform boneLeftForeArm;
    public Transform boneRightForeArm;

    [Header("Hiệu chỉnh")]
    public Vector3 boneForwardAxis = Vector3.up;
    public bool mirrorLeftRight = false;
    public bool flipZ = true;

    private Quaternion hipsInitialRot;

    [Range(0f, 1f)]
    public float smoothing = 0.3f;

    [Header("Debug")]
    public bool showDebugLog = true;
    public bool dumpAllBoneNames = false;

    private Quaternion leftArmInitialRot, rightArmInitialRot;
    private Quaternion leftForeArmInitialRot, rightForeArmInitialRot;

    private bool bonesFound = false;
    private bool landmarksFound = false;
    private bool initialRotCached = false;
    private bool dumpedBones = false;
    private float searchTimer = 0f;

    void Start()
    {
        TryFindBones();
    }

    void LateUpdate()
    {
        TryFindLandmarks();
        if (!bonesFound) TryFindBones();

        if (dumpAllBoneNames && !dumpedBones && armatureRoot != null)
        {
            Debug.Log("===== [Áo] DUMP BONE NAMES =====");
            DumpBoneNames(armatureRoot, 0);
            Debug.Log("===== HẾT DUMP =====");
            dumpedBones = true;
        }

        if (!landmarksFound || !bonesFound) return;

        if (!initialRotCached)
        {
            CacheInitialRotations();
            initialRotCached = true;
        }

        RotateLimb(mpLeftShoulder, mpLeftElbow, boneLeftArm, leftArmInitialRot, boneForwardAxis);
        RotateLimb(mpLeftElbow, mpLeftWrist, boneLeftForeArm, leftForeArmInitialRot, boneForwardAxis);
        RotateLimb(mpRightShoulder, mpRightElbow, boneRightArm, rightArmInitialRot, boneForwardAxis);
        RotateLimb(mpRightElbow, mpRightWrist, boneRightForeArm, rightForeArmInitialRot, boneForwardAxis);
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
            mpLeftElbow = parentObj.transform.GetChild(14);
            mpRightElbow = parentObj.transform.GetChild(13);
            mpLeftWrist = parentObj.transform.GetChild(16);
            mpRightWrist = parentObj.transform.GetChild(15);
        }
        else
        {
            mpLeftShoulder = parentObj.transform.GetChild(11);
            mpRightShoulder = parentObj.transform.GetChild(12);
            mpLeftElbow = parentObj.transform.GetChild(13);
            mpRightElbow = parentObj.transform.GetChild(14);
            mpLeftWrist = parentObj.transform.GetChild(15);
            mpRightWrist = parentObj.transform.GetChild(16);
        }

        if (!landmarksFound)
        {
            landmarksFound = true;
            if (showDebugLog)
                Debug.Log("🟢 [Áo] Đã tìm thấy Landmark, mirror=" + mirrorLeftRight);
        }
    }

    void TryFindBones()
    {
        // 1. Tự động tìm gốc xương (Armature Root) nếu chưa có
        if (armatureRoot == null)
        {
            Transform foundHips = FindInScene(boneNamePrefix + "Hips");
            if (foundHips != null)
            {
                armatureRoot = foundHips;
                while (armatureRoot.parent != null) armatureRoot = armatureRoot.parent;
                if (showDebugLog)
                    Debug.Log("🟢 [Áo] Auto-detected armatureRoot: " + armatureRoot.name);
            }
        }

        Transform searchRoot = armatureRoot != null ? armatureRoot : transform;

        // 2. Tìm Xương Hông (BẮT BUỘC ĐỂ KHÔNG BỊ NGƯỢC TAY)
        boneHips = FindBoneRecursive(searchRoot, boneNamePrefix + "Hips");

        // 3. Tìm các Xương Tay
        boneLeftArm = FindBoneRecursive(searchRoot, boneNamePrefix + "LeftArm");
        boneRightArm = FindBoneRecursive(searchRoot, boneNamePrefix + "RightArm");
        boneLeftForeArm = FindBoneRecursive(searchRoot, boneNamePrefix + "LeftForeArm");
        boneRightForeArm = FindBoneRecursive(searchRoot, boneNamePrefix + "RightForeArm");

        if (boneLeftArm != null && boneRightArm != null &&
            boneLeftForeArm != null && boneRightForeArm != null && boneHips != null)
        {
            bonesFound = true;
            if (showDebugLog)
                Debug.Log("🟢 [Áo] Đã tìm thấy TẤT CẢ xương tay và xương Hông.");
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

    void DumpBoneNames(Transform parent, int depth)
    {
        string indent = new string(' ', depth * 2);
        Debug.Log(indent + "- " + parent.name);
        for (int i = 0; i < parent.childCount; i++)
            DumpBoneNames(parent.GetChild(i), depth + 1);
    }

    void CacheInitialRotations()
    {
        hipsInitialRot = boneHips.rotation;
        leftArmInitialRot = boneLeftArm.rotation;
        rightArmInitialRot = boneRightArm.rotation;
        leftForeArmInitialRot = boneLeftForeArm.rotation;
        rightForeArmInitialRot = boneRightForeArm.rotation;
        if (showDebugLog)
            Debug.Log("🟢 [Áo] Đã cache T-pose gốc.");
    }

    void RotateLimb(Transform startPoint, Transform endPoint, Transform targetBone, Quaternion initialRotation, Vector3 forwardAxis)
    {
        if (startPoint == null || endPoint == null || targetBone == null) return;

        Vector3 direction = endPoint.position - startPoint.position;
        if (direction.magnitude < 0.001f) return;

        // LẬT TRỤC Z: Ngăn tay thọc ngược qua ngực khi quay lưng
        if (flipZ) direction.z = -direction.z;

        // 1. Tính xem Cột sống (Hips) đã xoay bao nhiêu độ so với T-Pose
        Quaternion hipDelta = Quaternion.identity;
        if (boneHips != null)
        {
            hipDelta = boneHips.rotation * Quaternion.Inverse(hipsInitialRot);
        }

        // 2. Mang góc xoay của Cột sống cộng dồn vào T-Pose của tay
        Quaternion currentRestRotation = hipDelta * initialRotation;
        Vector3 currentBoneDirection = currentRestRotation * forwardAxis;

        // 3. Tính góc từ tư thế chuẩn MỚI đến vector của MediaPipe
        Quaternion targetRotation = Quaternion.FromToRotation(currentBoneDirection, direction.normalized) * currentRestRotation;

        if (smoothing > 0f)
            targetBone.rotation = Quaternion.Slerp(targetBone.rotation, targetRotation, 1f - smoothing);
        else
            targetBone.rotation = targetRotation;
    }
}