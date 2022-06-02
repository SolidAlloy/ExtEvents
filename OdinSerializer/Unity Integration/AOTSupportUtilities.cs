//-----------------------------------------------------------------------
// <copyright file="AOTSupportUtilities.cs" company="Sirenix IVS">
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

#if UNITY_EDITOR

namespace ExtEvents.OdinSerializer.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using UnityEngine;
    using Utilities;
    using Object = UnityEngine.Object;

    public static class AOTSupportUtilities
    {
        public static void GenerateCode(ILGenerator il, HashSet<Type> types)
        {
            var falseLocal = il.DeclareLocal(typeof(bool));

            il.Emit(OpCodes.Ldc_I4_0);                  // Load false
            il.Emit(OpCodes.Stloc, falseLocal);         // Set to falseLocal

            if (UnityVersion.Major == 2019 && UnityVersion.Minor == 2)
            {
                // This here is a hack that fixes Unity's assembly updater triggering faultily in Unity 2019.2
                // (and in early 2019.3 alphas/betas, but we're not handling those). When it triggers, it edits
                // the generated AOT assembly such that it immediately causes Unity to hard crash. Having this
                // type reference present in the assembly prevents that from happening. (Any concrete type in
                // the serialization assembly would work, this one is just a random pick.)
                //
                // Unity should have fixed this in 2019.3, but said that a backport to 2019.2 is not guaranteed
                // to occur, though it might.
                types.Add(typeof(DateTimeFormatter));
            }

            foreach (var serializedType in types)
            {
                if (serializedType == null)
                    continue;

                bool isAbstract = serializedType.IsAbstract || serializedType.IsInterface;

                if (serializedType.IsGenericType && (serializedType.IsGenericTypeDefinition || !serializedType.IsFullyConstructedGenericType()))
                {
                    Debug.LogError("Skipping type '" + serializedType.GetNiceFullName() + "'! Type is a generic type definition, or its arguments contain generic parameters; type must be a fully constructed generic type.");
                    continue;
                }

                // Reference serialized type
                if (serializedType.IsValueType)
                {
                    var local = il.DeclareLocal(serializedType);

                    il.Emit(OpCodes.Ldloca, local);
                    il.Emit(OpCodes.Initobj, serializedType);
                }
                else if (!isAbstract)
                {
                    var constructor = serializedType.GetConstructor(Type.EmptyTypes);

                    if (constructor != null)
                    {
                        il.Emit(OpCodes.Newobj, constructor);
                        il.Emit(OpCodes.Pop);
                    }
                }

                // Reference and/or create formatter type
                if (!FormatterUtilities.IsPrimitiveType(serializedType) && !typeof(Object).IsAssignableFrom(serializedType) && !isAbstract)
                {
                    var actualFormatter = FormatterLocator.GetFormatter(serializedType, SerializationPolicies.Unity);

                    if (actualFormatter.GetType().IsDefined<EmittedFormatterAttribute>())
                    {
                        //TODO: Make emitted formatter code compatible with IL2CPP
                        //// Emit an actual AOT formatter into the generated assembly

                        //if (this.emitAOTFormatters)
                        //{
                        //    var emittedFormatter = FormatterEmitter.EmitAOTFormatter(typeEntry.Type, module, SerializationPolicies.Unity);
                        //    var emittedFormatterConstructor = emittedFormatter.GetConstructor(Type.EmptyTypes);

                        //    il.Emit(OpCodes.Newobj, emittedFormatterConstructor);
                        //    il.Emit(OpCodes.Pop);
                        //}
                    }

                    var formatters = FormatterLocator.GetAllCompatiblePredefinedFormatters(serializedType, SerializationPolicies.Unity);

                    foreach (var formatter in formatters)
                    {
                        // Reference the pre-existing formatter

                        var formatterConstructor = formatter.GetType().GetConstructor(Type.EmptyTypes);

                        if (formatterConstructor != null)
                        {
                            il.Emit(OpCodes.Newobj, formatterConstructor);
                            il.Emit(OpCodes.Pop);
                        }
                    }

                    //// Make sure we have a proper reflection formatter variant if all else goes wrong
                    //il.Emit(OpCodes.Newobj, typeof(ReflectionFormatter<>).MakeGenericType(serializedType).GetConstructor(Type.EmptyTypes));
                    //il.Emit(OpCodes.Pop);
                }

                ConstructorInfo serializerConstructor;

                // Reference serializer variant
                if (serializedType.IsValueType)
                {
                    serializerConstructor = Serializer.Get(serializedType).GetType().GetConstructor(Type.EmptyTypes);

                    il.Emit(OpCodes.Newobj, serializerConstructor);

                    // The following section is a fix for an issue on IL2CPP for PS4, where sometimes bytecode isn't
                    //   generated for methods in base types of needed types - FX, Serializer<T>.ReadValueWeak()
                    //   may be missing. This only seems to happen in a relevant way for value types.
                    {
                        var endLabel = il.DefineLabel();

                        // Load a false local value, then jump to the end of this segment of code due to that
                        //   false value. This is an attempt to trick any potential code flow analysis made
                        //   by IL2CPP that checks whether this segment of code is actually run.
                        //
                        // We don't run the code because if we did, that would actually throw a bunch of
                        //   exceptions from invalid calls to ReadValueWeak and WriteValueWeak.
                        il.Emit(OpCodes.Ldloc, falseLocal);
                        il.Emit(OpCodes.Brfalse, endLabel);

                        var baseSerializerType = typeof(Serializer<>).MakeGenericType(serializedType);

                        var readValueWeakMethod = baseSerializerType.GetMethod("ReadValueWeak", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new[] { typeof(IDataReader) }, null);
                        var writeValueWeakMethod = baseSerializerType.GetMethod("WriteValueWeak", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new[] { typeof(string), typeof(object), typeof(IDataWriter) }, null);

                        il.Emit(OpCodes.Dup);                               // Duplicate serializer instance
                        il.Emit(OpCodes.Ldnull);                            // Load null argument for IDataReader reader
                        il.Emit(OpCodes.Callvirt, readValueWeakMethod);     // Call ReadValueWeak on serializer instance
                        il.Emit(OpCodes.Pop);                               // Pop result of ReadValueWeak

                        il.Emit(OpCodes.Dup);                               // Duplicate serializer instance
                        il.Emit(OpCodes.Ldnull);                            // Load null argument for string name
                        il.Emit(OpCodes.Ldnull);                            // Load null argument for object value
                        il.Emit(OpCodes.Ldnull);                            // Load null argument for IDataWriter writer
                        il.Emit(OpCodes.Callvirt, writeValueWeakMethod);    // Call WriteValueWeak on serializer instance

                        il.MarkLabel(endLabel);                             // This is where the code always jumps to, skipping the above
                    }

                    il.Emit(OpCodes.Pop);       // Pop the serializer instance
                }
                else
                {
                    serializerConstructor = typeof(ComplexTypeSerializer<>).MakeGenericType(serializedType).GetConstructor(Type.EmptyTypes);
                    il.Emit(OpCodes.Newobj, serializerConstructor);
                    il.Emit(OpCodes.Pop);
                }
            }
        }
    }
}

#endif
