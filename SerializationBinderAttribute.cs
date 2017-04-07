using System;
using System.Collections.Generic;

namespace NetworkIO.Serialization
{
    /// <summary>
    /// ObjectSerializer içerisindeki TypeBinder özelliğinin AttributeBinder olması, eğer özellik ChainTypeBinder ise de zincir içerisinde AttributeBinder nesnesinin bulunması koşulunda çalışır.
    /// Hızlı bir şekilde BindToName ve BindToType yapmaya yarar.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public class SerializationBinderAttribute : Attribute
    {
        private static Dictionary<string, Dictionary<string, Type>> _boundTypes = new Dictionary<string, Dictionary<string, Type>>();
        public static Dictionary<string, Dictionary<string, Type>> BoundTypes { get { return SerializationBinderAttribute._boundTypes; } private set { SerializationBinderAttribute._boundTypes = value; } }

        public Type BoundType { get; private set; }
        public string BoundTypeName { get; private set; }
        public string BoundAssemblyName { get; private set; }

        public SerializationBinderAttribute(Type boundType)
        {
            string assemblyName = boundType.Assembly.FullName;
            string typeName = boundType.FullName;
            Dictionary<string, Type> nameToTypeDict = null;
            if (SerializationBinderAttribute._boundTypes.ContainsKey(assemblyName))
            {
                nameToTypeDict = SerializationBinderAttribute._boundTypes[assemblyName];
                if (!nameToTypeDict.ContainsKey(typeName))
                    nameToTypeDict.Add(typeName, boundType);
            }
            else
            {
                nameToTypeDict = new Dictionary<string, Type>();
                SerializationBinderAttribute._boundTypes.Add(assemblyName, nameToTypeDict);
                nameToTypeDict.Add(typeName, boundType);
            }
        }
        public SerializationBinderAttribute(string boundAssemblyName, string boundTypeName, Type boundType)
        {
            Dictionary<string, Type> nameToTypeDict = null;
            if (SerializationBinderAttribute._boundTypes.ContainsKey(boundAssemblyName))
            {
                nameToTypeDict = SerializationBinderAttribute._boundTypes[boundAssemblyName];
                if (!nameToTypeDict.ContainsKey(boundTypeName))
                    nameToTypeDict.Add(boundTypeName, boundType);
            }
            else
            {
                nameToTypeDict = new Dictionary<string, Type>();
                SerializationBinderAttribute._boundTypes.Add(boundAssemblyName, nameToTypeDict);
                nameToTypeDict.Add(boundTypeName, boundType);
            }
            this.BoundAssemblyName = boundAssemblyName;
            this.BoundTypeName = boundTypeName;
        }
    }
}
