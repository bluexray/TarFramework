using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.OleDb;

namespace TarFramework.T_araNet
{
    public interface ISessionEvent
    {
        event EventHandler<SessionExceptionEventArgs> SessionReceiveException;
        event EventHandler<SessionExceptionEventArgs> SessionSendException;
        event EventHandler<SessionEventArgs> DatagramDelimiterError;
        event EventHandler<SessionEventArgs> DatagramOversizeError;
        event EventHandler<SessionEventArgs> DatagramAccepted;
        event EventHandler<SessionEventArgs> DatagramError;
        event EventHandler<SessionEventArgs> DatagramHandled;
    }
}
