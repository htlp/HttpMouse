using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Rpfl.Client
{
    /// <summary>
    /// 客户端选项
    /// </summary>
    public class RpflClientOptions
    {
        /// <summary>
        /// 服务器地址
        /// </summary>
        [AllowNull]
        [Required]
        public Uri Server { get; set; }

        /// <summary>
        /// 客户端上游地址
        /// </summary>
        [AllowNull]
        [Required]
        public Uri ClientUpstream { get; set; }

        /// <summary>
        /// 映射到客户端的域名
        /// </summary>
        [AllowNull]
        [Required]
        public string ClientDomain { get; set; }
    }
}
