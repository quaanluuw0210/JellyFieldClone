using UnityEngine;

public enum JellyType
{
    Double,
    Full
}
public class JellyFactory : MonoBehaviour
{

    public static JellyFactory Instance;

    [SerializeField] private GameObject jellyDoublePrefab;
    [SerializeField] private GameObject jellyFullPrefab;

    private void Awake()
    {
        if(Instance == null)
        Instance = this;
        else
        {
            Destroy(this.gameObject);
        }    

    }
    public JellyBlockBase CreateJelly(JellyType type, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
    {
        GameObject prefabToInstantiate = null;

        switch (type)
        {
            case JellyType.Double:
                prefabToInstantiate = jellyDoublePrefab;
                break;
            case JellyType.Full:
                prefabToInstantiate = jellyFullPrefab;
                break;
            default:
                Debug.LogError($"[JellyFactory] JellyType {type} ch?a ???c h? tr?!");
                return null;
        }

        if (prefabToInstantiate == null)
        {
            Debug.LogError($"[JellyFactory] Prefab cho lo?i {type} ch?a ???c gán trong Inspector!");
            return null;
        }

        // T?o GameObject m?i t? Prefab
        GameObject jellyObj = Instantiate(prefabToInstantiate, position, rotation, parent);

        // L?y Component JellyBlockBase trên Prefab v?a t?o
        if (jellyObj.TryGetComponent<JellyBlockBase>(out var jellyBlock))
        {
            return jellyBlock;
        }

        Debug.LogError($"[JellyFactory] Prefab {prefabToInstantiate.name} không ch?a component JellyBlockBase!");
        return null;
    }
}
