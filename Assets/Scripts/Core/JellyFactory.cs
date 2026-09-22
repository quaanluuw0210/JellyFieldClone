using System.Collections.Generic;
using UnityEngine;

public enum JellyType
{
    DoubleHorizontal,
    DoubleVertical,
    Full
}

[System.Serializable]
public class JellyColorMaterial
{
    public JellyColor color;
    public Material material;
}

public class JellyFactory : MonoBehaviour
{
    public static JellyFactory Instance;

    [SerializeField] private GameObject jellyDoubleHorizontalPrefab;
    [SerializeField] private GameObject jellyDoubleVerticalPrefab;
    [SerializeField] private GameObject jellyFullPrefab;

    [Header("Color Materials")]
    [SerializeField] private List<JellyColorMaterial> colorMaterials;

    private Dictionary<JellyColor, Material> materialLookup;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        materialLookup = new Dictionary<JellyColor, Material>();
        foreach (var entry in colorMaterials)
        {
            if (entry.material != null)
                materialLookup[entry.color] = entry.material;
        }
    }

    public Material GetMaterialForColor(JellyColor color)
    {
        if (materialLookup.TryGetValue(color, out Material mat))
            return mat;

        Debug.LogWarning($"[JellyFactory] Không tìm thấy Material cho màu {color}!");
        return null;
    }

    public JellyBlockBase CreateJelly(JellyType type, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject prefabToInstantiate = type switch
        {
            JellyType.DoubleHorizontal => jellyDoubleHorizontalPrefab,
            JellyType.DoubleVertical => jellyDoubleVerticalPrefab,
            JellyType.Full => jellyFullPrefab,
            _ => null
        };

        if (prefabToInstantiate == null)
        {
            Debug.LogError($"[JellyFactory] Prefab cho loại {type} chưa được gán trong Inspector!");
            return null;
        }

        GameObject jellyObj = Instantiate(prefabToInstantiate, position, rotation, parent);

        if (jellyObj.TryGetComponent<JellyBlockBase>(out var jellyBlock))
            return jellyBlock;

        Debug.LogError($"[JellyFactory] Prefab {prefabToInstantiate.name} không chứa component JellyBlockBase!");
        Destroy(jellyObj);
        return null;
    }
}