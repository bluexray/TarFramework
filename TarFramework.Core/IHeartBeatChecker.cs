using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TarFramework.Core
{
    public interface IHeartBeatChecker
    {
        /// <summary>
        /// SurviveSpanInSecs 在没有心跳到来时，可以存活的最长时间。SurviveSpanInSecs小于等于0，表示存活时间为无限长，而不需要进行心跳检查
        /// </summary>
        int SurviveSpanInSecs { get; set; }

        /// <summary>
        /// DetectSpanInSecs 隔多长时间进行一次状态检查。
        /// </summary>
        int DetectSpanInSecs { get; set; }

        /// <summary>
        /// Initialize 初始化并启动心跳监测器。
        /// </summary>
        void Initialize();

        /// <summary>
        /// RegisterOrActivate 注册一个新的客户端或激活它（收到心跳消息）。
        /// </summary>       
        void RegisterOrActivate(string id);

        /// <summary>
        /// Unregister 服务端主动取消对目标客户端的监测。
        /// </summary>        
        void Unregister(string id);

        /// <summary>
        /// Clear 清空所有的监测。
        /// </summary>
        void Clear();

    }
}
