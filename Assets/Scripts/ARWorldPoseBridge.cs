using UnityEngine;

public class ARWorldPoseBridge : MonoBehaviour
{
    [Header("Cấu hình Không gian 3D")]
    public float scaleMultiplier = 100f;
    public float hipHeightMeter = 0.9f;

    private Transform[] worldPoints = new Transform[33];

    void Start()
    {
        transform.position = new Vector3(0, hipHeightMeter * scaleMultiplier, 0);
        for (int i = 0; i < 33; i++)
        {
            GameObject pt = new GameObject("WorldPoint_" + i);
            pt.transform.SetParent(this.transform);
            worldPoints[i] = pt.transform;
        }
    }

    void Update()
    {
#if UNITY_EDITOR || !UNITY_WEBGL
        if (!Mediapipe.Unity.Sample.PoseLandmarkDetection.PoseLandmarkerRunner.HasResult) return;

        lock (Mediapipe.Unity.Sample.PoseLandmarkDetection.PoseLandmarkerRunner.DataLock)
        {
            var result = Mediapipe.Unity.Sample.PoseLandmarkDetection.PoseLandmarkerRunner.CurrentResult;
            if (result.poseWorldLandmarks == null || result.poseWorldLandmarks.Count == 0) return;

            var landmarks = result.poseWorldLandmarks[0].landmarks;
            if (landmarks == null || landmarks.Count < 33) return;

            for (int i = 0; i < 33; i++)
            {
                var lm = landmarks[i];
                // KHÔNG lật trục X phức tạp. Dùng chuẩn MediaPipe -> Unity
                worldPoints[i].localPosition = new Vector3(-lm.x, -lm.y, lm.z) * scaleMultiplier;
            }
        }
#endif
    }
}