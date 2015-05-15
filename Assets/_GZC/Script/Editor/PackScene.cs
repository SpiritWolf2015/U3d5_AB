using UnityEngine;
//引入命名空间
using UnityEditor;
using System.IO;

public class PackScene //: AssetPostprocessor 
{
    /// <summary>
    /// 定义场景产生预设的路径
    /// </summary>
    static string prefabsPath = "Assets/GzcPrefabs/";

    [MenuItem("AssetBundles/打包场景，产生Prefab")]
    static void Excute ( ) {
        if (!Directory.Exists(prefabsPath)) {
            Directory.CreateDirectory(prefabsPath);
        }
        //循环遍历产生预设。
        foreach (GameObject o in GameObject.FindObjectsOfType<GameObject>( )) {
            // 自定义你自己的逻辑。例如：设置过滤选项等。还可以在此进行产生预设的同时产生config，这里就留给读者自己来完成吧。
            string prefabName = o.name + ".prefab";
            PrefabUtility.CreatePrefab(prefabsPath + prefabName, o, ReplacePrefabOptions.ConnectToPrefab);
            AssetImporter.GetAtPath(prefabsPath + prefabName).assetBundleName = o.name + "_prefab.unity3d";
        }
    }

}