using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AGL3D.LotA.Editor
{
    /// <summary>
    /// Supprime du Furniture Mega Pack tout ce que la maquette n'utilise pas. Le pack compte
    /// 511 prefabs et environ 1,7 Go de textures en 2048 px : importé en entier, il ralentit
    /// beaucoup l'ouverture de l'Editor et le build, alors qu'on n'en utilise qu'une douzaine
    /// de modèles.
    ///
    /// Le pack est ignoré par Git (licence de l'Asset Store) : la suppression ne voyage pas.
    /// Chacun lance donc ce menu une fois après avoir importé le pack (voir le README,
    /// « Packs de meubles »). Les fichiers gardés conservent leur .meta, donc leur GUID : les
    /// prefabs de Lot-A qui les référencent restent valides.
    ///
    /// Ajouter un modèle du pack aux générateurs : l'ajouter à <see cref="ModelesUtilises"/>,
    /// sinon ce menu le supprimerait (il refuse de tourner si un modèle de la liste manque, et
    /// l'outil ne voit pas les modèles absents de la liste).
    /// </summary>
    public static class AllegerMegaPack
    {
        private const string Dossier = "Assets/Furniture Mega Pack";
        private const string Prefabs = Dossier + "/Prefabs";

        /// <summary>Modèles du pack instanciés par les générateurs ElementsBuilder*.cs.</summary>
        public static readonly string[] ModelesUtilises =
        {
            "Sofas/Sofa17",            // salon : canapé
            "Beds/Bed02",              // chambre : lit
            "Drawers/Drawer06",        // chambre : tables de chevet
            "Kitchen/CabinetA_Sink",   // cuisine : meuble évier
            "Kitchen/Sink03",          // cuisine : bac et robinet
            "Kitchen/CabinetA03",      // cuisine : meuble bas à tiroirs
            "Kitchen/CabinetA04",      // cuisine : meuble bas à porte
            "Kitchen/GasStove04",      // cuisine : plaque
            "Kitchen/MicrowaveOven04", // cuisine : micro-ondes
            "Kitchen/Refrigerator07",  // cuisine : réfrigérateur
            "Tables/Table02",          // cuisine : table
            "Chairs/Chair01",          // cuisine : chaises
        };

        [MenuItem("AGL3D/Lot A/Alléger le Furniture Mega Pack")]
        public static void Menu()
        {
            if (!Preparer(out var gardes, out var superflus)) return;
            if (superflus.Count == 0)
            {
                EditorUtility.DisplayDialog("Furniture Mega Pack", "Le pack est déjà allégé : rien à supprimer.", "OK");
                return;
            }
            bool ok = EditorUtility.DisplayDialog("Alléger le Furniture Mega Pack",
                $"{superflus.Count} fichiers inutilisés seront supprimés du pack " +
                $"({gardes.Count} gardés : les {ModelesUtilises.Length} modèles utilisés et leurs " +
                "FBX, matériaux et textures).\n\nPour les retrouver, il faudra réimporter le pack.",
                "Supprimer", "Annuler");
            if (ok) Supprimer(superflus);
        }

        /// <summary>Version sans confirmation, pour la ligne de commande (-executeMethod).</summary>
        public static void SansConfirmation()
        {
            if (!Preparer(out _, out var superflus))
            {
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            Supprimer(superflus);
        }

        /// <summary>Calcule les fichiers à garder (modèles utilisés et leurs dépendances) et les autres.</summary>
        private static bool Preparer(out HashSet<string> gardes, out List<string> superflus)
        {
            gardes = new HashSet<string>();
            superflus = new List<string>();
            if (!AssetDatabase.IsValidFolder(Dossier))
            {
                Debug.LogWarning($"[AllegerMegaPack] {Dossier} est absent : importez d'abord le pack (README, « Packs de meubles »).");
                return false;
            }

            var chemins = ModelesUtilises.Select(m => $"{Prefabs}/{m}.prefab").ToArray();
            var manquants = chemins.Where(c => AssetDatabase.LoadMainAssetAtPath(c) == null).ToList();
            if (manquants.Count > 0)
            {
                // Pack incomplet : supprimer le reste pourrait faire disparaître des dépendances.
                Debug.LogError("[AllegerMegaPack] Modèles introuvables, rien n'est supprimé :\n" + string.Join("\n", manquants));
                return false;
            }

            foreach (var dep in AssetDatabase.GetDependencies(chemins, true))
                if (dep.StartsWith(Dossier + "/")) gardes.Add(dep);

            foreach (var fichier in Directory.GetFiles(Dossier, "*", SearchOption.AllDirectories))
            {
                if (fichier.EndsWith(".meta")) continue;
                var chemin = fichier.Replace('\\', '/');
                if (!gardes.Contains(chemin)) superflus.Add(chemin);
            }
            Debug.Log($"[AllegerMegaPack] {gardes.Count} fichiers gardés :\n" + string.Join("\n", gardes.OrderBy(g => g)));
            return true;
        }

        private static void Supprimer(List<string> superflus)
        {
            var echecs = new List<string>();
            AssetDatabase.DeleteAssets(superflus.ToArray(), echecs);

            // Dossiers vidés (démos, catégories de meubles inutilisées), des plus profonds aux plus hauts.
            int dossiers = 0;
            foreach (var d in Directory.GetDirectories(Dossier, "*", SearchOption.AllDirectories)
                         .Select(d => d.Replace('\\', '/')).OrderByDescending(d => d.Length))
            {
                if (Directory.EnumerateFiles(d, "*", SearchOption.AllDirectories).Any(f => !f.EndsWith(".meta"))) continue;
                if (AssetDatabase.DeleteAsset(d)) dossiers++;
            }
            AssetDatabase.Refresh();

            Debug.Log($"[AllegerMegaPack] {superflus.Count - echecs.Count} fichiers et {dossiers} dossiers supprimés.");
            if (echecs.Count > 0)
                Debug.LogError("[AllegerMegaPack] Non supprimés :\n" + string.Join("\n", echecs));
        }
    }
}
