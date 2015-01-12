using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace BundleManager
{
    public class PackAssetBundleWindow : EditorWindow
    {
        // Export Information
        private string mAssetBundlesFolder = "/Pack AssetBundles/";
        private string[] mContains;
        private string mExportFolder = "/StreamingAssets/";
        private string mConfigurationExportPath = "/Pack AssetBundles/AssetBundleData.asset";
        private string mBundleFileExtension = ".bundle";
        private int mBunldeVerion = 1;

        // Target Settings
        private string mLocalBundleTargetName = "Unkown";
        private BuildTarget mBuildTarget = BuildTarget.Android;

        // private field
        private const string mAssetPathHead = "Assets";
        /// <summary>
        /// 资源集合。(资源名:不含扩展名，资源目录)
        /// </summary>
        private Dictionary<string, string> mAssetPath = new Dictionary<string, string>();
        /// <summary>
        /// 依赖的资源。检测依赖的资源是否存在
        /// </summary>
        private HashSet<string> mAssetDepend = new HashSet<string>();

        [MenuItem("Window/Pack AssetBundles Window")]
        private static void Init()
        {
            EditorWindow.GetWindow<PackAssetBundleWindow>("Pack AssetBundles");
        }

        void OnEnable()
        {
            mBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            AssetBundleData abData = null;
            if (File.Exists(Application.dataPath + mConfigurationExportPath))
                abData = (AssetBundleData)AssetDatabase.LoadMainAssetAtPath(mAssetPathHead + mConfigurationExportPath);
            if (abData != null)
                mLocalBundleTargetName = abData.bundlePlatform;
            if (HasContains())
                LoadContains();
            else
                ResetContains();
        }

        void OnGUI()
        {
            GUILayout.Label("Export Information", EditorStyles.boldLabel);
            GUILayout.Label("Main Path\t\t     " + Application.dataPath);
            mAssetBundlesFolder = EditorGUILayout.TextField("AssetBundles Folder", mAssetBundlesFolder);
            GUILayout.Label("Contains");
            int size = EditorGUILayout.IntField("  Size", mContains.Length);
            if (size != mContains.Length)
            {
                string[] oldContains = new string[mContains.Length];
                mContains.CopyTo(oldContains, 0);
                mContains = new string[size];
                System.Array.Copy(oldContains, mContains, Mathf.Min(oldContains.Length, mContains.Length));
                SaveContains();
            }
            for (int i = 0; i < mContains.Length; ++i)
            {
                string newContain = EditorGUILayout.TextField("  Folder " + i, mContains[i]);
                if (newContain != mContains[i])
                {
                    mContains[i] = newContain;
                    SaveContains();
                }
            }
            if (GUILayout.Button("ResetContains"))
            {
                ResetContains();
                SaveContains();
            }
            mExportFolder = EditorGUILayout.TextField("Export Folder", mExportFolder);
            mConfigurationExportPath = EditorGUILayout.TextField("Configuration Export Path", mConfigurationExportPath);
            mBundleFileExtension = EditorGUILayout.TextField("Bundle File Extension", mBundleFileExtension);
            mBunldeVerion = EditorGUILayout.IntField("Bunlde Verion", mBunldeVerion);

            GUILayout.Space(5);
            GUILayout.Label("Target Settings", EditorStyles.boldLabel);
            GUILayout.Label("Local Bundle Target\t     " + mLocalBundleTargetName, EditorStyles.whiteLabel);
            mBuildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Build Target", mBuildTarget);

            GUILayout.Space(5);
            GUILayout.Label("Build", EditorStyles.boldLabel);
            if (GUILayout.Button("Pack AssetBundles"))
                Build();
            GUILayout.Space(10);
            GUILayout.Label("Cache", EditorStyles.boldLabel);
            if (GUILayout.Button("Clear Cache"))
                Caching.CleanCache();
            GUILayout.Space(10);
            GUILayout.Label("Dump", EditorStyles.boldLabel);
            if (GUILayout.Button("Bump Shader"))
            {
                HashSet<string> shaderNameList = new HashSet<string>();
                string log = "--Toto-- shader name list :";
                foreach (var parentTrans in Selection.transforms)
                {
                    foreach (var render in parentTrans.GetComponentsInChildren<Renderer>())
                    {
                        string shaderName = render.sharedMaterial.shader.name;
                        if (!shaderNameList.Contains(shaderName))
                        {
                            shaderNameList.Add(shaderName);
                            log += "\n" + shaderName;
                        }
                    }
                }

                Debug.Log(log);
            }
        }

        void Build()
        {
            mAssetPath.Clear();
            mAssetDepend.Clear();
            string rootPath = Application.dataPath + mAssetBundlesFolder;
            if (!Directory.Exists(rootPath))
            {
                Debug.LogError("--Toto-- PackAssetBundleWindow->Build: '" + mAssetBundlesFolder + "'the folder does not exist.");
                return;
            }
            if (!mAssetBundlesFolder.EndsWith("/"))
                mAssetBundlesFolder += "/";
            if (!mExportFolder.EndsWith("/"))
                mExportFolder += "/";

            // pack
            foreach (var v in mContains)
            {
                string exportPath = Application.dataPath + mExportFolder + v;
                if (Directory.Exists(exportPath))
                    Directory.Delete(exportPath, true);
                PackBundleByFolder(v);
            }

            if (mAssetPath.Count > 0)
            {
                // save configuration file
                AssetBundleData abData;
                bool isExist = File.Exists(Application.dataPath + mConfigurationExportPath);
                if (isExist)
                    abData = (AssetBundleData)AssetDatabase.LoadMainAssetAtPath(mAssetPathHead + mConfigurationExportPath);
                else
                    abData = ScriptableObject.CreateInstance<AssetBundleData>();
                abData.bundleVersion = mBunldeVerion;
                abData.bundlePlatform = mBuildTarget.ToString();
                mLocalBundleTargetName = mBuildTarget.ToString();
                abData.keys.Clear();
                abData.values.Clear();
                foreach (var v in mAssetPath)
                {
                    abData.keys.Add(v.Key);
                    abData.values.Add(v.Value);
                }
                if (!isExist)
                    AssetDatabase.CreateAsset(abData, mAssetPathHead + mConfigurationExportPath);
                else
                    EditorUtility.SetDirty(abData);
            }

            // depend bundle isExist
            foreach (var v in mAssetDepend)
            {
                if (!mAssetPath.ContainsKey(v))
                {
                    Debug.LogWarning("--Toto-- PackAssetBundleWindow->Build: depend bundle '" + v + "' does not exists.");
                }
            }

            Debug.Log("Build successfully " + mAssetPath.Count);
        }

        void PackBundleByFolder(string folder)
        {
            string folderPath = Application.dataPath + mAssetBundlesFolder + folder;
            string[] assetsInFolder = Directory.GetFiles(folderPath);
            foreach (var v in assetsInFolder)
            {
                string assetname = Path.GetFileName(v);
                PackBundle(folder, assetname);
            }

            string[] directoryNames = Directory.GetDirectories(folderPath);
            foreach (var v in directoryNames)
            {
                string subFolderName = Path.GetFileName(v);
                PackBundleByFolder(folder + "/" + subFolderName);
            }
        }

        /// <summary>
        /// 打包单个资源
        /// </summary>
        /// <param name="containFolder">包含的文件夹名，可以有多层目录结构。如"abc/efg"</param>
        /// <param name="assetname"></param>
        void PackBundle(string containFolder, string assetname)
        {
            if (assetname.EndsWith(".meta"))
                return;

            string fileName = Path.GetFileNameWithoutExtension(assetname);      // 资源名，无扩展名
            if (mAssetPath.ContainsKey(fileName))       // 检查重名
            {
                Debug.LogWarning("--Toto-- PackAssetBundleWindow->PackBundle: asset name '" + fileName + "' already exists, please rename the asset.");
                return;
            }
            string assetPath = mAssetPathHead + mAssetBundlesFolder + containFolder + "/" + assetname;
            Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);        // load
            if (asset == null)
            {
                Debug.LogWarning("--Toto-- PackAssetBundleWindow->PackBundle: '" + assetPath + "' load asset failed.");
                return;
            }
            if (assetname.EndsWith(".prefab"))      // check bundle script
            {
                Bundle bundle = ((GameObject)asset).GetComponent<Bundle>();
                if (bundle == null)
                {
                    Debug.LogWarning("--Toto-- PackAssetBundleWindow->PackBundle: '" + assetPath + "' not have 'Bundle' component.");
                    return;
                }
                if (bundle.hasDependBundle)     // check depend asset
                {
                    foreach (var v in bundle.dependBundles)
                    {
                        if (!mAssetDepend.Contains(v.assetName))
                            mAssetDepend.Add(v.assetName);
                    }
                }
            }
            string exportPath = Application.dataPath + mExportFolder + containFolder;
            if (!Directory.Exists(exportPath))      // create folder
            {
                Directory.CreateDirectory(exportPath);
            }
            exportPath += "/" + fileName + mBundleFileExtension;

            // build
            BuildPipeline.BuildAssetBundle(asset, null, exportPath, BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets, mBuildTarget);
            mAssetPath.Add(fileName, "/" + containFolder + "/" + fileName + mBundleFileExtension/*exportPath.Substring(Application.dataPath.Length)*/);
            Debug.Log("asset " + fileName + " : bundle path " + mAssetPath[fileName]);
        }

        void LoadContains()
        {
            if (!HasContains())
                return;
            string conts = PlayerPrefs.GetString("Contains");
            string[] contsSplit = conts.Split(new string[] { "&&" }, System.StringSplitOptions.RemoveEmptyEntries);
            if (contsSplit.Length == 0)
                return;
            mContains = new string[contsSplit.Length];
            for (int i = 0; i < contsSplit.Length; ++i)
            {
                mContains[i] = contsSplit[i];
            }
        }

        void SaveContains()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < mContains.Length; ++i)
            {
                sb.Append(mContains[i]);
                if (i != mContains.Length - 1)
                    sb.Append("&&");
            }
            PlayerPrefs.SetString("Contains", sb.ToString());
        }

        bool HasContains()
        {
            return PlayerPrefs.HasKey("Contains");
        }

        void ResetContains()
        {
            mContains = new string[] { "Level", "Card", "Character", "FX", "Icon" };
        }
    }
}