using Localization;
using UnityEngine;

/// <summary>
/// On Fire TV, limits the green focus line to buttons on this panel while it is open.
/// </summary>
[DisallowMultipleComponent]
public class TvOverlayFocus : MonoBehaviour
{
    void OnEnable()
    {
        OverlayFocus.BringToFront(gameObject);
    }
}