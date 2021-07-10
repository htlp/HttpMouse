using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HttpMouse.ServerHost
{
    /// <summary>
    /// 错误回退选项
    /// </summary>
    public class FallbackOptions
    {
        /// <summary>
        /// 响应状态码
        /// </summary>
        public int StatusCode { get; set; } = 503;

        /// <summary>
        /// 响应内容类型
        /// </summary>
        public string ContentType { get; set; } = "application/problem+json";

        /// <summary>
        /// 响应内容文件路径 
        /// </summary>
        public string ContentFile { get; set; } = "fallback.json";
    }
}
