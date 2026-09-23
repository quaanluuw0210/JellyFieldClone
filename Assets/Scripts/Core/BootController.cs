using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class BootController : MonoBehaviour
{
    public static BootController Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
            return; 
        }

        LoadGameplay();
    }

    public void LoadGameplay()
    {
        StartCoroutine(LoadSceneAsyncRoutine("SampleScene"));
    }

    private IEnumerator LoadSceneAsyncRoutine(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            Debug.Log($"Tiến trình tải: {progress * 100}%");
            yield return null;
        }

        yield return new WaitForSeconds(1f);

        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.BackToMainMenu();
        }
    }
}