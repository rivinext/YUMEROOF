using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    public event Action<int> OnMoneyChanged;

    private int currentMoney;

    public int CurrentMoney => currentMoney;
    public bool UnlimitedMoney { get; set; }

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

    public void AddMoney(int amount)
    {
        if (UnlimitedMoney && amount < 0)
        {
            return;
        }

        currentMoney += amount;
        OnMoneyChanged?.Invoke(currentMoney);
    }

    public void SetMoney(int amount)
    {
        currentMoney = amount;
        OnMoneyChanged?.Invoke(currentMoney);
    }
}
