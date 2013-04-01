using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TarFramework.Core.ReflectionWrapper
{
    public class AsmLoadConfig
    {
            #region Ctor
            public AsmLoadConfig() { }
            public AsmLoadConfig(bool copyToMem, bool loadAbstract, string postfix)
            {
                this.copyToMemory = copyToMem;
                this.loadAbstractType = loadAbstract;
                this.targetFilePostfix = postfix;
            }
            #endregion

            #region CopyToMemory
            private bool copyToMemory = false;
            /// <summary>
            /// CopyToMem 是否将程序集拷贝到内存后加载
            /// </summary>
            public bool CopyToMemory
            {
                get { return copyToMemory; }
                set { copyToMemory = value; }
            }
            #endregion

            #region LoadAbstractType
            private bool loadAbstractType = false;
            /// <summary>
            /// LoadAbstractType 是否加载抽象类型
            /// </summary>
            public bool LoadAbstractType
            {
                get { return loadAbstractType; }
                set { loadAbstractType = value; }
            }
            #endregion

            #region TargetFilePostfix
            private string targetFilePostfix = ".dll";
            /// <summary>
            /// TargetFilePostfix 搜索的目标程序集的后缀名
            /// </summary>
            public string TargetFilePostfix
            {
                get { return targetFilePostfix; }
                set { targetFilePostfix = value; }
            }
            #endregion
    }
}
