using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class StoryController : MonoBehaviour
{
    [Header("References")]
    public PlayableDirector playableDirector;

    [Header("Next Scene")]
    [Tooltip("Name of the scene to load when the timeline ends (must be in Build Settings)")]
    public string nextSceneName = "runner";

    void Update()
    {
       if(playableDirector.time >= playableDirector.duration - 0.01)
       {
           if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogWarning("StoryController: nextSceneName is empty. Add a scene name in the inspector.");
                return;
            }

            SceneManager.LoadScene(nextSceneName);
       } 
    }
}
