using System.Configuration;
using System.Web.Configuration;
using System.Web.Mvc;
using JsonMap.Utils;
using JsonMap.Web;

[assembly: WebActivatorEx.PostApplicationStartMethod(
    typeof(JsonMapBootstrapper), "PostStart")]

namespace JsonMap.Web
{
    public static class JsonMapBootstrapper
    {
        public static void PostStart()
        {
            bool debug = ((CompilationSection) ConfigurationManager.GetSection("system.web/compilation")).Debug;
            JsonMapStartup.Start(DependencyResolver.Current, debug);
        }
    }
}