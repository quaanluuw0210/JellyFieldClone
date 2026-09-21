using System;
using UnityEngine;

public class InputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask blockLayer; // Layer của các khối Block

    // Event bắn lên khi người dùng chạm/click vào một ô trên Grid
    public static event Action<Vector2Int> OnCellTapped;

    // Event bắn lên khi người dùng chọn/kéo một Block cụ thể
    public static event Action<Block> OnBlockSelected;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // Kiểm tra tương tác Click chuột hoặc Chạm màn hình (Touch)
        if (Input.GetMouseButtonDown(0))
        {
            ProcessTap(Input.mousePosition);
        }
    }

    private void ProcessTap(Vector3 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        // 1. Raycast kiểm tra xem có va chạm với Block nào không
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, blockLayer))
        {
            Block hitBlock = hit.collider.GetComponent<Block>();
            if (hitBlock != null)
            {
                // Phát event thông báo Block đã được chọn
                OnBlockSelected?.Invoke(hitBlock);
                return;
            }
        }

        // 2. Nếu không chạm Block, chuyển đổi điểm va chạm 3D sang Tọa độ Grid
        Plane boardPlane = new Plane(Vector3.up, Vector3.zero);
        if (boardPlane.Raycast(ray, out float enter))
        {
            Vector3 worldHitPoint = ray.GetPoint(enter);

            // Chuyển đổi World Pos -> Grid Position (thông qua hàm utility hoặc event)
            Vector2Int gridPos = ConvertWorldToGridPosition(worldHitPoint);

            // Bắn Event tọa độ ô vừa chạm lên GameManager
            OnCellTapped?.Invoke(gridPos);
        }
    }

    private Vector2Int ConvertWorldToGridPosition(Vector3 worldPos)
    {
        // Giả định Grid Center tại Vector3.zero và CellSize = 1.0f
        int x = Mathf.RoundToInt(worldPos.x);
        int y = Mathf.RoundToInt(worldPos.z);
        return new Vector2Int(x, y);
    }
}