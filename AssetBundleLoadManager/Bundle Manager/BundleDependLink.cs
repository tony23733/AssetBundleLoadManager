using UnityEngine;
using System.Collections;

namespace BundleManager
{
    /// <summary>
    /// Bundle依赖关系
    /// </summary>
    [System.Serializable]
    public struct BundleDependLink
    {
        /// <summary>
        /// 资源名
        /// </summary>
        public string assetName;
        /// <summary>
        /// 附着点，父节点
        /// </summary>
        public Transform attachPoint;
        /// <summary>
        /// 是否单位化本地缩放
        /// </summary>
        public bool identityLocalScale;
        /// <summary>
        /// 是否单位化本地选择
        /// </summary>
        public bool identityLocalRotation;
    }
}