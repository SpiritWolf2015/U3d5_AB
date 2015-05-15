using UnityEngine;
using System.Collections;
using UnityEditor;


public class CrashyAssetImporter : AssetPostprocessor {

    void OnPreprocessAudio ( ) {
        Debug.Log("assetBundleName = [" + assetImporter.assetBundleName + "]");
        if (assetImporter.assetBundleName != "foo") {
            assetImporter.assetBundleName = "foo";
        }        
    }

    static void OnPostprocessAllAssets (string[ ] importedAssets, string[ ] deletedAssets, string[ ] movedAssets, string[ ] movedFromAssetPaths) {
        foreach (var str in importedAssets) {
            Debug.Log("Reimported Asset: " + str);
          
        }

        foreach (var str in deletedAssets) {
            Debug.Log("Deleted Asset: " + str);
        }

        for (int i = 0; i < movedAssets.Length; i++) {
            Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
        }
         
    }

    void OnPostprocessAssetbundleNameChanged (string path, string previous, string next) {
        Debug.Log("AB: " + path + " old: " + previous + " new: " + next);
    }

    [MenuItem("AssetBundles/Get Asset Bundle names")]
    static void GetNames ( ) {
        string[] names = AssetDatabase.GetAllAssetBundleNames( );
        foreach (string name in names) {
            Debug.Log("Asset Bundle: " + name);
         
        }
    }

}
