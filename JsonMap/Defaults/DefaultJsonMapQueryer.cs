/* Copyright 2013 MacReport Media Publishing Inc.
 * Licensed under MPL-2.0 (see /LICENSE)
 * If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * 
 * Author: Sam Armstrong
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JsonMap.Default.Types;
using JsonMap.Interfaces;
using JsonMap.Interfaces.Extensions;
using Newtonsoft.Json.Linq;

namespace JsonMap.Default
{
    public enum DefaultJsonMapEnum
    {
        Default = 0,
        FormHelper = 1,
        Signup = 2,
        View = 3,
        Edit = 4,
        Inherited = -1
    }

    public enum DefaultJSTypeEnum
    {
        Primative = 0,
        Object = 1,
        ViewModel = 2,
        Observable = 3,
        ObservableArray = 4,
        Array = 5,
        Dictionary = 6
    }

    public class DefaultJsonMapQueryer : IJsonMapQueryer
    {
        private static readonly Lazy<DefaultJsonMapQueryer> _instance
             = new Lazy<DefaultJsonMapQueryer>(() => new DefaultJsonMapQueryer());

        private string _configuration;
        private List<Type> _validationClasses;
        private List<String> _mappingAssemblies;
        private List<String> _mappingNamespaces;
        private JToken jMap;
        private Enum _defaultMap;
        private Enum _inheritedMap;
        private Enum _list;
        private Enum _primitive;
        private Enum _viewModel;

        private DefaultJsonMapQueryer()
        {
            var jsonMapQueryerConfig = ((JsonMapSection)System.Configuration.ConfigurationManager.GetSection("jsonMapSection")).JsonMapQueryer;
            _configuration = jsonMapQueryerConfig.SavedMapFileName;
            try
            {
                FileStream rr = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "JsonMap." + _configuration + ".json");
            }
            catch (Exception e)
            {
                using (StreamReader r = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("JsonMap.JsonMap.json")))
                {
                    string json = r.ReadToEnd();
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "JsonMap." + _configuration + ".json", json);
                }

                // Create a instance of ResourceWriter and specify the name of the resource file.
            }
            finally
            {
                var json = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "JsonMap." + _configuration + ".json");
                if (json != null)
                {
                    jMap = JToken.Parse(json);
                }
            }

            _defaultMap = (Enum)Enum.Parse(Type.GetType(jsonMapQueryerConfig.JsonMapEnumConfig.EnumType), jsonMapQueryerConfig.JsonMapEnumConfig.DefaultMapEnum.ToString());
            _inheritedMap = (Enum)Enum.Parse(Type.GetType(jsonMapQueryerConfig.JsonMapEnumConfig.EnumType), jsonMapQueryerConfig.JsonMapEnumConfig.InheritedMapEnum.ToString());
            _list = (Enum)Enum.Parse(Type.GetType(jsonMapQueryerConfig.JSTypeEnumConfig.EnumType), jsonMapQueryerConfig.JSTypeEnumConfig.ObservableArrayEnum.ToString());
            _viewModel = (Enum)Enum.Parse(Type.GetType(jsonMapQueryerConfig.JSTypeEnumConfig.EnumType), jsonMapQueryerConfig.JSTypeEnumConfig.ViewModelEnum.ToString());
            _primitive = (Enum)Enum.Parse(Type.GetType(jsonMapQueryerConfig.JSTypeEnumConfig.EnumType), jsonMapQueryerConfig.JSTypeEnumConfig.PrimativeEnum.ToString());

            _validationClasses = jsonMapQueryerConfig.ValidationClasses.OfType<ValidationClass>().Select(x => Type.GetType(x.StaticClass)).ToList();
            _nullCheckValidationMethodName = jsonMapQueryerConfig.ValidationClasses.NotNullMethod;
            _mappingAssemblies = jsonMapQueryerConfig.MappingAssemblies.OfType<MappingAssembly>().Select(x => x.AssemblyName).ToList();
            _mappingNamespaces = jsonMapQueryerConfig.MappingNamespaces.OfType<MappingNamespace>().Select(x => x.Namespace).ToList();
        }

        public static DefaultJsonMapQueryer Instance
        {
            get { return _instance.Value; }
        }

        public Enum DefaultMap
        {
            get { return _defaultMap; }
        }

        public Enum InheritedMap
        {
            get { return _inheritedMap; }
        }

        public Enum List
        {
            get { return _list; }
        }

        private string _nullCheckValidationMethodName;

        public string NullCheckValidationMethodName
        {
            get { return _nullCheckValidationMethodName; }
        }

        public Enum Primitive
        {
            get { return _primitive; }
        }

        public IEnumerable<Type> ValidationClasses
        {
            get { return _validationClasses; }
        }

        public Enum ViewModel
        {
            get { return _viewModel; }
        }

        public void SaveJsonMap()
        {
            String json = jMap.ToString();
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "JsonMap." + _configuration + ".json", json);
        }

        public void AddChild(IJsonMap jsonMap, IJsonMap childJsonMap)
        {
            switch (jsonMap.MapEnumType())
            {
                case MapType.Entity:
                    (jsonMap as IEntity).Attributes = (jsonMap as IEntity).Attributes.Concat(new IAttribute[] { (IAttribute)childJsonMap });
                    break;

                case MapType.Attribute:
                    (jsonMap as IAttribute).Validations = (jsonMap as IAttribute).Validations.Concat(new IValidation[] { (IValidation)childJsonMap });
                    break;

                case MapType.Validation:
                    (jsonMap as IValidation).Arguments = (jsonMap as IValidation).Arguments.Concat(new IArgument[] { (IArgument)childJsonMap });
                    break;

                default:
                    break;
            }
        }

        public void AddValidationType(Type type)
        {
            _validationClasses.Add(type);
        }

        public void AddAssembly(String assemblyName)
        {
            _mappingAssemblies.Add(assemblyName);
        }

        public long Create(long parentId, IJsonMap jsonMap)
        {
            jsonMap.Id = this.GetList(jsonMap.MapEnumType()).OrderByDescending(x => x.Id).Select(x => x.Id).FirstOrDefault() + 1;
            this.SaveMap(jsonMap);
            var parentMap = this.Get(parentId, jsonMap.ParentMapEnumType());
            if (parentMap != null)
            {
                this.AddChild(parentMap, jsonMap);
            }
            return jsonMap.Id;
        }

        public void Delete(IJsonMap jsonMap)
        {
            foreach (var parent in this.GetList(jsonMap.ParentMapEnumType()).Where(x => x.Children().Select(z => z.Id).Contains(jsonMap.Id)))
            {
                this.RemoveChild(parent, jsonMap);
            }
            switch (jsonMap.MapEnumType())
            {
                case MapType.Entity:
                    (jMap["Entitys"] as JArray).Where(x => x["Id"].Value<long>() == jsonMap.Id).First().Remove();
                    break;

                case MapType.Attribute:
                    (jMap["Attributes"] as JArray).Where(x => x["Id"].Value<long>() == jsonMap.Id).First().Remove();
                    break;

                case MapType.Validation:
                    (jMap["Validations"] as JArray).Where(x => x["Id"].Value<long>() == jsonMap.Id).First().Remove();
                    break;

                case MapType.Argument:
                    (jMap["Arguments"] as JArray).Where(x => x["Id"].Value<long>() == jsonMap.Id).First().Remove();
                    break;

                default:
                    break;
            }
            var existingList = GetList(jsonMap.MapEnumType()).SelectMany(x => x.Children());
            foreach (var child in jsonMap.Children())
            {
                if (!existingList.Contains(child))
                { this.Delete(child); }
            }
        }

        public IEnumerable<IJsonMap> GetList(MapType mapType)
        {
            switch (mapType)
            {
                case MapType.Entity:
                    return jMap["Entitys"].Select(x => new JEntity(x));
                    break;

                case MapType.Attribute:
                    return jMap["Attributes"].Select(x => new JAttribute(x));
                    break;

                case MapType.Validation:
                    return jMap["Validations"].Select(x => new JValidation(x));
                    break;

                case MapType.Argument:
                    return jMap["Arguments"].Select(x => new JArgument(x));
                    break;

                default:
                    return Enumerable.Empty<IJsonMap>();
                    break;
            }
        }

        public void RemoveChild(IJsonMap jsonMap, IJsonMap childJsonMap)
        {
            switch (jsonMap.MapEnumType())
            {
                case MapType.Entity:
                    (jsonMap as IEntity).Attributes = (jsonMap as IEntity).Attributes.Where(x => x.Id != childJsonMap.Id);
                    break;

                case MapType.Attribute:
                    (jsonMap as IAttribute).Validations = (jsonMap as IAttribute).Validations.Where(x => x.Id != childJsonMap.Id);
                    break;

                case MapType.Validation:
                    (jsonMap as IValidation).Arguments = (jsonMap as IValidation).Arguments.Where(x => x.Id != childJsonMap.Id);
                    break;

                default:
                    break;
            }
        }

        public void Update(IJsonMap jsonMap)
        {
            JToken jTransformerToJToken = JTransformer.ToJToken(jsonMap, new ToJOptions());
            (jTransformerToJToken as JObject).Remove("$type");
            JToken updateMap;

            switch (jsonMap.MapEnumType())
            {
                case MapType.Entity:
                    updateMap = (jMap["Entitys"] as JArray).First(x => x["Id"].Value<long>() == jsonMap.Id);

                    break;

                case MapType.Attribute:
                    updateMap = (jMap["Attributes"] as JArray).First(x => x["Id"].Value<long>() == jsonMap.Id);
                    break;

                case MapType.Validation:
                    updateMap = (jMap["Validations"] as JArray).First(x => x["Id"].Value<long>() == jsonMap.Id);
                    break;

                case MapType.Argument:
                    updateMap = (jMap["Arguments"] as JArray).First(x => x["Id"].Value<long>() == jsonMap.Id);
                    break;

                default:
                    updateMap = new JObject();
                    break;
            }
            jsonMap.GetType().MergeTokenPull(updateMap, jTransformerToJToken);
        }

        private void SaveMap(IJsonMap jsonMap)
        {
            JToken jTransformerToJToken = JTransformer.ToJToken(jsonMap, new ToJOptions());
            (jTransformerToJToken as JObject).Remove("$type");

            switch (jsonMap.MapEnumType())
            {
                case MapType.Entity:
                    (jMap["Entitys"] as JArray).Add(jTransformerToJToken);
                    break;

                case MapType.Attribute:
                    (jMap["Attributes"] as JArray).Add(jTransformerToJToken);
                    break;

                case MapType.Validation:
                    (jMap["Validations"] as JArray).Add(jTransformerToJToken);
                    break;

                case MapType.Argument:
                    (jMap["Arguments"] as JArray).Add(jTransformerToJToken);
                    break;

                default:
                    break;
            }
        }

        public IEnumerable<string> MappingAssembies
        {
            get { return _mappingAssemblies.ToArray(); }
        }

        public IEnumerable<string> MappingNamespaces
        {
            get { return _mappingNamespaces.ToArray(); }
        }
    }
}

namespace JsonMap.Default.Types
{
    public class JArgument : IArgument
    {
        private JToken _ArgumentBase;

        public JArgument(JToken _ArgumentBase)
        {
            // TODO: Complete member initialization
            this._ArgumentBase = _ArgumentBase;
            if (this._ArgumentBase == null)
            {
                this._ArgumentBase = new JObject();
            }
            if (this._ArgumentBase.SelectToken("JsonMapEnum") != null)
            {
                (this._ArgumentBase as JObject).Property("JsonMapEnum").Remove();
            }
        }

        public long Id
        {
            get { return (long)_ArgumentBase["Id"]; }
            set { _ArgumentBase["Id"] = value; }
        }

        public string Name
        {
            get { return (String)_ArgumentBase["Name"]; }
            set { _ArgumentBase["Name"] = value; }
        }

        public string Path
        {
            get { return (String)_ArgumentBase["Path"]; }
            set { _ArgumentBase["Path"] = value; }
        }
    }

    public class JAttribute : IAttribute
    {
        private JToken _AttributeBase;

        public JAttribute(JToken _AttributeBase)
        {
            // TODO: Complete member initialization
            this._AttributeBase = _AttributeBase;
            if (this._AttributeBase == null)
            {
                this._AttributeBase = new JObject();
            }
            if (this._AttributeBase.SelectToken("JsonMapEnum") != null)
            {
                (this._AttributeBase as JObject).Property("JsonMapEnum").Remove();
            }
        }

        public long Id
        {
            get { return (long)_AttributeBase["Id"]; }
            set { _AttributeBase["Id"] = value; ;}
        }

        public LazyEnum JSType
        {
            get { return new LazyEnum(Enum.Parse(typeof(DefaultJSTypeEnum), (_AttributeBase["JSType"] as JValue).Value.ToString())); }
            set { _AttributeBase["JSType"] = new JValue(Enum.ToObject(typeof(DefaultJSTypeEnum), int.Parse(value.Value.ToString()))); }
        }

        public string Name
        {
            get { return (String)_AttributeBase["Name"]; }
            set { _AttributeBase["Name"] = value; }
        }

        public LazyEnum SubJsonMapEnum
        {
            get { return new LazyEnum(Enum.Parse(typeof(DefaultJsonMapEnum), (_AttributeBase["SubJsonMapEnum"] as JValue).Value.ToString())); }
            set { _AttributeBase["SubJsonMapEnum"] = new JValue(Enum.ToObject(typeof(DefaultJsonMapEnum), int.Parse(value.Value.ToString()))); }
        }

        public IEnumerable<IValidation> Validations
        {
            get { return JTransformer.Queryer.GetList(_AttributeBase["Validations"].Select(x => x["Id"].Value<long>()), MapType.Validation).Cast<IValidation>(); }
            set { _AttributeBase["Validations"] = JArray.FromObject(value.Select(x => new JObject { new JProperty("Id", x.Id) })); }
        }
    }

    public class JEntity : IEntity
    {
        private JToken _EntityBase;

        public JEntity(JToken _EntityBase)
        {
            // TODO: Complete member initialization
            this._EntityBase = _EntityBase;
            if (this._EntityBase == null)
            {
                this._EntityBase = new JObject();
            }
        }

        public string AbstractDefault
        {
            get { return (String)_EntityBase["AbstractDefault"]; }
            set { _EntityBase["AbstractDefault"] = value; }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return JTransformer.Queryer.GetList(_EntityBase["Attributes"].Select(x => x["Id"].Value<long>()), MapType.Attribute).Cast<IAttribute>(); }
            set { _EntityBase["Attributes"] = JArray.FromObject(value.Select(x => new JObject { new JProperty("Id", x.Id) })); }
        }

        public bool GenericOnly
        {
            get { return (bool)_EntityBase["GenericOnly"]; }
            set { _EntityBase["GenericOnly"] = value; }
        }

        public long Id
        {
            get { return (long)_EntityBase["Id"]; }
            set { _EntityBase["Id"] = value; }
        }

        public string JSClass
        {
            get { return (String)_EntityBase["JSClass"]; }
            set { _EntityBase["JSClass"] = value; }
        }

        public LazyEnum JsonMapEnum
        {
            get { return new LazyEnum(Enum.Parse(typeof(DefaultJsonMapEnum), (_EntityBase["JsonMapEnum"] as JValue).Value.ToString())); }
            set { _EntityBase["JsonMapEnum"] = new JValue(Enum.ToObject(typeof(DefaultJsonMapEnum), int.Parse(value.Value.ToString()))); }
        }

        public string Name
        {
            get { return (String)_EntityBase["Name"]; }
            set { _EntityBase["Name"] = value; }
        }
    }

    public class JValidation : IValidation
    {
        private JToken _ValidationBase;

        public long Step
        {
            get { return (long)_ValidationBase["Step"]; }
            set { _ValidationBase["Step"] = value; }
        }

        public JValidation(JToken _ValidationBase)
        {
            // TODO: Complete member initialization
            this._ValidationBase = _ValidationBase;
            if (this._ValidationBase == null)
            {
                this._ValidationBase = new JObject();
            }
            if (this._ValidationBase.SelectToken("JsonMapEnum") != null)
            {
                (this._ValidationBase as JObject).Property("JsonMapEnum").Remove();
            }
        }

        public IEnumerable<IArgument> Arguments
        {
            get { return JTransformer.Queryer.GetList(_ValidationBase["Arguments"].Select(x => x["Id"].Value<long>()), MapType.Argument).Cast<IArgument>(); }
            set { _ValidationBase["Arguments"] = JArray.FromObject(value.Select(x => new JObject { new JProperty("Id", x.Id) })); }
        }

        public long Id
        {
            get { return (long)_ValidationBase["Id"]; }
            set { _ValidationBase["Id"] = value; }
        }

        public string Name
        {
            get { return (String)_ValidationBase["Name"]; }
            set { _ValidationBase["Name"] = value; }
        }
    }
}