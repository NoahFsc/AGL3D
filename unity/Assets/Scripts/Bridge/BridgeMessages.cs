using System;

namespace AGL3D.Bridge
{
    // Payloads du contrat Vue <-> Unity (docs/contrat-messages.md).
    // Miroir de web/src/types/bridge.ts : garder les deux synchronisés.
    // Sérialisés avec JsonUtility : champs publics, noms identiques au JSON.

    /// <summary>Enveloppe lue en premier pour connaître le type du message.</summary>
    [Serializable]
    internal class MessageHeader
    {
        public string type;
    }

    /// <summary>Enveloppe typée : <c>{ "type": "...", "payload": { ... } }</c>.</summary>
    [Serializable]
    internal class Message<T>
    {
        public string type;
        public T payload;
    }

    /* ----------------------------- Vue -> Unity ----------------------------- */

    [Serializable]
    public class InitPayload
    {
        public string dossierId;
        /// <summary>"accueil" | "entree" | "sortie" | "comparer"</summary>
        public string mode;
    }

    /// <summary>Repère affiché sur un élément noté.</summary>
    [Serializable]
    public class PointData
    {
        public string key;
        public string label;
        /// <summary>"bon" (vert) | "usage" (orange) | "mauvais" (rouge)</summary>
        public string etat;
        /// <summary>En mode Comparer, l'élément passe progressivement au rouge.</summary>
        public bool degrade;
    }

    [Serializable]
    public class SetPointsPayload
    {
        public PointData[] points;
    }

    [Serializable]
    public class FocusRoomPayload
    {
        /// <summary>Clé de pièce ("salon"…) ou "overview".</summary>
        public string room;
    }

    [Serializable]
    public class FocusElementPayload
    {
        public string key;
    }

    [Serializable]
    public class SelectPayload
    {
        /// <summary>Clé d'élément, ou vide/null pour désélectionner.</summary>
        public string key;
    }

    /* ----------------------------- Unity -> Vue ----------------------------- */

    [Serializable]
    public class ReadyPayload
    {
        public string[] keys;
    }

    [Serializable]
    public class PointSelectedPayload
    {
        public string key;
        /// <summary>Position normalisée [0..1] dans le canvas, origine en haut à gauche.</summary>
        public float x;
        public float y;
    }

    /// <summary>Clic dans le vide : la page ferme la fiche. Sérialisé en <c>{}</c>.</summary>
    [Serializable]
    public class DeselectedPayload
    {
    }

    [Serializable]
    public class RoomChangedPayload
    {
        public string room;
    }
}
