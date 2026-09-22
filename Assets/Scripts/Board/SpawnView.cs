using System.Collections.Generic;
using UnityEngine;

public class SpawnView : MonoBehaviour
{
    [Header("Data & Settings")]
    [SerializeField] private float slotSpacing = 2.5f; // Khoảng cách giữa các ô Slot khay chứa


    [Header("Prefabs")]
    [SerializeField] private GameObject cellPrefab; // Prefab ô đế lót bên dưới khay (nếu có)

    [Header("Startup Test")]
    [SerializeField] private bool runHardcodedTestOnStart;
    [SerializeField] private List<Block> testBlockPrefabs; // Drag 2-3 Prefab Block vào đây để test

    private readonly List<GameObject> slotViews = new List<GameObject>();
    private readonly List<Block> spawnBlocks = new List<Block>();

    public IReadOnlyList<Block> SpawnBlocks => spawnBlocks;

    private void Start()
    {
        if (runHardcodedTestOnStart)
        {
            TestSpawnHardcodedSpawn();
        }
    }

    /// <summary>
    /// Hàm test sinh 3 slot khay và đặt các Prefab đã gán sẵn vào đúng index
    /// </summary>
    public void TestSpawnHardcodedSpawn()
    {
        if (testBlockPrefabs == null || testBlockPrefabs.Count == 0)
        {
            Debug.LogWarning("[SpawnView] Chưa gán testBlockPrefabs trong Inspector!");
            return;
        }

        // 1. Tạo 3 slot khay chứa
        int slotCount = Mathf.Min(3, testBlockPrefabs.Count);
        GenerateSpawnSlots(slotCount);

        // 2. Sinh lần lượt các Prefab vào từng slot index
        for (int i = 0; i < slotCount; i++)
        {
            if (testBlockPrefabs[i] != null)
            {
                SpawnBlockAtSlot(i, testBlockPrefabs[i]);
            }
        }
    }

    /// <summary>
    /// Tạo visual cho các ô Slot trên khay chứa dựa theo số lượng slot (numberOfSlots)
    /// </summary>
    public void GenerateSpawnSlots(int numberOfSlots)
    {
        ClearSpawnVisuals();

        if (numberOfSlots <= 0) return;

        // Căn giữa các slot theo trục X
        float startX = -((numberOfSlots - 1) * slotSpacing) / 2f;

        for (int i = 0; i < numberOfSlots; i++)
        {
            Vector3 slotPosition = transform.position + new Vector3(startX + (i * slotSpacing), 0f, 0f);

            // Tạo Cell background cho slot (nếu được gán Prefab)
            if (cellPrefab != null)
            {
                GameObject slotView = Instantiate(cellPrefab, slotPosition, Quaternion.identity, transform);
                slotView.name = string.Format("SpawnSlot_{0}", i);
                slotViews.Add(slotView);
            }
        }
    }

    /// <summary>
    /// Sinh ra 1 Block tại Slot index tương ứng trên khay
    /// </summary>
    public Block SpawnBlockAtSlot(int slotIndex, Block blockPrefab)
    {
        if (blockPrefab == null) return null;

        Vector3 spawnPosition = GetSlotWorldPosition(slotIndex);
        spawnPosition += Vector3.up * 0.1f; // Tránh z-fighting giống BoardView

        Block blockView = Instantiate(blockPrefab, spawnPosition, Quaternion.identity, transform);
        blockView.name = string.Format("SpawnBlock_{0}", slotIndex);

        

        // Đăng ký Event thả block cho GameManager giống như BoardView
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            blockView.OnBlockDropped += gameManager.HandleBlockDropped;
        }

        blockView.SetPlaced(false);

        spawnBlocks.Add(blockView);
        return blockView;
    }

    /// <summary>
    /// Gọi khi Block được đặt thành công lên Board (Xóa khỏi danh sách Spawn nhưng KHÔNG Destroy GameObject)
    /// </summary>
    public void RemoveBlockFromSpawn(Block block)
    {
        if (spawnBlocks.Contains(block))
        {
            spawnBlocks.Remove(block);
        }
    }

    /// <summary>
    /// Tính vị trí World Position của một Slot theo index
    /// </summary>
    public Vector3 GetSlotWorldPosition(int slotIndex)
    {
        if (slotViews.Count > 0 && slotIndex < slotViews.Count)
        {
            return slotViews[slotIndex].transform.position;
        }

        // Tự tính vị trí nếu không dùng cellPrefab
        int totalSlots = Mathf.Max(1, slotViews.Count);
        float startX = -((totalSlots - 1) * slotSpacing) / 2f;
        return transform.position + new Vector3(startX + (slotIndex * slotSpacing), 0f, 0f);
    }

    /// <summary>
    /// Dọn dẹp sạch toàn bộ Visual Slot và Block cũ
    /// </summary>
    public void ClearSpawnVisuals()
    {
        foreach (GameObject slotView in slotViews)
        {
            DestroyVisual(slotView);
        }

        foreach (Block blockView in spawnBlocks)
        {
            if (blockView != null)
            {
                DestroyVisual(blockView.gameObject);
            }
        }

        slotViews.Clear();
        spawnBlocks.Clear();
    }

    private static void DestroyVisual(Object visual)
    {
        if (visual == null) return;

        if (Application.isPlaying)
        {
            Destroy(visual);
        }
        else
        {
            DestroyImmediate(visual);
        }
    }
}