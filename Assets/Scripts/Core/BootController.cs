using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; 
public class BootController : MonoBehaviour
{
    public static BootController Instance;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }    
        DontDestroyOnLoad(gameObject);
        LoadGameplay();
    }
    public void LoadGameplay()
    {
        StartCoroutine(LoadSceneAsyncRoutine("GamePlay"));
    }

    private IEnumerator LoadSceneAsyncRoutine(string sceneName)
    {

        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);


        while (!asyncLoad.isDone)
        {
           
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            Debug.Log($"Tiến trình tải: {progress * 100}%");

            yield return null; 
        }
    }
}
