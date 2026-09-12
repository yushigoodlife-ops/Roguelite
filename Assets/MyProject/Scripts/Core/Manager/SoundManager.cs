using UnityEngine;

namespace Core.Manager
{
    public class SoundManager : MonoBehaviour {

        public static SoundManager Instance { get; private set; }

        [Header("スピーカー設定")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource seSource;

        [Header("音量調整")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float bgmVolume = 1f;
        [Range(0f, 1f)] public float seVolume = 1f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void PlayBgm(AudioClip clip)
        {
            if (clip != null)
            {
                bgmSource.clip = clip;
                bgmSource.volume = masterVolume * bgmVolume;
                bgmSource.Play();
            }
        }

        public void UpdateVolumes()
        {
            bgmSource.volume = masterVolume * bgmVolume;
            seSource.volume = masterVolume * seVolume;
        }
    }
}
