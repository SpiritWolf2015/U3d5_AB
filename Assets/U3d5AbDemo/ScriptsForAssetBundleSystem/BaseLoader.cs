using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BaseLoader : MonoBehaviour {

    const string kAssetBundlesPath = "/AssetBundles/";

    // Use this for initialization.
    IEnumerator Start ( ) {
        yield return StartCoroutine(Initialize( ));
    }

    // Initialize the downloading url and AssetBundleManifest object.
    protected IEnumerator Initialize ( ) {
        // Don't destroy the game object as we base on it to run the loading script.
        DontDestroyOnLoad(gameObject);

        string platformFolderForAssetBundles =
#if UNITY_EDITOR
        Util.GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget);
#else
		Util.GetPlatformFolderForAssetBundles(Application.platform);
#endif

        string relativePath = Util.GetRelativePath( );
        // Set base downloading url.
        AssetBundleManager.BaseDownloadingURL = relativePath + kAssetBundlesPath + platformFolderForAssetBundles + "/";

        // Initialize AssetBundleManifest which loads the AssetBundleManifest object.
        // 初始化，根据AssetBundleManifest配置文件载入
        AbsClsAssetBundleLoadAssetOperation request = AssetBundleManager.Initialize(platformFolderForAssetBundles);
        if (request != null)
            yield return StartCoroutine(request);
    }

    protected IEnumerator Load (string assetBundleName, string assetName) {
        Debug.Log(string.Format("在第{0}帧开始载入assetBundle， assetBundleName = {1}", Time.frameCount, assetBundleName));

        // Load asset from assetBundle.
        AbsClsAssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(assetBundleName, assetName, typeof(GameObject));
        if (request == null)
            yield break;
        yield return StartCoroutine(request);

        // Get the asset.
        GameObject prefab = request.GetAsset<GameObject>( );
        if (null == prefab) {
            DebugConsole.LogError(string.Format("在第{0}帧，assetName={1}载入失败", Time.frameCount, prefab.name));
            Debug.LogError(string.Format("在第{0}帧，assetName={1}载入失败", Time.frameCount, prefab.name));
        } else {
            DebugConsole.Log(string.Format("在第{0}帧，assetName={1}载入成功", Time.frameCount, prefab.name));
            Debug.Log(string.Format("在第{0}帧，assetName={1}载入成功", Time.frameCount, prefab.name));
        }        

        if (prefab != null)
            GameObject.Instantiate(prefab);
    }

    protected IEnumerator LoadLevel (string assetBundleName, string levelName, bool isAdditive) {        
        Debug.Log(string.Format("在第{0}帧，开始载入场景 {1}", Time.frameCount, levelName));    

        // Load level from assetBundle.
        AbsClsAssetBundleLoadOperation request = AssetBundleManager.LoadLevelAsync(assetBundleName, levelName, isAdditive);
        if (request == null)
            yield break;
        yield return StartCoroutine(request);

        // This log will only be output when loading level additively.
        Debug.Log(string.Format("在第{0}帧，载入场景 {1} 成功", Time.frameCount, levelName));        
    }

}
