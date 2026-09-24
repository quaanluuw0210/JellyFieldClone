using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class SpawnView : MonoBehaviour
{
    [Header("Data & Settings")]
    [SerializeField] private float slotSpacing = 2.5f;

    [Header("Prefabs")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private BlockFactory blockFactory;


    // slotViews[i] quản lý visual của ô slot, đi kèm thông tin Block đang nằm trên Slot đó
    private readonly List<GameObject> slotViews = new List<GameObject>();
    private readonly List<Block> activeSpawnBlocks = new List<Block>();

    private readonly List<SpawnBlockData> blockSequenceList = new List<SpawnBlockData>();
    private int currentBlockIndex = 0;

    public IReadOnlyList<Block> SpawnBlocks => activeSpawnBlocks;

    private void Start()
    {
      
    }

    /// <summary>
    /// Khởi tạo khu vực Spawn từ dữ liệu LevelData
    /// </summary>
    public void InitializeSpawn(int activeSlotCount, List<SpawnBlockData> blockSequence)
    {
        ClearSpawnVisuals();
        blockSequenceList.Clear();
        currentBlockIndex = 0;

        // 1. Đẩy danh sách Block vào Hàng chờ (Queue)
        if (blockSequence != null)
        {
            foreach (SpawnBlockData blockData in blockSequence)
            {
                if (blockData != null)
                {
                    blockSequenceList.Add(blockData);
                }
            }
        }

        // 2. Tạo Visual các Slot khay spawn
        GenerateSpawnSlots(activeSlotCount);

        // 3. Lấp đầy các Slot ban đầu từ Hàng chờ
        CheckAndReplenishSlots();
    }

    /// <summary>
    /// Kiểm tra các Slot trống và lấy Block từ Queue ra để spawn bổ sung
    /// </summary>
    public void CheckAndReplenishSlots()
    {
        if (blockSequenceList.Count == 0) return;

        for (int i = 0; i < slotViews.Count; i++)
        {
           
            if (activeSpawnBlocks[i] != null) continue;

            if (blockSequenceList.Count > 0)
            {
                SpawnBlockData nextBlockData = blockSequenceList[currentBlockIndex % blockSequenceList.Count];

                currentBlockIndex++;
                if (nextBlockData != null)
                {
                    Block spawnedBlock = SpawnBlockAtSlot(i, nextBlockData);
                    activeSpawnBlocks[i] = spawnedBlock;
                }
            }
        }
    }

    /// <summary>
    /// Gọi hàm này khi người chơi kéo 1 Block ra khỏi Spawn Area thành công
    /// </summary>
    public void RemoveBlockFromSpawn(Block block)
    {
        int index = activeSpawnBlocks.IndexOf(block);
        if (index != -1)
        {
            activeSpawnBlocks[index] = null; // Đánh dấu Slot đó hiện tại bị trống

            // Tự động lấp trống Slot bằng Block tiếp theo trong Queue
            CheckAndReplenishSlots();
        }
    }

    public void GenerateSpawnSlots(int numberOfSlots)
    {
        ClearSpawnVisuals();

        if (numberOfSlots <= 0) return;

        float startX = -((numberOfSlots - 1) * slotSpacing) / 2f;

        for (int i = 0; i < numberOfSlots; i++)
        {
            Vector3 slotPosition = transform.position + new Vector3(startX + (i * slotSpacing), 0f, 0f);

            if (cellPrefab != null)
            {
                GameObject slotView = Instantiate(cellPrefab, slotPosition, Quaternion.identity, transform);
                slotView.name = string.Format("SpawnSlot_{0}", i);
                slotViews.Add(slotView);
            }

            // Đặt giữ chỗ giá trị null tương ứng với số lượng slot
            activeSpawnBlocks.Add(null);
        }
    }

    private Block SpawnBlockAtSlot(int slotIndex, SpawnBlockData blockData)
    {
        if (blockData == null || blockFactory == null) return null;

        Vector3 spawnPosition = GetSlotWorldPosition(slotIndex);
        spawnPosition += Vector3.up * 0.1f;

        Block blockView = blockFactory.Create(blockData);

        if (blockView != null)
        {
            blockView.transform.SetParent(transform);
            blockView.transform.position = spawnPosition;
            blockView.name = string.Format("SpawnBlock_{0}", slotIndex);

            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                blockView.OnBlockDropped += gameManager.HandleBlockDropped;
            }

            blockView.SetPlaced(false);
        }

        return blockView;
    }


    public Vector3 GetSlotWorldPosition(int slotIndex)
    {
        if (slotViews.Count > 0 && slotIndex < slotViews.Count && slotViews[slotIndex] != null)
        {
            return slotViews[slotIndex].transform.position;
        }

        int totalSlots = Mathf.Max(1, slotViews.Count);
        float startX = -((totalSlots - 1) * slotSpacing) / 2f;
        return transform.position + new Vector3(startX + (slotIndex * slotSpacing), 0f, 0f);
    }

    public void ClearSpawnVisuals()
    {
        // 1. Dọn dẹp các Slot hiển thị khay
        foreach (GameObject slotView in slotViews)
        {
            DestroyVisual(slotView);
        }
        slotViews.Clear();

        // 2. Dọn dẹp các Block CÒN LẠI TRÊN KHAY
        foreach (Block blockView in activeSpawnBlocks)
        {
            if (blockView != null && blockView.gameObject != null)
            {
                blockView.StopAllCoroutines();
                blockView.transform.DOKill(true);
                DestroyVisual(blockView.gameObject);
            }
        }
        activeSpawnBlocks.Clear();
    }



    private static void DestroyVisual(Object visual)
    {
        if (visual == null) return;

        if (visual is GameObject go)
        {
            go.transform.DOKill(true);
        }

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