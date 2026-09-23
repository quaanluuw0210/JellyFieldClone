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

    [Header("Level Configurations")]
    [SerializeField] private List<LevelData> levelDatas;
    private int currentLevelIndex = 0;

    private Block selectedBlock;


    public static GameManager instance; 

    private void OnEnable()
    {
        // Đăng ký lắng nghe các Event từ InputController
        InputController.OnBlockSelected += HandleBlockSelected;

    }

    private void OnDisable()
    {
        // Hủy đăng ký Event để tránh Memory Leak
        InputController.OnBlockSelected -= HandleBlockSelected;
      
    }

    private void Start()
    {
       
    }

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
            
    }

    #region Level Management

    /// <summary>
    /// Tải màn chơi theo Index chỉ định
    /// </summary>
    public void LoadLevel(int levelIndex)
    {
        if (levelDatas == null || levelDatas.Count == 0)
        {
            Debug.LogError("[GameManager] Danh sách levelDatas chưa được thiết lập!");
            return;
        }

        if (levelIndex < 0 || levelIndex >= levelDatas.Count)
        {
            Debug.LogWarning($"[GameManager] Index Level {levelIndex} vượt quá giới hạn. Đã hoàn thành tất cả Level!");
            return;
        }

        currentLevelIndex = levelIndex;
        LevelData currentLevelData = levelDatas[currentLevelIndex];

        // 1. Khởi tạo dữ liệu điểm mục tiêu cho ScoreManager
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.InitLevelScores(currentLevelData);
        }

        // 2. Khởi tạo Bàn cờ và các Block đặt sẵn
        if (boardView != null)
        {
            boardView.InitializeBoard(currentLevelData);
        }

        // 3. Khởi tạo Khay Spawn và chuỗi Block chờ spawn
        if (spawnView != null)
        {
            spawnView.InitializeSpawn(currentLevelData.activeSlotCount, currentLevelData.blockPrefabsSequence);
        }

        if(UIManager.Instance != null)
        {
            UIManager.Instance.SetUpUI(currentLevelData);
        } 
            

        Debug.Log($"[GameManager] Đã khởi tạo Level {currentLevelIndex + 1} thành công!");
    }

    /// <summary>
    /// Chuyển sang Level kế tiếp (gọi khi Thắng màn)
    /// </summary>
    public void NextLevel()
    {
        int nextLevelIndex = currentLevelIndex + 1;
        if (nextLevelIndex < levelDatas.Count)
        {
            LoadLevel(nextLevelIndex);
        }
        else
        {
            Debug.Log("[GameManager] Chúc mừng! Bạn đã hoàn thành tất cả màn chơi!");
            PlayCurrentLevel();
        }
    }

    /// <summary>
    /// Chơi lại Level hiện tại
    /// </summary>
    public void RestartLevel()
    {
        LoadLevel(currentLevelIndex);
    }

    public void PlayCurrentLevel()
    {
        LoadLevel(currentLevelIndex);
    }    

    #endregion

    #region Event Handlers (Xử lý Game Rules)

    private void HandleBlockSelected(Block block)
    {
        selectedBlock = block;
        Debug.Log($"[GameManager] Đã chọn Block: {block.name}");
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

            yield return new WaitForSecondsRealtime(0.5f);

            currentSeeds = nextSeeds;
        }

        if (anyMatchInChain)
        {
            Debug.Log("[GameManager] Gộp màu thành công (chain hoàn tất)!");
        }

        // Sau khi kết thúc chuỗi Match, kiểm tra xem người chơi đã thắng chưa
        CheckWinCondition();
        CheckLossCondition();
    }

    private void CheckWinCondition()
    {
        if (ScoreManager.Instance != null && ScoreManager.Instance.IsPlayerWin())
        {

            UIManager.Instance.VictoryUI();
        }
    }

    private void CheckLossCondition()
    {
        if(boardView != null)
        {
            if(boardView.IsBoardFull()==true)
            {
                UIManager.Instance.LossUI();
            }    
        } 
            
    }    

    public void HandleBlockDropped(Block block, Vector2Int gridPos)
    {
        if (block == null || gridSystem == null) return;

        // 1. KIỂM TRA XEM CÓ NẰM TRONG BÀN CỜ KHÔNG
        bool isInsideBoard = gridSystem.IsValidPosition(gridPos);

        if (!isInsideBoard)
        {
            // === TRƯỜNG HỢP 1: THẢ NGOÀI BÀN CỜ ===
            Debug.LogWarning($"[GameManager] Block {block.name} thả ngoài phạm vi bàn cờ (Tọa độ {gridPos} không hợp lệ).");
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
                boardView.RegisterPlacedBlock(block);
            }

            // c. Đăng ký dữ liệu vào Cell & Khóa không cho kéo thả nữa
            gridSystem.PlaceBlock(block, gridPos);
            block.InitializePlacedState(targetCell);
            block.SetPlaced(true);

            // d. Thực hiện logic gộp màu (Match & Merge)
            StartCoroutine(RunMatchChain(gridPos));
        }
        else
        {
            // === TRƯỜNG HỢP 3: TRÚNG BOARD NHƯNG Ô ĐÓ ĐÃ CÓ BLOCK KHÁC ===
            Debug.LogWarning($"[GameManager] Ô {gridPos} trên Board đã bị chiếm chỗ!");
        }
    }

    #endregion
}