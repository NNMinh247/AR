using UnityEngine;

public class ARBaseScript : MonoBehaviour
{
    [Header("Chung - Landmark (BẮT BUỘC DÙNG 2D)")]
    public string annotationParentName = "Point List Annotation";

    [Tooltip("TÍCH VÀO ĐÂY để bật chế độ Soi Gương chuẩn")]
    public bool mirrorLeftRight = true;

    [Header("Lật Trục (Sửa lỗi ngược hướng)")]
    [Tooltip("BẬT NẾU tay đưa sang ngang (Trái/Phải) bị ngược")]
    public bool flipX = true;
    [Tooltip("BẬT NẾU tay đưa ra trước mặt bị đâm ra sau lưng")]
    public bool flipZ = false;

    [Range(0f, 1f)]
    public float smoothing = 0.3f;
    public bool showDebugLog = true;

    // Dữ liệu Landmark dùng chung
    protected Transform mpLeftShoulder, mpRightShoulder;
    protected Transform mpLeftHip, mpRightHip;
    protected Transform mpLeftElbow, mpRightElbow, mpLeftWrist, mpRightWrist;
    protected Transform mpLeftKnee, mpRightKnee, mpLeftAnkle, mpRightAnkle;

    protected bool landmarksFound = false;
    private float searchTimer = 0f;

    // LOGIC TÌM LANDMARK GỐC 100%
    protected void TryFindLandmarksBase()
    {
        if (landmarksFound) return;

        searchTimer += Time.deltaTime;
        if (searchTimer < 0.5f) return;
        searchTimer = 0f;

        GameObject parentObj = GameObject.Find(annotationParentName);
        if (parentObj == null || parentObj.transform.childCount < 33) return;

        Transform mpLeftShoulder_Raw = parentObj.transform.GetChild(11);
        Transform mpRightShoulder_Raw = parentObj.transform.GetChild(12);
        Transform mpLeftElbow_Raw = parentObj.transform.GetChild(13);
        Transform mpRightElbow_Raw = parentObj.transform.GetChild(14);
        Transform mpLeftWrist_Raw = parentObj.transform.GetChild(15);
        Transform mpRightWrist_Raw = parentObj.transform.GetChild(16);

        Transform mpLeftHip_Raw = parentObj.transform.GetChild(23);
        Transform mpRightHip_Raw = parentObj.transform.GetChild(24);
        Transform mpLeftKnee_Raw = parentObj.transform.GetChild(25);
        Transform mpRightKnee_Raw = parentObj.transform.GetChild(26);
        Transform mpLeftAnkle_Raw = parentObj.transform.GetChild(27);
        Transform mpRightAnkle_Raw = parentObj.transform.GetChild(28);

        if (mirrorLeftRight)
        {
            mpLeftShoulder = mpLeftShoulder_Raw; mpRightShoulder = mpRightShoulder_Raw;
            mpLeftElbow = mpLeftElbow_Raw; mpRightElbow = mpRightElbow_Raw;
            mpLeftWrist = mpLeftWrist_Raw; mpRightWrist = mpRightWrist_Raw;

            mpLeftHip = mpLeftHip_Raw; mpRightHip = mpRightHip_Raw;
            mpLeftKnee = mpLeftKnee_Raw; mpRightKnee = mpRightKnee_Raw;
            mpLeftAnkle = mpLeftAnkle_Raw; mpRightAnkle = mpRightAnkle_Raw;
        }
        else
        {
            mpLeftShoulder = mpRightShoulder_Raw; mpRightShoulder = mpLeftShoulder_Raw;
            mpLeftElbow = mpRightElbow_Raw; mpRightElbow = mpLeftElbow_Raw;
            mpLeftWrist = mpRightWrist_Raw; mpRightWrist = mpLeftWrist_Raw;

            mpLeftHip = mpRightHip_Raw; mpRightHip = mpLeftHip_Raw;
            mpLeftKnee = mpRightKnee_Raw; mpRightKnee = mpLeftKnee_Raw;
            mpLeftAnkle = mpRightAnkle_Raw; mpRightAnkle = mpLeftAnkle_Raw;
        }

        landmarksFound = true;
    }
}