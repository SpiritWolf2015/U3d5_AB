using UnityEngine;
using System.Collections;

// 实现IEnumerator接口，.NET的才能用foreach，U3D才能用协同函数的yield return， StartCoroutine
public abstract class AbsClsAssetBundleLoadOperation : IEnumerator {

    #region 实现IEnumerator接口

    public object Current { get { return null; } }
    public bool MoveNext ( ) { return !IsDone( ); }
    public void Reset ( ) { }
    #endregion 实现IEnumerator接口

    abstract public bool IsUpdate ( );
    abstract public bool IsDone ( );
}

public class AssetBundleLoadLevelOperation : AbsClsAssetBundleLoadOperation {

    protected string m_AssetBundleName;
    protected string m_LevelName;
    protected bool m_IsAdditive;
    protected string m_DownloadingError;
    protected AsyncOperation m_Request;

    public AssetBundleLoadLevelOperation (string assetbundleName, string levelName, bool isAdditive) {
        m_AssetBundleName = assetbundleName;
        m_LevelName = levelName;
        m_IsAdditive = isAdditive;
    }

    public override bool IsUpdate ( ) {
        if (m_Request != null)
            return false;

        LoadedAssetBundle bundle = AssetBundleManager.GetLoadedAssetBundle(m_AssetBundleName, out m_DownloadingError);
        if (bundle != null) {
            if (m_IsAdditive)
                m_Request = Application.LoadLevelAdditiveAsync(m_LevelName);
            else
                m_Request = Application.LoadLevelAsync(m_LevelName);
            return false;
        } else
            return true;
    }

    public override bool IsDone ( ) {
        // Return if meeting downloading error.
        // m_DownloadingError might come from the dependency downloading.
        if (m_Request == null && m_DownloadingError != null) {
            Debug.LogError(m_DownloadingError);
            return true;
        }

        return m_Request != null && m_Request.isDone;
    }
}

public abstract class AbsClsAssetBundleLoadAssetOperation : AbsClsAssetBundleLoadOperation {
    public abstract TU3dObj GetAsset<TU3dObj> ( ) where TU3dObj : UnityEngine.Object;
}

public class AssetBundleLoadAssetOperationFull : AbsClsAssetBundleLoadAssetOperation {

    protected string m_AssetBundleName;
    protected string m_AssetName;
    protected string m_DownloadingError;
    protected System.Type m_Type;
    protected AssetBundleRequest m_Request = null;

    public AssetBundleLoadAssetOperationFull (string bundleName, string assetName, System.Type type) {
        m_AssetBundleName = bundleName;
        m_AssetName = assetName;
        m_Type = type;
    }

    public override TU3dObj GetAsset<TU3dObj> ( ) {
        if (m_Request != null && m_Request.isDone)
            return m_Request.asset as TU3dObj;
        else
            return null;
    }

    // Returns true if more Update calls are required.
    public override bool IsUpdate ( ) {
        if (m_Request != null)
            return false;

        LoadedAssetBundle bundle = AssetBundleManager.GetLoadedAssetBundle(m_AssetBundleName, out m_DownloadingError);
        if (bundle != null) {
            m_Request = bundle.m_AssetBundle.LoadAssetAsync(m_AssetName, m_Type);
            return false;
        } else {
            return true;
        }
    }

    public override bool IsDone ( ) {
        // Return if meeting downloading error.
        // m_DownloadingError might come from the dependency downloading.
        if (m_Request == null && m_DownloadingError != null) {
            Debug.LogError(m_DownloadingError);
            return true;
        }

        return m_Request != null && m_Request.isDone;
    }
}

public class AssetBundleLoadManifestOperation : AssetBundleLoadAssetOperationFull {
    public AssetBundleLoadManifestOperation (string bundleName, string assetName, System.Type type)
        : base(bundleName, assetName, type) {
    }

    public override bool IsUpdate ( ) {
        base.IsUpdate( );

        if (m_Request != null && m_Request.isDone) {
            AssetBundleManager.AssetBundleManifestObject = GetAsset<AssetBundleManifest>( );
            return false;
        } else
            return true;
    }
}
