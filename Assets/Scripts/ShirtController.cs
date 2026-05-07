using UnityEngine;

// Đổi kế thừa từ MonoBehaviour sang ARClothingBase để dùng chung logic
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
        // 1. Gọi hàm tìm Landmark 2D đã fix lỗi Soi Gương ở file ARBaseScript
        TryFindLandmarksBase();
        if (!landmarksFound) return;

        // 2. Tìm xương nếu chưa tìm thấy
        if (!bonesFound) TryFindBones();
        if (!bonesFound) return;

        // 3. Lưu giá trị xoay gốc của xương
        if (!initialRotCached)
        {
            CacheInitialRotations();
            initialRotCached = true;
        }

        // 4. Sử dụng hàm bẻ khớp dùng chung RotateLimbBase từ ARClothingBase
        // Điều khiển cánh tay và cẳng tay
        RotateLimbBase(mpLeftShoulder, mpLeftElbow, boneLeftArm, leftArmInitialRot, boneForwardAxis);
        RotateLimbBase(mpLeftElbow, mpLeftWrist, boneLeftForeArm, leftForeArmInitialRot, boneForwardAxis);

        RotateLimbBase(mpRightShoulder, mpRightElbow, boneRightArm, rightArmInitialRot, boneForwardAxis);
        RotateLimbBase(mpRightElbow, mpRightWrist, boneRightForeArm, rightForeArmInitialRot, boneForwardAxis);
    }

    void TryFindBones()
    {
        // Sử dụng hàm tìm xương gốc từ ARClothingBase
        if (armatureRoot == null)
        {
            Transform hips = FindInSceneBase(boneNamePrefix + "Hips");
            if (hips != null) { armatureRoot = hips; while (armatureRoot.parent != null) armatureRoot = armatureRoot.parent; }
        }

        Transform searchRoot = armatureRoot != null ? armatureRoot : transform;

        // Cần Hips để làm mốc tính toán độ lệch xoay
        boneHips = FindBoneRecursiveBase(searchRoot, boneNamePrefix + "Hips");

        // Tìm các xương cánh tay cụ thể cho Áo
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