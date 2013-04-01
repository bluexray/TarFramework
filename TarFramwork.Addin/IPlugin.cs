using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TarFramwork.Addin
{
    public interface IPlugin
    {
        void Load(); //不同类型的插件对开始和停止的解释不一样，如果一个插件没有此意义，可实现为空
        void UnLoad();
        int ServiceKey { get; }//插件的的key,不同的插件具有不一样的key
        PluginInfo AddinAppendixInfo { get; }
        string PluginType { get; }
        bool Enabled { get; set; }
    }
}
