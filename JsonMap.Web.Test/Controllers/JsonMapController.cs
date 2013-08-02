using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using JsonMap.Default;
using JsonMap.Form;
using JsonMap.Form.Types;
using JsonMap.Interfaces;
using JsonMap.Interfaces.Extensions;
using Newtonsoft.Json.Linq;

namespace JsonMap.Web.Test.Controllers
{
    public class JsonMapController : Controller
    {
        public ActionResult Edit()
        {
            var Entitys = JTransformer.Queryer.GetList().Where(
                x => x.Name.Split('.').Length > 1 &&
                    JTransformer.Queryer.MappingNamespaces.Any(
                        z => x.Name.Contains(z)
                    )
                ).OrderBy(x => x.Name);
            var Attributes = JTransformer.Queryer.GetList(Entitys).Cast<IAttribute>();
            var Validations = JTransformer.Queryer.GetList(Attributes).Cast<IValidation>();
            var Arguments = JTransformer.Queryer.GetList(Validations).Cast<IArgument>();
            var jsonMaps = new JsonMaps
            {
                Entitys = Entitys.ToArray(),
                Arguments = Arguments.ToArray(),
                Validations = Validations.ToArray(),
                Attributes = Attributes.ToArray()
            };
            var vmList = new List<BaseViewModel> {
                new ViewModel {
                    obj = jsonMaps,
                    CustomPropName = "JsonMapsViewModel",
                    CustomVM= "JsonMapsViewModel",
                    jOpts = new ToJOptions {
                        mapType = DefaultJsonMapEnum.FormHelper
                    }
                },
                new SubViewModel {
                    CustomVM = "FormViewModel",
                    CustomPropName = "FormViewModel",
                    SubVMs =  new List<BaseViewModel>{
                        JsonMapForm.GetTypeAttributeLists(),
                        JsonMapForm.GetValidationMethodList(),
                        JsonMapForm.GetJsonMapTypeEnumList(),
                        JsonMapForm.GetJSTypeEnumList(),
                        JsonMapForm.GetSubTypeList()
                    }
                }
            };
            ViewBag.JSON = BaseViewModel.CreateAppViewModelString(vmList);
            return View();
        }

        [HttpPost]
        public ActionResult EntityCreate(String json)
        {
            var CreationJObj = JObject.Parse(Request.Form[0]);
            try
            {
                typeof(IEntity).CreateValidationToken(CreationJObj, new JValidOptions { mapType = JTransformer.Queryer.DefaultMap, throwError = true });
                var jsonMap = JTransformer.FromJObject<IEntity>(CreationJObj);
                return Content(new JObject { new JProperty("Id", JTransformer.Queryer.Create(0, jsonMap)) }.ToString());
            }
            catch (JValidationException ve)
            {
                Response.StatusCode = 403;
                return Content(ve.GetJMessage().ToString());
            }
        }

        [HttpPost]
        public ActionResult EntityUpdate(String json)
        {
            var CreationJObj = JObject.Parse(Request.Form[0]);
            try
            {
                typeof(IEntity).CreateValidationToken(CreationJObj, new JValidOptions { mapType = JTransformer.Queryer.DefaultMap, throwError = true });
                var jsonMap = JTransformer.FromJObject<IEntity>(CreationJObj);
                JTransformer.Queryer.Update(jsonMap);
            }
            catch (JValidationException ve)
            {
                Response.StatusCode = 403;
                return Content(ve.GetJMessage().ToString());
            }
            return Content("Success");
        }

        [HttpPost]
        public ActionResult EntityDelete(string json)
        {
            var DeletionId = int.Parse(Url.RequestContext.RouteData.Values["id"].ToString());
            JTransformer.Queryer.Delete(DeletionId, MapType.Entity);
            JStubs.ClearCache();
            return Content("Success");
        }

        [HttpPost]
        public ActionResult AttributeCreate(String json)
        {
            var parentId = long.Parse(Url.RequestContext.RouteData.Values["id"].ToString());
            var CreationJObj = JObject.Parse(Request.Form[0]);
            try
            {
                typeof(IAttribute).CreateValidationToken(CreationJObj, new JValidOptions { mapType = JTransformer.Queryer.DefaultMap, throwError = true });
                var jsonMap = JTransformer.FromJObject<IAttribute>(CreationJObj);
                return Content(new JObject { new JProperty("Id", JTransformer.Queryer.Create(parentId, jsonMap)) }.ToString());
            }
            catch (JValidationException ve)
            {
                Response.StatusCode = 403;
                return Content(ve.GetJMessage().ToString());
            }
        }

        [HttpPost]
        public ActionResult AttributeUpdate(String json)
        {
            var CreationJObj = JObject.Parse(Request.Form[0]);
            try
            {
                typeof(IAttribute).CreateValidationToken(CreationJObj, new JValidOptions { mapType = JTransformer.Queryer.DefaultMap, throwError = true });
                var jsonMap = JTransformer.FromJObject<IAttribute>(CreationJObj);
                JTransformer.Queryer.Update(jsonMap);
            }
            catch (JValidationException ve)
            {
                Response.StatusCode = 403;
                return Content(ve.GetJMessage().ToString());
            }
            return Content("Success");
        }

        [HttpPost]
        public ActionResult AttributeDelete(string json)
        {
            var DeletionId = int.Parse(Url.RequestContext.RouteData.Values["id"].ToString());
            JTransformer.Queryer.Delete(DeletionId, MapType.Attribute);
            JStubs.ClearCache();
            return Content("Success");
        }

        [HttpPost]
        public ActionResult ValidationCreate(String json)
        {
            var parentId = long.Parse(Url.RequestContext.RouteData.Values["id"].ToString());
            var CreationJObj = JObject.Parse(Request.Form[0]);
            try
            {
                var jsonMap = JTransformer.FromJObject<IValidation>(CreationJObj);
                return Content(new JObject { new JProperty("Id", JTransformer.Queryer.Create(parentId, jsonMap)) }.ToString());
            }
            catch (JValidationException ve)
            {
                Response.StatusCode = 403;
                return Content(ve.GetJMessage().ToString());
            }
        }

        [HttpPost]
        public ActionResult ValidationUpdate(String json)
        {
            var CreationJObj = JObject.Parse(Request.Form[0]);
            try
            {
                typeof(IValidation).CreateValidationToken(CreationJObj, new JValidOptions { mapType = JTransformer.Queryer.DefaultMap, throwError = true });
                var jsonMap = JTransformer.FromJObject<IValidation>(CreationJObj);
                JTransformer.Queryer.Update(jsonMap);
            }
            catch (JValidationException ve)
            {
                Response.StatusCode = 403;
                return Content(ve.GetJMessage().ToString());
            }
            return Content("Success");
        }

        [HttpPost]
        public ActionResult ValidationDelete(string json)
        {
            var DeletionId = int.Parse(Url.RequestContext.RouteData.Values["id"].ToString());
            JTransformer.Queryer.Delete(DeletionId, MapType.Validation);
            JStubs.ClearCache();
            return Content("Success");
        }

        [HttpPost]
        public ActionResult ArgumentCreate(String json)
        {
            var parentId = long.Parse(Url.RequestContext.RouteData.Values["id"].ToString());
            var CreationJObj = JObject.Parse(Request.Form[0]);
            try
            {
                typeof(IArgument).CreateValidationToken(CreationJObj, new JValidOptions { mapType = JTransformer.Queryer.DefaultMap, throwError = true });
                var jsonMap = JTransformer.FromJObject<IArgument>(CreationJObj);
                return Content(new JObject { new JProperty("Id", JTransformer.Queryer.Create(parentId, jsonMap)) }.ToString());
            }
            catch (JValidationException ve)
            {
                Response.StatusCode = 403;
                return Content(ve.GetJMessage().ToString());
            }
        }

        [HttpPost]
        public ActionResult ArgumentUpdate(String json)
        {
            var CreationJObj = JObject.Parse(Request.Form[0]);
            try
            {
                typeof(IArgument).CreateValidationToken(CreationJObj, new JValidOptions { mapType = JTransformer.Queryer.DefaultMap, throwError = true });
                var jsonMap = JTransformer.FromJObject<IArgument>(CreationJObj);
                JTransformer.Queryer.Update(jsonMap);
            }
            catch (JValidationException ve)
            {
                Response.StatusCode = 403;
                return Content(ve.GetJMessage().ToString());
            }
            return Content("Success");
        }

        [HttpPost]
        public ActionResult ArgumentDelete(string json)
        {
            var DeletionId = int.Parse(Url.RequestContext.RouteData.Values["id"].ToString());
            JTransformer.Queryer.Delete(DeletionId, MapType.Argument);
            JStubs.ClearCache();
            return Content("Success");
        }

        public ActionResult SaveMaps()
        {
            if (JTransformer.Queryer is DefaultJsonMapQueryer) (JTransformer.Queryer as DefaultJsonMapQueryer).SaveJsonMap();
            return Content("Saved JsonMap");
        }
    }
}