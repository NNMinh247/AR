using UnityEngine;

// Kế thừa ARClothingBase để dùng chung não bộ với Áo
public class PantsController : ARClothingBase
{
    [Header("Xương 3D - Quần")]
    public Transform boneLeftUpLeg;
    public Transform boneRightUpLeg;
    public Transform boneLeftLeg;
    public Transform boneRightLeg;

    [Header("Điều khiển vị trí toàn thân")]
    public bool controlBodyPosition = false;
    public float positionScale = 1f;
    public Vector3 positionOffset = Vector3.zero;

    [Header("Hiệu chỉnh Quần")]
    public Vector3 legForwardAxis = Vector3.up;

    [Header("Xoay Hips (thân)")]
    public bool rotateHips = true;
    public bool invertBodyForward = true;
    public float zThreshold = 0.02f;
    public bool invertRoll = false;
    public bool invertYaw = false;

    [Header("WebGL World Landmarks")]
    public bool useWebGLWorldLandmarks = true;
    public string worldAnnotationParentName = "World Point List Annotation";

    private Transform worldParent;
    private bool worldParentFound = false;
    private float worldSearchTimer = 0f;

    private Quaternion leftUpLegInitialRot, rightUpLegInitialRot, leftLegInitialRot, rightLegInitialRot;

    void Start()
    {
        TryFindBones();
    }

    void LateUpdate()
    {
        // 1. Lấy dữ liệu 2D/screen từ Point List Annotation
        TryFindLandmarksBase();
        if (!landmarksFound) return;

        // 2. Tìm xương Quần
        if (!bonesFound) TryFindBones();
        if (!bonesFound) return;

        if (!initialRotCached)
        {
            CacheInitialRotations();
            initialRotCached = true;
        }

        // 3. Option: Di chuyển Hips (Nếu dùng ARBodyTracker thì tắt cái này đi)
        if (controlBodyPosition && boneHips != null && mpLeftHip != null && mpRightHip != null)
        {
            Vector3 centerHip = (mpLeftHip.position + mpRightHip.position) / 2f;
            Vector3 targetPos = centerHip * positionScale + positionOffset;

            if (smoothing > 0f) boneHips.position = Vector3.Lerp(boneHips.position, targetPos, 1f - smoothing);
            else boneHips.position = targetPos;
        }

        // 4. Xoay cột sống
        if (rotateHips) RotateHips();

        // 5. Bẻ khớp chân
        RotateLimbBase(mpLeftHip, mpLeftKnee, boneLeftUpLeg, leftUpLegInitialRot, legForwardAxis);
        RotateLimbBase(mpLeftKnee, mpLeftAnkle, boneLeftLeg, leftLegInitialRot, legForwardAxis);
        RotateLimbBase(mpRightHip, mpRightKnee, boneRightUpLeg, rightUpLegInitialRot, legForwardAxis);
        RotateLimbBase(mpRightKnee, mpRightAnkle, boneRightLeg, rightLegInitialRot, legForwardAxis);
    }

    void TryFindWorldParent()
    {
        if (worldParentFound) return;

        worldSearchTimer += Time.deltaTime;
        if (worldSearchTimer < 0.5f) return;
        worldSearchTimer = 0f;

        GameObject parentObj = GameObject.Find(worldAnnotationParentName);
        if (parentObj == null || parentObj.transform.childCount < 33) return;

        worldParent = parentObj.transform;
        worldParentFound = true;
    }

    void TryFindBones()
    {
        if (armatureRoot == null)
        {
            Transform hips = FindInSceneBase(boneNamePrefix + "Hips");
            if (hips != null) { armatureRoot = hips; while (armatureRoot.parent != null) armatureRoot = armatureRoot.parent; }
        }

        Transform searchRoot = armatureRoot != null ? armatureRoot : transform;

        boneHips = FindBoneRecursiveBase(searchRoot, boneNamePrefix + "Hips");
        boneLeftUpLeg = FindBoneRecursiveBase(searchRoot, boneNamePrefix + "LeftUpLeg");
        boneRightUpLeg = FindBoneRecursiveBase(searchRoot, boneNamePrefix + "RightUpLeg");
        boneLeftLeg = FindBoneRecursiveBase(searchRoot, boneNamePrefix + "LeftLeg");
        boneRightLeg = FindBoneRecursiveBase(searchRoot, boneNamePrefix + "RightLeg");

        if (boneHips && boneLeftUpLeg && boneRightUpLeg && boneLeftLeg && boneRightLeg)
            bonesFound = true;
    }

    void CacheInitialRotations()
    {
        if (boneHips == null) return;
        hipsInitialRot = boneHips.rotation;
        leftUpLegInitialRot = boneLeftUpLeg.rotation;
        rightUpLegInitialRot = boneRightUpLeg.rotation;
        leftLegInitialRot = boneLeftLeg.rotation;
        rightLegInitialRot = boneRightLeg.rotation;
    }

    void RotateHips()
    {
        if (boneHips == null || mpLeftHip == null || mpRightHip == null ||
            mpLeftShoulder == null || mpRightShoulder == null) return;

        // GIỮ NGUYÊN logic cũ: X/Y từ 2D
        Vector3 shoulderCenter = (mpLeftShoulder.position + mpRightShoulder.position) / 2f;
        Vector3 hipCenter = (mpLeftHip.position + mpRightHip.position) / 2f;

        Vector3 upVector = (shoulderCenter - hipCenter).normalized;
        Vector3 rightVector = (mpRightShoulder.position - mpLeftShoulder.position).normalized;

        // GIỮ NGUYÊN logic cũ: chỉ nhét depth Z của world shoulder vào rightVector.z
        bool has3DDepth = false;

#if UNITY_WEBGL
        if (useWebGLWorldLandmarks)
        {
            TryFindWorldParent();

            if (worldParentFound && worldParent != null)
            {
                int indexL = mirrorLeftRight ? 12 : 11;
                int indexR = mirrorLeftRight ? 11 : 12;

                Vector3 shoulderL3D = worldParent.GetChild(indexL).position;
                Vector3 shoulderR3D = worldParent.GetChild(indexR).position;

                rightVector.z = (shoulderR3D - shoulderL3D).normalized.z;
                rightVector = rightVector.normalized;
                has3DDepth = true;
            }
        }
#endif

#if UNITY_EDITOR || !UNITY_WEBGL
        if (!has3DDepth && Mediapipe.Unity.Sample.PoseLandmarkDetection.PoseLandmarkerRunner.HasResult)
        {
            lock (Mediapipe.Unity.Sample.PoseLandmarkDetection.PoseLandmarkerRunner.DataLock)
            {
                var result = Mediapipe.Unity.Sample.PoseLandmarkDetection.PoseLandmarkerRunner.CurrentResult;
                if (result.poseWorldLandmarks != null && result.poseWorldLandmarks.Count > 0)
                {
                    var landmarks = result.poseWorldLandmarks[0].landmarks;

                    // GIỮ NGUYÊN index logic cũ của bạn
                    int indexL = mirrorLeftRight ? 12 : 11;
                    int indexR = mirrorLeftRight ? 11 : 12;

                    Vector3 shoulderL3D = new Vector3(-landmarks[indexL].x, -landmarks[indexL].y, landmarks[indexL].z);
                    Vector3 shoulderR3D = new Vector3(-landmarks[indexR].x, -landmarks[indexR].y, landmarks[indexR].z);

                    rightVector.z = (shoulderR3D - shoulderL3D).normalized.z;
                    rightVector = rightVector.normalized;
                    has3DDepth = true;
                }
            }
        }
#endif

        if (!has3DDepth && Mathf.Abs(mpRightShoulder.position.z - mpLeftShoulder.position.z) < zThreshold)
        {
            rightVector.z = 0;
            rightVector = rightVector.normalized;
        }

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
}
