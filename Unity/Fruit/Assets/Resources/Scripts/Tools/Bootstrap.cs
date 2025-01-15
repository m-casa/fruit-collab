using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    private const string Kitchen = "Kitchen";

    void Start()
    {
        SceneManager.LoadScene(Kitchen);
    }
}
