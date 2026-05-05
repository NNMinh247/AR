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
    public Transform boneLeftArm;
    public Transform boneRightArm;
    public Transform boneLeftForeArm;
    public Transform boneRightForeArm;

    [Header("Hiệu chỉnh")]
    public Vector3 boneForwardAxis = Vector3.up;
    public bool mirrorLeftRight = true;

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

        RotateBoneMixamo(mpLeftShoulder, mpLeftElbow, boneLeftArm, leftArmInitialRot);
        RotateBoneMixamo(mpLeftElbow, mpLeftWrist, boneLeftForeArm, leftForeArmInitialRot);
        RotateBoneMixamo(mpRightShoulder, mpRightElbow, boneRightArm, rightArmInitialRot);
        RotateBoneMixamo(mpRightElbow, mpRightWrist, boneRightForeArm, rightForeArmInitialRot);
    }

    void TryFindLandmarks()
    {
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
        if (armatureRoot == null)
        {
            Transform hips = FindInScene(boneNamePrefix + "Hips");
            if (hips != null)
            {
                armatureRoot = hips;
                while (armatureRoot.parent != null) armatureRoot = armatureRoot.parent;
                if (showDebugLog)
                    Debug.Log("🟢 [Áo] Auto-detected armatureRoot: " + armatureRoot.name);
            }
        }

        Transform searchRoot = armatureRoot != null ? armatureRoot : transform;

        boneLeftArm = FindBoneRecursive(searchRoot, boneNamePrefix + "LeftArm");
        boneRightArm = FindBoneRecursive(searchRoot, boneNamePrefix + "RightArm");
        boneLeftForeArm = FindBoneRecursive(searchRoot, boneNamePrefix + "LeftForeArm");
        boneRightForeArm = FindBoneRecursive(searchRoot, boneNamePrefix + "RightForeArm");

        if (boneLeftArm != null && boneRightArm != null &&
            boneLeftForeArm != null && boneRightForeArm != null)
        {
            bonesFound = true;
            if (showDebugLog)
                Debug.Log("🟢 [Áo] Đã tìm thấy TẤT CẢ xương tay.");
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
        leftArmInitialRot = boneLeftArm.rotation;
        rightArmInitialRot = boneRightArm.rotation;
        leftForeArmInitialRot = boneLeftForeArm.rotation;
        rightForeArmInitialRot = boneRightForeArm.rotation;
        if (showDebugLog)
            Debug.Log("🟢 [Áo] Đã cache T-pose gốc.");
    }

    void RotateBoneMixamo(Transform startPoint, Transform endPoint, Transform targetBone,
                          Quaternion initialRotation)
    {
        if (startPoint == null || endPoint == null || targetBone == null) return;

        Vector3 direction = endPoint.position - startPoint.position;
        if (direction.magnitude < 0.001f) return;

        Vector3 currentBoneDirection = initialRotation * boneForwardAxis;
        Quaternion deltaRotation = Quaternion.FromToRotation(currentBoneDirection, direction.normalized);
        Quaternion targetRotation = deltaRotation * initialRotation;

        if (smoothing > 0f)
            targetBone.rotation = Quaternion.Slerp(targetBone.rotation, targetRotation, 1f - smoothing);
        else
            targetBone.rotation = targetRotation;
    }
}