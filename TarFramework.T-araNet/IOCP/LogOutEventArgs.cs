using System;

namespace TarFramework.T_araNet.IOCP
{
    public class LogOutEventArgs : EventArgs
    {

        /// <summary>
        /// ��Ϣ����
        /// </summary>     
        private LogType messClass;

        /// <summary>
        /// ��Ϣ����
        /// </summary>  
        public LogType MessClass
        {
            get { return messClass; }
        }



        /// <summary>
        /// ��Ϣ
        /// </summary>
        private string mess;

        public string Mess
        {
            get { return mess; }
        }

        public LogOutEventArgs(LogType messclass, string str)
        {
            messClass = messclass;
            mess = str;

        }


    }
}
