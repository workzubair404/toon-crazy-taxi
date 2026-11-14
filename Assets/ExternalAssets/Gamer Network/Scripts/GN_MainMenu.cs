using Localization;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class GN_MainMenu : MonoBehaviour {

	[Header ("Scene Selection")]
	public Scenes NextScene;

	[Header ("UI Panels")]
	public GameObject UnlockPopUpPanel;
	public GameObject storePanel;
	public GameObject settingPanel;
	public GameObject rewardPanel;
	public GameObject ExitDialogue;
	public Text coinsText;
	
	[Header ("Live Main Menu objects")]
    public bool isLiveMainMenu= true;
	public GameObject[] LiveMainMenuObjects;

    public static GN_MainMenu instance = null;
	void Awake () {
        instance = this;
    }

	void OnEnable () {
		LocalizationManager.LanguageChanged += OnLanguageChanged;
	}

	void OnDisable () {
		LocalizationManager.LanguageChanged -= OnLanguageChanged;
	}

	void OnLanguageChanged () {
		checkCoins();
	}

	void Start () {

		Time.timeScale = 1;
		if (!GameManager.Instance.Initialized) {
			InitializeGame ();
		}
		InitializeUI ();
		if (!SaveData.Instance.UnlockEverything && UnlockPopUpPanel != null)
			OpenPanel(UnlockPopUpPanel);
    }

	void InitializeGame () {
		SaveData.Instance = new SaveData ();
		GN_SaveLoad.LoadProgress ();
		GameManager.Instance.Initialized = true;
	}
	void InitializeUI ()
	{
		checkCoins();
        if (isLiveMainMenu)
        {
            foreach(GameObject gm in LiveMainMenuObjects)
            {
                gm.SetActive(false);
            }
            if (LiveMainMenuObjects.Length > 0)
            {
                LiveMainMenuObjects[Random.Range(0, LiveMainMenuObjects.Length)].SetActive(true);
            }
        }
        else
        {
            foreach (GameObject gm in LiveMainMenuObjects)
            {
                gm.SetActive(false);
            }
        }
		if (ExitDialogue != null)
			ExitDialogue.SetActive (false);
	}

	public void PlayBtn () {
		Debug.Log(NextScene.ToString());
		SceneManager.LoadScene (NextScene.ToString ());
	}

	public void checkCoins()
	{
		if (coinsText != null)
			coinsText.text = SaveData.Instance.Coins.ToString();
	}
	public void soundOn()
	{
        SaveData.Instance.IsSoundOn = true;
		GN_SaveLoad.SaveProgress();
        AudioListener.pause = false;
	}
	public void soundOff()
	{
        SaveData.Instance.IsSoundOn = false;
        GN_SaveLoad.SaveProgress();
        AudioListener.pause = true;
    }

	public void selectControl(int control)
	{
		if (control == 1)
		{
			GameManager.Instance.steeringControl = true;
			GameManager.Instance.arrowControl = false;
		}
		else if(control == 2)
		{
			GameManager.Instance.arrowControl = true;
			GameManager.Instance.steeringControl = false;
		}
	}


	public void ShowRateUs () {
		Application.OpenURL("");
	}
    public void MoreGamesBtn()
    {
        Application.OpenURL("");
    }
    public void Exit () {
		Application.Quit ();
	}
    
    public void ResetSaveData () {
		SaveData.Instance = null;
		GN_SaveLoad.DeleteProgress ();
		SaveData.Instance = new SaveData ();
		GN_SaveLoad.LoadProgress ();
	}
    #region Ui Buttons
    static void OpenPanel(GameObject panel)
    {
        if (panel == null)
            return;
        panel.SetActive(true);
        if (panel.GetComponent<TvOverlayFocus>() == null)
            panel.AddComponent<TvOverlayFocus>();
    }

    public void ShowStore()
    {
        OpenPanel(storePanel);
    }
    public void CloseStore()
    {
		if (storePanel != null)
			storePanel.SetActive (false);
    }
    public void ShowSetting()
    {
        OpenPanel(settingPanel);
    }
    public void CloseSetting()
    {
		if (settingPanel != null)
			settingPanel.SetActive (false);
    }
    public void ShowReward()
    {
        OpenPanel(rewardPanel);
    }
    public void CloseReward()
    {
		if (rewardPanel != null)
			rewardPanel.SetActive (false);
    }
    #endregion
    #region InApp
    public void CoinsPurchase(int coins)
    {
        GameManager.Instance.CoinsPurchase(coins);
       checkCoins();
    }

    public void RemoveAds()
    {
        GameManager.Instance.RemoveAds();
        if (AdsManager.instance)
        {
            AdsManager.instance.DestroyBanner();
        }
    }
    public void UnlockEverything()
    {
        GameManager.Instance.UnlockEverything();
    }
    #endregion
}
