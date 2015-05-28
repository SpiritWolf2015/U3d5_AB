using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;


/// <summary>
/// 工具类
/// </summary>
public static class Util  {

    /// <summary>
    /// 拿到streamingAssetsPath的相对路径
    /// </summary>
    /// <returns></returns>
    public static string GetRelativePath ( ) {
        if (Application.isEditor)
            return "file://" + System.Environment.CurrentDirectory.Replace("\\", "/"); // Use the build output folder directly.
        else if (Application.isWebPlayer)
            return System.IO.Path.GetDirectoryName(Application.absoluteURL).Replace("\\", "/") + "/StreamingAssets";
        else if (Application.isMobilePlatform || Application.isConsolePlatform)
            return Application.streamingAssetsPath;
        else // For standalone player.
            return "file://" + Application.streamingAssetsPath;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 打包编译输出的平台
    /// </summary>
    public static string GetPlatformFolderForAssetBundles (BuildTarget target) {
        switch (target) {
            case BuildTarget.Android:
                return Const.ANDROID_PLATFORM_FOLDER_STRING;
            case BuildTarget.iOS:
                return Const.IOS_PLATFORM_FOLDER_STRING;
            case BuildTarget.WebPlayer:
                return Const.WEB_PLATFORM_FOLDER_STRING;
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return Const.WINDOWS_PLATFORM_FOLDER_STRING;
            case BuildTarget.StandaloneOSXIntel:
            case BuildTarget.StandaloneOSXIntel64:
            case BuildTarget.StandaloneOSXUniversal:
                return Const.MAC_PLATFORM_FOLDER_STRING;
            // Add more build targets for your own.
            // If you add more targets, don't forget to add the same platforms to GetPlatformFolderForAssetBundles(RuntimePlatform) function.
            default:
                Debug.LogError("不支持的打包编译输出的平台！");
                return null;
        }
    }
#endif

    /// <summary>
    /// 运行时读入的是哪个平台平台
    /// </summary>  
    public static string GetPlatformFolderForAssetBundles (RuntimePlatform platform) {
        switch (platform) {
            case RuntimePlatform.Android:
                return Const.ANDROID_PLATFORM_FOLDER_STRING;
            case RuntimePlatform.IPhonePlayer:
                return Const.IOS_PLATFORM_FOLDER_STRING;
            case RuntimePlatform.WindowsWebPlayer:
            case RuntimePlatform.OSXWebPlayer:
                return Const.WEB_PLATFORM_FOLDER_STRING;
            case RuntimePlatform.WindowsPlayer:
                return Const.WINDOWS_PLATFORM_FOLDER_STRING;
            case RuntimePlatform.OSXPlayer:
                return Const.MAC_PLATFORM_FOLDER_STRING;
            // Add more build platform for your own.
            // If you add more platforms, don't forget to add the same targets to GetPlatformFolderForAssetBundles(BuildTarget) function.
            default:
                Debug.LogError("不支持的运行平台！");
                return null;
        }
    }

}
