using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TarFramwork.Addin
{
    public class PluginInfo
    {
        /// <summary>
        /// 插件名称
        /// </summary>
        protected string name;
        /// <summary>
        /// 插件作者
        /// </summary>
        protected string author;
        /// <summary>
        /// 插件版本
        /// </summary>
        protected string version;
        /// <summary>
        /// 插件网站URL
        /// </summary>
        protected string url;
        /// <summary>
        /// 插件更新地址
        /// </summary>
        protected string updateUrl;

        /// <summary>
        /// 插件信息构造函数
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="author">作者</param>
        /// <param name="version">版本</param>
        /// <param name="url">网站</param>
        /// <param name="updateUrl">更新地址</param>
        public PluginInfo(string name, string author, string version, string url, string updateUrl)
        {
            this.name = name;
            this.author = author;
            this.version = version;
            this.url = url;
            this.updateUrl = updateUrl;
        }

        /// <summary>
        /// 插件名称
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// 插件作者
        /// </summary>
        public string Author
        {
            get { return author; }
        }

        /// <summary>
        /// 插件版本
        /// </summary>
        public string Version
        {
            get { return version; }
        }

        /// <summary>
        /// 插件网站URL
        /// </summary>
        public string Url
        {
            get { return url; }
        }

        /// <summary>
        /// 插件更新地址
        /// </summary>
        public string UpdateUrl
        {
            get { return updateUrl; }
        }
    }
}
