using UnityEngine;
using UnityEngine.UI;

namespace Localization
{
    public class LocalizedLayout : MonoBehaviour
    {
        [SerializeField] float minWidth = 180f;
        [SerializeField] bool fitWhenLayoutGroup;

        RectTransform rect;
        LayoutElement element;

        public void Apply()
        {
            if (rect == null)
                rect = transform as RectTransform;
            if (rect == null)
                return;

            if (element == null)
                element = GetComponent<LayoutElement>();
            if (element != null)
            {
                if (element.minWidth < minWidth)
                    element.minWidth = minWidth;
                if (element.flexibleWidth < 1f)
                    element.flexibleWidth = 1f;
            }

            if (rect.sizeDelta.x > 0f && rect.sizeDelta.x < minWidth)
                rect.sizeDelta = new Vector2(minWidth, rect.sizeDelta.y);

            if (!fitWhenLayoutGroup || GetComponent<LayoutGroup>() == null)
                return;
            ContentSizeFitter fitter = GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = gameObject.AddComponent<ContentSizeFitter>();
            if (fitter.horizontalFit == ContentSizeFitter.FitMode.Unconstrained)
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}
