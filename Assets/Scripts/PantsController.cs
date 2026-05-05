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
    public bool mirrorLeftRight = true;

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

        RotateBoneMixamo(mpLeftHip, mpLeftKnee, boneLeftUpLeg, leftUpLegInitialRot);
        RotateBoneMixamo(mpLeftKnee, mpLeftAnkle, boneLeftLeg, leftLegInitialRot);
        RotateBoneMixamo(mpRightHip, mpRightKnee, boneRightUpLeg, rightUpLegInitialRot);
        RotateBoneMixamo(mpRightKnee, mpRightAnkle, boneRightLeg, rightLegInitialRot);
    }

    void TryFindLandmarks()
    {
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
        if (boneHips == null) return;
        if (mpLeftHip == null || mpRightHip == null) return;
        if (mpLeftShoulder == null || mpRightShoulder == null) return;

        Vector2 leftShoulder2D = new Vector2(mpLeftShoulder.position.x, mpLeftShoulder.position.y);
        Vector2 rightShoulder2D = new Vector2(mpRightShoulder.position.x, mpRightShoulder.position.y);
        Vector2 leftHip2D = new Vector2(mpLeftHip.position.x, mpLeftHip.position.y);
        Vector2 rightHip2D = new Vector2(mpRightHip.position.x, mpRightHip.position.y);

        Vector2 shoulderCenter2D = (leftShoulder2D + rightShoulder2D) / 2f;
        Vector2 hipCenter2D = (leftHip2D + rightHip2D) / 2f;

        Vector2 torsoDir = shoulderCenter2D - hipCenter2D;
        float torsoHeight = torsoDir.magnitude;
        if (torsoHeight < 0.001f) return;

        // ROLL — nghiêng trái/phải quanh trục Z
        float rollAngle = Mathf.Atan2(-torsoDir.x, torsoDir.y) * Mathf.Rad2Deg;
        if (invertRoll) rollAngle = -rollAngle;

        // YAW — xoay trái/phải quanh trục Y (±45°)
        float currentShoulderWidth = Vector2.Distance(leftShoulder2D, rightShoulder2D);
        float widthRatio = Mathf.Clamp01(currentShoulderWidth / (torsoHeight * maxShoulderToTorsoRatio));
        float yawAbs = Mathf.Acos(widthRatio) * Mathf.Rad2Deg;

        float yawSign = 0f;
        float deltaZ = mpLeftShoulder.position.z - mpRightShoulder.position.z;
        if (Mathf.Abs(deltaZ) > zThreshold)
        {
            yawSign = Mathf.Sign(deltaZ);
        }
        if (invertYaw) yawSign = -yawSign;
        float yawAngle = yawSign * yawAbs;

        Quaternion rollRot = Quaternion.AngleAxis(rollAngle, Vector3.forward);
        Quaternion yawRot = Quaternion.AngleAxis(yawAngle, Vector3.up);

        Quaternion targetRotation = yawRot * rollRot * hipsInitialRot;
        if (invertBodyForward) targetRotation = Quaternion.AngleAxis(180, Vector3.up) * targetRotation;

        if (smoothing > 0f)
            boneHips.rotation = Quaternion.Slerp(boneHips.rotation, targetRotation, 1f - smoothing);
        else
            boneHips.rotation = targetRotation;
    }

    void RotateBoneMixamo(Transform startPoint, Transform endPoint, Transform targetBone,
                          Quaternion initialRotation)
    {
        if (startPoint == null || endPoint == null || targetBone == null) return;

        Vector3 direction = endPoint.position - startPoint.position;
        if (direction.magnitude < 0.001f) return;

        Vector3 currentBoneDirection = initialRotation * legForwardAxis;
        Quaternion deltaRotation = Quaternion.FromToRotation(currentBoneDirection, direction.normalized);
        Quaternion targetRotation = deltaRotation * initialRotation;

        if (smoothing > 0f)
            targetBone.rotation = Quaternion.Slerp(targetBone.rotation, targetRotation, 1f - smoothing);
        else
            targetBone.rotation = targetRotation;
    }
}