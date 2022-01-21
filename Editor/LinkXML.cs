namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    public class LinkXML
    {
        private readonly Dictionary<string, Assembly> _assemblies = new Dictionary<string, Assembly>();

        public void AddMethods(IEnumerable<MethodInfo> methods)
        {
            foreach (var method in methods)
            {
                AddMethod(method);
            }
        }
        
        public void AddMethod(MethodInfo methodInfo)
        {
            var declaringType = methodInfo.DeclaringType;
            // ReSharper disable once PossibleNullReferenceException
            string assemblyName = declaringType.Assembly.GetName().Name;

            if (!_assemblies.TryGetValue(assemblyName, out var assembly))
            {
                assembly = new Assembly(assemblyName);
                _assemblies.Add(assemblyName, assembly);
            }

            string declaringTypeName = declaringType.FullName;
            // ReSharper disable once AssignNullToNotNullAttribute
            if (!assembly.Types.TryGetValue(declaringTypeName, out var type))
            {
                type = new ClassType(declaringTypeName);
                assembly.Types.Add(declaringTypeName, type);
            }

            var method = new Method(methodInfo.Name, methodInfo.ReturnType.FullName, GetParameterTypeNames(methodInfo));
            type.Methods.Add(method);
        }

        public string Generate()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("<linker>");

            foreach (var assembly in _assemblies.Values)
            {
                assembly.Generate(stringBuilder);
            }
            
            stringBuilder.AppendLine("</linker>");

            return stringBuilder.ToString();
        }

        private static string[] GetParameterTypeNames(MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            var typeNames = new string[parameters.Length];

            for (int i = 0; i < typeNames.Length; i++)
            {
                typeNames[i] = parameters[i].ParameterType.FullName;
            }

            return typeNames;
        }

        private class Assembly : IEquatable<Assembly>
        {
            private readonly string _fullName;
            public readonly Dictionary<string, ClassType> Types = new Dictionary<string, ClassType>();

            public Assembly(string fullName)
            {
                _fullName = fullName;
            }

            public void Generate(StringBuilder stringBuilder)
            {
                stringBuilder.Append("  <assembly fullname=\"").Append(_fullName).AppendLine("\">");

                foreach (var type in Types.Values)
                {
                    type.Generate(stringBuilder);
                }

                stringBuilder.AppendLine("  </assembly>");
            }

            #region Equality

            public override bool Equals(object obj) => this.Equals(obj as Assembly);

            public bool Equals(Assembly other)
            {
                if (other is null)
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return _fullName == other._fullName;
            }

            public override int GetHashCode() => _fullName.GetHashCode();

            public static bool operator ==(Assembly lhs, Assembly rhs)
            {
                if (lhs is null)
                {
                    if (rhs is null)
                    {
                        return true;
                    }

                    return false;
                }
                
                return lhs.Equals(rhs);
            }

            public static bool operator !=(Assembly lhs, Assembly rhs) => !(lhs == rhs);

            #endregion
        }

        private class ClassType : IEquatable<ClassType>
        {
            private readonly string _fullName;
            public readonly HashSet<Method> Methods = new HashSet<Method>();
            
            public ClassType(string fullName)
            {
                _fullName = fullName;
            }

            public void Generate(StringBuilder stringBuilder)
            {
                stringBuilder.Append("    <type fullname=\"").Append(_fullName).AppendLine("\">");

                foreach (Method method in Methods)
                {
                    stringBuilder.AppendLine(method.Generate());
                }

                stringBuilder.AppendLine("    </type>");
            }
            
            #region Equality

            public override bool Equals(object obj) => this.Equals(obj as ClassType);

            public bool Equals(ClassType other)
            {
                if (other is null)
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return _fullName == other._fullName;
            }

            public override int GetHashCode() => _fullName.GetHashCode();

            public static bool operator ==(ClassType lhs, ClassType rhs)
            {
                if (lhs is null)
                {
                    if (rhs is null)
                    {
                        return true;
                    }

                    return false;
                }
                
                return lhs.Equals(rhs);
            }

            public static bool operator !=(ClassType lhs, ClassType rhs) => !(lhs == rhs);

            #endregion
        }

        private class Method : IEquatable<Method>
        {
            private readonly string _name;
            private readonly string _returnTypeFullName;
            private readonly string[] _argumentTypesFullNames;
            
            public Method(string name, string returnTypeFullName, string[] argumentTypesFullNames)
            {
                _name = name;
                _returnTypeFullName = returnTypeFullName;
                _argumentTypesFullNames = argumentTypesFullNames;
            }

            public string Generate()
            {
                return _name.IsPropertySetter() ? GenerateProperty() : GenerateMethod();
            }

            private string GenerateProperty()
            {
                return $"      <property signature=\"{_returnTypeFullName} {_name}\" accessors=\"set\" />";
            }

            private string GenerateMethod()
            {
                return $"      <method signature=\"{_returnTypeFullName} {_name}({string.Join(",", _argumentTypesFullNames)})\" />";
            }
            
            #region Equality

            public override bool Equals(object obj) => this.Equals(obj as Method);

            public bool Equals(Method other)
            {
                if (other is null)
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return _name == other._name && _returnTypeFullName == other._returnTypeFullName && _argumentTypesFullNames.SequenceEqual(_argumentTypesFullNames);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    
                    hash = hash * 31 + _name.GetHashCode();
                    hash = hash * 31 + _returnTypeFullName.GetHashCode();
                    
                    foreach (var element in _argumentTypesFullNames)
                    {
                        hash = hash * 31 + element.GetHashCode();
                    }
                    
                    return hash;
                }
            }

            public static bool operator ==(Method lhs, Method rhs)
            {
                if (lhs is null)
                {
                    if (rhs is null)
                    {
                        return true;
                    }

                    return false;
                }
                
                return lhs.Equals(rhs);
            }

            public static bool operator !=(Method lhs, Method rhs) => !(lhs == rhs);

            #endregion
        }
    }
}