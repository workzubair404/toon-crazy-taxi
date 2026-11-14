using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Localization
{
    /// <summary>
    /// One language panel for the whole game. Lives in the first scene, survives scene loads,
    /// and opens on the main menu until the player confirms a language once.
    /// </summary>
    public class GameLocalizationHost : MonoBehaviour
    {
        public static GameLocalizationHost Instance { get; private set; }

        [SerializeField] LanguageSelectionPanel panel;
        [SerializeField] string mainMenuSceneName;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (panel != null)
                panel.Hide();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this)
                Instance = null;
        }

        void Start()
        {
            WireLanguageButtons();
            TryAutoOpen();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            WireLanguageButtons();
            TryAutoOpen();
        }

        void TryAutoOpen()
        {
            if (panel == null || LocalizationManager.HasChosenLanguage)
                return;
            if (SceneManager.GetActiveScene().name != mainMenuSceneName)
                return;
            panel.Show(false, true, null);
        }

        public void OpenFromButton()
        {
            if (panel != null)
                panel.Show(true, false, null);
        }

        void WireLanguageButtons()
        {
            Button[] buttons = FindObjectsOfType<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null || button.name != "Language")
                    continue;
                button.onClick.RemoveListener(OpenFromButton);
                button.onClick.AddListener(OpenFromButton);
            }
        }
    }
}
