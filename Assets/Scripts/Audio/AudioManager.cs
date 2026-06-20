using UnityEngine;
using UnityEngine.Audio;

namespace ArmyVArmy.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] AudioMixer mixer;

        const string MusicParam = "MusicVol";
        const string SfxParam = "SfxVol";
        const string MusicPrefsKey = "ArmyVArmy.MusicVolume";
        const string SfxPrefsKey = "ArmyVArmy.SfxVolume";
        const float MutedDb = -80f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SetMusicVolume(GetMusicVolume());
            SetSfxVolume(GetSfxVolume());
        }

        public float GetMusicVolume() => PlayerPrefs.GetFloat(MusicPrefsKey, 1f);
        public float GetSfxVolume() => PlayerPrefs.GetFloat(SfxPrefsKey, 1f);

        public void SetMusicVolume(float linear01)
        {
            PlayerPrefs.SetFloat(MusicPrefsKey, linear01);
            PlayerPrefs.Save();
            mixer.SetFloat(MusicParam, LinearToDb(linear01));
        }

        public void SetSfxVolume(float linear01)
        {
            PlayerPrefs.SetFloat(SfxPrefsKey, linear01);
            PlayerPrefs.Save();
            mixer.SetFloat(SfxParam, LinearToDb(linear01));
        }

        static float LinearToDb(float linear01)
        {
            return linear01 <= 0.0001f ? MutedDb : Mathf.Log10(linear01) * 20f;
        }
    }
}
