using UnityEngine;

public class ARClothingBase : ARBaseScript
{
    [Header("Chung - Xương Mixamo")]
    public string boneNamePrefix = "mixamorig:";
    public Transform armatureRoot;
    public bool dumpAllBoneNames = false;

    protected Transform boneHips;
    protected Quaternion hipsInitialRot;

    protected bool bonesFound = false;
    protected bool initialRotCached = false;
    protected bool dumpedBones = false;

    protected Transform FindInSceneBase(string targetName)
    {
        var scene = gameObject.scene;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform result = FindBoneRecursiveBase(root.transform, targetName);
            if (result != null) return result;
        }
        return null;
    }

    protected Transform FindBoneRecursiveBase(Transform parent, string targetName)
    {
        if (parent.name == targetName) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindBoneRecursiveBase(parent.GetChild(i), targetName);
            if (result != null) return result;
        }
        return null;
    }

    protected void DumpBoneNamesBase(Transform parent, int depth)
    {
        string indent = new string(' ', depth * 2);
        Debug.Log(indent + "- " + parent.name);
        for (int i = 0; i < parent.childCount; i++) DumpBoneNamesBase(parent.GetChild(i), depth + 1);
    }

    // LOGIC XOAY XƯƠNG GỐC 100%
    protected void RotateLimbBase(Transform startPoint, Transform endPoint, Transform targetBone, Quaternion initialRotation, Vector3 forwardAxis)
    {
        if (startPoint == null || endPoint == null || targetBone == null) return;

        Vector3 direction = endPoint.position - startPoint.position;
        if (direction.magnitude < 0.001f) return;

        // --- XỬ LÝ LẬT TRỤC (FIX LỖI NGƯỢC HƯỚNG TAY) ---
        if (flipX) direction.x = -direction.x;
        if (flipZ) direction.z = -direction.z;
        // ------------------------------------------------

        Quaternion hipDelta = Quaternion.identity;
        if (boneHips != null) hipDelta = boneHips.rotation * Quaternion.Inverse(hipsInitialRot);

        Quaternion currentRestRotation = hipDelta * initialRotation;
        Vector3 currentBoneDirection = currentRestRotation * forwardAxis;
        Quaternion targetRotation = Quaternion.FromToRotation(currentBoneDirection, direction.normalized) * currentRestRotation;

        if (smoothing > 0f) targetBone.rotation = Quaternion.Slerp(targetBone.rotation, targetRotation, 1f - smoothing);
        else targetBone.rotation = targetRotation;
    }
}