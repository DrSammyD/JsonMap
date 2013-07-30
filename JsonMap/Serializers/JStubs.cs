/* Copyright 2013 MacReport Media Publishing Inc.
 * Licensed under MPL-2.0 (see /LICENSE)
 * If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * 
 * Author: Sam Armstrong
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using JsonMap.Interfaces;
using JsonMap.Interfaces.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonMap
{
    public static class JStubs
    {
        private static IEnumerable<Type> _loadedTypes;

        public static IEnumerable<Type> LoadedTypes
        {
            get { return JStubs._loadedTypes; }
        }

        public static Type GetType(String typeName)
        {
            Type type = LoadedTypes.FirstOrDefault(x => x.FullName == typeName);
            return type;
        }

        internal static bool Impliments(this Type type, Type inheritedType)
        {
            return (type.IsSubclassOf(inheritedType) || type.GetInterface(inheritedType.FullName) != null);
        }

        private static Dictionary<String, JObject> StubCache;

        static JStubs()
        {
            _loadedTypes = AppDomain.CurrentDomain.GetAssemblies().Where(x => JTransformer.Queryer.MappingAssembies.Contains(x.GetName().Name)).SelectMany(x => x.GetTypes()).Distinct();
            StubCache = new Dictionary<string, JObject>();
        }

        public static void ClearCache()
        {
            StubCache.Clear();
        }

        private static Dictionary<String, Object> dictSemaphore = new Dictionary<String, Object>();

        private static void StartSemaphore(String dictKey)
        {
            object mut = new object();
            try
            {
                dictSemaphore.Add(dictKey, mut);
            }
            catch (Exception e)
            {
            }
            finally
            {
                mut = dictSemaphore[dictKey];
            }
            try
            {
                Monitor.Enter(mut);
            }
            catch (Exception e)
            {
            }
        }

        private static void EndSemaphore(String dictKey)
        {
            try
            {
                Monitor.Exit(dictSemaphore[dictKey]);
            }
            catch (Exception e)
            {
            }
        }

        public static JToken GetJStub(this Type objType, ToJOptions options)
        {
            var token = objType.getJStub(options);
            return JToken.Parse(token.ToString());
        }

        private static JToken getJStub(this Type objType, ToJOptions options)
        {
            JToken mapStub;

            Type[] elmTypeInterfaces = objType.GetInterfaces();
            if (objType.GetInterface("IEnumerable") == null)
            {
                string dictKey = objType.FullName + "." + options.mapType.ToString();
                bool exists = !options.IsInStack(dictKey);
                if (!StubCache.ContainsKey(dictKey))
                {
                    StartSemaphore(dictKey);
                    try
                    {
                        if (!StubCache.ContainsKey(dictKey))
                        {
                            JObject valueJObject = new JObject();
                            StubCache.Add(dictKey, valueJObject);
                            var stubProperty = new JProperty("_stub", null);
                            var objProperty = new JProperty("Obj", null);
                            valueJObject.Add(stubProperty);
                            valueJObject.Add(objProperty);

                            options.stubJToken = new JObject();

                            options.PushTypeStack(objType);
                            EndSemaphore(dictKey + "....Wait");
                            mapStub = objType.GetJStubObject(new ToJOptions(options));
                            options.PopTypeStack();
                            foreach (var token in options.GetRecursiveList(dictKey))
                            {
                                JToken keyParent = token.Key;
                                JToken valueParent = token.Value;
                                var reference = 0;
                                while (keyParent != mapStub)
                                {
                                    if (keyParent is JObject) reference++;
                                    keyParent = keyParent.Parent;
                                }
                                token.Key["$ref"] = reference;
                                reference = 0;
                                while (valueParent != options.stubJToken)
                                {
                                    if (valueParent is JObject) reference++;
                                    valueParent = valueParent.Parent;
                                }
                                token.Value["$ref"] = reference;
                            }

                            valueJObject.Add(options.classJProperty);
                            stubProperty.Value = options.stubJToken;
                            objProperty.Value = mapStub;
                        }
                    }
                    finally
                    {
                        EndSemaphore(dictKey);
                    }
                }

                if (options.IsInStack(dictKey))
                {
                    mapStub = new JObject();
                    options.stubJToken = new JObject();
                    options.PushRecursiveDictionary(dictKey, (mapStub as JObject), (options.stubJToken as JObject));
                    options.classJProperty.Value = options.GetStackJProperty(dictKey).Value;
                }
                else
                {
                    StartSemaphore(dictKey);
                    mapStub = StubCache[dictKey]["Obj"];
                    options.stubJToken = StubCache[dictKey]["_stub"];
                    options.classJProperty.Value = StubCache[dictKey].Properties().Last().Value;
                    EndSemaphore(dictKey);
                }
                options.topLevelObject = typeof(Object);
            }
            else
            {
                if (objType.GetInterface("IDictionary") == null)
                {
                    options.stubJToken = new JArray();
                    mapStub = objType.GetJStubArray(options);
                    options.topLevelObject = typeof(IList);
                }
                else
                {
                    options.stubJToken = new JObject { };
                    mapStub = objType.GetJStubDictionary(options);
                    options.topLevelObject = typeof(IDictionary);
                }
            }
            return mapStub;
        }

        public static JObject GetJStubObject(this Type objType, ToJOptions options)
        {
            var jObj = new JObject(); ;

            if (options.stubJToken == null)
            {
                options.stubJToken = new JObject();
            }
            if (options.classJProperty == null)
            {
                options.classJProperty = new JProperty(objType.Name.Split('.').Last(), null);
            }

            //Retrieve EntityJsonMaps for current object
            IEntity baseEntity = JTransformer.Queryer.Get(objType.GetAllTypes().Select(x => x.FullName), options);

            if (baseEntity == null)
            { throw new JsonMapNotFoundException(objType, options); }

            var baseType = JStubs.GetType(baseEntity.Name);
            var entities = JTransformer.Queryer.GetList().Where(x => { var y = JStubs.GetType(x.Name); return x.JsonMapEnum.Equals(options.mapType) && (y.Impliments(baseType)); }).ToList();

            jObj = objType.CreateStub(baseEntity, options);
            JObject tmpstub = options.stubJToken as JObject;
            JProperty tmpclass = options.classJProperty;

            JObject subTypes = new JObject();
            jObj.Add(new JProperty("_subTypes", subTypes));

            JObject subStub = new JObject();
            tmpstub.Add(new JProperty("_subStubs", subStub));

            var abstractBase = baseEntity.AbstractDefault != "" ? baseEntity.AbstractDefault : objType.FullName + "_" + objType.Assembly.GetName().Name;
            JValue defaultType = new JValue(abstractBase);
            JObject subClasses = new JObject();
            subClasses.Add(new JProperty("_base", tmpclass.Value));
            subClasses.Add(new JProperty("_default", defaultType));

            var subclasses = AppDomain.CurrentDomain.GetAssemblies().Where(x => JTransformer.Queryer.MappingAssembies.Contains(x.GetName().Name)).SelectMany(x => x.GetTypes()).Distinct().Where(y => y.Impliments(baseType) && !y.IsGenericType).ToArray();
            foreach (var subClass in subclasses)
            {
                subClasses[subClass.FullName + "_" + subClass.Assembly.GetName().Name] = tmpclass.Value.DeepClone();
            };
            if (subclasses.Length > 0)
            {
                jObj.Add(new JProperty("$type", ""));
                tmpstub["_jsTypes"]["$type"] = new JValue(Enum.Parse(JTransformer.Queryer.Primitive.GetType(), JTransformer.Queryer.Primitive.ToString()));
            }
            tmpclass.Value = subClasses;
            foreach (var entity in entities)
            {
                options.stubJToken = new JObject();
                subTypes.Add(new JProperty(entity.Name, objType.CreateStub(entity, options)));
                subClasses[entity.Name] = options.classJProperty.Value;
                subStub.Add(new JProperty(entity.Name, options.stubJToken));
            }

            options.stubJToken = tmpstub;
            options.classJProperty = tmpclass;

            return jObj;
        }

        public static JArray GetJStubArray(this Type objType, ToJOptions options)
        {
            if (options == null)
            {
                options = new ToJOptions();
            }
            var jArr = new JArray();
            var istop = false;
            if (options.stubJToken == null)
            {
                options.stubJToken = new JArray();
                istop = true;
            }
            if (options.classJProperty == null)
            {
                options.classJProperty = new JProperty(objType.Name.Split('.').Last().Replace("[]", "s"), null);
            }

            Type elmType = objType.GetElementOrFirstGenericArgumentType();
            if (!elmType.IsJPrimitive())
            {
                jArr.Add(elmType.getJStub(options));
            }
            return jArr;
        }

        public static JArray GetJStubDictionary(this Type objType, ToJOptions options)
        {
            var keyType = objType.GetGenericArguments()[0];
            var valueType = objType.GetGenericArguments()[1];

            if (!keyType.IsJPrimitive())
            {
                throw new Exception("Key type " + keyType + " is not a primative.");
            }

            if (options == null)
            {
                options = new ToJOptions();
            }
            var jArr = new JArray();
            var istop = false;
            if (options.stubJToken == null)
            {
                options.stubJToken = new JObject();
                istop = true;
            }
            if (options.classJProperty == null)
            {
                options.classJProperty = new JProperty("Value", null);
            }
            var tempClassJProp = options.classJProperty;
            JObject tempStubJVar = options.stubJToken as JObject;
            JObject dictMapVar = new JObject();
            JObject stubDictObj = new JObject();

            JObject classes = new JObject();
            JObject jsTypes = new JObject();

            tempStubJVar.Add(new JProperty("this", stubDictObj));
            stubDictObj.Add(new JProperty("_classes", classes));
            stubDictObj.Add(new JProperty("_jsTypes", jsTypes));

            dictMapVar.Add(new JProperty("Key", ""));

            Enum type = JTransformer.Queryer.Primitive;
            if (!valueType.IsJPrimitive())
            {
                JToken valueMapStub = valueType.getJStub(options);
                dictMapVar.Add(new JProperty("Value", valueMapStub));
                stubDictObj.Add(new JProperty("Value", options.stubJToken));
                classes.Add(new JProperty("Value", options.classJProperty.Value));

                switch (options.topLevelObject.Name)
                {
                    case "IList":
                        type = JTransformer.Queryer.List;
                        break;

                    default:
                        type = JTransformer.Queryer.ViewModel;
                        break;
                }
            }
            else
            {
                stubDictObj.Add(new JProperty("Value", new JValue("")));
                dictMapVar.Add(new JProperty("Value", new JValue("")));
            }
            jsTypes.Add(new JProperty("Value", new JValue(type)));

            jArr.Add(dictMapVar);

            options.classJProperty = tempClassJProp;
            options.stubJToken = tempStubJVar;
            return jArr;
        }

        private static JObject CreateStub(this Type objType, IEntity entity, ToJOptions options, IEntity genericEntity = null)
        {
            if (genericEntity == null) genericEntity = entity;
            var tempClassJProp = options.classJProperty; var tempJsTypesJObj = new JObject(); var jObj = new JObject(); var jValid = new JObject();
            var tempClassesJObj = new JObject();
            var tempStubJVar = options.stubJToken as JObject;

            tempStubJVar.Add(new JProperty("_classes", tempClassesJObj));
            tempStubJVar.Add(new JProperty("_jsTypes", tempJsTypesJObj));
            if (entity != null)
            {
                tempClassJProp.Value = (entity.JSClass == null || entity.JSClass == "") ? "ViewModel" : entity.JSClass;

                foreach (var key in genericEntity.Attributes.Distinct((x, y) => x.Name == y.Name))
                {
                    PropertyInfo propertyInfo = objType.GetAllProperties().FirstOrDefault(x => x.Name == key.Name);
                    if (!key.SubJsonMapEnum.Equals(JTransformer.Queryer.InheritedMap))
                    {
                        options.mapType = key.SubJsonMapEnum;
                    }
                    if (propertyInfo != null)
                    {
                        tempJsTypesJObj.Add(new JProperty(key.Name, (Enum)key.JSType));
                        var subType = propertyInfo.PropertyType;
                        JObject stubJToken = options.stubJToken as JObject;
                        if (subType.IsJPrimitive())
                        {
                            jObj.Add(new JProperty(key.Name, null));
                        }
                        else
                        {
                            options.classJProperty = new JProperty(key.Name, null);
                            tempClassesJObj.Add(options.classJProperty);
                            jObj.Add(new JProperty(key.Name, subType.getJStub(options)));
                            tempStubJVar.Add(new JProperty(key.Name, options.stubJToken));
                        }
                    }

                    var vJObj = new JObject();
                    foreach (var val in key.Validations.Distinct((x, y) => x.Name == y.Name))
                    {
                        var vAJObj = new JArray();
                        foreach (var valArg in val.Arguments)
                        {
                            vAJObj.Add(valArg.Path);
                        }
                        vJObj.Add(new JProperty(val.Name, new JObject { new JProperty("args", vAJObj), new JProperty("step", val.Step) }));
                    }
                    if (vJObj.HasValues) jValid.Add(key.Name, vJObj);
                    options.mapType = entity.JsonMapEnum;
                }
            }

            if (jValid.HasValues)
            {
                tempStubJVar.Add(new JProperty("_validations", jValid));
            }
            options.stubJToken = tempStubJVar;
            options.classJProperty = tempClassJProp;
            return jObj;
        }

        public static bool HasParent(this JToken token, JToken parentToken)
        {
            while (token != parentToken && token != null)
            {
                if (token == parentToken) return true;
                token = token.Parent;
            }
            return false;
        }
    }

    public class JsonMapNotFoundException : Exception
    {
        private String _message;

        public JsonMapNotFoundException(Type objType, JOptions options)
        {
            _message = "A JsonMap with the type of " + objType.FullName + " and a map type of " + options.mapType.ToString() + " was not found.";
        }

        public override string Message
        {
            get
            {
                return _message;
            }
        }
    }
}