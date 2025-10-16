using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnvironmentStatsManager : MonoBehaviour
{
    public static EnvironmentStatsManager Instance { get; private set; }

    public event Action<int, int> OnStatsChanged;

    private int cozyTotal;
    private int natureTotal;

    public int CozyTotal => cozyTotal;
    public int NatureTotal => natureTotal;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            if (Instance == this) Instance = null;
            Destroy(gameObject);
        }
    }

    public void SetValues(int cozy, int nature)
    {
        cozyTotal = cozy;
        natureTotal = nature;
        OnStatsChanged?.Invoke(cozyTotal, natureTotal);
        MilestoneManager.Instance?.UpdateEnvironment(cozyTotal, natureTotal);
    }

    public void AddValues(int cozy, int nature)
    {
        cozyTotal += cozy;
        natureTotal += nature;
        OnStatsChanged?.Invoke(cozyTotal, natureTotal);
        MilestoneManager.Instance?.UpdateEnvironment(cozyTotal, natureTotal);
    }

    public void RemoveValues(int cozy, int nature)
    {
        cozyTotal -= cozy;
        natureTotal -= nature;
        OnStatsChanged?.Invoke(cozyTotal, natureTotal);
        MilestoneManager.Instance?.UpdateEnvironment(cozyTotal, natureTotal);
    }
}
