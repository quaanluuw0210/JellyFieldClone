using UnityEngine;

public class GridHighlightManager : MonoBehaviour
{
    public static GridHighlightManager Instance { get; private set; }

    [SerializeField] private GameObject highlightFramePrefab;
    private GameObject currentFrame;

    private void Awake()
    {
        Instance = this;
        if (highlightFramePrefab != null)
        {
            currentFrame = Instantiate(highlightFramePrefab, transform);
            currentFrame.SetActive(false); 
        }
    }

    /// <summary>
    /// Hiển thị khung highlight tại vị trí World của ô Grid
    /// </summary>
    public void ShowHighlight(Vector3 worldPos)
    {
        if (currentFrame == null) return;

        
        currentFrame.transform.position = worldPos + Vector3.up * 0.05f;
        currentFrame.SetActive(true);
    }

    /// <summary>
    /// Ẩn khung đi khi thả tay hoặc kéo ra ngoài bàn cờ
    /// </summary>
    public void HideHighlight()
    {
        if (currentFrame != null)
        {
            currentFrame.SetActive(false);
        }
    }
}