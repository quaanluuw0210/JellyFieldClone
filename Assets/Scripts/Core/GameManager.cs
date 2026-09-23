using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("System References")]
    [SerializeField] private GridSystem gridSystem;
    [SerializeField] private BoardView boardView;
    [SerializeField] private InputController inputController;
    [SerializeField] private SpawnView spawnView;
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

        if (selectedBlock != null && targetCell != null)
        {
            // Lấy vị trí chuẩn trực tiếp từ GridSystem
            Vector3 targetWorldPos = gridSystem.GetWorldPosition(gridPos);

            // Đặt block tới vị trí mới
            selectedBlock.transform.position = targetWorldPos + Vector3.up * 0.1f;

            // Cập nhật dữ liệu & gọi Match
            gridSystem.PlaceBlock(selectedBlock, gridPos);
            StartCoroutine(RunMatchChain(gridPos));

            selectedBlock = null;
        }
    }

    private IEnumerator RunMatchChain(Vector2Int startPos)
    {
        HashSet<Vector2Int> currentSeeds = new HashSet<Vector2Int> { startPos };
        bool anyMatchInChain = false;

        while (currentSeeds.Count > 0)
        {
            bool hasMatch = MatchLogic.ProcessMatchWave(
                gridSystem,
                currentSeeds,
                out HashSet<Block> affectedBlocks,
                out HashSet<Vector2Int> nextSeeds);

            if (!hasMatch)
            {
                break; // hết combo, dừng chain
            }

            anyMatchInChain = true;

            // Chờ TẤT CẢ block bị ảnh hưởng xử lý xong (remove anim + recover shape/morph)
            foreach (Block b in affectedBlocks)
            {
                if (b == null) continue;
                yield return new WaitUntil(() => b == null || !b.IsProcessingRemoval);
            }

            currentSeeds = nextSeeds;
        }

        if (anyMatchInChain)
        {
            Debug.Log("[GameManager] Gộp màu thành công (chain hoàn tất)!");
        }
    }


    public void HandleBlockDropped(Block block, Vector2Int gridPos)
    {
        if (block == null || gridSystem == null) return;

        // 1. KIỂM TRA XEM CÓ NẰM TRONG BÀN CỜ KHÔNG
        bool isInsideBoard = gridSystem.IsValidPosition(gridPos);

        if (!isInsideBoard)
        {
            // === TRƯỜNG HỢP 1: THẢ NGOÀI BÀN CỜ (Hoặc rơi lại khu vực Spawn) ===
            Debug.LogWarning($"[GameManager] Block {block.name} thả ngoài phạm vi bàn cờ (Tọa độ {gridPos} không hợp lệ).");

            // Trả khối Jelly về vị trí cũ trên khay Spawn
            //block.OnPlaceFailed();
            return;
        }

        // 2. LẤY CELL TRÊN BOARD ĐỂ CHECK XEM CÓ TRỐNG KHÔNG
        Cell targetCell = gridSystem.GetCell(gridPos);

        if (targetCell != null)
        {
            // === TRƯỜNG HỢP 2: ĐÃ VÀO BOARD THÀNH CÔNG VÀ Ô ĐANG TRỐNG ===
            Debug.Log($"[GameManager] Block {block.name} ĐÃ VÀO BOARD thành công tại vị trí: {gridPos}");

            // a. Báo SpawnView gạch tên khối này khỏi khay
            spawnView?.RemoveBlockFromSpawn(block);

            // b. Chuyển Parent transform sang BoardView
            if (boardView != null)
            {
                block.transform.SetParent(boardView.transform);
            }

            // c. Đăng ký dữ liệu vào Cell & Khóa không cho kéo thả nữa
            gridSystem.PlaceBlock(block, gridPos);
            block.InitializePlacedState(targetCell); // Hàm này dán hasBeenPlaced = true
            block.SetPlaced(true);

            // d. Thực hiện logic gộp màu (Match & Merge)
            StartCoroutine(RunMatchChain(gridPos));

            // e. Kiểm tra nếu khay Spawn hết khối thì sinh đợt mới
            if (spawnView != null && spawnView.SpawnBlocks.Count == 0)
            {
                spawnView.TestSpawnHardcodedSpawn();
            }
        }
        else
        {
            // === TRƯỜNG HỢP 3: TRÚNG BOARD NHƯNG Ô ĐÓ ĐÃ CÓ BLOCK KHÁC ===
            Debug.LogWarning($"[GameManager] Ô {gridPos} trên Board đã bị chiếm chỗ!");

            // Trả về khay Spawn
            //block.OnPlaceFailed();
        }
    }

    #endregion
}