using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;

using CineGame.MobileComponents;

namespace CineGameEditor.MobileComponents {
    /// <summary>
    /// AHAP file importer. "1" represents the importer version; increment this if your format parsing logic changes later.
    /// </summary>
    [ScriptedImporter (1, "ahap")]
    public class AhapImporter : ScriptedImporter {
        public override void OnImportAsset (AssetImportContext ctx) {
            var ahapJson = File.ReadAllText (ctx.assetPath);

            var hapticAsset = ScriptableObject.CreateInstance<HapticPatternAsset> ();
            hapticAsset.Initialize (ahapJson);

            // Register the object inside the AssetImportContext so Unity saves it to the library
            ctx.AddObjectToAsset ("mainObj", hapticAsset);
            ctx.SetMainObject (hapticAsset);

            var iconTexture = EditorGUIUtility.IconContent ("TextAsset Icon").image as Texture2D;
            EditorGUIUtility.SetIconForObject (hapticAsset, iconTexture);
        }
    }
}
