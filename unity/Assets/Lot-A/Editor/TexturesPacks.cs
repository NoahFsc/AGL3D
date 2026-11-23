using UnityEditor;

namespace AGL3D.LotA.Editor
{
    /// <summary>
    /// Limite la taille des textures du Furniture Mega Pack dans le build Web. Chaque modèle du
    /// pack a ses propres textures en 2048 px (couleur, relief, brillance) : une douzaine de
    /// meubles suffisaient à faire passer le build de 10 à près de 50 Mo. Vus de loin dans la
    /// maquette, 512 px suffisent.
    ///
    /// Le pack est ignoré par Git (voir le README, « Packs de meubles ») : ses réglages d'import
    /// ne voyagent pas d'une machine à l'autre. Ce script, lui, est versionné, et s'applique à
    /// chaque import. Changer <see cref="GetVersion"/> fait réimporter les textures du pack.
    /// </summary>
    public class TexturesPacks : AssetPostprocessor
    {
        private const string Dossier = "Assets/Furniture Mega Pack/";
        private const int TailleMaxWeb = 512;

        public override uint GetVersion() => 1;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Dossier)) return;
            var importer = (TextureImporter)assetImporter;
            var web = importer.GetPlatformTextureSettings("WebGL");
            web.overridden = true;
            web.maxTextureSize = TailleMaxWeb;
            web.format = TextureImporterFormat.Automatic;
            importer.SetPlatformTextureSettings(web);
        }
    }
}
