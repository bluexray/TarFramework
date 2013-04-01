/*##Project : Black Matrix--Multi-Thread Asynchronous Socket Server Framework
 *##Author  : bluexray
 *##create Date: 2013/03/19 
*/

using System;
using System.Collections.Generic;

namespace TarFramework.T_araNet
{
    public enum DisconnectType
    {
        Normal,     // disconnect normally
        Timeout,    // disconnect because of timeout
        Exception   // disconnect because of exception
    }
}
