using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small UI bridge between a Toggle and BackgroundMusicController.
/// Toggle `isOn` = music enabled.
/// </summary>
public class BackgroundMusicUI : MonoBehaviour
{
    public Toggle musicToggle;
    public Text labelText;

    private void Start()
    {
        if (musicToggle == null)
        {
            musicToggle = GetComponent<Toggle>();
        }

        var ctrl = BackgroundMusicController.Instance;
        if (ctrl != null)
        {
            bool isMuted = ctrl.IsMuted;
            bool musicOn = !isMuted;
            if (musicToggle != null) musicToggle.isOn = musicOn;
            UpdateLabel(musicOn);
        }

        if (musicToggle != null)
        {
            musicToggle.onValueChanged.AddListener(OnToggleChanged);
        }
    }

    private void OnDestroy()
    {
        if (musicToggle != null)
        {
            musicToggle.onValueChanged.RemoveListener(OnToggleChanged);
        }
    }

    private void OnToggleChanged(bool isOn)
    {
        var ctrl = BackgroundMusicController.Instance;
        if (ctrl != null)
        {
            ctrl.SetMusicMuted(!isOn);
        }

        UpdateLabel(isOn);
    }

    private void UpdateLabel(bool isOn)
    {
        if (labelText != null)
        {
            labelText.text = isOn ? "Music: On" : "Music: Off";
        }
    }
}
