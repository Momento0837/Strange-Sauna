using UnityEngine;

namespace SuspiciousSauna
{
    public sealed class SaunaAudio : MonoBehaviour
    {
        public AudioSource music, effects;
        [Header("오디오 에셋 교체 (비워두면 임시 음원)")]
        public AudioClip backgroundMusic, orderSound, successSound, failureSound, cleaningSound;
        AudioClip bell, success, fail, brush;
        public float Music { get; private set; }
        public float Effects { get; private set; }
        void Awake()
        {
            if (music == null) music = gameObject.AddComponent<AudioSource>();
            if (effects == null) effects = gameObject.AddComponent<AudioSource>();
            music.loop = true; music.clip = backgroundMusic != null ? backgroundMusic : Melody();
            bell = orderSound != null ? orderSound : Tone(660, .2f);
            success = successSound != null ? successSound : Tone(880, .26f);
            fail = failureSound != null ? failureSound : Tone(180, .3f);
            brush = cleaningSound != null ? cleaningSound : Tone(330, .06f);
            SetMusic(PlayerPrefs.GetFloat("sauna.bgm", .3f)); SetEffects(PlayerPrefs.GetFloat("sauna.sfx", .65f)); music.Play();
        }
        public void SetMusic(float value) { Music = value; music.volume = value * .25f; PlayerPrefs.SetFloat("sauna.bgm", value); }
        public void SetEffects(float value) { Effects = value; effects.volume = value * .45f; PlayerPrefs.SetFloat("sauna.sfx", value); }
        public void Play(int kind = 0) => effects.PlayOneShot(kind == 1 ? success : kind == 2 ? fail : kind == 3 ? brush : bell);
        static AudioClip Tone(float hz, float seconds)
        {
            int count = (int)(22050 * seconds); var data = new float[count];
            for (int i = 0; i < count; i++) { float t = i / 22050f; data[i] = Mathf.Sin(t * hz * Mathf.PI * 2) * Mathf.Exp(-t * 15) * Mathf.Min(t * 150, 1); }
            var clip = AudioClip.Create("Sauna chime", count, 1, 22050, false); clip.SetData(data, 0); return clip;
        }
        static AudioClip Melody()
        {
            const int rate = 22050; float beat = .55f; int count = (int)(beat * 32 * rate); var data = new float[count];
            int[] notes = { 60, 64, 67, 71, 69, 67, 64, 62, 57, 60, 64, 67, 64, 62, 60, 55, 53, 57, 60, 64, 62, 60, 57, 55, 55, 59, 62, 65, 64, 62, 59, 55 };
            for (int n = 0; n < notes.Length; n++)
            {
                float hz = 440 * Mathf.Pow(2, (notes[n] - 69) / 12f); int start = (int)(n * beat * rate);
                for (int i = 0; i < beat * rate; i++)
                {
                    float t = i / (float)rate, envelope = Mathf.Min(t * 70, 1) * Mathf.Exp(-t * 6) * Mathf.Min((beat - t) * 30, 1);
                    data[start + i] = (Mathf.Sin(2 * Mathf.PI * hz * t) * .7f + Mathf.Sin(2 * Mathf.PI * hz * .5f * t) * .3f) * envelope * .35f;
                }
            }
            var clip = AudioClip.Create("Warm tiles · original loop", count, 1, rate, false); clip.SetData(data, 0); return clip;
        }
    }
}
