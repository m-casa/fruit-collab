using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    private const string Kitchen = "Kitchen";

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        LoadScene(Kitchen);
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        // Load the scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // Don't let the scene activate right away
        asyncLoad.allowSceneActivation = false;

        // Wait until the load is almost complete (90%)
        while (asyncLoad.progress < 0.9f)
        {
            // Optional: Display loading here
            // float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            // Debug.Log("Loading progress: " + (progress * 100) + "%");

            yield return null;
        }

        // Allow the scene to activate
        asyncLoad.allowSceneActivation = true;

        // Wait one additional frame
        yield return null;

        // We can also wait until the scene fully loads with below code
        //while (!asyncLoad.isDone)
        //{
        //    // Optional: Display loading here
        //    // float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
        //    // Debug.Log("Loading progress: " + (progress * 100) + "%");

        //    yield return null;
        //}

        // Scene has finished loading
        OnSceneLoaded();
    }

    private void OnSceneLoaded()
    {
        Debug.Log("Scene has finished loading!");

        UIManager.Instance.ShowStartScreen();

        CameraManager.Instance.EnableCamera();

        Destroy(gameObject);
    }
}
