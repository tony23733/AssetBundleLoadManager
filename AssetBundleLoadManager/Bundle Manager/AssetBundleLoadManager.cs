using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BundleManager
{
    /// <summary>
    /// 使用GameObject资源委托
    /// </summary>
    /// <param name="assetName"></param>
    /// <param name="assetInstance"></param>
    public delegate void UseGameObjectAssetDelegate(string assetName, Bundle assetInstance);
//     /// <summary>
//     /// 使用纹理资源委托
//     /// </summary>
//     /// <param name="assetName"></param>
//     /// <param name="asset"></param>
//     public delegate void UseTextureAssetDelegate(string assetName, Texture asset);
    /// <summary>
    /// 使用原始资源委托
    /// </summary>
    /// <param name="assetName"></param>
    /// <param name="asset"></param>
    public delegate void UseOriginalAssetDelegate(string assetName, Object asset);

    /// <summary>
    /// 资源管理器
    /// </summary>
    public class AssetBundleLoadManager : MonoBehaviour
    {
        private abstract class LoadRequestBase
        {
            /// <summary>
            /// 资源名
            /// </summary>
            public string assetName;
            /// <summary>
            /// 资源包
            /// </summary>
            public AssetBundle assetBundle;
            public abstract void RequestComplete();
        }

        /// <summary>
        /// 资源加载请求
        /// </summary>
        private class AssetLoadRequest : LoadRequestBase
        {
            /// <summary>
            /// 是否为子资源
            /// </summary>
            public bool isSub;
            /// <summary>
            /// 使用资源完成委托
            /// </summary>
            public UseGameObjectAssetDelegate onComplete;

            /// <summary>
            /// 请求完成
            public override void RequestComplete()
            {
//                 if (assetBundle.Contains(assetName))
//                 {
//                     if (onComplete != null)
//                     {
//                         GameObject obj = assetBundle.Load(assetName) as GameObject; 
//                         Bundle asset = obj == null ? null : obj.GetComponent<Bundle>();
//                         if (asset != null)
//                         {
//                             asset.assetName = assetName;        // 设置名字，用于Bundle自我查找
//                             AssetBundleLoadManager.instance.mAssetCached.Add(assetName, asset);
//                             if (asset.hasDependBundle)
//                             {
//                                 asset.CollectSubBundles(this.DoInstantiateComplete);
//                             }
//                             else
//                             {
//                                 DoInstantiateComplete();
//                             }
//                         }
//                         else
//                         {
//                             onComplete(assetName, null);
//                         }
//                     }
//                 }
//                 else
//                 {
//                     if (onComplete != null)
//                         onComplete(assetName, null);
//                     Debug.LogWarning("--Toto-- AssetLoadRequest->RequestComplete: assetBundle does not contain '" + assetName + "'.");
//                 }
                // 改load为mainAsset方式

                if (onComplete != null)
                {
                    GameObject obj = assetBundle.mainAsset as GameObject;
                    Bundle asset = obj == null ? null : obj.GetComponent<Bundle>();
                    if (asset != null)
                    {
                        asset.assetName = assetName;        // 设置名字，用于Bundle自我查找
                        AssetBundleLoadManager.instance.mAssetCached.Add(assetName, asset);
                        if (asset.hasDependBundle)
                            asset.CollectSubBundles(this.DoInstantiateComplete);
                        else
                            DoInstantiateComplete();
                    }
                    else
                    {
                        onComplete(assetName, null);
                        Debug.LogWarning("--Toto-- AssetLoadRequest->RequestComplete: assetBundle does not contain '" + assetName + "'.");
                    }
                }
                assetBundle.Unload(false);
            }

            private void DoInstantiateComplete()
            {
                if (onComplete != null)
                {
                    Bundle assetInstance = isSub ? (Bundle)AssetBundleLoadManager.instance.mAssetCached[assetName] 
                        : AssetBundleLoadManager.InstantiateBundle((Bundle)AssetBundleLoadManager.instance.mAssetCached[assetName]);        // 如果是子资源返回bundle资源本身，否则返回其实例对象
                    onComplete(assetName, assetInstance);
                }
            }
        }

        private class AssetLoadRequestWait
        {
            public string assetName;
            public UseGameObjectAssetDelegate onComplete;
        }

        private class OriginalAssetLoadRequest : LoadRequestBase
        {
            /// <summary>
            /// 使用资源完成委托
            /// </summary>
            public UseOriginalAssetDelegate onComplete;
            public override void RequestComplete()
            {
//                 if (assetBundle.Contains(assetName))
//                 {
//                     if (onComplete != null)
//                     {
//                         var asset = assetBundle.Load(assetName);
//                         AssetBundleLoadManager.instance.mAssetCached.Add(assetName, asset);
//                         onComplete(assetName, asset);
//                     }
//                 }
//                 else
//                 {
//                     if (onComplete != null)
//                         onComplete(assetName, null);
//                     Debug.LogWarning("--Toto-- OriginalAssetLoadRequest->RequestComplete: assetBundle does not contain '" + assetName + "'.");
//                 }
                // 改load为mainAsset方式

                if (onComplete != null)
                {
                    var asset = assetBundle.mainAsset;
                    if (asset != null)
                        AssetBundleLoadManager.instance.mAssetCached.Add(assetName, asset);
                    else
                        Debug.LogWarning("--Toto-- OriginalAssetLoadRequest->RequestComplete: assetBundle does not contain '" + assetName + "'.");
                    onComplete(assetName, asset);
                }
                assetBundle.Unload(false);
            }
        }

        /// <summary>
        /// 实例
        /// </summary>
        public static AssetBundleLoadManager instance { get; private set; }
        /// <summary>
        /// 资源信息
        /// </summary>
        public AssetBundleData data;
        /// <summary>
        /// 自动卸载未使用的资源
        /// </summary>
        public bool autoUnloadUnusedAssets = true;
        /// <summary>
        /// 加载场景时清楚回收站缓存，仅清楚缓存，销毁有系统执行。
        /// </summary>
        public bool clearRecycleCachedOnLevelWasLoaded = true;
        /// <summary>
        /// asset缓存
        /// </summary>
        private Hashtable mAssetCached = new Hashtable();
        /// <summary>
        /// 回收资源缓存
        /// </summary>
        private Dictionary<string, Queue<Bundle>> mRecycleBundleCached = new Dictionary<string, Queue<Bundle>>();
        /// <summary>
        /// 资源请求列表
        /// </summary>
        private List<LoadRequestBase> mAssetLoadRequestList = new List<LoadRequestBase>();
        /// <summary>
        /// 资源加载请求等待列表，当请求列表里有相同资源请求时加入备用请求。
        /// </summary>
        private List<AssetLoadRequestWait> mAssetLoadRequestWaitList = new List<AssetLoadRequestWait>();
        /// <summary>
        /// Bundle实例计数器，记录实例化的对象数量
        /// </summary>
        private Dictionary<string, int> mAssetInstanceCounter = new Dictionary<string, int>();
        /// <summary>
        /// 原始资源计数器
        /// </summary>
        private Dictionary<string, int> mOriginalAssetCounter = new Dictionary<string, int>();
        /// <summary>
        /// 卸载所有资源
        /// </summary>
        private bool mIsUnloadUnusedAssets;

        // Use this for initialization
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Update is called once per frame
        void Update()
        {
            // 遍历请求
            for (int i = 0; i < mAssetLoadRequestList.Count; ++i)
            {
                if (mAssetLoadRequestList[i].assetBundle != null)
                {
                    mAssetLoadRequestList[i].RequestComplete();
                    mAssetLoadRequestList.RemoveAt(i);
                    --i;
                }
            }
            for (int i = 0; i < mAssetLoadRequestWaitList.Count; ++i)
            {
                string assetName = mAssetLoadRequestWaitList[i].assetName;
                // 先判断回收站
                if (instance.mRecycleBundleCached.ContainsKey(assetName))
                {
                    var list = instance.mRecycleBundleCached[assetName];
                    if (list.Count > 0)
                    {
                        Bundle bundleInstantiate = list.Dequeue();
                        bundleInstantiate.gameObject.SetActive(true);
                        if (mAssetLoadRequestWaitList[i].onComplete != null)
                            mAssetLoadRequestWaitList[i].onComplete(assetName, bundleInstantiate);
                        mAssetLoadRequestWaitList.RemoveAt(i);
                        --i;
                        break;
                    }
                }

                // 判断预设
                if (instance.mAssetCached.ContainsKey(assetName))
                {
                    Bundle b = (Bundle)instance.mAssetCached[assetName];
                    if (CheckBundleCachedExistRecursively(b))
                    {
                        if (mAssetLoadRequestWaitList[i].onComplete != null)
                            mAssetLoadRequestWaitList[i].onComplete(assetName, InstantiateBundle(b));
                        mAssetLoadRequestWaitList.RemoveAt(i);
                        --i;
                    }
                }
            }

            if (mIsUnloadUnusedAssets && autoUnloadUnusedAssets)
            {
                Resources.UnloadUnusedAssets();
                mIsUnloadUnusedAssets = false;
            }
        }

        void OnLevelWasLoaded(int level)
        {
            if (!clearRecycleCachedOnLevelWasLoaded)
                return;
            instance.mRecycleBundleCached.Clear();
        }

        /// <summary>
        /// 使用GameObject资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="onComplete"></param>
        public static void UseGameObjectAsset(string assetName, UseGameObjectAssetDelegate onComplete)
        {
            LoadBundle(assetName, onComplete, false);
        }

        /// <summary>
        /// 不使用GameObject资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="assetInstance"></param>
        internal static void UnusedGameObjectAsset(string assetName, Bundle assetInstance)
        {
            // 更新计数、删除资源
            if (instance.mAssetInstanceCounter.ContainsKey(assetName))
            {
                --instance.mAssetInstanceCounter[assetName];
                if (instance.mAssetInstanceCounter[assetName] <= 0)
                {
                    instance.mAssetInstanceCounter.Remove(assetName);
                    if (instance.mAssetCached.ContainsKey(assetName))
                    {
                        DestroyImmediate(((Bundle)(instance.mAssetCached[assetName])).gameObject, true);
                        instance.mAssetCached.Remove(assetName);
                        instance.mIsUnloadUnusedAssets = true;
                    }
                }
            }
        }

        /// <summary>
        /// 回收GameObject资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="assetInstance"></param>
        public static void RecycleGameObjectAsset(Bundle assetInstance)
        {
            if (!instance.mRecycleBundleCached.ContainsKey(assetInstance.assetName))
                instance.mRecycleBundleCached.Add(assetInstance.assetName, new Queue<Bundle>());
            var list = instance.mRecycleBundleCached[assetInstance.assetName];
            if (list.Contains(assetInstance))
            {
                Debug.LogWarning("--Toto-- AssetBundleLoadManager->RecycleGameObjectAsset: mRecycleBundleCached contains the " + assetInstance.name);
                return;
            }
            assetInstance.transform.parent = null;
            if (instance.mAssetCached.ContainsKey(assetInstance.assetName))
            {
                Bundle b = (Bundle)instance.mAssetCached[assetInstance.assetName];
                if (b != null)
                {
                    assetInstance.transform.localScale = b.transform.localScale;
                }
            }
            assetInstance.gameObject.SetActive(false);
            list.Enqueue(assetInstance);
        }

        /// <summary>
        /// 清除回收资源
        /// </summary>
        public static void ClearRecycle()
        {
            foreach (KeyValuePair<string, Queue<Bundle>> pair in instance.mRecycleBundleCached)
            {
                foreach (var v in pair.Value)
                    Destroy(v.gameObject);
            }
            instance.mRecycleBundleCached.Clear();
        }

        /// <summary>
        /// 使用纹理资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="onComplete"></param>
        public static void UseTextureAsset(string assetName, UseOriginalAssetDelegate onComplete)
        {
             LoadOriginalAsset(assetName, onComplete);
        }

        /// <summary>
        /// 不使用纹理资源
        /// </summary>
        /// <param name="assetName"></param>
        public static void UnusedTextureAsset(string assetName)
        {
            UnusedOriginalAsset(assetName);
        }

        /// <summary>
        /// 加载Bundle
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="onComplete"></param>
        /// <param name="isSub"></param>
        internal static void LoadBundle(string assetName, UseGameObjectAssetDelegate onComplete, bool isSub)
        {
            if (!instance.data.assetBundlePath.ContainsKey(assetName))
            {
                Debug.LogWarning("--Toto-- AssetBundleLoadManager->UseGameObjectAsset: '" + assetName + "' asset does not exist.");
                return;
            }
            // 判断回收站
            if (instance.mRecycleBundleCached.ContainsKey(assetName))
            {
                var list = instance.mRecycleBundleCached[assetName];
                if (list.Count > 0)
                {
                    Bundle bundleInstantiate = list.Dequeue();
                    bundleInstantiate.gameObject.SetActive(true);
                    if (onComplete != null)
                        onComplete(assetName, bundleInstantiate);
                    return;
                }
            }
            // 判断缓存
            if (instance.mAssetCached.ContainsKey(assetName))
            {
                Bundle b = (Bundle)instance.mAssetCached[assetName];
                if (b != null)
                {
                    if (onComplete != null)
                        onComplete(assetName, InstantiateBundle(b));
                }
                return;
            }
            AssetLoadRequest loadRequest = (AssetLoadRequest)FindLoadRequestByName(assetName);
            if (loadRequest != null)
            {
                instance.mAssetLoadRequestWaitList.Add(new AssetLoadRequestWait() { assetName = assetName, onComplete = onComplete });
                return;
            }

            string abPath = instance.data.assetBundlePath[assetName];
            AssetLoadRequest abLoadRequest = new AssetLoadRequest();
            abLoadRequest.assetName = assetName;
            abLoadRequest.onComplete = onComplete;
            abLoadRequest.isSub = isSub;
            instance.mAssetLoadRequestList.Add(abLoadRequest);      // 添加至资源请求list
            instance.StartCoroutine(instance.LoadAssetBundle(abPath, abLoadRequest));
        }

        private IEnumerator LoadAssetBundle(string assetBundlePath, LoadRequestBase assetLoadRequest)
        {
            // Wait for the Caching system to be ready
            while (!Caching.ready)
            {
                yield return null;
            }

            using (WWW www = WWW.LoadFromCacheOrDownload(assetBundlePath, data.bundleVersion))
            {
                yield return www;
                if (www.error != null)
                {
                    throw new System.Exception("AssetBundle - WWW download:" + www.error);
                }
                assetLoadRequest.assetBundle = www.assetBundle;

                www.Dispose();
            }
        }

        /// <summary>
        /// 加载原始资源通用接口
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="?"></param>
        private static void LoadOriginalAsset(string assetName, UseOriginalAssetDelegate onComplete)
        {
            if (!instance.data.assetBundlePath.ContainsKey(assetName))
            {
                Debug.LogWarning("--Toto-- AssetBundleLoadManager->LoadOriginalAsset: '" + assetName + "' asset does not exist.");
                return;
            }
            if (!instance.mOriginalAssetCounter.ContainsKey(assetName))     // 使用计数
                instance.mOriginalAssetCounter[assetName] = 0;
            ++instance.mOriginalAssetCounter[assetName];
            if (instance.mAssetCached.ContainsKey(assetName))
            {
                Object t = (Object)instance.mAssetCached[assetName];
                if (t != null && onComplete != null)
                {
                    onComplete(assetName, t);
                }
                return;
            }
            OriginalAssetLoadRequest loadRequest = (OriginalAssetLoadRequest)FindLoadRequestByName(assetName);
            if (loadRequest != null)
            {
                loadRequest.onComplete += onComplete;
                return;
            }

            string abPath = instance.data.assetBundlePath[assetName];
            OriginalAssetLoadRequest abLoadRequest = new OriginalAssetLoadRequest();
            abLoadRequest.assetName = assetName;
            abLoadRequest.onComplete = onComplete;
            instance.mAssetLoadRequestList.Add(abLoadRequest);      // 添加至资源请求list
            instance.StartCoroutine(instance.LoadAssetBundle(abPath, abLoadRequest));
        }

        /// <summary>
        /// 不使用原始资源
        /// </summary>
        /// <param name="assetName"></param>
        private static void UnusedOriginalAsset(string assetName)
        {
            if (instance.mOriginalAssetCounter.ContainsKey(assetName))
            {
                --instance.mOriginalAssetCounter[assetName];
                if (instance.mOriginalAssetCounter[assetName] <= 0)
                {
                    instance.mOriginalAssetCounter.Remove(assetName);
                    if (instance.mAssetCached.ContainsKey(assetName))
                    {
                        var asset = (Object)instance.mAssetCached[assetName];
                        Resources.UnloadAsset(asset);
                        instance.mAssetCached.Remove(assetName);
                    }
                }
            }
        }

        /// <summary>
        /// 实例化bundle
        /// </summary>
        /// <param name="bundle"></param>
        /// <returns></returns>
        private static Bundle InstantiateBundle(Bundle bundle)
        {
            MatchingBundleParentRecursively(bundle, true, true);        // 先匹配
            Bundle bundleIns = (Bundle)Object.Instantiate(bundle);
            MatchingBundleParentRecursively(bundle, false, false);        // 在拆分开

            return bundleIns;
        }

        /// <summary>
        /// 递归匹配bundle父关系
        /// </summary>
        /// <param name="bundle"></param>
        /// <param name="isMatch">true：匹配父子关系，反之parent设为null</param>
        /// <param name="isCounter">true时计数实例计数器</param>
        /// <returns></returns>
        private static Bundle MatchingBundleParentRecursively(Bundle bundle, bool isMatch, bool isCounter)
        {
            if (bundle.hasDependBundle)
            {
                for (int i = 0; i < bundle.subBundles.Length; ++i)
                {
                    Bundle subBundle = MatchingBundleParentRecursively(bundle.subBundles[i], isMatch, isCounter);
                    subBundle.transform.parent = isMatch ? bundle.dependBundles[i].attachPoint : null;      // true时绑定，false时分开
                }
            }
            if (isCounter)
            {
                if (!instance.mAssetInstanceCounter.ContainsKey(bundle.assetName))
                    instance.mAssetInstanceCounter.Add(bundle.assetName, 0);

                ++instance.mAssetInstanceCounter[bundle.assetName];     // 数量加1
            }

            return bundle;
        }

        private static LoadRequestBase FindLoadRequestByName(string name)
        {
            return instance.mAssetLoadRequestList.Find(x => x.assetName == name);
        }

        private static bool CheckBundleCachedExistRecursively(Bundle bundle)
        {
            if (bundle == null)
                return false;

            if (!instance.mAssetCached.ContainsKey(bundle.assetName))
                return false;

            if (bundle.hasDependBundle)
            {
                foreach (var v in bundle.subBundles)
                {
                    if (!CheckBundleCachedExistRecursively(v))
                        return false;
                }
            }

            return true;
        }
    }
}