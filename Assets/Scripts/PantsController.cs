using UnityEngine;

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

    private Quaternion leftUpLegInitialRot, rightUpLegInitialRot, leftLegInitialRot, rightLegInitialRot;

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

        if (controlBodyPosition && boneHips != null && mpLeftHip != null && mpRightHip != null)
        {
            Vector3 centerHip = (mpLeftHip.position + mpRightHip.position) / 2f;
            Vector3 targetPos = centerHip * positionScale + positionOffset;

            if (smoothing > 0f) boneHips.position = Vector3.Lerp(boneHips.position, targetPos, 1f - smoothing);
            else boneHips.position = targetPos;
        }

        if (rotateHips) RotateHips();

        Transform lHip = PickRotationPoint(worldLeftHip, mpLeftHip);
        Transform lKnee = PickRotationPoint(worldLeftKnee, mpLeftKnee);
        Transform lAnkle = PickRotationPoint(worldLeftAnkle, mpLeftAnkle);

        Transform rHip = PickRotationPoint(worldRightHip, mpRightHip);
        Transform rKnee = PickRotationPoint(worldRightKnee, mpRightKnee);
        Transform rAnkle = PickRotationPoint(worldRightAnkle, mpRightAnkle);

        RotateLimbBase(lHip, lKnee, boneLeftUpLeg, leftUpLegInitialRot, legForwardAxis);
        RotateLimbBase(lKnee, lAnkle, boneLeftLeg, leftLegInitialRot, legForwardAxis);

        RotateLimbBase(rHip, rKnee, boneRightUpLeg, rightUpLegInitialRot, legForwardAxis);
        RotateLimbBase(rKnee, rAnkle, boneRightLeg, rightLegInitialRot, legForwardAxis);
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
        if (boneHips == null) return;

        Transform lShoulder = PickRotationPoint(worldLeftShoulder, mpLeftShoulder);
        Transform rShoulder = PickRotationPoint(worldRightShoulder, mpRightShoulder);
        Transform lHip = PickRotationPoint(worldLeftHip, mpLeftHip);
        Transform rHip = PickRotationPoint(worldRightHip, mpRightHip);

        if (lShoulder == null || rShoulder == null || lHip == null || rHip == null) return;

        // Tính toán hoàn toàn bằng 3D World (Nếu 3D đã load xong)
        Vector3 shoulderCenter = (lShoulder.position + rShoulder.position) / 2f;
        Vector3 hipCenter = (lHip.position + rHip.position) / 2f;

        Vector3 upVector = (shoulderCenter - hipCenter).normalized;
        Vector3 rightVector = (rShoulder.position - lShoulder.position).normalized;

        // Check Threshold bằng tọa độ 3D chuẩn
        if (worldLandmarksFound && worldLeftShoulder != null && worldRightShoulder != null)
        {
            if (Mathf.Abs(worldRightShoulder.position.z - worldLeftShoulder.position.z) < zThreshold)
            {
                rightVector.z = 0;
                rightVector = rightVector.normalized;
            }
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