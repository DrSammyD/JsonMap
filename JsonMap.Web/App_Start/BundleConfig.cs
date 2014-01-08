using System.Web.Optimization;

namespace JsonMap.Web
{
    public class BundleConfig
    {
        // For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/dependencies").Include(
                "~/Scripts/lodash.js",
                "~/Scripts/jquery-{version}.js",
                "~/Scripts/knockout-{version}.js",
                "~/Scripts/underscore-ko-{version}.js",
                "~/Scripts/ko.observabledictionary.js",
                "~/Scripts/ko.keyboardshortcuts.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.

            bundles.Add(new ScriptBundle("~/bundles/jsonmapviewmodel").Include(
                "~/Scripts/Coffee/ViewModelPrototypes/JsonMapViewModel.js",
                "~/Scripts/Coffee/AppViewModelPrototypes/EditJsonMapAppViewModel.js"));

            bundles.Add(new ScriptBundle("~/bundles/viewmodel").Include(
                "~/Scripts/Coffee/ViewModelPrototypes/ViewModel.js",
                "~/Scripts/Coffee/KOBindings/ko.selectvalue.js",
                "~/Scripts/Coffee/KOBindings/ko.jsonrevealmodal.js",
                "~/Scripts/Coffee/Utils/ko.asynchArray.js",
                "~/Scripts/Coffee/KOBindings/ko.enableClick.js"));

            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));
            bundles.Add(new StyleBundle("~/bundles/foundation").Include(
                "~/Content/jsonMap.css"));

        }
    }
}