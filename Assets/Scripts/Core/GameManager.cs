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
        // Khởi tạo Game Flow
        InitializeGame();
    }

    private void InitializeGame()
    {
        // 1. Khởi tạo dữ liệu Bàn chơi (Test 5x5)
        gridSystem.InitializeRectangularGrid(5, 5);

        // 2. Render giao diện bàn chơi lên Scene
        boardView.GenerateBoardVisuals();

        // 3. Spawn Block thử nghiệm
        boardView.SpawnBlockAt(new Vector2Int(2, 2));
    }

    #region Event Handlers (Xử lý Game Rules)

    private void HandleBlockSelected(Block block)
    {
        selectedBlock = block;
        Debug.Log($"[GameManager] Đã chọn Block: {block.name}");

        // Kích hoạt hiệu ứng nảy Jiggle khi chọn
        //block.PlayJiggleAnimation(0.2f);
    }

    private void HandleCellTapped(Vector2Int gridPos)
    {
        Debug.Log($"[GameManager] Người dùng chạm vào ô Grid: {gridPos}");

        // Kiểm tra vị trí chạm có nằm trên Lưới hợp lệ không
        if (!gridSystem.IsValidPosition(gridPos))
        {
            Debug.LogWarning("[GameManager] Vị trí chạm nằm ngoài Bàn chơi!");
            return;
        }

        Cell targetCell = gridSystem.GetCell(gridPos);

        // Thực hiện Rule Game: Nếu đang chọn 1 Block và ô được chạm còn trống
        if (selectedBlock != null && targetCell != null && targetCell.IsEmpty())
        {
            // Di chuyển Block tới ô mới
            Vector3 targetWorldPos = boardView.GetWorldPositionForCell(gridPos, boardView.GetBoardCenterOffset());

            // Xử lý di chuyển và cập nhật lại Logic Grid
            selectedBlock.transform.position = targetWorldPos + Vector3.up * 0.1f;
            //selectedBlock.PlayJiggleAnimation(0.25f);

            // Xóa tham chiếu Block đang chọn
            selectedBlock = null;
        }
    }

    #endregion
}