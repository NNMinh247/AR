using UnityEngine;

public class ShirtController : ARClothingBase
{
    [Header("Xương 3D - Áo")]
    public Transform boneLeftArm;
    public Transform boneRightArm;
    public Transform boneLeftForeArm;
    public Transform boneRightForeArm;

    [Header("Hiệu chỉnh Áo")]
    public Vector3 boneForwardAxis = Vector3.up;

    private Quaternion leftArmInitialRot, rightArmInitialRot;
    private Quaternion leftForeArmInitialRot, rightForeArmInitialRot;

    void Start()
    {
        TryFindBones();
    }

    void LateUpdate()
    {
        TryFindLandmarksBase();
        if (!landmarksFound) return;

        if (!bonesFound) TryFindBones();
        if (!bonesFound) return;

        if (!initialRotCached)
        {
            CacheInitialRotations();
            initialRotCached = true;
        }

        // Chọn lọc điểm an toàn
        Transform lShoulder = PickRotationPoint(worldLeftShoulder, mpLeftShoulder);
        Transform lElbow = PickRotationPoint(worldLeftElbow, mpLeftElbow);
        Transform lWrist = PickRotationPoint(worldLeftWrist, mpLeftWrist);

        Transform rShoulder = PickRotationPoint(worldRightShoulder, mpRightShoulder);
        Transform rElbow = PickRotationPoint(worldRightElbow, mpRightElbow);
        Transform rWrist = PickRotationPoint(worldRightWrist, mpRightWrist);

        RotateLimbBase(lShoulder, lElbow, boneLeftArm, leftArmInitialRot, boneForwardAxis);
        RotateLimbBase(lElbow, lWrist, boneLeftForeArm, leftForeArmInitialRot, boneForwardAxis);

        RotateLimbBase(rShoulder, rElbow, boneRightArm, rightArmInitialRot, boneForwardAxis);
        RotateLimbBase(rElbow, rWrist, boneRightForeArm, rightForeArmInitialRot, boneForwardAxis);
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
        boneLeftArm = FindBoneRecursiveBase(searchRoot, boneNamePrefix + "LeftArm");
        boneRightArm = FindBoneRecursiveBase(searchRoot, boneNamePrefix + "RightArm");
        boneLeftForeArm = FindBoneRecursiveBase(searchRoot, boneNamePrefix + "LeftForeArm");
        boneRightForeArm = FindBoneRecursiveBase(searchRoot, boneNamePrefix + "RightForeArm");

        if (boneHips && boneLeftArm && boneRightArm && boneLeftForeArm && boneRightForeArm)
            bonesFound = true;
    }

    void CacheInitialRotations()
    {
        if (boneHips == null) return;
        hipsInitialRot = boneHips.rotation;
        leftArmInitialRot = boneLeftArm.rotation;
        rightArmInitialRot = boneRightArm.rotation;
        leftForeArmInitialRot = boneLeftForeArm.rotation;
        rightForeArmInitialRot = boneRightForeArm.rotation;
    }
}