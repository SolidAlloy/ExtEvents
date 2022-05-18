//-----------------------------------------------------------------------
// <copyright file="TypeExtensions.cs" company="Sirenix IVS">
// Copyright (c) 2018 Sirenix IVS
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//-----------------------------------------------------------------------

namespace ExtEvents.OdinSerializer.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// Type method extensions.
    /// </summary>
    public static class TypeExtensions
    {
        private static readonly object GenericConstraintsSatisfaction_LOCK = new object();
        private static readonly Dictionary<Type, Type> GenericConstraintsSatisfactionInferredParameters = new Dictionary<Type, Type>();
        private static readonly Dictionary<Type, Type> GenericConstraintsSatisfactionResolvedMap = new Dictionary<Type, Type>();
        private static readonly HashSet<Type> GenericConstraintsSatisfactionProcessedParams = new HashSet<Type>();

        private static readonly Type GenericListInterface = typeof(IList<>);
        private static readonly Type GenericCollectionInterface = typeof(ICollection<>);

        private static readonly object WeaklyTypedTypeCastDelegates_LOCK = new object();
        private static readonly DoubleLookupDictionary<Type, Type, Func<object, object>> WeaklyTypedTypeCastDelegates = new DoubleLookupDictionary<Type, Type, Func<object, object>>();

        /// <summary>
        /// Type name alias lookup.
        /// TypeNameAlternatives["Single"] will give you "float", "UInt16" will give you "ushort", "Boolean[]" will give you "bool[]" etc..
        /// </summary>
        public static readonly Dictionary<string, string> TypeNameAlternatives = new Dictionary<string, string>
        {
            { "Single",     "float"     },
            { "Double",     "double"    },
            { "SByte",      "sbyte"     },
            { "Int16",      "short"     },
            { "Int32",      "int"       },
            { "Int64",      "long"      },
            { "Byte",       "byte"      },
            { "UInt16",     "ushort"    },
            { "UInt32",     "uint"      },
            { "UInt64",     "ulong"     },
            { "Decimal",    "decimal"   },
            { "String",     "string"    },
            { "Char",       "char"      },
            { "Boolean",    "bool"      },
            { "Single[]",   "float[]"   },
            { "Double[]",   "double[]"  },
            { "SByte[]",    "sbyte[]"   },
            { "Int16[]",    "short[]"   },
            { "Int32[]",    "int[]"     },
            { "Int64[]",    "long[]"    },
            { "Byte[]",     "byte[]"    },
            { "UInt16[]",   "ushort[]"  },
            { "UInt32[]",   "uint[]"    },
            { "UInt64[]",   "ulong[]"   },
            { "Decimal[]",  "decimal[]" },
            { "String[]",   "string[]"  },
            { "Char[]",     "char[]"    },
            { "Boolean[]",  "bool[]"    },
        };

        private static readonly object CachedNiceNames_LOCK = new object();
        private static readonly Dictionary<Type, string> CachedNiceNames = new Dictionary<Type, string>();

        private static string GetCachedNiceName(Type type)
        {
            string result;
            lock (CachedNiceNames_LOCK)
            {
                if (!CachedNiceNames.TryGetValue(type, out result))
                {
                    result = CreateNiceName(type);
                    CachedNiceNames.Add(type, result);
                }
            }
            return result;
        }

        private static string CreateNiceName(Type type)
        {
            if (type.IsArray)
            {
                int rank = type.GetArrayRank();
                return type.GetElementType().GetNiceName() + (rank == 1 ? "[]" : "[,]");
            }

            if (type.InheritsFrom(typeof(Nullable<>)))
            {
                return type.GetGenericArguments()[0].GetNiceName() + "?";
            }

            if (type.IsByRef)
            {
                return "ref " + type.GetElementType().GetNiceName();
            }

            if (type.IsGenericParameter || !type.IsGenericType)
            {
                return TypeNameGauntlet(type);
            }

            var builder = new StringBuilder();
            var name = type.Name;
            var index = name.IndexOf("`");

            if (index != -1)
            {
                builder.Append(name.Substring(0, index));
            }
            else
            {
                builder.Append(name);
            }

            builder.Append('<');
            var args = type.GetGenericArguments();

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (i != 0)
                {
                    builder.Append(", ");
                }

                builder.Append(GetNiceName(arg));
            }

            builder.Append('>');
            return builder.ToString();
        }

        private static readonly Type VoidPointerType = typeof(void).MakePointerType();

        private static readonly Dictionary<Type, HashSet<Type>> PrimitiveImplicitCasts = new Dictionary<Type, HashSet<Type>>
        {
            { typeof(Int64),    new HashSet<Type> { typeof(Single), typeof(Double), typeof(Decimal) } },
            { typeof(Int32),    new HashSet<Type> { typeof(Int64), typeof(Single), typeof(Double), typeof(Decimal) } },
            { typeof(Int16),    new HashSet<Type> { typeof(Int32), typeof(Int64), typeof(Single), typeof(Double), typeof(Decimal) } },
            { typeof(SByte),    new HashSet<Type> { typeof(Int16), typeof(Int32), typeof(Int64), typeof(Single), typeof(Double), typeof(Decimal) } },
            { typeof(UInt64),   new HashSet<Type> { typeof(Single), typeof(Double), typeof(Decimal) } },
            { typeof(UInt32),   new HashSet<Type> { typeof(Int64), typeof(UInt64), typeof(Single), typeof(Double), typeof(Decimal) } },
            { typeof(UInt16),   new HashSet<Type> { typeof(Int32), typeof(UInt32), typeof(Int64), typeof(UInt64), typeof(Single), typeof(Double), typeof(Decimal) } },
            { typeof(Byte),     new HashSet<Type> { typeof(Int16), typeof(UInt16), typeof(Int32), typeof(UInt32), typeof(Int64), typeof(UInt64), typeof(Single), typeof(Double), typeof(Decimal) } },
            { typeof(Char),     new HashSet<Type> { typeof(UInt16), typeof(Int32), typeof(UInt32), typeof(Int64), typeof(UInt64), typeof(Single), typeof(Double), typeof(Decimal) } },
            { typeof(Boolean),  new HashSet<Type>() },
            { typeof(Decimal),  new HashSet<Type>() },
            { typeof(Single),   new HashSet<Type> { typeof(Double) } },
            { typeof(Double),   new HashSet<Type>() },
            { typeof(IntPtr),   new HashSet<Type>() },
            { typeof(UIntPtr),  new HashSet<Type>() },
            { VoidPointerType,  new HashSet<Type>() },
        };

        private static readonly HashSet<Type> ExplicitCastIntegrals = new HashSet<Type>
        {
            typeof(Int64),
            typeof(Int32),
            typeof(Int16),
            typeof(SByte),
            typeof(UInt64),
            typeof(UInt32),
            typeof(UInt16),
            typeof(Byte),
            typeof(Char),
            typeof(Decimal),
            typeof(Single),
            typeof(Double),
            typeof(IntPtr),
            typeof(UIntPtr)
        };

        internal static bool HasCastDefined(this Type from, Type to, bool requireImplicitCast)
        {
            if (from.IsEnum)
            {
                return Enum.GetUnderlyingType(from).IsCastableTo(to);
            }

            if (to.IsEnum)
            {
                return Enum.GetUnderlyingType(to).IsCastableTo(from);
            }

            if ((from.IsPrimitive || from == VoidPointerType) && (to.IsPrimitive || to == VoidPointerType))
            {
                if (requireImplicitCast)
                {
                    return PrimitiveImplicitCasts[from].Contains(to);
                }

                if (from == typeof(IntPtr))
                {
                    if (to == typeof(UIntPtr))
                    {
                        return false;
                    }

                    if (to == VoidPointerType)
                    {
                        return true;
                    }
                }
                else if (from == typeof(UIntPtr))
                {
                    if (to == typeof(IntPtr))
                    {
                        return false;
                    }

                    if (to == VoidPointerType)
                    {
                        return true;
                    }
                }

                return ExplicitCastIntegrals.Contains(from) && ExplicitCastIntegrals.Contains(to);
            }

            return from.GetCastMethod(to, requireImplicitCast) != null;
        }

        /// <summary>
        /// Determines whether a type can be casted to another type.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="requireImplicitCast">if set to <c>true</c> an implicit or explicit operator must be defined on the given type.</param>
        public static bool IsCastableTo(this Type from, Type to, bool requireImplicitCast = false)
        {
            if (from == null)
            {
                throw new ArgumentNullException("from");
            }

            if (to == null)
            {
                throw new ArgumentNullException("to");
            }

            if (from == to)
            {
                return true;
            }

            return to.IsAssignableFrom(from) || from.HasCastDefined(to, requireImplicitCast);
        }

        /// <summary>
        /// If a type can be casted to another type, this provides a function to manually convert the type.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="requireImplicitCast">if set to <c>true</c> an implicit or explicit operator must be defined on the given type.</param>
        public static Func<object, object> GetCastMethodDelegate(this Type from, Type to, bool requireImplicitCast = false)
        {
            Func<object, object> result;

            lock (WeaklyTypedTypeCastDelegates_LOCK)
            {
                if (WeaklyTypedTypeCastDelegates.TryGetInnerValue(from, to, out result) == false)
                {
                    var method = GetCastMethod(from, to, requireImplicitCast);

                    if (method != null)
                    {
                        result = obj => method.Invoke(null, new[] { obj });
                    }

                    WeaklyTypedTypeCastDelegates.AddInner(from, to, result);
                }
            }

            return result;
        }

        /// <summary>
        /// If a type can be casted to another type, this provides the method info of the method in charge of converting the type.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="requireImplicitCast">if set to <c>true</c> an implicit or explicit operator must be defined on the given type.</param>
        public static MethodInfo GetCastMethod(this Type from, Type to, bool requireImplicitCast = false)
        {
            var fromMethods = from.GetAllMembers<MethodInfo>(BindingFlags.Public | BindingFlags.Static);

            foreach (var method in fromMethods)
            {
                if ((method.Name == "op_Implicit" || (requireImplicitCast == false && method.Name == "op_Explicit")) && to.IsAssignableFrom(method.ReturnType))
                {
                    return method;
                }
            }

            var toMethods = to.GetAllMembers<MethodInfo>(BindingFlags.Public | BindingFlags.Static);

            foreach (var method in toMethods)
            {
                if ((method.Name == "op_Implicit" || (requireImplicitCast == false && method.Name == "op_Explicit")) && method.GetParameters()[0].ParameterType.IsAssignableFrom(from))
                {
                    return method;
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether a type implements or inherits from another type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="to">To.</param>
        public static bool ImplementsOrInherits(this Type type, Type to)
        {
            return to.IsAssignableFrom(type);
        }

        /// <summary>
        /// Determines whether a type implements an open generic interface such as IList&lt;&gt;.
        /// </summary>
        /// <param name="candidateType">Type of the candidate.</param>
        /// <param name="openGenericInterfaceType">Type of the open generic interface.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentException">Type " + openGenericInterfaceType.Name + " is not a generic type definition and an interface.</exception>
        public static bool ImplementsOpenGenericInterface(this Type candidateType, Type openGenericInterfaceType)
        {
            if (candidateType == openGenericInterfaceType)
                return true;

            if (candidateType.IsGenericType && candidateType.GetGenericTypeDefinition() == openGenericInterfaceType)
                return true;

            var interfaces = candidateType.GetInterfaces();

            for (int i = 0; i < interfaces.Length; i++)
            {
                if (interfaces[i].ImplementsOpenGenericInterface(openGenericInterfaceType))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether a type implements an open generic class such as List&lt;&gt;.
        /// </summary>
        /// <param name="candidateType">Type of the candidate.</param>
        /// <param name="openGenericType">Type of the open generic interface.</param>
        public static bool ImplementsOpenGenericClass(this Type candidateType, Type openGenericType)
        {
            if (candidateType.IsGenericType && candidateType.GetGenericTypeDefinition() == openGenericType)
                return true;

            var baseType = candidateType.BaseType;

            if (baseType != null && baseType.ImplementsOpenGenericClass(openGenericType))
                return true;

            return false;
        }

        /// <summary>
        /// Gets the generic arguments of an inherited open generic class.
        /// </summary>
        /// <param name="candidateType">Type of the candidate.</param>
        /// <param name="openGenericType">Type of the open generic class.</param>
        public static Type[] GetArgumentsOfInheritedOpenGenericClass(this Type candidateType, Type openGenericType)
        {
            if (candidateType.IsGenericType && candidateType.GetGenericTypeDefinition() == openGenericType)
                return candidateType.GetGenericArguments();

            var baseType = candidateType.BaseType;

            if (baseType != null)
                return baseType.GetArgumentsOfInheritedOpenGenericClass(openGenericType);

            return null;
        }

        /// <summary>
        /// Gets the generic arguments of an inherited open generic interface.
        /// </summary>
        /// <param name="candidateType">Type of the candidate.</param>
        /// <param name="openGenericInterfaceType">Type of the open generic interface.</param>
        public static Type[] GetArgumentsOfInheritedOpenGenericInterface(this Type candidateType, Type openGenericInterfaceType)
        {
            // This if clause fixes an "error" in newer .NET Runtimes where enum arrays 
            //   implement interfaces like IList<int>, which will be matched on by Odin
            //   before the IList<TheEnum> interface and cause a lot of issues because
            //   you can't actually use an enum array as if it was an IList<int>.
            if ((openGenericInterfaceType == GenericListInterface || openGenericInterfaceType == GenericCollectionInterface) && candidateType.IsArray)
            {
                return new[] { candidateType.GetElementType() };
            }

            if (candidateType == openGenericInterfaceType)
                return candidateType.GetGenericArguments();

            if (candidateType.IsGenericType && candidateType.GetGenericTypeDefinition() == openGenericInterfaceType)
                return candidateType.GetGenericArguments();

            var interfaces = candidateType.GetInterfaces();

            for (int i = 0; i < interfaces.Length; i++)
            {
                var @interface = interfaces[i];
                if (!@interface.IsGenericType) continue;

                var result = @interface.GetArgumentsOfInheritedOpenGenericInterface(openGenericInterfaceType);

                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// Gets all members of a specific type from a type, including members from all base types, if the <see cref="BindingFlags.DeclaredOnly"/> flag isn't set.
        /// </summary>
        public static IEnumerable<T> GetAllMembers<T>(this Type type, BindingFlags flags = BindingFlags.Default) where T : MemberInfo
        {
            if (type == null) throw new ArgumentNullException("type");
            if (type == typeof(object)) yield break;

            Type currentType = type;

            if ((flags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
            {
                foreach (var member in currentType.GetMembers(flags))
                {
                    var found = member as T;

                    if (found != null)
                    {
                        yield return found;
                    }
                }
            }
            else
            {
                flags |= BindingFlags.DeclaredOnly;

                do
                {
                    foreach (var member in currentType.GetMembers(flags))
                    {
                        var found = member as T;

                        if (found != null)
                        {
                            yield return found;
                        }
                    }

                    currentType = currentType.BaseType;
                }
                while (currentType != null);
            }
        }

        /// <summary>
        /// Used to filter out unwanted type names. Ex "int" instead of "Int32"
        /// </summary>
        private static string TypeNameGauntlet(this Type type)
        {
            string typeName = type.Name;

            string altTypeName = string.Empty;

            if (TypeNameAlternatives.TryGetValue(typeName, out altTypeName))
            {
                typeName = altTypeName;
            }

            return typeName;
        }

        /// <summary>
        /// Returns a nicely formatted name of a type.
        /// </summary>
        public static string GetNiceName(this Type type)
        {
            if (type.IsNested && type.IsGenericParameter == false)
            {
                return type.DeclaringType.GetNiceName() + "." + GetCachedNiceName(type);
            }

            return GetCachedNiceName(type);
        }

        /// <summary>
        /// Returns a nicely formatted full name of a type.
        /// </summary>
        public static string GetNiceFullName(this Type type)
        {
            string result;

            if (type.IsNested && type.IsGenericParameter == false)
            {
                return type.DeclaringType.GetNiceFullName() + "." + GetCachedNiceName(type);
            }

            result = GetCachedNiceName(type);

            if (type.Namespace != null)
            {
                result = type.Namespace + "." + result;
            }

            return result;
        }

        /// <summary>
        /// Gets the full name of the compilable nice.
        /// </summary>
        /// <param name="type">The type.</param>
        public static string GetCompilableNiceFullName(this Type type)
        {
            return type.GetNiceFullName().Replace('<', '_').Replace('>', '_').TrimEnd('_');
        }

        /// <summary>
        /// Returns true if the attribute whose type is specified by the generic argument is defined on this type
        /// </summary>
        public static bool IsDefined<T>(this Type type) where T : Attribute
        {
            return type.IsDefined(typeof(T), false);
        }

        /// <summary>
        /// Determines whether a type inherits or implements another type. Also include support for open generic base types such as List&lt;&gt;.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="baseType"></param>
        public static bool InheritsFrom(this Type type, Type baseType)
        {
            if (baseType.IsAssignableFrom(type))
            {
                return true;
            }

            if (type.IsInterface && baseType.IsInterface == false)
            {
                return false;
            }

            if (baseType.IsInterface)
            {
                return type.GetInterfaces().Contains(baseType);
            }

            var t = type;
            while (t != null)
            {
                if (t == baseType)
                {
                    return true;
                }

                if (baseType.IsGenericTypeDefinition && t.IsGenericType && t.GetGenericTypeDefinition() == baseType)
                {
                    return true;
                }

                t = t.BaseType;
            }

            return false;
        }

        /// <summary>
        /// FieldInfo will return the fieldType, propertyInfo the PropertyType, MethodInfo the return type and EventInfo will return the EventHandlerType.
        /// </summary>
        /// <param name="memberInfo">The MemberInfo.</param>
        public static Type GetReturnType(this MemberInfo memberInfo)
        {
            var fieldInfo = memberInfo as FieldInfo;
            if (fieldInfo != null)
            {
                return fieldInfo.FieldType;
            }

            var propertyInfo = memberInfo as PropertyInfo;
            if (propertyInfo != null)
            {
                return propertyInfo.PropertyType;
            }

            var methodInfo = memberInfo as MethodInfo;
            if (methodInfo != null)
            {
                return methodInfo.ReturnType;
            }

            var eventInfo = memberInfo as EventInfo;
            if (eventInfo != null)
            {
                return eventInfo.EventHandlerType;
            }
            return null;
        }

        /// <summary>
        /// Tries to infer a set of valid generic parameters for a generic type definition, given a subset of known parameters.
        /// </summary>
        /// <param name="genericTypeDefinition">The generic type definition to attempt to infer parameters for.</param>
        /// <param name="inferredParams">The inferred parameters, if inferral was successful.</param>
        /// <param name="knownParameters">The known parameters to infer from.</param>
        /// <returns>True if the parameters could be inferred, otherwise, false.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// genericTypeDefinition is null
        /// or
        /// knownParameters is null
        /// </exception>
        /// <exception cref="System.ArgumentException">The genericTypeDefinition parameter must be a generic type definition.</exception>
        public static bool TryInferGenericParameters(this Type genericTypeDefinition, out Type[] inferredParams, params Type[] knownParameters)
        {
            if (genericTypeDefinition == null)
            {
                throw new ArgumentNullException("genericTypeDefinition");
            }

            if (knownParameters == null)
            {
                throw new ArgumentNullException("knownParameters");
            }

            if (!genericTypeDefinition.IsGenericType)
            {
                throw new ArgumentException("The genericTypeDefinition parameter must be a generic type.");
            }

            lock (GenericConstraintsSatisfaction_LOCK)
            {
                Dictionary<Type, Type> matches = GenericConstraintsSatisfactionInferredParameters;
                matches.Clear();

                Type[] definitions = genericTypeDefinition.GetGenericArguments();

                if (!genericTypeDefinition.IsGenericTypeDefinition)
                {
                    Type[] constructedParameters = definitions;
                    genericTypeDefinition = genericTypeDefinition.GetGenericTypeDefinition();
                    definitions = genericTypeDefinition.GetGenericArguments();

                    int unknownCount = 0;

                    for (int i = 0; i < constructedParameters.Length; i++)
                    {
                        if (!constructedParameters[i].IsGenericParameter && (!constructedParameters[i].IsGenericType || constructedParameters[i].IsFullyConstructedGenericType()))
                        {
                            matches[definitions[i]] = constructedParameters[i];
                        }
                        else
                        {
                            unknownCount++;
                        }
                    }

                    if (unknownCount == knownParameters.Length)
                    {
                        int count = 0;

                        for (int i = 0; i < constructedParameters.Length; i++)
                        {
                            if (constructedParameters[i].IsGenericParameter)
                            {
                                constructedParameters[i] = knownParameters[count++];
                            }
                        }

                        if (genericTypeDefinition.AreGenericConstraintsSatisfiedBy(constructedParameters))
                        {
                            inferredParams = constructedParameters;
                            return true;
                        }
                    }
                }

                if (definitions.Length == knownParameters.Length && genericTypeDefinition.AreGenericConstraintsSatisfiedBy(knownParameters))
                {
                    inferredParams = knownParameters;
                    return true;
                }

                foreach (var type in definitions)
                {
                    if (matches.ContainsKey(type)) continue;

                    var constraints = type.GetGenericParameterConstraints();

                    foreach (var constraint in constraints)
                    {
                        foreach (var parameter in knownParameters)
                        {
                            if (!constraint.IsGenericType)
                            {
                                continue;
                            }

                            Type constraintDefinition = constraint.GetGenericTypeDefinition();

                            var constraintParams = constraint.GetGenericArguments();
                            Type[] paramParams;

                            if (parameter.IsGenericType && constraintDefinition == parameter.GetGenericTypeDefinition())
                            {
                                paramParams = parameter.GetGenericArguments();
                            }
                            else if (constraintDefinition.IsInterface && parameter.ImplementsOpenGenericInterface(constraintDefinition))
                            {
                                paramParams = parameter.GetArgumentsOfInheritedOpenGenericInterface(constraintDefinition);
                            }
                            else if (constraintDefinition.IsClass && parameter.ImplementsOpenGenericClass(constraintDefinition))
                            {
                                paramParams = parameter.GetArgumentsOfInheritedOpenGenericClass(constraintDefinition);
                            }
                            else
                            {
                                continue;
                            }

                            matches[type] = parameter;

                            for (int i = 0; i < constraintParams.Length; i++)
                            {
                                if (constraintParams[i].IsGenericParameter)
                                {
                                    matches[constraintParams[i]] = paramParams[i];
                                }
                            }
                        }
                    }
                }

                if (matches.Count == definitions.Length)
                {
                    inferredParams = new Type[matches.Count];

                    for (int i = 0; i < definitions.Length; i++)
                    {
                        inferredParams[i] = matches[definitions[i]];
                    }

                    if (AreGenericConstraintsSatisfiedBy(genericTypeDefinition, inferredParams))
                    {
                        return true;
                    }
                }

                inferredParams = null;
                return false;
            }
        }
        /// <summary>
        /// <para>Checks whether an array of types satisfy the constraints of a given generic type definition.</para>
        /// <para>If this method returns true, the given parameters can be safely used with <see cref="Type.MakeGenericType(Type[])"/> with the given generic type definition.</para>
        /// </summary>
        /// <param name="genericType">The generic type definition to check.</param>
        /// <param name="parameters">The parameters to check validity for.</param>
        /// <exception cref="System.ArgumentNullException">
        /// genericType is null
        /// or
        /// types is null
        /// </exception>
        /// <exception cref="System.ArgumentException">The genericType parameter must be a generic type definition.</exception>
        public static bool AreGenericConstraintsSatisfiedBy(this Type genericType, params Type[] parameters)
        {
            if (genericType == null)
            {
                throw new ArgumentNullException("genericType");
            }

            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            if (!genericType.IsGenericType)
            {
                throw new ArgumentException("The genericTypeDefinition parameter must be a generic type.");
            }

            return AreGenericConstraintsSatisfiedBy(genericType.GetGenericArguments(), parameters);
        }

        public static bool AreGenericConstraintsSatisfiedBy(Type[] definitions, Type[] parameters)
        {
            if (definitions.Length != parameters.Length)
            {
                return false;
            }

            lock (GenericConstraintsSatisfaction_LOCK)
            {
                Dictionary<Type, Type> resolvedMap = GenericConstraintsSatisfactionResolvedMap;
                resolvedMap.Clear();

                for (int i = 0; i < definitions.Length; i++)
                {
                    Type definition = definitions[i];
                    Type parameter = parameters[i];

                    if (!definition.GenericParameterIsFulfilledBy(parameter, resolvedMap))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Before calling this method we must ALWAYS hold a lock on the GenericConstraintsSatisfaction_LOCK object, as that is an implicit assumption it works with.
        /// </summary>
        private static bool GenericParameterIsFulfilledBy(this Type genericParameterDefinition, Type parameterType, Dictionary<Type, Type> resolvedMap, HashSet<Type> processedParams = null)
        {
            if (genericParameterDefinition == null)
            {
                throw new ArgumentNullException("genericParameterDefinition");
            }

            if (parameterType == null)
            {
                throw new ArgumentNullException("parameterType");
            }

            if (resolvedMap == null)
            {
                throw new ArgumentNullException("resolvedMap");
            }

            if (genericParameterDefinition.IsGenericParameter == false && genericParameterDefinition == parameterType)
            {
                return true;
            }

            if (genericParameterDefinition.IsGenericParameter == false)
            {
                return false;
            }

            if (processedParams == null)
            {
                processedParams = GenericConstraintsSatisfactionProcessedParams; // This is safe because we are currently holding the lock
                processedParams.Clear();
            }

            processedParams.Add(genericParameterDefinition);

            // First, check up on the special constraint flags
            GenericParameterAttributes specialConstraints = genericParameterDefinition.GenericParameterAttributes;

            if (specialConstraints != GenericParameterAttributes.None)
            {
                // Struct constraint (must not be nullable)
                if ((specialConstraints & GenericParameterAttributes.NotNullableValueTypeConstraint) == GenericParameterAttributes.NotNullableValueTypeConstraint)
                {
                    if (!parameterType.IsValueType || (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                    {
                        return false;
                    }
                }
                // Class constraint
                else if ((specialConstraints & GenericParameterAttributes.ReferenceTypeConstraint) == GenericParameterAttributes.ReferenceTypeConstraint)
                {
                    if (parameterType.IsValueType)
                    {
                        return false;
                    }
                }

                // Must have a public parameterless constructor
                if ((specialConstraints & GenericParameterAttributes.DefaultConstructorConstraint) == GenericParameterAttributes.DefaultConstructorConstraint)
                {
                    if (parameterType.IsAbstract || (!parameterType.IsValueType && parameterType.GetConstructor(Type.EmptyTypes) == null))
                    {
                        return false;
                    }
                }
            }

            // If this parameter has already been resolved to a type, check if that resolved type is assignable with the argument type
            if (resolvedMap.ContainsKey(genericParameterDefinition))
            {
                if (!parameterType.IsAssignableFrom(resolvedMap[genericParameterDefinition]))
                {
                    return false;
                }
            }

            // Then, check up on the actual type constraints, of which there can be three kinds:
            // Type inheritance, Interface implementation and fulfillment of another generic parameter.
            Type[] constraints = genericParameterDefinition.GetGenericParameterConstraints();

            for (int i = 0; i < constraints.Length; i++)
            {
                Type constraint = constraints[i];

                // Replace resolved constraint parameters with their resolved types
                if (constraint.IsGenericParameter && resolvedMap.ContainsKey(constraint))
                {
                    constraint = resolvedMap[constraint];
                }

                if (constraint.IsGenericParameter)
                {
                    if (!constraint.GenericParameterIsFulfilledBy(parameterType, resolvedMap, processedParams))
                    {
                        return false;
                    }
                }
                else if (constraint.IsClass || constraint.IsInterface || constraint.IsValueType)
                {
                    if (constraint.IsGenericType)
                    {
                        Type constraintDefinition = constraint.GetGenericTypeDefinition();

                        Type[] constraintParams = constraint.GetGenericArguments();
                        Type[] paramParams;

                        if (parameterType.IsGenericType && constraintDefinition == parameterType.GetGenericTypeDefinition())
                        {
                            paramParams = parameterType.GetGenericArguments();
                        }
                        else
                        {
                            if (constraintDefinition.IsClass)
                            {
                                if (parameterType.ImplementsOpenGenericClass(constraintDefinition))
                                {
                                    paramParams = parameterType.GetArgumentsOfInheritedOpenGenericClass(constraintDefinition);
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                if (parameterType.ImplementsOpenGenericInterface(constraintDefinition))
                                {
                                    paramParams = parameterType.GetArgumentsOfInheritedOpenGenericInterface(constraintDefinition);
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }

                        for (int j = 0; j < constraintParams.Length; j++)
                        {
                            var c = constraintParams[j];
                            var p = paramParams[j];

                            // Replace resolved constraint parameters with their resolved types
                            if (c.IsGenericParameter && resolvedMap.ContainsKey(c))
                            {
                                c = resolvedMap[c];
                            }

                            if (c.IsGenericParameter)
                            {
                                if (!processedParams.Contains(c) && !GenericParameterIsFulfilledBy(c, p, resolvedMap, processedParams))
                                {
                                    return false;
                                }
                            }
                            else if (c != p && !c.IsAssignableFrom(p))
                            {
                                return false;
                            }
                        }
                    }
                    else if (!constraint.IsAssignableFrom(parameterType))
                    {
                        return false;
                    }
                }
                else
                {
                    throw new Exception("Unknown parameter constraint type! " + constraint.GetNiceName());
                }
            }

            resolvedMap[genericParameterDefinition] = parameterType;
            return true;
        }

        /// <summary>
        /// Determines whether a type is a fully constructed generic type.
        /// </summary>
        public static bool IsFullyConstructedGenericType(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (type.IsGenericTypeDefinition)
            {
                return false;
            }

            if (type.HasElementType)
            {
                var element = type.GetElementType();
                if (element.IsGenericParameter || element.IsFullyConstructedGenericType() == false)
                {
                    return false;
                }
            }

            var args = type.GetGenericArguments();

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg.IsGenericParameter)
                {
                    return false;
                }

                if (!arg.IsFullyConstructedGenericType())
                {
                    return false;
                }
            }

            return !type.IsGenericTypeDefinition;

            //if (type.IsGenericType == false || type.IsGenericTypeDefinition)
            //{
            //    return false;
            //}

            //var args = type.GetGenericArguments();

            //for (int i = 0; i < args.Length; i++)
            //{
            //    var arg = args[i];

            //    if (arg.IsGenericParameter)
            //    {
            //        return false;
            //    }
            //    else if (arg.IsGenericType && !arg.IsFullyConstructedGenericType())
            //    {
            //        return false;
            //    }
            //}

            //return true;
        }

        public static bool SafeIsDefined(this Assembly assembly, Type attribute, bool inherit)
        {
            try
            {
                return assembly.IsDefined(attribute, inherit);
            }
            catch
            {
                return false;
            }
        }

        public static object[] SafeGetCustomAttributes(this Assembly assembly, Type type, bool inherit)
        {
            try
            {
                return assembly.GetCustomAttributes(type, inherit);
            }
            catch
            {
                return new object[0];
            }
        }
    }
}