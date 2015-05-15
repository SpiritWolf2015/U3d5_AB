using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
    In this demo, we demonstrate:
    1.	Automatic asset bundle dependency resolving & loading.
        It shows how to use the manifest assetbundle like how to get the dependencies etc.
    2.	Automatic unloading of asset bundles (When an asset bundle or a dependency thereof is no longer needed, the asset bundle is unloaded)
    3.	Editor simulation. A bool defines if we load asset bundles from the project or are actually using asset bundles(doesn't work with assetbundle variants for now.)
        With this, you can player in editor mode without actually building the assetBundles.
    4.	Optional setup where to download all asset bundles
    5.	Build pipeline build postprocessor, integration so that building a player builds the asset bundles and puts them into the player data (Default implmenetation for loading assetbundles from disk on any platform)
    6.	Use WWW.LoadFromCacheOrDownload and feed 128 bit hash to it when downloading via web
        You can get the hash from the manifest assetbundle.
    7.	AssetBundle variants. A prioritized list of variants that should be used if the asset bundle with that variant exists, first variant in the list is the most preferred etc.

    在这个演示中,我们演示:
    1。自动资产包依赖解析和加载。
         它展示了如何使用manifest assetbundle如何依赖关系等。
    2。自动卸载资产包(当一个资产包或依赖不再需要,资产包卸载)
    3。编辑器模拟。保龄球定义如果我们负载资产包项目或实际使用资产包(不使用assetbundle变体。)
         ,你可以在编辑模式下玩家没有实际构建assetBundles。
    4。可选的设置在哪里下载所有资产包
    5。构建管道构建后处理程序,集成,构建一个玩家构建资产包并将它们放置到玩家数据(默认implmenetation从磁盘加载assetbundles在任何平台上)
    6。使用WWW。LoadFromCacheOrDownload和饲料128位散列通过网络下载的时候
         您可以从清单assetbundle得到的散列。
    7。AssetBundle变体。的优先列表应该使用变体,如果资产包变体的存在,第一变体是最优先列表等等。
*/





/// <summary>
/// 已经载入到内存中的AssetBundle，搞了个引用计数，来释放AssetBundle
/// </summary> 
public class LoadedAssetBundle {

    public AssetBundle m_AssetBundle;
    public int m_ReferencedCount;

    public LoadedAssetBundle (AssetBundle assetBundle) {
        m_AssetBundle = assetBundle;
        m_ReferencedCount = 1;
    }
}

// Class takes care of loading assetBundle and its dependencies automatically, loading variants automatically.
// 加载assetBundle自动加载及其依赖项,自动加载变体。
public class AssetBundleManager : MonoBehaviour 
{

    static string m_BaseDownloadingURL = null;
    static string[ ] m_Variants = { };
    static AssetBundleManifest m_AssetBundleManifest = null;

    /// <summary>
    /// 已经载入了的AssetBundle
    /// </summary>
    static Dictionary<string, LoadedAssetBundle> m_LoadedAssetBundles = new Dictionary<string, LoadedAssetBundle>( );
    static Dictionary<string, WWW> m_DownloadingWWWs = new Dictionary<string, WWW>( );
    static Dictionary<string, string> m_DownloadingErrors = new Dictionary<string, string>( );
    static List<AbsClsAssetBundleLoadOperation> m_InProgressOperations = new List<AbsClsAssetBundleLoadOperation>( );
    static Dictionary<string, string[ ]> m_Dependencies = new Dictionary<string, string[ ]>( );

    #region 公有属性

    // The base downloading url which is used to generate the full downloading url with the assetBundle names.
    public static string BaseDownloadingURL {
        get { return m_BaseDownloadingURL; }
        set { m_BaseDownloadingURL = value; }
    }

    // Variants which is used to define the active variants.
    public static string[ ] Variants {
        get { return m_Variants; }
        set { m_Variants = value; }
    }

    // AssetBundleManifest object which can be used to load the dependecies and check suitable assetBundle variants.
    public static AssetBundleManifest AssetBundleManifestObject {
        set { m_AssetBundleManifest = value; }
    } 

    #endregion 公有属性

    static List<string> keysToRemove = new List<string>( );
    void Update ( ) {
        keysToRemove.Clear( );

        // Collect all the finished WWWs.
        foreach (KeyValuePair<string, WWW> keyValue in m_DownloadingWWWs) {
            WWW download = keyValue.Value;

            // If downloading fails.
            if (!string.IsNullOrEmpty(download.error)) {
                m_DownloadingErrors.Add(keyValue.Key, download.error);
                keysToRemove.Add(keyValue.Key);
                continue;
            }

            // If downloading succeeds.
            if (download.isDone) {
                Debug.Log(string.Format("在第{0}帧完成了3W, {1}的下载", Time.frameCount, keyValue.Key));
                m_LoadedAssetBundles.Add(keyValue.Key, new LoadedAssetBundle(download.assetBundle));
                keysToRemove.Add(keyValue.Key);
            }
        }

        // Remove the finished WWWs.
        foreach (string key in keysToRemove) {
            WWW download = m_DownloadingWWWs[key];
            m_DownloadingWWWs.Remove(key);
            download.Dispose( );
            download = null;
        }

        // Update all in progress operations
        for (int i = 0; i < m_InProgressOperations.Count; ) {
            if (!m_InProgressOperations[i].IsUpdate( )) {
                m_InProgressOperations.RemoveAt(i);
            } else {
                i++;
            }
        }
    }


    /// <summary>
    /// 初始化，Load AssetBundleManifest.
    /// </summary>    
    static public AssetBundleLoadManifestOperation Initialize (string manifestAssetBundleName) {
        GameObject go = new GameObject("AssetBundleManager", typeof(AssetBundleManager));
        DontDestroyOnLoad(go);

        LoadAssetBundle(manifestAssetBundleName, true);
        AssetBundleLoadManifestOperation operation = new AssetBundleLoadManifestOperation(manifestAssetBundleName, "AssetBundleManifest", typeof(AssetBundleManifest));
        m_InProgressOperations.Add(operation);
        return operation;
    }

    /// <summary>
    /// Get loaded AssetBundle, only return vaild object when all the dependencies are downloaded successfully.
    /// 得到已经载入的了AssetBundle，只返回其依赖项全载入成功的AssetBundle，如出错则返回null
    /// </summary>   
    static public LoadedAssetBundle GetLoadedAssetBundle (string assetBundleName, out string error) {
        if (m_DownloadingErrors.TryGetValue(assetBundleName, out error))
            return null;
        
        LoadedAssetBundle bundle = null;
        m_LoadedAssetBundles.TryGetValue(assetBundleName, out bundle);
        if (bundle == null)
            return null;

        // No dependencies are recorded, only the bundle itself is required.
        // 这个assetBundle，没依赖的assetBundle，直接返回这个assetBundle
        string[ ] dependencies = null;
        if (!m_Dependencies.TryGetValue(assetBundleName, out dependencies))
            return bundle;

        // Make sure all dependencies are loaded
        // 载入所有依赖的assetBundle
        foreach (string dependency in dependencies) {
            if (m_DownloadingErrors.TryGetValue(assetBundleName, out error))
                return bundle;

            // Wait all the dependent assetBundles being loaded.
            LoadedAssetBundle dependentBundle = null;
            m_LoadedAssetBundles.TryGetValue(dependency, out dependentBundle);
            if (dependentBundle == null)
                return null;
        }

        return bundle;
    }

    #region Load AssetBundle

    /// <summary>
    /// Load AssetBundle and its dependencies.
    /// 载入AssetBundle及其依赖项
    /// </summary>
    /// <param name="assetBundleName"></param>
    /// <param name="isLoadingAssetBundleManifest"></param> 
    static protected void LoadAssetBundle (string assetBundleName, bool isLoadingAssetBundleManifest = false) {
        if (!isLoadingAssetBundleManifest) {
            assetBundleName = RemapVariantName(assetBundleName);
        }           

        // Check if the assetBundle has already been processed.
        // 是否已经在载入了
        bool isAlreadyProcessed = LoadAssetBundleInternal(assetBundleName, isLoadingAssetBundleManifest);

        // Load dependencies.
        // 载入依赖项
        if (!isAlreadyProcessed && !isLoadingAssetBundleManifest) {
            LoadDependencies(assetBundleName);
        }            
    }
      
    // Where we actual call WWW to download the assetBundle.
    // 调用WWW下载assetBundle
    static protected bool LoadAssetBundleInternal (string assetBundleName, bool isLoadingAssetBundleManifest) {
        // Already loaded.
        LoadedAssetBundle bundle = null;
        m_LoadedAssetBundles.TryGetValue(assetBundleName, out bundle);
        if (bundle != null) {
            bundle.m_ReferencedCount++;
            return true;
        }

        // @TODO: Do we need to consider the referenced count of WWWs?
        // In the demo, we never have duplicate WWWs as we wait LoadAssetAsync()/LoadLevelAsync() to be finished before calling another LoadAssetAsync()/LoadLevelAsync().
        // But in the real case, users can call LoadAssetAsync()/LoadLevelAsync() several times then wait them to be finished which might have duplicate WWWs.
        if (m_DownloadingWWWs.ContainsKey(assetBundleName))
            return true;

        WWW download = null;
        string url = m_BaseDownloadingURL + assetBundleName;

        // For manifest assetbundle, always download it as we don't have hash for it.
        if (isLoadingAssetBundleManifest){
            download = new WWW(url);
            Debug.Log(string.Format("new一个W3，URL={0}", url));
        }else {
            download = WWW.LoadFromCacheOrDownload(url, m_AssetBundleManifest.GetAssetBundleHash(assetBundleName), 0);
            Debug.Log(string.Format("W3 LoadFromCacheOrDownload，URL={0}, HASH={1}", url, m_AssetBundleManifest.GetAssetBundleHash(assetBundleName)));
        }            

        m_DownloadingWWWs.Add(assetBundleName, download);

        return false;
    }

    // Where we get all the dependencies and load them all.
    // 载入依赖的assetBundle
    static protected void LoadDependencies (string assetBundleName) {
        if (m_AssetBundleManifest == null) {
            Debug.LogError("Please initialize AssetBundleManifest by calling AssetBundleManager.Initialize()");
            return;
        }

        // Get dependecies from the AssetBundleManifest object..
        string[ ] dependencies = m_AssetBundleManifest.GetAllDependencies(assetBundleName);
        if (dependencies.Length == 0)
            return;

        for (int i = 0; i < dependencies.Length; i++)
            dependencies[i] = RemapVariantName(dependencies[i]);

        // Record and load all dependencies.
        m_Dependencies.Add(assetBundleName, dependencies);
        for (int i = 0; i < dependencies.Length; i++)
            LoadAssetBundleInternal(dependencies[i], false);
    }
    
    #endregion Load AssetBundle

    // Remaps the asset bundle name to the best fitting asset bundle variant.
    static protected string RemapVariantName (string assetBundleName) {
        string[ ] bundlesWithVariant = m_AssetBundleManifest.GetAllAssetBundlesWithVariant( );

        // If the asset bundle doesn't have variant, simply return.
        if (System.Array.IndexOf(bundlesWithVariant, assetBundleName) < 0)
            return assetBundleName;

        string[ ] split = assetBundleName.Split('.');

        int bestFit = int.MaxValue;
        int bestFitIndex = -1;
        // Loop all the assetBundles with variant to find the best fit variant assetBundle.
        for (int i = 0; i < bundlesWithVariant.Length; i++) {
            string[ ] curSplit = bundlesWithVariant[i].Split('.');
            if (curSplit[0] != split[0])
                continue;

            int found = System.Array.IndexOf(m_Variants, curSplit[1]);
            if (found != -1 && found < bestFit) {
                bestFit = found;
                bestFitIndex = i;
            }
        }

        if (bestFitIndex != -1)
            return bundlesWithVariant[bestFitIndex];
        else
            return assetBundleName;
    }

    #region UnloadAssetBundle

    /// <summary>
    /// Unload assetbundle and its dependencies.
    /// </summary>
    /// <param name="assetBundleName"></param> 
    static public void UnloadAssetBundle (string assetBundleName) {
        Debug.Log(string.Format("卸载assetBundleName={0}前, 还有{1}个assetBundle在内存中", assetBundleName, m_LoadedAssetBundles.Count));

        UnloadAssetBundleInternal(assetBundleName);
        UnloadDependencies(assetBundleName);

        Debug.Log(string.Format("卸载assetBundleName={0}后, 还有{1}个assetBundle在内存中", assetBundleName, m_LoadedAssetBundles.Count));
        foreach (var t in  m_LoadedAssetBundles.Keys) {
            Debug.Log(string.Format("assetBundleName={0}还在内存中", t));
        }
    }

    static protected void UnloadDependencies (string assetBundleName) {
        string[ ] dependencies = null;
        if (!m_Dependencies.TryGetValue(assetBundleName, out dependencies))
            return;

        // Loop dependencies.
        foreach (string dependency in dependencies) {
            UnloadAssetBundleInternal(dependency);
        }

        m_Dependencies.Remove(assetBundleName);
    }

    static protected void UnloadAssetBundleInternal (string assetBundleName) {
        string error;
        LoadedAssetBundle bundle = GetLoadedAssetBundle(assetBundleName, out error);
        if (bundle == null)
            return;

        if (--bundle.m_ReferencedCount == 0) {
            bundle.m_AssetBundle.Unload(false);
            m_LoadedAssetBundles.Remove(assetBundleName);
            Debug.Log("assetBundleName= " + assetBundleName + " , has been unloaded successfully");
        }
    }

    #endregion UnloadAssetBundle

    /// <summary>
    /// 载入资源 from the given assetBundle.
    /// </summary>
    /// <param name="assetBundleName"></param>
    /// <param name="assetName"></param>
    /// <param name="type"></param>
    /// <returns></returns> 
    static public AbsClsAssetBundleLoadAssetOperation LoadAssetAsync (string assetBundleName, string assetName, System.Type type) {
        LoadAssetBundle(assetBundleName);
        AbsClsAssetBundleLoadAssetOperation operation = new AssetBundleLoadAssetOperationFull(assetBundleName, assetName, type);
        m_InProgressOperations.Add(operation);
        return operation;
    }

    /// <summary>
    /// 载入场景 from the given assetBundle.
    /// </summary>
    /// <param name="assetBundleName"></param>
    /// <param name="levelName"></param>
    /// <param name="isAdditive">是增量添加场景</param>
    /// <returns></returns> 
    static public AbsClsAssetBundleLoadOperation LoadLevelAsync (string assetBundleName, string levelName, bool isAdditive) {
        LoadAssetBundle(assetBundleName);
        AbsClsAssetBundleLoadOperation operation = new AssetBundleLoadLevelOperation(assetBundleName, levelName, isAdditive);
        m_InProgressOperations.Add(operation);
        return operation;
    }

} 