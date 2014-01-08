using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using JsonMap.Default;
using JsonMap.Interfaces;
using JsonMap.Interfaces.Extensions;
using Newtonsoft.Json;

namespace JsonMap.Utils
{
    public static class JsonMapStartup
    {
        private static IEnumerable<IJsonMap> _preLoadedEntities;
        public static void Start(IDependencyResolver resolver, bool debug = true)
        {
            resolver.GetService<IJsonMapQueryer>();
            if (JTransformer.Queryer == null)
            {
                JTransformer.Queryer = DefaultJsonMapQueryer.Instance;
            }
            _preLoadedEntities = JTransformer.Queryer.GetList();
            
            if (debug)
            {
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings { Formatting = Formatting.Indented, ReferenceLoopHandling = ReferenceLoopHandling.Serialize, PreserveReferencesHandling = PreserveReferencesHandling.Objects };
            }
            else
            {
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings { Formatting = Formatting.None, ReferenceLoopHandling = ReferenceLoopHandling.Serialize, PreserveReferencesHandling = PreserveReferencesHandling.Objects };
            }
            if (!debug)
            {
                var thread = new Thread(DoWork);
                thread.Start();
            }
        }

        private static void DoWork()
        {
            Parallel.ForEach(_preLoadedEntities, jsonMap =>
            {
                var type = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetType(jsonMap.Name) != null).Select(x => x.GetType(jsonMap.Name)).FirstOrDefault();
                while (type == null)
                {
                    Thread.Sleep(1000);
                    type = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetType(jsonMap.Name) != null).Select(x => x.GetType(jsonMap.Name)).FirstOrDefault();
                }
                if (!(type.IsAbstract || type.IsInterface))
                {
                    var obj = Activator.CreateInstance(type, false);
                    var entity = jsonMap as IEntity;
                    if (entity != null)
                        obj.ToJToken(new ToJOptions { mapType = entity.JsonMapEnum });
                }
            });
        }
    }
}
