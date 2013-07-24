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

namespace JsonMap
{
    public enum MapType
    {
        Entity,
        Attribute,
        Validation,
        Argument,
        JsonMap
    }
}

namespace JsonMap.Interfaces
{
    /// <summary>
    /// This Interface is used by JsonMap
    ///
    /// Overrideable Interface Extensions
    /// Get(String mapName, ToJOptions jOpts);
    /// Delete(long id, MapType mapType)
    /// </summary>
    public interface IJsonMapQueryer
    {
        IEnumerable<Type> ValidationClasses { get; }

        IEnumerable<String> MappingAssembies { get; }

        IEnumerable<String> MappingNamespaces { get; }

        String NullCheckValidationMethodName { get; }

        Enum DefaultMap { get; }

        Enum InheritedMap { get; }

        Enum Primitive { get; }

        Enum List { get; }

        Enum ViewModel { get; }

        IEnumerable<IJsonMap> GetList(MapType mapType);

        long Create(long parentId, IJsonMap jsonMap);

        void Update(IJsonMap jsonMap);

        void Delete(IJsonMap jsonMap);

        void AddChild(IJsonMap jsonMap, IJsonMap childJsonMap);

        //Overrideable Interface Extension IEntity Get(String mapName, ToJOptions jOpts);
    }

    public interface IJsonMap
    {
        long Id { get; set; }

        String Name { get; set; }
    }

    public interface IEntity : IJsonMap
    {
        IEnumerable<IAttribute> Attributes { get; set; }

        String JSClass { get; set; }

        bool GenericOnly { get; set; }

        String AbstractDefault { get; set; }

        LazyEnum JsonMapEnum { get; set; }
    }

    public interface IAttribute : IJsonMap
    {
        IEnumerable<IValidation> Validations { get; set; }

        LazyEnum JSType { get; set; }

        LazyEnum SubJsonMapEnum { get; set; }
    }

    public interface IValidation : IJsonMap
    {
        IEnumerable<IArgument> Arguments { get; set; }

        long Step { get; set; }
    }

    public interface IArgument : IJsonMap
    {
        String Path { get; set; }
    }
}

namespace JsonMap.Interfaces.Extensions
{
    public static class IJsonMapExt
    {
        public static MapType MapEnumType(this IJsonMap jsonMap)
        {
            if (jsonMap is IEntity)
                return MapType.Entity;
            if (jsonMap is IAttribute)
                return MapType.Attribute;
            if (jsonMap is IValidation)
                return MapType.Validation;
            if (jsonMap is IArgument)
                return MapType.Argument;
            return MapType.JsonMap;
        }

        public static MapType ParentMapEnumType(this IJsonMap jsonMap)
        {
            if (jsonMap is IEntity)
                return MapType.JsonMap;
            if (jsonMap is IAttribute)
                return MapType.Entity;
            if (jsonMap is IValidation)
                return MapType.Attribute;
            if (jsonMap is IArgument)
                return MapType.Validation;
            return MapType.JsonMap;
        }

        public static IEnumerable<IJsonMap> Children(this IJsonMap jsonMap)
        {
            switch (jsonMap.MapEnumType())
            {
                case MapType.Entity:
                    return (jsonMap as IEntity).Attributes;
                    break;

                case MapType.Attribute:
                    return (jsonMap as IAttribute).Validations;
                    break;

                case MapType.Validation:
                    return (jsonMap as IValidation).Arguments;
                    break;

                default:
                    return Enumerable.Empty<IJsonMap>();
                    break;
            }
        }
    }

    public static class IJsonMapQueryerExt
    {
        public static IEnumerable<IJsonMap> GetList(this IJsonMapQueryer queryer, IEnumerable<IJsonMap> parentList)
        {
            IEnumerable<IJsonMap> SubList = new List<IJsonMap>();
            foreach (var parent in parentList)
                SubList = SubList.Concat(parent.Children().OrderBy(x => x.Name));
            return SubList.Distinct((x, y) => x.Id == y.Id);
        }

        public static IEnumerable<IEntity> GetList(this IJsonMapQueryer queryer)
        {
            return queryer.GetList(MapType.Entity).Cast<IEntity>();
        }

        public static IEnumerable<IEntity> GetList(this IJsonMapQueryer queryer, IEnumerable<String> Names)
        {
            return queryer.GetList(MapType.Entity).Where(x => Names.Contains(x.Name)).ToList().Cast<IEntity>();
        }

        public static IEnumerable<IJsonMap> GetList(this IJsonMapQueryer queryer, IEnumerable<long> Ids, MapType mapType)
        {
            return queryer.GetList(mapType).Where(x => Ids.Contains(x.Id)).ToList();
        }

        public static IEntity Get(this IJsonMapQueryer queryer, IEnumerable<string> mapNames, JOptions jOpts)
        {
            var Method = queryer.ExtensionOverrider(System.Reflection.MethodBase.GetCurrentMethod());
            if (Method != null)
                return (IEntity)Method.Invoke(queryer, new Object[2] { mapNames, jOpts });
            return queryer.GetList(MapType.Entity).Where(x => (x as IEntity).JsonMapEnum.Equals(jOpts.mapType) && mapNames.Contains(x.Name)).OrderBy(x => x.GetType(), new InheritanceComparer()).Cast<IEntity>().FirstOrDefault();
        }

        public static IJsonMap Get(this IJsonMapQueryer queryer, long id, MapType mapType)
        {
            var Method = queryer.ExtensionOverrider(System.Reflection.MethodBase.GetCurrentMethod());
            if (Method != null)
                return (IEntity)Method.Invoke(queryer, new Object[2] { id, mapType });
            return queryer.GetList(mapType).Where(x => x.Id == id).FirstOrDefault();
        }

        public static void Delete(this IJsonMapQueryer queryer, long id, MapType mapType)
        {
            var Method = queryer.ExtensionOverrider(System.Reflection.MethodBase.GetCurrentMethod());
            if (Method != null)
                Method.Invoke(queryer, new Object[2] { id, mapType });
            queryer.Delete(queryer.Get(id, mapType));
        }

        public static System.Reflection.MethodBase ExtensionOverrider(this Object obj, System.Reflection.MethodBase method)
        {
            return obj.GetType().GetMethods().Where(
                x => x.Name == method.Name && x.ReturnType == (method as System.Reflection.MethodInfo).ReturnType &&
                x.GetParameters().Select(z => z.ParameterType).SequenceEqual(method.GetParameters().Skip(1).Select(w => w.ParameterType))).FirstOrDefault();
        }
    }
}