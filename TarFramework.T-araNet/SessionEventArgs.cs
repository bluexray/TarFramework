/*##Project : Black Matrix--Multi-Thread Asynchronous Socket Server Framework
 *##Author  : bluexray
 *##create Date: 2013/03/19 
*/

using System;
using System.Collections.Generic;

namespace TarFramework.T_araNet
{
    public class SessionEventArgs : EventArgs
    {
        SessionCoreInfo m_sessionBaseInfo;

        public SessionEventArgs(SessionCoreInfo sessionCoreInfo)
        {
            m_sessionBaseInfo = sessionCoreInfo;
        }

        public SessionCoreInfo SessionBaseInfo
        {
            get { return m_sessionBaseInfo; }
        }
    }
}
