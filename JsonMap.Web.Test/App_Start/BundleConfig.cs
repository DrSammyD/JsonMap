using System.Web.Optimization;
using BundleTransformer.Core.Transformers;

namespace JsonMap.Web.Test
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
                "~/Scripts/Coffee/ViewModelPrototypes/JsonMapViewModel.coffee",
                "~/Scripts/Coffee/AppViewModelPrototypes/EditJsonMapAppViewModel.coffee"));

            bundles.Add(new ScriptBundle("~/bundles/viewmodel").Include(
                "~/Scripts/Coffee/ViewModelPrototypes/ViewModel.coffee",
                "~/Scripts/Coffee/KOBindings/ko.selectvalue.coffee",
                "~/Scripts/Coffee/KOBindings/ko.jsonrevealmodal.coffee",
                "~/Scripts/Coffee/Utils/ko.asynchArray.coffee",
                "~/Scripts/Coffee/KOBindings/ko.enableClick.coffee"));

            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));
            bundles.Add(new StyleBundle("~/bundles/foundation").Include(
                "~/Content/jsonMap.css"));

            var jsTransformer = new JsTransformer();
            foreach (var bundle in bundles)
            {
                if (bundle is ScriptBundle) { bundle.Transforms.Add(jsTransformer); }
            }
        }
    }
}