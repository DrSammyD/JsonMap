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
using JsonMap.Interfaces;
using JsonMap.Interfaces.Extensions;
using Newtonsoft.Json.Linq;

namespace JsonMap
{
    public static class JValidator
    {
        /// <summary>
        /// returns a validation JArray
        /// </summary>
        /// <param name="jArr"></param>
        /// <param name="objType"></param>
        /// <param name="validations"></param>
        /// <param name="mapType"></param>
        /// <returns></returns>
        internal static JArray CreateValidationArray(this Type objType, JArray jArr, JValidOptions options)
        {
            JArray jValidArr = new JArray();
            JToken insert = null;
            foreach (JToken jToken in jArr)
            {
                insert = objType.GetElementOrFirstGenericArgumentType().CreateValidationToken(jToken, options);
                if (insert.HasValues)
                    jValidArr.Add(insert);
            }
            return jValidArr;
        }

        /// <summary>
        /// Validates a JObject returned from the client, and runs each validation tied to the class in the database
        /// </summary>
        /// <param name="jObj"></param>
        /// <param name="objType"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        internal static JObject CreateValidationObject(this Type objType, JObject jObj, JValidOptions options)
        {
            var entityJObj = new JObject();
            IEntity entity = JTransformer.Queryer.Get(objType.GetAllTypes().Select(x => x.FullName), options);
            if (entity == null)
            { throw new JsonMapNotFoundException(objType, options); }
            JObject validationModelJObj = new JObject();
            foreach (var key in entity.Attributes)
            {
                var tmpMapType = options.mapType;
                if (!key.SubJsonMapEnum.Equals(JTransformer.Queryer.InheritedMap))
                {
                    options.mapType = key.SubJsonMapEnum;
                }

                JToken jToken = jObj.SelectToken(key.Name, false);
                PropertyInfo attributeInfo = objType.GetAllProperties().FirstOrDefault(x => x.Name == key.Name);
                JToken insert = (attributeInfo != null && jToken != null) ? attributeInfo.PropertyType.CreateValidationToken(jToken, options) : null;
                options.mapType = tmpMapType;

                if (insert != null && insert.HasValues)
                    entityJObj.Add(new JProperty(key.Name, insert));

                //Use the map type with the mapType enum. If no map exists with given, use default. If it does, don't include default
                var attributeJObj = new JObject();
                var notNullFlag = false;
                if (key.Validations.FirstOrDefault(x => x.Name == JTransformer.Queryer.NullCheckValidationMethodName) != null)
                    notNullFlag = true;
                if (jToken is JToken)
                {
                    foreach (var val in key.Validations.Where(x => x.Step == options.Step || options.Step == -1))
                    {
                        if (val.Arguments.Count() == 0)
                        {
                            ValidationMessage standAloneValidation = JValidator.RunStandAloneValidation(val.Name, jToken);
                            if ((jToken.ToString() == "" || jToken.ToString() == null) && !notNullFlag)
                                standAloneValidation = new ValidationMessage();
                            if (standAloneValidation != null)
                                attributeJObj.Add(new JProperty(val.Name, new JObject(standAloneValidation.Message)));

                            if (standAloneValidation.Message.ToString() != ValidationMessage.ValidMessage.ToString())
                            {
                                if (options.throwError) throw new JValidationException(standAloneValidation, jObj); else options.Fail();
                            }
                        }
                    }
                }
                else if (key.Name == "this")
                {
                    foreach (var val in key.Validations.Where(x => x.Step == options.Step || options.Step == -1))
                    {
                        if (val.Arguments.Count() > 0)
                        {
                            ValidationMessage dependentValidation = JValidator.RunDependentValidation(val.Name, jObj, val.Arguments);
                            if (dependentValidation != null)
                            {
                                attributeJObj.Add(new JProperty(val.Name, new JObject(dependentValidation.Message)));

                                if (dependentValidation.Message.ToString() != ValidationMessage.ValidMessage.ToString())
                                {
                                    if (options.throwError) throw new JValidationException(dependentValidation, jObj); else options.Fail();
                                }
                            }
                        }
                    }
                }
                if (attributeJObj.HasValues)
                    validationModelJObj.Add(new JProperty(key.Name, attributeJObj));
            }
            if (validationModelJObj.HasValues)
                entityJObj.Add(new JProperty("validationModels", validationModelJObj));
            return entityJObj;
        }

        public static JToken CreateValidationToken(this Type objType, JToken jObj, JValidOptions options)
        {
            JToken mapStub = null;
            if (!objType.IsJPrimitive())
            {
                Type[] elmTypeInterfaces = objType.GetInterfaces();
                if (objType.GetInterface("IEnumerable") == null && !objType.IsJPrimitive())
                {
                    mapStub = objType.CreateValidationObject(jObj as JObject, options);
                }
                else
                {
                    if (objType.GetInterface("IDictionary") == null)
                    {
                        mapStub = objType.CreateValidationArray(jObj as JArray, options);
                    }
                    else
                    {
                    }
                }
            }
            return mapStub;
        }

        /// <summary>
        /// Runs validations with arguments other than the attribute the validation was placed on.
        /// Any validation in the database with validation args uses this to run it's validation method.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="value"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static ValidationMessage RunDependentValidation(string methodName, JToken value, IEnumerable<IArgument> args)
        {
            var className = String.Join(".", methodName.Split('.').Reverse().Skip(1).Reverse());
            methodName = methodName.Split('.').Reverse().FirstOrDefault();
            var validationType = JTransformer.Queryer.ValidationClasses.First(x => x.FullName == className);
            var validationMethods = validationType.GetMethod(methodName);
            var methodParams = validationMethods.GetParameters();

            //check if argument counts match
            if (args.Count() == methodParams.Count())
            {
                List<object> passingArgs = new List<object>();
                foreach (var param in methodParams)
                {
                    //finds arguments with the correct parameter name in the Database and places it in the correct spot in the passingArgs
                    var arg = args.First(x => x.Name == param.Name);

                    //parses out the argument from the passed in JToken and put's it in the correct type
                    JToken JArg = value.SelectToken(arg.Path);
                    if (JArg is JToken)
                        passingArgs.Add(JTransformer.FromJObject(param.ParameterType, JArg));
                }

                //runs validation method and returns the JObject with the correct message
                if (passingArgs.Count == methodParams.Count())
                {
                    try
                    {
                        return (ValidationMessage)validationMethods.Invoke(validationType, passingArgs.ToArray());
                    }
                    catch (Exception e)
                    {
                        return new ValidationMessage("Not valid.");
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Runs a validation which has no validation args besides the attribute it was placed on
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static ValidationMessage RunStandAloneValidation(string methodName, JToken value)
        {
            var className = String.Join(".", methodName.Split('.').Reverse().Skip(1).Reverse());
            methodName = methodName.Split('.').Reverse().FirstOrDefault();
            var validationType = JTransformer.Queryer.ValidationClasses.First(x => x.FullName == className);
            var validationMethods = validationType.GetMethod(methodName);
            var methodParams = validationMethods.GetParameters();

            //checks if argument count is one, since it will only pass one argument to the method
            if (methodParams.Count() == 1)
            {
                List<object> passingArgs = new List<object>();
                foreach (var param in methodParams)
                {
                    //places value into the correct type and puts into the params object array
                    passingArgs.Add(JTransformer.FromJObject(param.ParameterType, value));
                }

                try
                {
                    //runs validation method and returns the JObject with the correct message
                    return (ValidationMessage)validationMethods.Invoke(validationMethods, passingArgs.ToArray());
                }
                catch (Exception e)
                {
                    return new ValidationMessage("Not valid.");
                }
            }
            return null;
        }
    }

    public class JValidationException : Exception
    {
        private JToken _failToken;
        private ValidationMessage _message;

        public JValidationException(ValidationMessage message, JToken failToken)
        {
            _message = message;
            _failToken = failToken;
        }

        public JObject GetJMessage()
        {
            return new JObject { _message.Message, new JProperty("token", _failToken) };
        }
    }

    public class JValidOptions : JOptions
    {
        private int _step;

        public bool throwError;
        private bool _failed;

        public JValidOptions()
        {
            _step = -1;
            mapType = JTransformer.Queryer.DefaultMap;
            throwError = false;
        }

        public JValidOptions(JValidOptions options)
        {
            this.mapType = options.mapType;
            this.throwError = options.throwError;
        }

        public int Step
        {
            get { return _step; }
            set { _step = value; }
        }

        public bool Failed
        {
            get { return _failed; }
        }

        public void Fail()
        {
            _failed = true;
        }
    }

    public class ValidationMessage
    {
        public static JProperty ValidMessage;
        private JProperty message;

        static ValidationMessage()
        {
            ValidMessage = new JProperty("message", "Valid");
        }

        public ValidationMessage(String newMessage)
        {
            message = ValidMessage.DeepClone() as JProperty;
            message.Value = newMessage;
        }

        public ValidationMessage()
        {
            message = ValidMessage.DeepClone() as JProperty;
            message.Value = "Valid";
        }

        public JProperty Message
        {
            get { return message; }
            set { message = value; }
        }
    }
}