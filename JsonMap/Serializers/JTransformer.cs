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
using JsonMap.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonMap
{
    public static class JTransformer
    {
        public static IJsonMapQueryer Queryer;

        static JTransformer()
        {
        }

        private static JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };

        /// <summary>
        /// Returns an object created from the jObject and placed in a stub object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jObj"></param>
        /// <returns></returns>
        public static T FromJObject<T>(JToken jObj)
        {
            if (jObj == null)
            {
                jObj = new JObject();
            }
            if (jObj is JValue)
            {
                return (T)((JValue)jObj).Value;
            }
            else
            {
                jObj = jObj.ReorderJToken();
                return (T)JsonConvert.DeserializeObject<T>(jObj.ToString(), settings);
            }
        }

        private static JToken ReorderJToken(this JToken jTok)
        {
            if (jTok is JArray)
            {
                var jArr = new JArray();
                foreach (var token in jTok as JArray)
                {
                    jArr.Add(token.ReorderJToken());
                }
                return jArr;
            }
            else if (jTok is JObject)
            {
                var jObj = new JObject();
                foreach (var prop in (jTok as JObject).Properties().OrderBy(x => x.Name))
                {
                    prop.Value = prop.Value.ReorderJToken();
                    jObj.Add(prop);
                }
                return jObj;
            }
            return jTok;
        }

        public static Object FromJObject(Type objType, JToken jObj)
        {
            if (jObj != null)
            {
                if (jObj is JValue)
                    return ((JValue)jObj).Value;
                else
                {
                    return JsonConvert.DeserializeObject(jObj.ToString(), objType);
                }
            }
            return null;
        }

        public static bool IsJPrimitive(this Type type)
        {
            var t = type;
            if (t.IsGenericType &&
                t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                t = t.GetGenericArguments()[0];
            }
            return (t.IsPrimitive
                || t == typeof(Decimal)
                || t == typeof(String)
                || t == typeof(float)
                || t == typeof(Double)
                || t == typeof(Enum)
                || t == typeof(LazyEnum)
                || t.IsSubclassOf(typeof(Enum))
                || t.IsSubclassOf(typeof(LazyEnum))
                || t == typeof(DateTime)
                || t == typeof(Type)
                || t == typeof(Byte[]));
        }

        /// <summary>
        /// Returns all base and inherited types of a type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetAllTypes(this Type type)
        {
            IEnumerable<Type> types = new List<Type>();
            types = types.Concat(type.GetInterfaces());
            while (type != null)
            {
                types = types.Concat(new Type[] { type });
                type = type.BaseType;
            }
            return types.Distinct();
        }

        /// <summary>
        /// Get's all properties of a type including inherited properties
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<PropertyInfo> GetAllProperties(this Type type)
        {
            IEnumerable<PropertyInfo> properties = new List<PropertyInfo>();
            foreach (var iFace in type.GetInterfaces())
            {
                properties = properties.Concat(iFace.GetAllProperties());
            }
            properties = properties.Concat(type.GetProperties());
            while (type.BaseType != null)
            {
                type = type.BaseType;
                properties = properties.Concat(type.GetProperties());
            }
            return properties.Distinct();
        }

        /// <summary>
        /// Returns element type or the generic argument for those elements
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Type GetElementOrFirstGenericArgumentType(this Type t)
        {
            return (t.GetElementType() != null) ? t.GetElementType() : t.GetGenericArguments()[0];
        }

        /// <summary>
        /// Returns a JToken with the shape of the jsonmap for that type and jsonmap enum
        /// </summary>
        /// <param name="elm"></param>
        /// <param name="options">The options object used for determining what jsonmap should be used</param>
        /// <returns></returns>
        public static JToken ToJToken(this Object elm, ToJOptions options = null)
        {
            options = options ?? new ToJOptions();
            var jmap = elm.GetType().GetJStub(options);
            MergeObjPull(jmap, elm);
            return jmap;
        }

        /// <summary>
        /// Creates a JObject representation of an object, using a JsonMapType, that determines through the database which values
        /// of each object will be sent to and from the client
        /// </summary>
        /// <param name="obj">The object to be converted</param>
        /// <param name="options.mapType">The JSonMapType that determines what values in the object will be added</param>
        /// <param name="options.postUrl">A string to determine which controller the client ViewModel will interact with for all purposes</param>
        /// <returns></returns>
        ///
        public static void MergeObjPull(JToken receiver, Object donor)
        {
            var stack = new MergeStack();
            MergeObjPull(receiver, donor, stack);
            stack.SetId();
        }

        internal static void MergeObjPull(JToken receiver, Object donor, MergeStack stack)
        {
            if (receiver is JArray)
            {
                MergeObjPullArray(receiver as JArray, donor as IEnumerable, stack);
            }
            else
            {
                MergeObjPullObject(receiver as JObject, donor as Object, stack);
            }
        }

        internal static void MergeObjPullObject(JObject receiver, Object donor, MergeStack stack)
        {
            JValue reff = (JValue)receiver.SelectToken("$ref", false);
            if (reff != null)
            {
                receiver.RemoveAll();
                receiver.Add(stack.GetStub(reff.ToObject<long>()).Properties());
            }

            stack.PushStubObj(JObject.Parse(receiver.ToString()));

            if (donor != null && stack.MergeObjStack.Any(x => Object.ReferenceEquals(x.Key, donor)))
            {
                receiver.RemoveAll();
                receiver["$ref"] = stack.GetId(donor);
            }
            else
            {
                if (receiver != null && receiver.Property("_subTypes") != null)
                {
                    var subStubs = receiver["_subTypes"].Cast<KeyValuePair<string, JToken>>();
                    var subStub = subStubs.Where(y => y.Key.Contains(donor.GetType().FullName)).Select(x => x.Value).FirstOrDefault();
                    receiver.Remove("_subTypes");
                    if (subStub != null)
                    {
                        receiver.RemoveAll();
                        foreach (var property in (subStub as JObject).Properties())
                        {
                            receiver.Add(property);
                        }
                    }
                }
                if (receiver != null && donor != null)
                {
                    stack.MergeObjStack.Add(new KeyValuePair<Object, JObject>(donor, receiver));
                    foreach (var property in receiver)
                    {
                        var donorProp = donor.GetType().GetProperty(property.Key);
                        if (property.Key == "$type")
                        {
                            receiver["$type"] = new JValue(donor.GetType().FullName + ", " + donor.GetType().Assembly.GetName().Name);
                        }
                        else
                        {
                            object donorPropValue = donorProp.GetValue(donor, null);
                            if (property.Value is JValue)
                            {
                                if (donorProp.PropertyType.IsJPrimitive() || (donorPropValue != null && donorPropValue.GetType().IsJPrimitive()))
                                {
                                    var val = donorPropValue;
                                    val = (val is LazyEnum) ? (val as LazyEnum).Value : val;
                                    receiver[property.Key] = new JValue(val);
                                }
                            }
                            else
                            {
                                if (donorPropValue != null) MergeObjPull(receiver[property.Key], donorPropValue, stack);
                                else receiver[property.Key] = null;
                            }
                        }
                    }
                    stack.PopStubObj();
                }
                else if (receiver != null)
                {
                    foreach (var property in receiver)
                    {
                        if (property.Key == "$type")
                        {
                            receiver["$type"] = null;
                        }
                        else if (property.Value is JValue)
                        {
                            receiver[property.Key] = null;
                        }
                        else
                        {
                            MergeObjPull(receiver[property.Key], null, stack);
                        }
                    }
                }
            }
        }

        internal static void MergeObjPullArray(JArray reciever, IEnumerable donor, MergeStack stack)
        {
            if (reciever != null && donor != null)
            {
                JToken baseJObj = reciever.FirstOrDefault();
                reciever.RemoveAll();
                if (baseJObj is JValue || baseJObj == null)
                {
                    foreach (var val in donor)
                    {
                        if (val.GetType().IsJPrimitive())
                        {
                            reciever.Add(val);
                        }
                    }
                }
                else
                {
                    foreach (var val in donor)
                    {
                        JToken copyObj = JToken.Parse(baseJObj.ToString());
                        MergeObjPull(copyObj, val, stack);
                        reciever.Add(copyObj);
                    }
                }
            }
            else
            {
                reciever.RemoveAll();
            }
        }

        public static void MergeTokenPull(this Type type, JToken receiver, JToken donor)
        {
            if (type.Impliments(typeof(IEnumerable)) && !type.IsJPrimitive())
            {
                type.MergeTokenPullJArray(receiver as JArray, donor as JArray);
            }
            else
            {
                type.MergeTokenPullJObject(receiver as JObject, donor as JObject);
            }
        }

        public static void MergeTokenPullJObject(this Type type, JObject receiver, JObject donor)
        {
            if (receiver != null && donor != null)
            {
                foreach (var property in type.GetAllProperties())
                {
                    var donorProp = donor[property.Name];
                    if (donorProp is JValue)
                    {
                        if (property.PropertyType.IsJPrimitive())
                        {
                            receiver[property.Name] = donorProp.DeepClone();
                        }
                    }
                    else
                    {
                        property.PropertyType.MergeTokenPull(receiver[property.Name], donorProp);
                    }
                }
            }
        }

        public static void MergeTokenPullJArray(this Type type, JArray reciever, JArray donor)
        {
            if (donor != null)
            {
                Type baseType = type.GetElementOrFirstGenericArgumentType();
                reciever.RemoveAll();
                if (baseType.IsJPrimitive())
                {
                    foreach (var val in donor)
                    {
                        reciever.Add(val.DeepClone());
                    }
                }
                else
                {
                    foreach (var val in donor)
                    {
                        JToken copyObj;
                        if (baseType is IEnumerable) copyObj = new JArray(); else copyObj = new JObject();
                        baseType.MergeTokenPull(copyObj, val);
                        reciever.Add(copyObj);
                    }
                }
            }
            else
            {
                if (reciever != null)
                {
                    reciever.RemoveAll();
                }
            }
        }
    }

    internal class MergeStack
    {
        private Stack<JObject> _mergeStubTokStack;

        public void PushStubObj(JObject obj)
        {
            _mergeStubTokStack.Push(obj);
        }

        public void PopStubObj()
        {
            _mergeStubTokStack.Pop();
        }

        public JObject GetStub(long height)
        {
            var stackEnum = _mergeStubTokStack.GetEnumerator();
            for (long i = 0; i < height && stackEnum.MoveNext(); i++) { }
            return JObject.Parse(stackEnum.Current.ToString());
        }

        private Dictionary<JToken, JValue> _idMap;

        public JValue GetId(Object donor)
        {
            JToken Id = new JValue(0);
            JValue VId = null;
            if (_idMap.TryGetValue(_mergeObjDict.First(x => Object.ReferenceEquals(donor, x.Key)).Value, out VId))
            {
                Id = VId;
                return Id as JValue;
            }
            else
            {
                Id = _idMap.Select(x => x.Value).OrderBy(x => x.ToObject<long>()).LastOrDefault() ?? new JValue(0);

                Id = new JValue(((long)(Id as JValue).Value) + 1);

                _idMap[_mergeObjDict.First(x => Object.ReferenceEquals(donor, x.Key)).Value] = Id as JValue;
                return Id as JValue;
            }
        }

        public void SetId()
        {
            foreach (var pair in _idMap)
            {
                pair.Key["$id"] = pair.Value;
            }
        }

        public MergeStack()
        {
            _mergeStubTokStack = new Stack<JObject>();
            _mergeObjDict = new List<KeyValuePair<Object, JObject>>();
            _idMap = new Dictionary<JToken, JValue>();
        }

        private List<KeyValuePair<Object, JObject>> _mergeObjDict;

        public List<KeyValuePair<Object, JObject>> MergeObjStack
        {
            get { return _mergeObjDict; }
            set { _mergeObjDict = value; }
        }
    }

    public abstract class JOptions
    {
        public Enum mapType;
    }

    public class ToJOptions : JOptions
    {
        /// <summary>
        /// The Url that the client needs for this object to communicate back with the server
        /// </summary>

        /// <summary>
        /// This is a reference to the top level object in the ToJObject function
        /// </summary>
        internal Type topLevelObject;

        public JToken stubJToken;
        internal JProperty classJProperty;
        private Stack<KeyValuePair<String, JProperty>> _typeStack;
        private Dictionary<String, List<KeyValuePair<JObject, JObject>>> _recursiveDictionary;

        public void PopTypeStack()
        {
            _typeStack.Pop();
        }

        public void PushTypeStack(Type objType)
        {
            _typeStack.Push(new KeyValuePair<String, JProperty>(objType.FullName + "." + mapType.ToString(), classJProperty));
        }

        public JProperty GetStackJProperty(String typeStackKey)
        {
            return _typeStack.First(x => x.Key == typeStackKey).Value;
        }

        public bool IsInStack(String typeStackKey)
        {
            return _typeStack.Any(x => x.Key == typeStackKey);
        }

        public void PushRecursiveDictionary(String key, JObject main, JObject stub)
        {
            var value = new KeyValuePair<JObject, JObject>(main, stub);
            if (_recursiveDictionary.ContainsKey(key))
            {
                _recursiveDictionary[key].Add(value);
            }
            else
            {
                _recursiveDictionary[key] = new List<KeyValuePair<JObject, JObject>> { value };
            }
        }

        public IEnumerable<KeyValuePair<JObject, JObject>> GetRecursiveList(String key)
        {
            if (_recursiveDictionary.ContainsKey(key))
            {
                return _recursiveDictionary[key];
            }
            else
            {
                return Enumerable.Empty<KeyValuePair<JObject, JObject>>();
            }
        }

        public ToJOptions()
        {
            _recursiveDictionary = new Dictionary<string, List<KeyValuePair<JObject, JObject>>>();
            _typeStack = new Stack<KeyValuePair<String, JProperty>>();
            mapType = JTransformer.Queryer.DefaultMap;
            topLevelObject = null;
            stubJToken = null;
            classJProperty = new JProperty("class", null);
        }

        public ToJOptions(ToJOptions options)
        {
            this._recursiveDictionary = options._recursiveDictionary;
            this._typeStack = options._typeStack;
            this.mapType = options.mapType;
            this.topLevelObject = options.topLevelObject;
            this.stubJToken = options.stubJToken;
            this.classJProperty = options.classJProperty;
        }
    }
}