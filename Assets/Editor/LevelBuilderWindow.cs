#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// --- ĐỊNH NGHĨA DỮ LIỆU ĐỂ TRÁNH LỖI CS0246 ---
[Serializable]
public struct BlockData
{
    public string block_type;
    public JellyColor TL;
    public JellyColor TR;
    public JellyColor BL;
    public JellyColor BR;
}

[Serializable]
public class LevelConfigBuilder
{
    public int level;
    public List<BlockData> spawn_queue;
}

// Đã tách rõ vị trí thanh dài (bar) cho 2 dạng "1 thanh + 2 ô đơn"
// Hor: thanh nằm ngang -> có thể ở hàng TRÊN hoặc hàng DƯỚI
// Ver: thanh nằm dọc   -> có thể ở cột TRÁI hoặc cột PHẢI
//public enum BlockTypeBuilder
//{
//    FourBlock,           // 4 màu riêng biệt (TL, TR, BL, BR độc lập)
//    FullBlock,           // 1 màu duy nhất cho cả 4 ô
//    DoubleHor,           // Hàng trên 1 màu, Hàng dưới 1 màu
//    DoubleVer,           // Cột trái 1 màu, Cột phải 1 màu
//    OneDTwoS_Hor_Top,    // Thanh ngang ở TRÊN (TL=TR) + BL, BR là 2 ô đơn
//    OneDTwoS_Hor_Bottom, // Thanh ngang ở DƯỚI (BL=BR) + TL, TR là 2 ô đơn
//    OneDTwoS_Ver_Left,   // Thanh dọc ở TRÁI (TL=BL) + TR, BR là 2 ô đơn
//    OneDTwoS_Ver_Right   // Thanh dọc ở PHẢI (TR=BR) + TL, BL là 2 ô đơn
//}

public class LevelBuilderWindow : EditorWindow
{
    private int levelNumber = 1;

    // --- Config Khối đang tạo ---
    private BlockType selectedType = BlockType.FourBlock;
    private JellyColor colorTL = JellyColor.Pink;
    private JellyColor colorTR = JellyColor.Purple;
    private JellyColor colorBL = JellyColor.Green;
    private JellyColor colorBR = JellyColor.Yellow;

    // --- Danh sách khối trong Level ---
    private List<BlockData> spawnQueue = new List<BlockData>();

    [MenuItem("Tools/Jelly Level Builder")]
    public static void ShowWindow()
    {
        GetWindow<LevelBuilderWindow>("Jelly Level Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("TOOL THIẾT KẾ LEVEL JELLY FIELD", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // 1. Cấu hình Level
        levelNumber = EditorGUILayout.IntField("Level Số:", levelNumber);
        EditorGUILayout.Space(10);

        // 2. Chọn loại khối
        GUILayout.Label("1. CHỌN LOẠI KHỐI (BLOCK TYPE)", EditorStyles.boldLabel);
        selectedType = (BlockType)EditorGUILayout.EnumPopup("Loại khối:", selectedType);

        EditorGUILayout.Space(5);

        // 3. Chọn màu tùy theo Loại khối
        GUILayout.Label("2. CHỌN MÀU CHO KHỐI", EditorStyles.boldLabel);
        DrawColorControlsBasedOnType();

        EditorGUILayout.Space(10);

        // Xem trước nhanh dạng chữ để tránh chọn nhầm hướng thanh dài
        EditorGUILayout.HelpBox(GetPreviewText(), MessageType.None);

        EditorGUILayout.Space(10);

        // 4. Nút Thêm Khối
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button(" + Thêm Khối Vào Danh Sách ", GUILayout.Height(30)))
        {
            AddBlockToQueue();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(15);

        // 5. Hiển thị danh sách khối đã thêm
        GUILayout.Label($"3. DANH SÁCH KHỐI ĐÃ TẠO ({spawnQueue.Count} blocks)", EditorStyles.boldLabel);
        DrawBlockQueueList();

        EditorGUILayout.Space(15);

        // 6. Nút Xuất JSON
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button(" BUILD RA FILE JSON ", GUILayout.Height(40)))
        {
            ExportToJSON();
        }
        GUI.backgroundColor = Color.white;
    }

    private void DrawColorControlsBasedOnType()
    {
        switch (selectedType)
        {
            case BlockType.FullBlock:
                colorTL = (JellyColor)EditorGUILayout.EnumPopup("Màu toàn bộ khối:", colorTL);
                colorTR = colorBL = colorBR = colorTL;
                break;

            case BlockType.DoubleHor:
                colorTL = (JellyColor)EditorGUILayout.EnumPopup("Màu Hàng Trên:", colorTL);
                colorTR = colorTL;
                colorBL = (JellyColor)EditorGUILayout.EnumPopup("Màu Hàng Dưới:", colorBL);
                colorBR = colorBL;
                break;

            case BlockType.DoubleVer:
                colorTL = (JellyColor)EditorGUILayout.EnumPopup("Màu Cột Trái:", colorTL);
                colorBL = colorTL;
                colorTR = (JellyColor)EditorGUILayout.EnumPopup("Màu Cột Phải:", colorTR);
                colorBR = colorTR;
                break;

            // --- Thanh ngang ở TRÊN: TL=TR là thanh, BL/BR là 2 ô đơn ---
            case BlockType.OneDTwoS_Hor_Top:
                colorTL = (JellyColor)EditorGUILayout.EnumPopup("Màu Thanh Ngang (trên):", colorTL);
                colorTR = colorTL;
                colorBL = (JellyColor)EditorGUILayout.EnumPopup("Màu Ô Đơn (dưới-trái):", colorBL);
                colorBR = (JellyColor)EditorGUILayout.EnumPopup("Màu Ô Đơn (dưới-phải):", colorBR);
                break;

            // --- Thanh ngang ở DƯỚI: BL=BR là thanh, TL/TR là 2 ô đơn ---
            case BlockType.OneDTwoS_Hor_Bottom:
                colorBL = (JellyColor)EditorGUILayout.EnumPopup("Màu Thanh Ngang (dưới):", colorBL);
                colorBR = colorBL;
                colorTL = (JellyColor)EditorGUILayout.EnumPopup("Màu Ô Đơn (trên-trái):", colorTL);
                colorTR = (JellyColor)EditorGUILayout.EnumPopup("Màu Ô Đơn (trên-phải):", colorTR);
                break;

            // --- Thanh dọc ở TRÁI: TL=BL là thanh, TR/BR là 2 ô đơn ---
            case BlockType.OneDTwoS_Ver_Left:
                colorTL = (JellyColor)EditorGUILayout.EnumPopup("Màu Thanh Dọc (trái):", colorTL);
                colorBL = colorTL;
                colorTR = (JellyColor)EditorGUILayout.EnumPopup("Màu Ô Đơn (trên-phải):", colorTR);
                colorBR = (JellyColor)EditorGUILayout.EnumPopup("Màu Ô Đơn (dưới-phải):", colorBR);
                break;

            // --- Thanh dọc ở PHẢI: TR=BR là thanh, TL/BL là 2 ô đơn ---
            case BlockType.OneDTwoS_Ver_Right:
                colorTR = (JellyColor)EditorGUILayout.EnumPopup("Màu Thanh Dọc (phải):", colorTR);
                colorBR = colorTR;
                colorTL = (JellyColor)EditorGUILayout.EnumPopup("Màu Ô Đơn (trên-trái):", colorTL);
                colorBL = (JellyColor)EditorGUILayout.EnumPopup("Màu Ô Đơn (dưới-trái):", colorBL);
                break;

            case BlockType.FourBlock:
                EditorGUILayout.BeginHorizontal();
                colorTL = (JellyColor)EditorGUILayout.EnumPopup("Trên-Trái (TL):", colorTL);
                colorTR = (JellyColor)EditorGUILayout.EnumPopup("Trên-Phải (TR):", colorTR);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                colorBL = (JellyColor)EditorGUILayout.EnumPopup("Dưới-Trái (BL):", colorBL);
                colorBR = (JellyColor)EditorGUILayout.EnumPopup("Dưới-Phải (BR):", colorBR);
                EditorGUILayout.EndHorizontal();
                break;
        }
    }

    // Text mô tả nhanh layout để người dùng kiểm tra trước khi Thêm Khối
    private string GetPreviewText()
    {
        switch (selectedType)
        {
            case BlockType.OneDTwoS_Hor_Top:
                return "Layout: [ THANH NGANG ][ THANH NGANG ]\n         [ ô đơn ][ ô đơn ]";
            case BlockType.OneDTwoS_Hor_Bottom:
                return "Layout: [ ô đơn ][ ô đơn ]\n         [ THANH NGANG ][ THANH NGANG ]";
            case BlockType.OneDTwoS_Ver_Left:
                return "Layout: [THANH][ ô đơn ]\n         [DỌC ][ ô đơn ]";
            case BlockType.OneDTwoS_Ver_Right:
                return "Layout: [ ô đơn ][THANH]\n         [ ô đơn ][DỌC ]";
            default:
                return $"TL:{colorTL} | TR:{colorTR} | BL:{colorBL} | BR:{colorBR}";
        }
    }

    private void AddBlockToQueue()
    {
        BlockData data = new BlockData
        {
            block_type = selectedType.ToString(),
            TL = colorTL,
            TR = colorTR,
            BL = colorBL,
            BR = colorBR
        };
        spawnQueue.Add(data);
    }

    private void DrawBlockQueueList()
    {
        if (spawnQueue.Count == 0)
        {
            EditorGUILayout.HelpBox("Chưa có khối nào trong danh sách. Hãy bấm nút Thêm Khối ở trên!", MessageType.Info);
            return;
        }

        for (int i = 0; i < spawnQueue.Count; i++)
        {
            EditorGUILayout.BeginHorizontal("box");
            var block = spawnQueue[i];
            GUILayout.Label($"Khối #{i + 1} [{block.block_type}] - TL:{block.TL} | TR:{block.TR} | BL:{block.BL} | BR:{block.BR}");

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Xóa", GUILayout.Width(50)))
            {
                spawnQueue.RemoveAt(i);
                break; // tránh lỗi sửa list đang lặp
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
    }

    private void ExportToJSON()
    {
        if (spawnQueue.Count == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Danh sách khối đang trống!", "OK");
            return;
        }

        LevelConfigBuilder config = new LevelConfigBuilder
        {
            level = levelNumber,
            spawn_queue = spawnQueue
        };

        string json = JsonUtility.ToJson(config, true);

        // Lưu vào thư mục Assets/Resources/Levels/
        string dirPath = Application.dataPath + "/Resources/Levels/";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        string filePath = dirPath + $"Level{levelNumber}.json";
        File.WriteAllText(filePath, json);

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Thành công!", $"Đã lưu file JSON tại:\n{filePath}", "OK");
    }
}
#endif