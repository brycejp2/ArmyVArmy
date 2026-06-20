using ArmyVArmy.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace ArmyVArmy.UI
{
    public class SettingsPanelController : MonoBehaviour
    {
        [SerializeField] AudioManager audioManager;
        [SerializeField] Slider musicSlider;
        [SerializeField] Slider sfxSlider;

        void OnEnable()
        {
            musicSlider.SetValueWithoutNotify(audioManager.GetMusicVolume());
            sfxSlider.SetValueWithoutNotify(audioManager.GetSfxVolume());
        }

        public void OnMusicVolumeChanged(float value) => audioManager.SetMusicVolume(value);
        public void OnSfxVolumeChanged(float value) => audioManager.SetSfxVolume(value);
        public void OnBack() => gameObject.SetActive(false);
    }
}
