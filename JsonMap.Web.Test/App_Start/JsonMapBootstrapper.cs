using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using JsonMap.Default;
using JsonMap.Interfaces;
using JsonMap.Interfaces.Extensions;
using Newtonsoft.Json;

[assembly: WebActivatorEx.PreApplicationStartMethod(
    typeof(JsonMap.App_Start.JsonMapBootstrapper), "PreStart")]

namespace JsonMap.App_Start
{
    public static class JsonMapBootstrapper
    {
        private static IEnumerable<IJsonMap> PreLoadedEntities;

        public static void PreStart()
        {
            if (((System.Web.Configuration.CompilationSection)ConfigurationManager.GetSection("system.web/compilation")).Debug)
            {
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings { Formatting = Formatting.Indented, ReferenceLoopHandling = ReferenceLoopHandling.Serialize, PreserveReferencesHandling = PreserveReferencesHandling.Objects };
            }
            else
            {
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings { Formatting = Formatting.None, ReferenceLoopHandling = ReferenceLoopHandling.Serialize, PreserveReferencesHandling = PreserveReferencesHandling.Objects };
            }

            DependencyResolver.Current.GetService<IJsonMapQueryer>();
            if (JTransformer.Queryer == null)
            {
                JTransformer.Queryer = DefaultJsonMapQueryer.Instance;
            }
            PreLoadedEntities = JTransformer.Queryer.GetList();

            if (!((System.Web.Configuration.CompilationSection)ConfigurationManager.GetSection("system.web/compilation")).Debug)
            {
                var thread = new Thread(new ThreadStart(() => DoWork()));
                thread.Start();
            }
        }

        private static void DoWork()
        {
            Parallel.ForEach(PreLoadedEntities, jsonMap =>
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
                    JTransformer.ToJToken(obj, new ToJOptions { mapType = (jsonMap as IEntity).JsonMapEnum });
                }
            });
        }
    }
}