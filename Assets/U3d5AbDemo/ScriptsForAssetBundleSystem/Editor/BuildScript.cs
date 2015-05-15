using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class BuildScript {

    const string kAssetBundlesOutputPath = "AssetBundles";

    public static void BuildAssetBundles ( ) {
        // Choose the output path according to the build target.
        string outputPath = Path.Combine(kAssetBundlesOutputPath, Util.GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget));
        if (!Directory.Exists(outputPath)) {
            Directory.CreateDirectory(outputPath);
        }

        // 用现在激活的平台打包AssetBundles
        BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
    }

    static void CopyAssetBundlesTo (string outputPath) {
        // Clear streaming assets folder.
        FileUtil.DeleteFileOrDirectory(Application.streamingAssetsPath);
        Directory.CreateDirectory(outputPath);

        string outputFolder = Util.GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget);

        // Setup the source folder for assetbundles.
        string source = Path.Combine(Path.Combine(System.Environment.CurrentDirectory, kAssetBundlesOutputPath), outputFolder);
        if (!System.IO.Directory.Exists(source))
            Debug.Log("No assetBundle output folder, try to build the assetBundles first.");

        // Setup the destination folder for assetbundles.
        string destination = System.IO.Path.Combine(outputPath, outputFolder);
        if (System.IO.Directory.Exists(destination))
            FileUtil.DeleteFileOrDirectory(destination);

        FileUtil.CopyFileOrDirectory(source, destination);
    }

}
