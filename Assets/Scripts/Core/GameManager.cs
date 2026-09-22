using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("System References")]
    [SerializeField] private GridSystem gridSystem;
    [SerializeField] private BoardView boardView;
    [SerializeField] private InputController inputController;

    private Block selectedBlock;

    private void OnEnable()
    {
        // Đăng ký lắng nghe các Event từ InputController
        InputController.OnBlockSelected += HandleBlockSelected;
        InputController.OnCellTapped += HandleCellTapped;
    }

    private void OnDisable()
    {
        // Hủy đăng ký Event để tránh Memory Leak
        InputController.OnBlockSelected -= HandleBlockSelected;
        InputController.OnCellTapped -= HandleCellTapped;
    }

    private void Start()
    {
       
    }

    #region Event Handlers (Xử lý Game Rules)

    private void HandleBlockSelected(Block block)
    {
        selectedBlock = block;
        Debug.Log($"[GameManager] Đã chọn Block: {block.name}");
    }

    private void HandleCellTapped(Vector2Int gridPos)
    {
        if (!gridSystem.IsValidPosition(gridPos))
        {
            selectedBlock = null;
            return;
        }

        Cell targetCell = gridSystem.GetCell(gridPos);

        if (selectedBlock != null && targetCell != null && targetCell.IsEmpty())
        {
            // Lấy vị trí chuẩn trực tiếp từ GridSystem
            Vector3 targetWorldPos = gridSystem.GetWorldPosition(gridPos);

            // Đặt block tới vị trí mới
            selectedBlock.transform.position = targetWorldPos + Vector3.up * 0.1f;

            // Cập nhật dữ liệu & gọi Match
            gridSystem.PlaceBlock(selectedBlock, gridPos);
            MatchLogic.ProcessMatchAndMerge(gridSystem, gridPos);

            selectedBlock = null;
        }
    }
    public void HandleBlockDropped(Block block, Vector2Int gridPos)
    {
        Debug.Log($"[GameManager] Block {block.name} vừa được thả vào vị trí Grid: {gridPos}");

        // Thực hiện logic Gộp màu (Match & Merge) ngay sau khi thả Block thành công
        bool hasMatch = MatchLogic.ProcessMatchAndMerge(gridSystem, gridPos);

        if (hasMatch)
        {
            Debug.Log("[GameManager] Gộp màu thành công!");
        }
    }

    #endregion
}