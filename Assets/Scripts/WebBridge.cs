using UnityEngine;

// Định nghĩa cấu trúc JSON để Unity hiểu
[System.Serializable]
public class MediaPipeLandmark
{
    public float x, y, z, visibility;
}

[System.Serializable]
public class LandmarkWrapper
{
    public MediaPipeLandmark[] items;
}

public class WebBridge : MonoBehaviour
{
    public Transform annotationParent; // Kéo thả GameObject "Point List Annotation" vào đây
    private Transform[] landmarks = new Transform[33];

    void Start()
    {
        // Lấy tham chiếu đến 33 điểm con
        if (annotationParent != null && annotationParent.childCount >= 33)
        {
            for (int i = 0; i < 33; i++)
            {
                landmarks[i] = annotationParent.GetChild(i);
            }
        }
    }

    // Hàm này được gọi từ JavaScript
    public void ReceiveLandmarks(string jsonString)
    {
        if (annotationParent == null) return;

        // Giải mã JSON
        LandmarkWrapper data = JsonUtility.FromJson<LandmarkWrapper>(jsonString);

        if (data != null && data.items != null && data.items.Length == 33)
        {
            for (int i = 0; i < 33; i++)
            {
                // MediaPipe Web trả về tọa độ chuẩn hóa (0-1). 
                // X và Y cần được scale lên để khớp với không gian Unity.
                // Trục Y của Web hướng xuống, Unity hướng lên nên cần đảo ngược.
                // Bạn có thể nhân với 10 (hoặc số khác) tùy theo scale model của bạn.
                float scaleFac = 10f;

                // Trục X có thể cần đảo dấu (tùy thuộc vào việc lật gương camera trên Web)
                float posX = (data.items[i].x - 0.5f) * -scaleFac;
                float posY = (0.5f - data.items[i].y) * scaleFac;
                float posZ = data.items[i].z * scaleFac; // Z của MediaPipe thường khá nhỏ

                landmarks[i].localPosition = new Vector3(posX, posY, posZ);
            }
        }
    }
}