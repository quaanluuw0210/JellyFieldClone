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
            currentFrame.SetActive(false); // Mặc định ẩn đi
        }
    }

    /// <summary>
    /// Hiển thị khung highlight tại vị trí World của ô Grid
    /// </summary>
    public void ShowHighlight(Vector3 worldPos)
    {
        if (currentFrame == null) return;

        // Đặt vị trí khung trùng ô Grid (nâng Y lên một tí để không bị lẹm mặt bàn)
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