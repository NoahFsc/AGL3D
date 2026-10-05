using System;
using System.Linq;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace AGL3D.Bridge
{
    /// <summary>
    /// Point d'entrée unique des messages Vue -> Unity et de sortie Unity -> Vue.
    /// Doit être posé sur un GameObject nommé exactement "WebBridge" dans la scène
    /// principale : Vue appelle <c>SendMessage("WebBridge", "Receive", json)</c>.
    /// Les autres systèmes (caméra, marqueurs, picking) s'abonnent aux événements C#
    /// ci-dessous plutôt que de parser du JSON eux-mêmes.
    /// </summary>
    [DisallowMultipleComponent]
    public class WebBridge : MonoBehaviour
    {
        public const string GameObjectName = "WebBridge";

        public static WebBridge Instance { get; private set; }

        public event Action<InitPayload> InitReceived;
        public event Action<SetPointsPayload> SetPointsReceived;
        public event Action<FocusRoomPayload> FocusRoomReceived;
        public event Action<SelectPayload> SelectReceived;
        public event Action<SetContrastPayload> SetContrastReceived;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void AGL3D_Emit(string type, string json);
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[WebBridge] Plusieurs WebBridge dans la scène, celui-ci est désactivé.", this);
                enabled = false;
                return;
            }
            Instance = this;

            if (gameObject.name != GameObjectName)
            {
                Debug.LogWarning($"[WebBridge] GameObject renommé en \"{GameObjectName}\" (requis par SendMessage).", this);
                gameObject.name = GameObjectName;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // Laisse le clavier aux champs de la page Vue (recherche, formulaires…).
            WebGLInput.captureAllKeyboardInput = false;
#endif
        }

        private void Start()
        {
            // Les InspectableElement s'enregistrent dans leur OnEnable, donc avant Start.
            Emit("ready", new ReadyPayload { keys = InspectableElement.All.Select(e => e.Key).ToArray() });
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Appelé par Vue via SendMessage. Reçoit <c>{ "type": "...", "payload": { ... } }</c>.</summary>
        public void Receive(string json)
        {
            MessageHeader header;
            try
            {
                header = JsonUtility.FromJson<MessageHeader>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebBridge] JSON invalide : {json}\n{e}");
                return;
            }

            switch (header?.type)
            {
                case "init": Dispatch(json, InitReceived); break;
                case "setPoints": Dispatch(json, SetPointsReceived); break;
                case "focusRoom": Dispatch(json, FocusRoomReceived); break;
                case "select": Dispatch(json, SelectReceived); break;
                case "setContrast": Dispatch(json, SetContrastReceived); break;
                default:
                    Debug.LogWarning($"[WebBridge] Type de message inconnu : \"{header?.type}\"");
                    break;
            }
        }

        private static void Dispatch<T>(string json, Action<T> handler)
        {
            var message = JsonUtility.FromJson<Message<T>>(json);
            if (handler == null)
            {
                Debug.LogWarning($"[WebBridge] Aucun abonné pour \"{message.type}\".");
                return;
            }
            handler(message.payload);
        }

        /* ----------------------------- Unity -> Vue ----------------------------- */

        /// <summary>Envoie un message à la page. No-op (log) dans l'Éditeur.</summary>
        public static void Emit(string type, object payload)
        {
            var json = payload != null ? JsonUtility.ToJson(payload) : string.Empty;
#if UNITY_WEBGL && !UNITY_EDITOR
            AGL3D_Emit(type, json);
#else
            Debug.Log($"[WebBridge] Emit {type} {json}");
#endif
        }

        /// <summary>Raccourci pour le picking : convertit une position écran Unity en coordonnées normalisées du contrat.</summary>
        public static void EmitPointSelected(string key, Vector2 screenPosition)
        {
            Emit("pointSelected", new PointSelectedPayload
            {
                key = key,
                x = Mathf.Clamp01(screenPosition.x / Screen.width),
                // Unity : origine en bas à gauche ; contrat : en haut à gauche.
                y = Mathf.Clamp01(1f - screenPosition.y / Screen.height),
            });
        }

        public static void EmitRoomChanged(string room)
        {
            Emit("roomChanged", new RoomChangedPayload { room = room });
        }
    }
}
