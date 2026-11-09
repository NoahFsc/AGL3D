using System.Collections.Generic;
using AGL3D.Bridge;
using UnityEngine;

namespace AGL3D.LotB
{
    /// <summary>
    /// Joue un son court quand un constat est validé (tâche B7).
    /// Le contrat n'a pas de message « constat validé » : la page envoie <c>setPoints</c>
    /// juste après chaque validation. Le son part donc quand un repère apparaît ou change
    /// d'état par rapport au <c>setPoints</c> précédent.
    /// Un <c>setPoints</c> reçu dans la même image qu'un <c>init</c> (changement de mode,
    /// jeu de démo, démarrage) ne fait pas de bruit : la page envoie les deux d'un coup.
    /// Le son dépend de l'état : deux notes montantes pour Bon, une note pour Usage, deux
    /// notes descendantes pour Mauvais. Il est synthétisé au démarrage : pas de fichier audio.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class ValidationSound : MonoBehaviour
    {
        [Tooltip("Volume du son, de 0 à 1.")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.35f;

        private const float NoteDuration = 0.16f;

        private readonly Dictionary<string, string> previous = new Dictionary<string, string>();
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private AudioSource source;
        private WebBridge bridge;
        private int initFrame = -1;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            // La4 = 440 Hz. Notes d'un accord de la majeur, douces à l'oreille.
            clips["bon"] = CreateClip("ValidationBon", 659.26f, 880f);      // Mi5 puis La5
            clips["usage"] = CreateClip("ValidationUsage", 739.99f);        // Fa#5
            clips["mauvais"] = CreateClip("ValidationMauvais", 659.26f, 440f); // Mi5 puis La4
        }

        private void Start()
        {
            bridge = WebBridge.Instance;
            if (bridge == null)
            {
                Debug.LogWarning("[ValidationSound] Aucun WebBridge dans la scène : pas de son.", this);
                return;
            }
            bridge.InitReceived += OnInit;
            bridge.SetPointsReceived += OnSetPoints;
        }

        private void OnDestroy()
        {
            if (bridge == null) return;
            bridge.InitReceived -= OnInit;
            bridge.SetPointsReceived -= OnSetPoints;
        }

        private void OnInit(InitPayload payload)
        {
            initFrame = Time.frameCount;
        }

        private void OnSetPoints(SetPointsPayload payload)
        {
            var points = payload?.points ?? new PointData[0];
            string changed = null;
            foreach (var point in points)
            {
                if (point == null || string.IsNullOrEmpty(point.key)) continue;
                if (!previous.TryGetValue(point.key, out var etat) || etat != point.etat) changed = point.etat;
            }

            previous.Clear();
            foreach (var point in points)
            {
                if (point != null && !string.IsNullOrEmpty(point.key)) previous[point.key] = point.etat;
            }

            if (changed == null || Time.frameCount == initFrame) return;
            if (clips.TryGetValue(changed, out var clip)) source.PlayOneShot(clip, volume);
        }

        /// <summary>Une ou deux notes enchaînées, avec une attaque courte et une chute douce.</summary>
        private static AudioClip CreateClip(string clipName, params float[] notes)
        {
            int rate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 44100;
            int perNote = Mathf.RoundToInt(NoteDuration * rate);
            // La dernière note résonne un peu plus longtemps.
            int tail = perNote;
            var data = new float[perNote * notes.Length + tail];

            for (int n = 0; n < notes.Length; n++)
            {
                int start = n * perNote;
                int length = perNote + (n == notes.Length - 1 ? tail : perNote / 2);
                for (int i = 0; i < length && start + i < data.Length; i++)
                {
                    float t = i / (float)rate;
                    float attack = Mathf.Clamp01(t / 0.008f);
                    float envelope = attack * Mathf.Exp(-t * 9f);
                    // Fondamentale et une harmonique légère : un son de clochette discret.
                    float sample = Mathf.Sin(2f * Mathf.PI * notes[n] * t)
                                   + 0.25f * Mathf.Sin(4f * Mathf.PI * notes[n] * t);
                    data[start + i] += 0.6f * envelope * sample;
                }
            }

            var clip = AudioClip.Create(clipName, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
