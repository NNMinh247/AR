using UnityEngine;

public class ARBaseScript : MonoBehaviour
{
    [Header("Chung - Landmark (BẮT BUỘC DÙNG 2D)")]
    public string annotationParentName = "Point List Annotation";

    [Header("Chung - World Landmark (BẮT BUỘC DÙNG 3D ĐỂ XOAY)")]
    public string worldAnnotationParentName = "World Point List Annotation";

    public bool mirrorLeftRight = true;
    public bool flipX = true;
    public bool flipZ = false;

    [Range(0f, 1f)] public float smoothing = 0.3f;
    public bool showDebugLog = true;

    // Dữ liệu 2D
    protected Transform mpLeftShoulder, mpRightShoulder, mpLeftHip, mpRightHip;
    protected Transform mpLeftElbow, mpRightElbow, mpLeftWrist, mpRightWrist;
    protected Transform mpLeftKnee, mpRightKnee, mpLeftAnkle, mpRightAnkle;

    // Dữ liệu 3D
    protected Transform worldLeftShoulder, worldRightShoulder, worldLeftHip, worldRightHip;
    protected Transform worldLeftElbow, worldRightElbow, worldLeftWrist, worldRightWrist;
    protected Transform worldLeftKnee, worldRightKnee, worldLeftAnkle, worldRightAnkle;

    protected bool landmarksFound = false;
    protected bool worldLandmarksFound = false; // Thêm cờ kiểm tra 3D
    private float searchTimer = 0f;

    protected void TryFindLandmarksBase()
    {
        if (landmarksFound && worldLandmarksFound) return;

        searchTimer += Time.deltaTime;
        if (searchTimer < 0.5f) return;
        searchTimer = 0f;

        GameObject parentObj = GameObject.Find(annotationParentName);
        if (parentObj != null && parentObj.transform.childCount >= 33)
        {
            AssignPoints(parentObj.transform, true);
            landmarksFound = true;
        }

        GameObject worldObj = GameObject.Find(worldAnnotationParentName);
        if (worldObj != null && worldObj.transform.childCount >= 33)
        {
            AssignPoints(worldObj.transform, false);
            worldLandmarksFound = true;
        }
    }

    // Hàm an toàn: Ưu tiên dùng 3D, nếu chưa kịp load thì dùng tạm 2D để không bị đơ
    protected Transform PickRotationPoint(Transform worldPt, Transform screenPt)
    {
        return (worldLandmarksFound && worldPt != null) ? worldPt : screenPt;
    }

    private void AssignPoints(Transform parent, bool is2D)
    {
        Transform lS = parent.GetChild(11), rS = parent.GetChild(12);
        Transform lE = parent.GetChild(13), rE = parent.GetChild(14);
        Transform lW = parent.GetChild(15), rW = parent.GetChild(16);
        Transform lH = parent.GetChild(23), rH = parent.GetChild(24);
        Transform lK = parent.GetChild(25), rK = parent.GetChild(26);
        Transform lA = parent.GetChild(27), rA = parent.GetChild(28);

        if (!mirrorLeftRight)
        {
            Transform temp;
            temp = lS; lS = rS; rS = temp;
            temp = lE; lE = rE; rE = temp;
            temp = lW; lW = rW; rW = temp;
            temp = lH; lH = rH; rH = temp;
            temp = lK; lK = rK; rK = temp;
            temp = lA; lA = rA; rA = temp;
        }

        if (is2D)
        {
            mpLeftShoulder = lS; mpRightShoulder = rS;
            mpLeftElbow = lE; mpRightElbow = rE;
            mpLeftWrist = lW; mpRightWrist = rW;
            mpLeftHip = lH; mpRightHip = rH;
            mpLeftKnee = lK; mpRightKnee = rK;
            mpLeftAnkle = lA; mpRightAnkle = rA;
        }
        else
        {
            worldLeftShoulder = lS; worldRightShoulder = rS;
            worldLeftElbow = lE; worldRightElbow = rE;
            worldLeftWrist = lW; worldRightWrist = rW;
            worldLeftHip = lH; worldRightHip = rH;
            worldLeftKnee = lK; worldRightKnee = rK;
            worldLeftAnkle = lA; worldRightAnkle = rA;
        }
    }
}