using DG.Tweening; // Thêm namespace DOTween
using System.Collections.Generic;
using UnityEngine;

public class SpawnView : MonoBehaviour
{
    [Header("Data & Settings")]
    [SerializeField] private float slotSpacing = 2.5f;

    [Header("Prefabs")]
    [SerializeField] private GameObject cellPrefab;

    [Header("Startup Test")]
    [SerializeField] private bool runHardcodedTestOnStart;
    [SerializeField] private List<Block> testBlockPrefabs;

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

    public void TestSpawnHardcodedSpawn()
    {
        if (testBlockPrefabs == null || testBlockPrefabs.Count == 0)
        {
            Debug.LogWarning("[SpawnView] Chưa gán testBlockPrefabs trong Inspector!");
            return;
        }

        int slotCount = Mathf.Min(3, testBlockPrefabs.Count);
        GenerateSpawnSlots(slotCount);

        for (int i = 0; i < slotCount; i++)
        {
            if (testBlockPrefabs[i] != null)
            {
                SpawnBlockAtSlot(i, testBlockPrefabs[i]);
            }
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
        }
    }

    public Block SpawnBlockAtSlot(int slotIndex, Block blockPrefab)
    {
        if (blockPrefab == null) return null;

        Vector3 spawnPosition = GetSlotWorldPosition(slotIndex);
        spawnPosition += Vector3.up * 0.1f;

        Block blockView = Instantiate(blockPrefab, spawnPosition, Quaternion.identity, transform);
        blockView.name = string.Format("SpawnBlock_{0}", slotIndex);

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            blockView.OnBlockDropped += gameManager.HandleBlockDropped;
        }

        blockView.SetPlaced(false);

        spawnBlocks.Add(blockView);
        return blockView;
    }

    public void RemoveBlockFromSpawn(Block block)
    {
        if (spawnBlocks.Contains(block))
        {
            spawnBlocks.Remove(block);
        }
    }

    public Vector3 GetSlotWorldPosition(int slotIndex)
    {
        if (slotViews.Count > 0 && slotIndex < slotViews.Count)
        {
            return slotViews[slotIndex].transform.position;
        }

        int totalSlots = Mathf.Max(1, slotViews.Count);
        float startX = -((totalSlots - 1) * slotSpacing) / 2f;
        return transform.position + new Vector3(startX + (slotIndex * slotSpacing), 0f, 0f);
    }

    /// <summary>
    /// Dọn dẹp sạch toàn bộ Visual Slot và Block cũ chưa được kéo đặt lên Board
    /// </summary>
    public void ClearSpawnVisuals()
    {
        // 1. Dọn dẹp các Slot hiển thị khay
        foreach (GameObject slotView in slotViews)
        {
            DestroyVisual(slotView);
        }
        slotViews.Clear();

        // 2. Dọn dẹp các Block CÒN LẠI TRÊN KHAY (chưa kéo lên Board)
        for (int i = spawnBlocks.Count - 1; i >= 0; i--)
        {
            Block blockView = spawnBlocks[i];
            if (blockView != null && blockView.gameObject != null)
            {
                // Dừng mọi Coroutine và Tween đang chạy trên Block trước khi Hủy
                blockView.StopAllCoroutines();
                blockView.transform.DOKill(true);
                DestroyVisual(blockView.gameObject);
            }
        }
        spawnBlocks.Clear();
    }

    private static void DestroyVisual(Object visual)
    {
        if (visual == null) return;

        // Nếu là GameObject, kill sạch Tween liên quan đến nó
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