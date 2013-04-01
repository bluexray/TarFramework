using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TarFramework.Core;
using TarFramework.Core.ReflectionWrapper;

namespace TarFramwork.Addin
{
        public    delegate void sfunction();



    public  class AddinManager:IAddinManager
    {
         public event sfunction AddinsChanged; 

        public string addinFolderPath = AppDomain.CurrentDomain.BaseDirectory + "Plugins";
        public const string AddinSign = "Addin.dll";//所有的插件命名都需要以addin结束

        private IDictionary<int, IPlugin> dicAddins = new Dictionary<int, IPlugin>();

        public AddinManager()
        {
            this.AddinsChanged += delegate { };
        }

        public IList<IPlugin> PluginList
        {
            get { return this.dicAddins.Values.ToList(); }
        }

        public void LoadDefault()
        {
            this.LoadAllAddins(true);
        }

        public void LoadAllAddins(bool searchChildFolder)
        {
            AsmLoadConfig config = new AsmLoadConfig(this.copyToMem, false, AddinSign);
            IList<Type> addinTypeList = ReflectionHelper.LoadDerivedType(typeof(IPlugin), addinFolderPath, searchChildFolder, config);
            foreach (Type addinType in addinTypeList)
            {
                IPlugin addin = (IPlugin)Activator.CreateInstance(addinType);
                this.dicAddins.Add(addin.ServiceKey, addin);
                addin.Load();
            }

            this.AddinsChanged();
        }

        public void LoadAddinAssembly(string assemblyPath)
        {
            Assembly asm = null;
            if (this.copyToMem)
            {
                byte[] addinStream = ReflectionHelper.ReadFileReturnBytes(assemblyPath);
                asm = Assembly.Load(addinStream);
            }
            else
            {
                asm = Assembly.LoadFrom(assemblyPath);
            }


            IList<IPlugin> newList = ReflectionHelper.LoadDerivedInstance<IPlugin>(asm);
            foreach (IPlugin newAddin in newList)
            {
                this.dicAddins.Add(newAddin.ServiceKey, newAddin);
                newAddin.Load();
            }

            this.AddinsChanged();
        }

        public void Clear()
        {
            foreach (IPlugin addin in this.dicAddins.Values)
            {
                try
                {
                    addin.UnLoad();
                }
                catch { }
            }

            this.dicAddins.Clear();
            this.AddinsChanged();
        }

        public void DynRemoveAddin(int addinKey)
        {
            throw new NotImplementedException();
        }

        public void EnableAddin(int addinKey)
        {
            IPlugin plugin = this.GetAddin(addinKey);
            if (plugin != null)
            {
                plugin.Enabled = true;
            }
        }

        public void DisableAddin(int addinKey)
        {
            throw new NotImplementedException();
        }

        public IPlugin GetAddin(int addinKey)
        {
            if (!this.dicAddins.ContainsKey(addinKey))
            {
                return null;
            }

            return this.dicAddins[addinKey];
        }

        public void  LoadAllAddins(string addinFolderPath, bool searchChildFolder)
        {

        }


        #region event ,property
        private bool copyToMem = false;
        public bool CopyToMemory
        {
            get
            {
                return this.copyToMem;
            }
            set
            {
                this.copyToMem = value;
            }
        }

        #endregion
    }
}