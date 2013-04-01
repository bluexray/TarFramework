/*##Project : Black Matrix--Multi-Thread Asynchronous Socket Server Framework
 *##Author  : bluexray
 *##create Date: 2013/03/19 
*/

using System;
using System.Collections.Generic;

namespace TarFramework.T_araNet
{
    public class ExceptionEventArgs : EventArgs
    {
        private string m_exceptionMessage;

        public ExceptionEventArgs(Exception exception)
        {
            m_exceptionMessage = exception.Message;
        }

        public ExceptionEventArgs(string exceptionMessage)
        {
            m_exceptionMessage = exceptionMessage;
        }

        public string ExceptionMessage
        {
            get { return m_exceptionMessage; }
        }
    }
}
