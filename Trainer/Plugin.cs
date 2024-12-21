using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OHVTrainer;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("OHV.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    GameObject prefab;
    GameObject trainerWindow;

    TMP_Text timeScaleText;
    TMP_Text timeAddText; // Text that show hours that will be advanced to day time.
    Toggle infiniteStaminaToggle;
    Toggle infiniteHealthToggle;

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            Logger.LogMessage("F5 Pressed");
            trainerWindow.SetActive(!trainerWindow.activeSelf);

            if (trainerWindow.activeSelf)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
            }
            else
            {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
            }
        }
    }

    private void OnEnable()
    {
        // Rejestracja zdarzeń dla załadowania nowej sceny
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Wyrejestrowanie zdarzeń (ważne, aby uniknąć wycieków pamięci)
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    Transform GetMainPanel()
    {
        return trainerWindow.transform.Find("Panel").Find("Padding").Find("Scroll View").Find("Viewport").Find("Content");
    }

    // Funkcja wywoływana, gdy nowa scena zostanie załadowana
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Logger.LogInfo($"Scene loaded: {scene.name} (Index: {scene.buildIndex})");
        if (scene.name == "MainGame")
        {
            string assetBundlePath = Paths.PluginPath + "/trainer";
            AssetBundle bundle = AssetBundle.LoadFromFile(assetBundlePath);
            if (bundle == null)
            {
                Logger.LogError("Failed to load AssetBundle!");
                return;
            }
            

            prefab = bundle.LoadAsset<GameObject>("canvas");
            trainerWindow = Instantiate(prefab);
            trainerWindow.SetActive(false);
            Logger.LogMessage($"IsTrainerWindowActive: {trainerWindow.gameObject.activeSelf}");
            Logger.LogMessage($"TimeScale: {Time.timeScale}");

            timeScaleText = GetMainPanel().Find("Time").Find("TimeText").Find("Text").GetComponent<TMP_Text>();
            timeAddText = GetMainPanel().Find("GameTime").Find("TimeText").Find("Text").GetComponent<TMP_Text>();
            infiniteStaminaToggle = GetMainPanel().Find("Stamina").Find("InfiniteStamina").GetComponent<Toggle>();
            infiniteHealthToggle = GetMainPanel().Find("Stamina").Find("InfiniteHealth").GetComponent<Toggle>();

            SetupAddMoneyButton();
            FindTimeSpeedButtons();
            SetupOnInfiniteStaminaToggleChange();
            SetupOnInfinityHealthToggleChange();
            SetupDayTimeChange();
        }
    }

    void SetupOnInfinityHealthToggleChange()
    {
        infiniteHealthToggle.onValueChanged.AddListener(infiniteHealthToggleListener);
    }

    private void infiniteHealthToggleListener(bool isChecked)
    {
        OHVPlayerManager.I.isImmortal = isChecked;
    }

    void SetupDayTimeChange()
    {
        GetMainPanel().Find("GameTime").Find("TimeAdd").GetComponent<Button>().onClick.AddListener(OnDayTimeAddClicked);
        GetMainPanel().Find("GameTime").Find("TimeSubtract").GetComponent<Button>().onClick.AddListener(OnDayTimeSubtractClicked);
        GetMainPanel().Find("GameTime").Find("TimeApply").GetComponent<Button>().onClick.AddListener(AdvanceTime);
    }

    void AdvanceTime()
    {
        OHVDayNightCycleManager.I.AdvanceTimeBy(int.Parse(timeAddText.text));
    }

    void OnDayTimeAddClicked()
    {
        timeAddText.text = (int.Parse(timeAddText.text) + 1).ToString();
    }

    void OnDayTimeSubtractClicked()
    {
        if (int.Parse(timeAddText.text) != 0) timeAddText.text = (int.Parse(timeAddText.text) - 1).ToString();
    }

    void SetupOnInfiniteStaminaToggleChange()
    {
        infiniteStaminaToggle.onValueChanged.AddListener(infiniteStaminaToggleListener);
    }

    private void infiniteStaminaToggleListener(bool isChecked)
    {
        OHVPlayerManagerPhysique.I.infiniteStamina = isChecked;
    }

    void FindTimeSpeedButtons()
    {
        var timeSpeedTimeUp = GetMainPanel().Find("Time").Find("TimeFaster");
        var timeSpeedTimeDown = GetMainPanel().Find("Time").Find("TimeSlower");

        timeSpeedTimeUp.GetComponent<Button>().onClick.AddListener(TimeDeltaFaster);
        timeSpeedTimeDown.GetComponent<Button>().onClick.AddListener(TimeDeltaSlower);
    }

    void TimeDeltaFaster()
    {
        Time.timeScale = Time.timeScale + 0.2f;
        timeScaleText.text = $"{Time.timeScale:F1}x";
    }

    void TimeDeltaSlower()
    {
        Time.timeScale = Time.timeScale - 0.2f;
        if (Time.timeScale < 0.2f) Time.timeScale = 0.2f;
        timeScaleText.text = $"{Time.timeScale:F1}x";
    }

    void SetupAddMoneyButton()
    {
        var moneyButtonFind = GetMainPanel().Find("Money").Find("SetMoneyButton");
        var moneyButton = moneyButtonFind.GetComponent<Button>();
        moneyButton.onClick.AddListener(onSaveMoneyButtonClicked);
    }

    private void onSaveMoneyButtonClicked()
    {
        //var textInput = trainerWindow.transform.Find("Panel").Find("Padding").Find("Money").Find("MoneyAmountText").GetComponent<TMP_InputField>();
        //EconomyManagerPatchClass.Prefix(int.Parse(textInput.text));
        OHVEconomyManager.instance.AddCash(2000);
    }
}
