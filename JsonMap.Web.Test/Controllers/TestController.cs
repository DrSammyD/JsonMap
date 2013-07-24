using System.Collections.Generic;
using System.Web.Mvc;
using JsonMap.Default;
using JsonMap.Web.Test.Models;

namespace JsonMap.Web.Test.Controllers
{
    public class TestController : Controller
    {
        //
        // GET: /Test/

        public ActionResult Morph()
        {
            var t1 = new TestFirst { SubModel = new List<ITest> { new TestSecond { Number = 7, ExtraWord = "ExtraAwesome", Word = "SuperAwesome" }, new TestFirst { Number = 5, ExtraNumber = 6, Word = "InsideAwesome" } }, Number = 1, ExtraNumber = 2, Word = "Awesome" };
            var t2 = new TestSecond { SubModel = new List<ITest> { new TestSecond { Number = 8, ExtraWord = "WickedAwesome", Word = "FreakyAwesome" }, new TestFirst { Number = 9, ExtraNumber = 10, Word = "InsideAwesome" } }, Number = 4, ExtraWord = "AlsoAwesome", Word = "ReallyAwesome" };
            var vmList = new List<BaseViewModel>{
                new ViewModel{ jOpts = new ToJOptions(), obj = t1, CustomPropName="Test1ViewModel"},
                new ViewModel{ jOpts = new ToJOptions{ mapType = DefaultJsonMapEnum.Edit}, obj = t2, CustomPropName="Test2ViewModel"}
            };
            ViewBag.JSON = BaseViewModel.CreateAppViewModel(vmList);
            return View();
        }

        public ActionResult Recursion()
        {
            var b = new Bro { BroId = 1, Bros = new List<Bro> { new Bro { BroId = 2 } }, Name = "Broseph Stalin" };
            b.Bros[0].Bros.Add(b);
            b.BestBro = b;

            var vmList = new List<BaseViewModel>{
                new ViewModel{obj = b, CustomPropName="BroViewModel"}
            };
            ViewBag.JSON = BaseViewModel.CreateAppViewModel(vmList);
            return View("Morph");
        }
    }
}