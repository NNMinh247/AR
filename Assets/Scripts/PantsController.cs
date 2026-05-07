using UnityEngine;

public class PantsController : MonoBehaviour
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
    public Transform mpLeftHip;
    public Transform mpRightHip;
    public Transform mpLeftKnee;
    public Transform mpRightKnee;
    public Transform mpLeftAnkle;
    public Transform mpRightAnkle;

    [Header("Xương 3D")]
    public Transform boneHips;
    public Transform boneLeftUpLeg;
    public Transform boneRightUpLeg;
    public Transform boneLeftLeg;
    public Transform boneRightLeg;

    [Header("Điều khiển vị trí toàn thân")]
    public bool controlBodyPosition = false;
    public float positionScale = 1f;
    public Vector3 positionOffset = Vector3.zero;

    [Header("Hiệu chỉnh chung")]
    public Vector3 legForwardAxis = Vector3.up;
    public bool mirrorLeftRight = false;
    public bool flipZ = true;

    [Header("Xoay Hips (thân)")]
    public bool rotateHips = true;
    public bool invertBodyForward = false;

    [Tooltip("Tỷ lệ vai/thân khi đối mặt cam. Tăng nếu model xoay quá sớm, giảm nếu không xoay.")]
    [Range(0.5f, 1.5f)]
    public float maxShoulderToTorsoRatio = 0.9f;

    [Tooltip("Ngưỡng chênh lệch Z để xác định hướng xoay. Tăng nếu model giật khi đứng yên.")]
    public float zThreshold = 0.02f;

    [Tooltip("Đảo dấu roll nếu nghiêng ngược chiều.")]
    public bool invertRoll = false;

    [Tooltip("Đảo dấu yaw nếu xoay ngược chiều.")]
    public bool invertYaw = false;

    [Range(0f, 1f)]
    public float smoothing = 0.3f;

    [Header("Debug")]
    public bool showDebugLog = true;
    public bool dumpAllBoneNames = false;

    private Quaternion hipsInitialRot;
    private Quaternion leftUpLegInitialRot, rightUpLegInitialRot;
    private Quaternion leftLegInitialRot, rightLegInitialRot;

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
            Debug.Log("===== [Quần] DUMP BONE NAMES =====");
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

        if (controlBodyPosition && boneHips != null && mpLeftHip != null && mpRightHip != null)
        {
            Vector3 centerHip = (mpLeftHip.position + mpRightHip.position) / 2f;
            Vector3 targetPos = centerHip * positionScale + positionOffset;

            if (smoothing > 0f)
                boneHips.position = Vector3.Lerp(boneHips.position, targetPos, 1f - smoothing);
            else
                boneHips.position = targetPos;
        }

        if (rotateHips) RotateHips();

        RotateLimb(mpLeftHip, mpLeftKnee, boneLeftUpLeg, leftUpLegInitialRot, legForwardAxis);
        RotateLimb(mpLeftKnee, mpLeftAnkle, boneLeftLeg, leftLegInitialRot, legForwardAxis);
        RotateLimb(mpRightHip, mpRightKnee, boneRightUpLeg, rightUpLegInitialRot, legForwardAxis);
        RotateLimb(mpRightKnee, mpRightAnkle, boneRightLeg, rightLegInitialRot, legForwardAxis);
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
            mpLeftKnee = parentObj.transform.GetChild(26);
            mpRightKnee = parentObj.transform.GetChild(25);
            mpLeftAnkle = parentObj.transform.GetChild(28);
            mpRightAnkle = parentObj.transform.GetChild(27);
        }
        else
        {
            mpLeftShoulder = parentObj.transform.GetChild(11);
            mpRightShoulder = parentObj.transform.GetChild(12);
            mpLeftHip = parentObj.transform.GetChild(23);
            mpRightHip = parentObj.transform.GetChild(24);
            mpLeftKnee = parentObj.transform.GetChild(25);
            mpRightKnee = parentObj.transform.GetChild(26);
            mpLeftAnkle = parentObj.transform.GetChild(27);
            mpRightAnkle = parentObj.transform.GetChild(28);
        }

        if (!landmarksFound)
        {
            landmarksFound = true;
            if (showDebugLog)
                Debug.Log("🟢 [Quần] Đã tìm thấy Landmark, mirror=" + mirrorLeftRight);
        }
    }

    void TryFindBones()
    {
        if (armatureRoot == null)
        {
            Transform hips = FindInScene(boneNamePrefix + "Hips");
            if (hips != null)
            {
                armatureRoot = hips;
                while (armatureRoot.parent != null) armatureRoot = armatureRoot.parent;
                if (showDebugLog)
                    Debug.Log("🟢 [Quần] Auto-detected armatureRoot: " + armatureRoot.name);
            }
        }

        Transform searchRoot = armatureRoot != null ? armatureRoot : transform;

        boneHips = FindBoneRecursive(searchRoot, boneNamePrefix + "Hips");
        boneLeftUpLeg = FindBoneRecursive(searchRoot, boneNamePrefix + "LeftUpLeg");
        boneRightUpLeg = FindBoneRecursive(searchRoot, boneNamePrefix + "RightUpLeg");
        boneLeftLeg = FindBoneRecursive(searchRoot, boneNamePrefix + "LeftLeg");
        boneRightLeg = FindBoneRecursive(searchRoot, boneNamePrefix + "RightLeg");

        if (boneHips != null && boneLeftUpLeg != null && boneRightUpLeg != null &&
            boneLeftLeg != null && boneRightLeg != null)
        {
            bonesFound = true;
            if (showDebugLog)
                Debug.Log("🟢 [Quần] Đã tìm thấy TẤT CẢ xương chân + Hips.");
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
        leftUpLegInitialRot = boneLeftUpLeg.rotation;
        rightUpLegInitialRot = boneRightUpLeg.rotation;
        leftLegInitialRot = boneLeftLeg.rotation;
        rightLegInitialRot = boneRightLeg.rotation;
        if (showDebugLog)
            Debug.Log("🟢 [Quần] Đã cache T-pose gốc.");
    }

    void RotateHips()
    {
        if (boneHips == null || mpLeftHip == null || mpRightHip == null ||
            mpLeftShoulder == null || mpRightShoulder == null) return;

        Vector3 shoulderCenter = (mpLeftShoulder.position + mpRightShoulder.position) / 2f;
        Vector3 hipCenter = (mpLeftHip.position + mpRightHip.position) / 2f;

        Vector3 upVector = (shoulderCenter - hipCenter).normalized;
        Vector3 rightVector = (mpRightShoulder.position - mpLeftShoulder.position).normalized;

        // 1. ÁP DỤNG Z-THRESHOLD (Chống giật rung lắc khi đứng thẳng)
        if (Mathf.Abs(mpRightShoulder.position.z - mpLeftShoulder.position.z) < zThreshold)
        {
            rightVector.z = 0;
            rightVector = rightVector.normalized;
        }

        // 2. XỬ LÝ INVERT ROLL VÀ YAW (Sửa lỗi lật trục khi soi gương)
        if (invertRoll) upVector.x = -upVector.x;
        if (invertYaw) rightVector.z = -rightVector.z;

        Vector3 forwardVector = Vector3.Cross(rightVector, upVector).normalized;
        if (invertBodyForward) forwardVector = -forwardVector;

        if (forwardVector == Vector3.zero || upVector == Vector3.zero) return;

        Quaternion absoluteRotation = Quaternion.LookRotation(forwardVector, upVector);
        Quaternion targetRotation = absoluteRotation * hipsInitialRot;

        if (smoothing > 0f)
            boneHips.rotation = Quaternion.Slerp(boneHips.rotation, targetRotation, 1f - smoothing);
        else
            boneHips.rotation = targetRotation;
    }

    // Thay thế hoàn toàn hàm RotateBoneMixamo cũ bằng hàm này
    void RotateLimb(Transform startPoint, Transform endPoint, Transform targetBone, Quaternion initialRotation, Vector3 forwardAxis)
    {
        if (startPoint == null || endPoint == null || targetBone == null) return;

        Vector3 direction = endPoint.position - startPoint.position;
        if (direction.magnitude < 0.001f) return;

        if (flipZ) direction.z = -direction.z;

        Quaternion hipDelta = Quaternion.identity;
        if (boneHips != null)
        {
            hipDelta = boneHips.rotation * Quaternion.Inverse(hipsInitialRot);
        }

        Quaternion currentRestRotation = hipDelta * initialRotation;
        Vector3 currentBoneDirection = currentRestRotation * forwardAxis;

        Quaternion targetRotation = Quaternion.FromToRotation(currentBoneDirection, direction.normalized) * currentRestRotation;

        if (smoothing > 0f)
            targetBone.rotation = Quaternion.Slerp(targetBone.rotation, targetRotation, 1f - smoothing);
        else
            targetBone.rotation = targetRotation;
    }
}