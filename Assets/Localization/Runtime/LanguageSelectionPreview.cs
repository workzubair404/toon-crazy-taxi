using UnityEngine;

namespace Localization
{
    public class LanguageSelectionPreview : MonoBehaviour
    {
        [SerializeField] LanguageSelectionPanel panel;

        void Start()
        {
            if (panel != null)
                panel.Show(true, true, null);
        }
    }
}
