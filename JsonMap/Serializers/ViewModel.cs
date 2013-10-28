/* Copyright 2013 MacReport Media Publishing Inc.
 * Licensed under MPL-2.0 (see /LICENSE)
 * If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 *
 * Author: Sam Armstrong
 */

using JsonMap.Default;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace JsonMap
{
    public abstract class BaseViewModel
    {
        public String CustomVM;
        public String CustomPropName;

        public BaseViewModel()
        {
            CustomVM = "ViewModel";
            CustomPropName = "";
        }

        public abstract void AddVM(JObject appVM);

        public static JToken CreateAppViewModel(IEnumerable<BaseViewModel> VMs)
        {
            JObject appVM = new JObject{
                new JProperty(
                    "_stubs",
                    new JObject{
                        new JProperty(
                            "_classes",
                            new JObject()
                        )
                    }
                ),
                new JProperty(
                    "_vmType",
                    new JObject()
                    )
            };
            foreach (var ivm in VMs)
            {
                ivm.AddVM(appVM);
            }
            return appVM;
        }

        public static String CreateAppViewModelString(IEnumerable<BaseViewModel> VMs)
        {
            return CreateAppViewModel(VMs).ToString(Newtonsoft.Json.JsonConvert.DefaultSettings.Invoke().Formatting, null);
        }
    }

    public class ViewModel : BaseViewModel
    {
        public Object obj;
        public ToJOptions jOpts;

        public ViewModel()
        {
            CustomVM = "";
            obj = null;
            jOpts = new ToJOptions();
        }

        public override void AddVM(JObject appVM)
        {
            JToken jVM = JTransformer.ToJToken(this.obj, this.jOpts);
            if (this.CustomVM == "") { this.CustomVM = this.jOpts.classJProperty.Value.ToString(); }
            if (this.CustomVM == "") { this.CustomVM = "ViewModel"; }
            if (this.CustomPropName == "") { this.CustomPropName = this.jOpts.classJProperty.Name; }
            (appVM as JObject).Add(new JProperty(this.CustomPropName, jVM));
            (appVM["_stubs"] as JObject).Add(new JProperty(this.CustomPropName, this.jOpts.stubJToken));
            (appVM["_stubs"]["_classes"] as JObject).Add(new JProperty(this.CustomPropName, this.CustomVM));
            (appVM["_vmType"] as JObject).Add(new JProperty(this.CustomPropName, this.jOpts.topLevelObject.Name));
        }
    }

    public class SubViewModel : BaseViewModel
    {
        public List<BaseViewModel> SubVMs;

        public SubViewModel()
        {
            CustomVM = "AppViewModel";
            SubVMs = null;
        }

        public override void AddVM(JObject appVM)
        {
            appVM.Add(new JProperty(this.CustomPropName, CreateAppViewModel(this.SubVMs)));
        }
    }

    public class CollectionViewModel : BaseViewModel
    {
        public IEnumerable objs;
        public ToJOptions jOpts;
        public String CustomTopVMs;
        public String CustomTopProperty;

        public CollectionViewModel()
        {
            jOpts = new ToJOptions();
            CustomTopVMs = "";
            CustomPropName = "VMCollection";
            CustomTopProperty = "";
        }

        public override void AddVM(JObject appVM)
        {
            JToken jVM = JTransformer.ToJToken(this.objs, this.jOpts);
            var jsType = DefaultJSTypeEnum.Dictionary;
            if (this.jOpts.topLevelObject == typeof(IList)) jsType = DefaultJSTypeEnum.ObservableArray;
            this.jOpts.topLevelObject = typeof(Object);
            if (this.CustomVM == "") { this.CustomVM = this.jOpts.classJProperty.Value.ToString(); }
            if (this.CustomVM == "") { this.CustomVM = "ViewModel"; }
            if (this.CustomPropName == "") { this.CustomPropName = this.jOpts.classJProperty.Name; }
            jVM = new JObject { new JProperty(this.CustomPropName, jVM) };
            this.jOpts.stubJToken = new JObject{
                        new JProperty(this.CustomPropName, this.jOpts.stubJToken),
                        new JProperty("_classes", new JObject{new JProperty(this.CustomPropName, this.CustomVM)}),
                        new JProperty("_jsTypes", new JObject{new JProperty(this.CustomPropName, jsType)})};
            (appVM as JObject).Add(new JProperty(this.CustomTopProperty, jVM));
            (appVM["_stubs"] as JObject).Add(new JProperty(this.CustomTopProperty, this.jOpts.stubJToken));
            (appVM["_stubs"]["_classes"] as JObject).Add(new JProperty(this.CustomTopProperty, this.CustomTopVMs));
            (appVM["_vmType"] as JObject).Add(new JProperty(this.CustomTopProperty, this.jOpts.topLevelObject.Name));
        }
    }
}