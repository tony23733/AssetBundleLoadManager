using UnityEngine;
using System.Collections;

namespace BundleManager
{
    /// <summary>
    /// Bundle定义
    /// </summary>
    public class Bundle : MonoBehaviour
    {
        /// <summary>
        /// 本Bundle包含的唯一一个资源的名字
        /// </summary>
        public string assetName
        {
            get
            {
                return mAssetName;
            }
            internal set
            {
                mAssetName = value;
            }
        }
        [HideInInspector]
        [SerializeField]
        internal string mAssetName;
        /// <summary>
        /// 依赖bundle
        /// </summary>
        public BundleDependLink[] dependBundles;
        /// <summary>
        /// 有依赖bundle
        /// </summary>
        public bool hasDependBundle
        {
            get
            {
                if (dependBundles == null)
                    return false;

                return dependBundles.Length > 0;
            }
        }
        /// <summary>
        /// 子资源
        /// </summary>
        public Bundle[] subBundles
        {
            get
            {
                return mSubBundles;
            }
            internal set
            {
                mSubBundles = value;
            }
        }
        [HideInInspector]
        [SerializeField]
        internal Bundle[] mSubBundles;
        /// <summary>
        /// 完成聚集事件
        /// </summary>
        protected System.Action OnCompleteEvent;
        /// <summary>
        /// 子资源加载完成计数
        /// </summary>
        protected int mSubBundleLoadCompleteCount;

        /// <summary>
        /// 聚集子bundle
        /// </summary>
        /// <param name="onComplete"></param>
        internal void CollectSubBundles(System.Action onComplete)
        {
            OnCompleteEvent = onComplete;
            if (hasDependBundle)
            {
                subBundles = new Bundle[dependBundles.Length];

                foreach (var v in dependBundles)
                    AssetBundleLoadManager.LoadBundle(v.assetName, this.OnLoadBundle, true);
            }
            else
            {
                CallComplete();
            }
        }

        protected void CallComplete()
        {
            if (OnCompleteEvent != null)
                OnCompleteEvent();
        }

        #region Call back
        /// <summary>
        /// 加载bundle回调
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="asset">bundle资源，非实例对象</param>
        protected void OnLoadBundle(string assetName, Bundle asset)
        {
            for (int i = 0; i < dependBundles.Length; ++i)
            {
                if (dependBundles[i].assetName == assetName)
                {
                    if (asset != null)
                    {
                        subBundles[i] = asset;
//                         asset.transform.parent = dependBundles[i].attachPoint;
//                         asset.transform.localPosition = Vector3.zero;
//                         if (dependBundles[i].identityLocalScale)
//                             asset.transform.localScale = Vector3.one;
//                         if (dependBundles[i].identityLocalRotation)
//                             asset.transform.localRotation = Quaternion.identity;
                        asset.CollectSubBundles(this.OnCollectSubBundle);
                    }

                    return;
                }
            }
        }

        protected void OnCollectSubBundle()
        {
            ++mSubBundleLoadCompleteCount;
            if (mSubBundleLoadCompleteCount >= dependBundles.Length)
                CallComplete();
        }

        protected virtual void Awake()
        {
//             foreach (var v in GetComponentsInChildren<Renderer>())
//             {
//                 v.sharedMaterial.shader = Shader.Find(v.sharedMaterial.shader.name);
//             }
            if (hasDependBundle)
            {
                for (int i = 0; i < dependBundles.Length; ++i)
                {
                    subBundles[i].transform.localPosition = Vector3.zero;
                    if (dependBundles[i].identityLocalScale)
                        subBundles[i].transform.localScale = Vector3.one;
                    if (dependBundles[i].identityLocalRotation)
                        subBundles[i].transform.localRotation = Quaternion.identity;
                }
            }
        }

        protected virtual void OnDestroy()
        {
            AssetBundleLoadManager.UnusedGameObjectAsset(assetName, this);
        }
        #endregion
    }
}