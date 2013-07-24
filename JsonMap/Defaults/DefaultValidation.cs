/* Copyright 2013 MacReport Media Publishing Inc.
 * Licensed under MPL-2.0 (see /LICENSE)
 * If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * 
 * Author: Sam Armstrong
 */

using System;
using System.Linq;
using System.Text.RegularExpressions;
using JsonMap.Interfaces.Extensions;
using Newtonsoft.Json.Linq;

namespace JsonMap.Default
{
    public static class DefaultValidation
    {
        /// <summary>
        /// Validation requiring value to be an integer
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ValidationMessage IsInt(dynamic value)
        {
            try
            {
                int x = 0;
                if (int.TryParse(value.ToString(), out x))
                {
                    return new ValidationMessage();
                }
                else
                {
                    return new ValidationMessage("Needs to be a Number");
                }
            }
            catch (Exception e)
            {
                return new ValidationMessage("Needs to be a Number");
            }
        }

        /// <summary>
        /// Validation requiring value to not be null
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ValidationMessage NotNull(dynamic value)
        {
            if (value != null && value != "")
            {
                return new ValidationMessage();
            }
            else
            {
                return new ValidationMessage("Cannot be blank");
            }
        }

        /// <summary>
        /// Validate an email address
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ValidationMessage IsValidEmail(dynamic value)
        {
            if (value != null && value != "")
            {
                string pattern = @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|"
                + @"([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)"
                + @"@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$";
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
                if (regex.IsMatch(value))
                {
                    return new ValidationMessage();
                }
                else
                {
                    return new ValidationMessage("Not a valid email address!");
                }
            }
            else
            {
                return new ValidationMessage();
            }
        }

        public static String StringsMatch(dynamic DependantValueA, dynamic DependantValueB)
        {
            if (DependantValueA == DependantValueB)
            {
                return "";
            }
            return "Strings don't match";
        }

        public static ValidationMessage IsSubType(dynamic Parent, dynamic Child)
        {
            try
            {
                if (String.IsNullOrEmpty(Child) || JStubs.GetType(((String)Child).Split('_')[0]).Impliments(JStubs.GetType((String)Parent)) || ((String)Child).Split('_')[0] == Parent)
                {
                    return new ValidationMessage();
                }
            }
            catch
            {
            }
            return new ValidationMessage("Type does not inherit from selected type");
        }

        public static ValidationMessage DistinctArguments(dynamic ArgumentIds)
        {
            try
            {
                var argList = JTransformer.Queryer.GetList((ArgumentIds as JArray).Select(x => x["Id"].Value<long>()), MapType.Argument);
                if (argList.Distinct((x, y) => x.Name == y.Name).Count() == argList.Count())
                {
                    return new ValidationMessage();
                }
            }
            catch (Exception e)
            {
            }
            return new ValidationMessage("Duplicate parameters cannot exist");
        }

        public static ValidationMessage FullArguments(dynamic MethodName, dynamic ArgumentIds)
        {
            try
            {
                var method = (MethodName as String);
                var className = String.Join(".", method.Split('.').Reverse().Skip(1).Reverse());
                method = method.Split('.').Reverse().FirstOrDefault();
                var validationType = JTransformer.Queryer.ValidationClasses.First(x => x.FullName == className);
                var validationMethods = validationType.GetMethod(method);
                var methodParams = validationMethods.GetParameters();
                if (methodParams.Count() == 1 || methodParams.Count() == (ArgumentIds as JArray).Count)
                {
                    return new ValidationMessage();
                }
            }
            catch (Exception e)
            {
            }
            return new ValidationMessage("Incorrect Number of Arguments for Validation");
        }
    }
}