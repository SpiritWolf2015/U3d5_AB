using UnityEngine;
using UnityEditor;
using System.Collections;

public class AssetbundlesMenuItems {    

    [MenuItem("AssetBundles/Build AssetBundles")]
    static public void BuildAssetBundles ( ) {
        BuildScript.BuildAssetBundles( );
    }
}
