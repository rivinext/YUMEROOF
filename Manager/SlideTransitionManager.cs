using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages scene transitions using a sliding panel animation.
/// </summary>
public class SlideTransitionManager : MonoBehaviour
{
    public static SlideTransitionManager Instance { get; private set; }

    [SerializeField] private UISlidePanel slidePanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Loads a scene with a slide in/out transition.
    /// </summary>
    public void LoadSceneWithSlide(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// Slides out the panel and waits for completion.
    /// </summary>
    public IEnumerator RunSlideOut()
    {
        if (slidePanel != null)
        {
            bool outComplete = false;
            System.Action onOut = () => outComplete = true;
            slidePanel.OnSlideOutComplete += onOut;
            slidePanel.SlideOut();
            yield return new WaitUntil(() => outComplete);
            slidePanel.OnSlideOutComplete -= onOut;
        }
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        if (slidePanel != null)
        {
            bool inComplete = false;
            System.Action onIn = () => inComplete = true;
            slidePanel.OnSlideInComplete += onIn;
            slidePanel.SlideIn();
            yield return new WaitUntil(() => inComplete);
            slidePanel.OnSlideInComplete -= onIn;
        }

        SaveGameManager.Instance.SaveCurrentSlot();

        string slotKey = UIMenuManager.SelectedSlotKey;
        if (string.IsNullOrEmpty(slotKey))
        {
            slotKey = SaveGameManager.Instance?.CurrentSlotKey;
        }
        GameSessionInitializer.CreateIfNeeded(slotKey);
        UIMenuManager.ClearSelectedSlot();

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
        {
            yield return null;
        }

        if (SceneManager.GetActiveScene().name != "MainMenu" &&
            FindObjectOfType<GameClock>() == null)
        {
            new GameObject("GameClock").AddComponent<GameClock>();
        }

        var mgr = SlideTransitionManager.Instance;
        if (mgr != null && mgr != this)
        {
            yield return mgr.RunSlideOut();
        }
        else
        {
            yield return RunSlideOut();
        }
    }
}
