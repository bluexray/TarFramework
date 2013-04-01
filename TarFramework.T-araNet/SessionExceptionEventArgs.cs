/*##Project : Black Matrix--Multi-Thread Asynchronous Socket Server Framework
 *##Author  : bluexray
 *##create Date: 2013/03/19 
*/

using System;
using System.Collections.Generic;

namespace TarFramework.T_araNet
{
    public class SessionExceptionEventArgs : SessionEventArgs
    {
        private string m_exceptionMessage;

        public SessionExceptionEventArgs(Exception exception, SessionCoreInfo sessionCoreInfo)
            : base(sessionCoreInfo)
        {
            m_exceptionMessage = exception.Message;
        }

        public string ExceptionMessage
        {
            get { return m_exceptionMessage; }
        }
    }
}
