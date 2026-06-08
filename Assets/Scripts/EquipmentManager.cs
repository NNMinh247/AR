using UnityEngine;
using System.Collections.Generic;

public enum EquipmentSlot
{
    Shirt,
    Pants,
    FullBody
}

[System.Serializable]
public class EquipmentItem
{
    public string id;
    public EquipmentSlot slot;
    public GameObject objectInScene;
}

public class EquipmentManager : MonoBehaviour
{
    [Header("Danh sách trang phục")]
    public EquipmentItem[] items;

    [Header("Mặc định khi chạy")]
    public bool equipDefaultOnStart = true;
    public string defaultItemId = "outfit_01";

    [Header("Debug hiển thị")]
    public bool showDebugLog = true;

    [Tooltip("Chỉ bật để test render. Nếu bật, item sẽ bị đặt trước camera, không còn bám ARBodyTracker.")]
    public bool debugPlaceInFrontOfCamera = false;

    [Tooltip("Khoảng cách đặt model trước camera khi debugPlaceInFrontOfCamera = true")]
    public float debugCameraDistance = 3f;

    [Tooltip("Scale tạm khi debugPlaceInFrontOfCamera = true")]
    public Vector3 debugLocalScale = Vector3.one;

    private readonly Dictionary<EquipmentSlot, GameObject> activeBySlot =
        new Dictionary<EquipmentSlot, GameObject>();

    void Start()
    {
        HideAllItems();

        if (equipDefaultOnStart && !string.IsNullOrEmpty(defaultItemId))
        {
            EquipItem(defaultItemId);
        }
    }

    public void EquipItemFromWeb(string itemId)
    {
        EquipItem(itemId);
    }

    public void EquipItem(string itemId)
    {
        EquipmentItem selected = FindItem(itemId);

        if (selected == null)
        {
            Debug.LogWarning("[EquipmentManager] Không tìm thấy item id: " + itemId);
            return;
        }

        if (selected.objectInScene == null)
        {
            Debug.LogWarning("[EquipmentManager] Item có id nhưng Object In Scene bị null: " + itemId);
            return;
        }

        if (selected.slot == EquipmentSlot.FullBody)
        {
            TurnOffSlot(EquipmentSlot.Shirt);
            TurnOffSlot(EquipmentSlot.Pants);
        }

        if (selected.slot == EquipmentSlot.Shirt || selected.slot == EquipmentSlot.Pants)
        {
            TurnOffSlot(EquipmentSlot.FullBody);
        }

        TurnOffSlot(selected.slot);

        GameObject obj = selected.objectInScene;

        EnsureParentsActive(obj);
        obj.SetActive(true);
        EnableAllRenderers(obj);

        if (debugPlaceInFrontOfCamera)
        {
            PlaceInFrontOfCamera(obj);
        }

        activeBySlot[selected.slot] = obj;

        if (showDebugLog)
        {
            DebugItemState(selected);
        }

        Debug.Log("[EquipmentManager] Equipped: " + itemId);
    }

    public void ClearAll()
    {
        HideAllItems();
        activeBySlot.Clear();

        Debug.Log("[EquipmentManager] Cleared all items");
    }

    private EquipmentItem FindItem(string itemId)
    {
        foreach (var item in items)
        {
            if (item != null && item.id == itemId)
                return item;
        }

        return null;
    }

    private void HideAllItems()
    {
        foreach (var item in items)
        {
            if (item != null && item.objectInScene != null)
                item.objectInScene.SetActive(false);
        }
    }

    private void TurnOffSlot(EquipmentSlot slot)
    {
        if (activeBySlot.TryGetValue(slot, out GameObject activeObj))
        {
            if (activeObj != null)
                activeObj.SetActive(false);

            activeBySlot.Remove(slot);
        }
    }

    private void EnsureParentsActive(GameObject obj)
    {
        Transform current = obj.transform.parent;

        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);

                if (showDebugLog)
                {
                    Debug.Log("[EquipmentManager] Đã bật parent: " + current.name);
                }
            }

            current = current.parent;
        }
    }

    private void EnableAllRenderers(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;

            if (!renderer.gameObject.activeSelf)
                renderer.gameObject.SetActive(true);
        }

        if (showDebugLog)
        {
            Debug.Log("[EquipmentManager] Renderers found: " + renderers.Length);
        }
    }

    private void PlaceInFrontOfCamera(GameObject obj)
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            Debug.LogWarning("[EquipmentManager] Không tìm thấy Main Camera để đặt object debug.");
            return;
        }

        obj.transform.position = cam.transform.position + cam.transform.forward * debugCameraDistance;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = debugLocalScale;

        Debug.Log("[EquipmentManager] Debug placed item in front of camera.");
    }

    private void DebugItemState(EquipmentItem item)
    {
        GameObject obj = item.objectInScene;

        Debug.Log("========== [EquipmentManager DEBUG] ==========");
        Debug.Log("ID: " + item.id);
        Debug.Log("Slot: " + item.slot);
        Debug.Log("Object: " + obj.name);
        Debug.Log("Active Self: " + obj.activeSelf);
        Debug.Log("Active In Hierarchy: " + obj.activeInHierarchy);
        Debug.Log("World Position: " + obj.transform.position);
        Debug.Log("Local Position: " + obj.transform.localPosition);
        Debug.Log("World Scale/Lossy: " + obj.transform.lossyScale);
        Debug.Log("Local Scale: " + obj.transform.localScale);

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        Debug.Log("Renderer Count: " + renderers.Length);

        foreach (Renderer renderer in renderers)
        {
            Debug.Log(
                "Renderer: " + renderer.name +
                " | enabled=" + renderer.enabled +
                " | activeInHierarchy=" + renderer.gameObject.activeInHierarchy +
                " | layer=" + LayerMask.LayerToName(renderer.gameObject.layer) +
                " | bounds.center=" + renderer.bounds.center +
                " | bounds.size=" + renderer.bounds.size
            );
        }

        Debug.Log("==============================================");
    }

#if UNITY_EDITOR
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            EquipItem("outfit_01");
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ClearAll();
        }
    }
#endif
}