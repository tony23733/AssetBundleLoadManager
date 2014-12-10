using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BundleManager
{
    /// <summary>
    /// Bundle数据信息
    /// </summary>
    public class AssetBundleData : ScriptableObject
    {
        /// <summary>
        /// 版本
        /// </summary>
        public int bundleVersion;
        /// <summary>
        /// bundle平台
        /// </summary>
        public string bundlePlatform;
        /// <summary>
        /// 资源相对目录。<asset名，bundle地址>
        /// </summary>
        public Dictionary<string, string> assetBundlePath
        {
            get
            {
                if (mAssetBundlePath.Count == 0)
                {
                    for (int i = 0; i < keys.Count; ++i)
                    {
                        string path = "";
                        // init path
//                         if (Application.platform == RuntimePlatform.WindowsEditor)
//                         {
//                             path = "file:" + Application.dataPath + values[i];
//                         }
                        switch(Application.platform)
                        {
                            case RuntimePlatform.Android:
                                path = Application.streamingAssetsPath + values[i];;
                                break;
                            default:        // Windows、Mac
                                path = "file://" + Application.streamingAssetsPath + values[i];
                                break;
                        }
                        mAssetBundlePath.Add(keys[i], path);
                    }
                }

                return mAssetBundlePath;
            }
        }
        private Dictionary<string, string> mAssetBundlePath = new Dictionary<string, string>();
        /// <summary>
        /// key列表
        /// </summary>
        public List<string> keys = new List<string>();
        /// <summary>
        /// value列表
        /// </summary>
        public List<string> values = new List<string>();
    }
}
