using UnityEngine;

/// <summary>
/// Đọc level JSON từ Resources/Levels/Level_{levelNumber}.json.
/// TextAsset được Unity load an toàn trong build, không cần tự xử lý path hệ điều hành.
/// </summary>
public static class LevelLoader
{
    private const string LevelResourceFolder = "Levels/Level";

    public static LevelConfig LoadLevel(int levelNumber)
    {
        if (levelNumber < 1)
        {
            Debug.LogError(string.Format("[LevelLoader] Level không hợp lệ: {0}.", levelNumber));
            return null;
        }

        string resourcePath = LevelResourceFolder + levelNumber;
        TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);
        if (jsonAsset == null)
        {
            Debug.LogError(string.Format(
                "[LevelLoader] Không tìm thấy JSON tại Resources/{0}.json.", resourcePath));
            return null;
        }

        if (string.IsNullOrWhiteSpace(jsonAsset.text))
        {
            Debug.LogError(string.Format("[LevelLoader] JSON level {0} đang rỗng.", levelNumber));
            return null;
        }

        try
        {
            LevelConfig config = JsonUtility.FromJson<LevelConfig>(jsonAsset.text);
            if (config == null)
            {
                Debug.LogError(string.Format("[LevelLoader] Parse level {0} trả về null.", levelNumber));
                return null;
            }

            if (config.board_setup == null) config.board_setup = new System.Collections.Generic.List<BoardCellData>();
            if (config.spawn_queue == null) config.spawn_queue = new System.Collections.Generic.List<SpawnBlockData>();

            if (config.level != 0 && config.level != levelNumber)
            {
                Debug.LogWarning(string.Format(
                    "[LevelLoader] Field level trong JSON là {0}, nhưng file được tải theo Level{1}.",
                    config.level, levelNumber));
            }

            return config;
        }
        catch (System.Exception exception)
        {
            Debug.LogError(string.Format(
                "[LevelLoader] Parse JSON level {0} thất bại: {1}", levelNumber, exception.Message));
            return null;
        }
    }
}