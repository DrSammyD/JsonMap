/* Copyright 2013 MacReport Media Publishing Inc.
 * Licensed under MPL-2.0 (see /LICENSE)
 * If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * 
 * Author: Sam Armstrong
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JsonMap.Form.Types;
using JsonMap.Interfaces;

namespace JsonMap.Form
{
    public static class JsonMapForm
    {

        public static ViewModel GetJsonMapTypeEnumList()
        {
            Type enumType = JTransformer.Queryer.DefaultMap.GetType();
            string[] query = enumType.GetEnumNames();

            var Arr = new List<EnumList>();
            foreach (var enumName in query)
            {
                Arr.Add(new EnumList { Name = enumName, Value = (int)Enum.Parse(enumType, enumName) });
            }
            return new ViewModel { jOpts = new ToJOptions { mapType = JTransformer.Queryer.DefaultMap }, obj = Arr, CustomPropName = "JsonMapTypeEnumsListViewModel" };
        }

        public static ViewModel GetJSTypeEnumList()
        {
            Type enumType = JTransformer.Queryer.Primitive.GetType();
            string[] query = enumType.GetEnumNames();

            var Arr = new List<EnumList>();
            foreach (var enumName in query)
            {
                Arr.Add(new EnumList { Name = enumName, Value = (int)Enum.Parse(enumType, enumName) });
            }
            return new ViewModel { jOpts = new ToJOptions { mapType = JTransformer.Queryer.DefaultMap }, obj = Arr, CustomPropName = "JSTypeEnumsListViewModel" };
        }

        public static CollectionViewModel GetTypeAttributeLists(IEnumerable<String> nameSpaces = null)
        {
            if (nameSpaces == null) nameSpaces = Enumerable.Empty<String>();
            nameSpaces = JTransformer.Queryer.MappingNamespaces.Concat(nameSpaces);
            var dict = new Dictionary<string, List<AttributeList>>();
            var vm = new CollectionViewModel { objs = dict, CustomTopVMs = "TypeListViewModel", CustomTopProperty = "TypeListViewModel", CustomPropName = "TypeAttributeList" };

            var query = JStubs.LoadedTypes.Where(x => nameSpaces.Contains(x.Namespace)).OrderBy(y => y.Name).Distinct();

            foreach (var type in query)
            {
                dict.Add(type.ToString(), new List<AttributeList>());
                foreach (var member in type.GetAllProperties().OrderBy(x => x.Name).Distinct())
                {
                    var subclasses = JStubs.LoadedTypes.Where(y => y.Impliments(type))
                        .Select(x => x.FullName + "_" + x.Assembly.GetName().Name).ToArray();
                    dict[type.ToString()].Add(new AttributeList { Name = member.Name });
                }
                dict[type.ToString()].Add(new AttributeList { Name = "this" });
                dict[type.ToString()] = dict[type.ToString()].Distinct((x, y) => x.Name == y.Name).ToList();
            }

            return vm;
        }

        public static CollectionViewModel GetSubTypeList(IEnumerable<String> nameSpaces = null)
        {
            if (nameSpaces == null) nameSpaces = Enumerable.Empty<String>();
            nameSpaces = JTransformer.Queryer.MappingNamespaces.Concat(nameSpaces);
            var dict = new Dictionary<string, List<AttributeList>>();
            var vm = new CollectionViewModel { objs = dict, CustomTopVMs = "ViewModel", CustomTopProperty = "SubTypeListViewModel", CustomPropName = "SubTypeList" };

            var query = JStubs.LoadedTypes.Where(x => nameSpaces.Contains(x.Namespace)).OrderBy(y => y.Name).Distinct();

            foreach (var type in query)
            {
                dict.Add(type.ToString(), new List<AttributeList>());
                foreach (var subtype in JStubs.LoadedTypes.Where(y => y.Impliments(type))
                        .Select(x => x.FullName + "_" + x.Assembly.GetName().Name).ToArray())
                {
                    dict[type.ToString()].Add(new AttributeList { Name = subtype });
                }
                if (!type.IsAbstract && !type.IsInterface)
                {
                    dict[type.ToString()].Add(new AttributeList { Name = type.FullName + "_" + type.Assembly.GetName().Name });
                }
                dict[type.ToString()] = dict[type.ToString()].Distinct((x, y) => x.Name == y.Name).ToList();
            }

            return vm;
        }

        /// <summary>
        /// Gets static validation methods from a passed in type
        /// </summary>
        /// <param name="validationType">The static type which holds all validation methods</param>
        /// <returns></returns>
        public static CollectionViewModel GetValidationMethodList()
        {
            var dict = new Dictionary<string, List<AttributeList>>();

            var vm = new CollectionViewModel { objs = dict, CustomTopVMs = "ValidationMethodListViewModel", CustomTopProperty = "ValidationMethodListViewModel", CustomPropName = "ValidationMethodList" };
            var ret = typeof(ValidationMessage);
            var query = JTransformer.Queryer.ValidationClasses.SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public)).Where(x => x.ReturnType == ret).OrderBy(x => x.Name);

            foreach (var method in query)
            {
                dict.Add(method.DeclaringType.FullName + '.' + method.Name, new List<AttributeList>());
                foreach (var param in method.GetParameters().OrderBy(x => x.Name))
                {
                    dict[method.DeclaringType.FullName + '.' + method.Name].Add(new AttributeList { Name = param.Name });
                }
            }

            return vm;
        }
    }
}

namespace JsonMap.Form.Types
{
    public class EnumList
    {
        #region Attributes and Associations

        private String m_Name;
        private int m_Value;

        #endregion Attributes and Associations

        #region Constructors

        public EnumList()
        {
        }

        /// <summary>
        /// Constructor taking all properties.
        /// </summary>
        public EnumList(
            String Name,
            int Value)
        {
            this.m_Name = Name;
            this.m_Value = Value;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        public EnumList(EnumList otherEnumList)
        {
            if (otherEnumList != null)
            {
                this.m_Name = otherEnumList.Name;
                this.m_Value = otherEnumList.Value;
            }
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///
        /// </summary>
        public String Name
        {
            get { return m_Name; }
            set { this.m_Name = value; }
        }

        /// <summary>
        ///
        /// </summary>
        public int Value
        {
            get { return m_Value; }
            set { this.m_Value = value; }
        }

        #endregion Properties
    }

    public class AttributeList
    {
        #region Attributes and Associations

        private String m_Name;

        #endregion Attributes and Associations

        #region Constructors

        public AttributeList()
        {
        }

        /// <summary>
        /// Constructor taking all properties.
        /// </summary>
        public AttributeList(
            String Name
            )
        {
            this.m_Name = Name;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        public AttributeList(AttributeList otherAttributeList)
        {
            if (otherAttributeList != null)
            {
                this.m_Name = otherAttributeList.Name;
            }
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///
        /// </summary>
        public String Name
        {
            get { return m_Name; }
            set { this.m_Name = value; }
        }

        #endregion Properties
    }

    public class JsonMaps
    {
        #region Attributes and Associations

        private IEntity[] m_Entitys;
        private IAttribute[] m_Attributes;
        private IValidation[] m_Validations;
        private IArgument[] m_Arguments;

        #endregion Attributes and Associations

        #region Constructors

        public JsonMaps()
        {
        }

        /// <summary>
        /// Constructor taking all properties.
        /// </summary>
        public JsonMaps(
            IEntity[] Entitys,
            IAttribute[] Attributes,
            IValidation[] Validations,
            IArgument[] Arguments)
        {
            this.m_Entitys = Entitys;
            this.m_Attributes = Attributes;
            this.m_Validations = Validations;
            this.m_Arguments = Arguments;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        public JsonMaps(JsonMaps otherJsonMaps)
        {
            if (otherJsonMaps != null)
            {
                this.m_Entitys = otherJsonMaps.Entitys;
                this.m_Attributes = otherJsonMaps.Attributes;
                this.m_Validations = otherJsonMaps.Validations;
                this.m_Arguments = otherJsonMaps.Arguments;
            }
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///
        /// </summary>
        public IEntity[] Entitys
        {
            get { return m_Entitys; }
            set { this.m_Entitys = value; }
        }

        /// <summary>
        ///
        /// </summary>
        public IAttribute[] Attributes
        {
            get { return m_Attributes; }
            set { this.m_Attributes = value; }
        }

        /// <summary>
        ///
        /// </summary>
        public IValidation[] Validations
        {
            get { return m_Validations; }
            set { this.m_Validations = value; }
        }

        /// <summary>
        ///
        /// </summary>
        public IArgument[] Arguments
        {
            get { return m_Arguments; }
            set { this.m_Arguments = value; }
        }

        #endregion Properties
    }
}