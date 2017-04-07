using System;
using System.Collections.Generic;

namespace NetworkIO.Serialization
{
    public abstract class TypeBinder
    {

        public abstract void BindToName(Type serializedType, ref string assemblyName, ref string typeName);
        public abstract Type BindToType(string assemblyName, string typeName);
    }
    /// <summary>
    /// TypeBinder zinciri oluşturur. İçerisine eklenen tüm binder'lar üzerinden BindToName ve BindToType bilgisini kontrol eder. Zincir üzerinde ilk bulduğu değeri döndürür.
    /// Eğer hiç bir binder üzerinde istenen tipin bağlanma bilgisi yoksa, otomatik olarak tipin kendi Type.FullName değerini döndürür ya da verilen isimler üzerinden Type.GetType(string) ise 
    /// tipi arar. Yoksa null değer döner.
    /// </summary>
    public sealed class TypeBinderChain : TypeBinder
    {
        private List<TypeBinder> _binderList;

        public TypeBinderChain()
        {
            this._binderList = new List<TypeBinder>();
        }

        public void AddBinder(TypeBinder binder)
        {
            if (!this._binderList.Contains(binder))
                this._binderList.Add(binder);
        }
        public override void BindToName(Type serializedType, ref string assemblyName, ref string typeName)
        {
            for (int i = 0; i < this._binderList.Count; i++)
            {
                this._binderList[i].BindToName(serializedType, ref assemblyName, ref typeName);
                if (assemblyName == null || typeName == null)
                    continue;
                else return;
            }
            assemblyName = serializedType.Assembly.FullName;
            typeName = serializedType.FullName;
        }
        public override Type BindToType(string assemblyName, string typeName)
        {
            for (int i = 0; i < this._binderList.Count; i++)
            {
                Type boundType = this._binderList[i].BindToType(assemblyName, typeName);
                if (boundType == null)
                    continue;
                else return boundType;
            }
            return Type.GetType(typeName + ", " + assemblyName);
        }
    }
    public sealed class TypeBinderDictionary : TypeBinder
    {
        private Dictionary<string, Dictionary<string, Type>> _asmNameToType;
        private Dictionary<Type, AsmTypePair> _typeToNames;

        public TypeBinderDictionary()
        {
            this._asmNameToType = new Dictionary<string, Dictionary<string, Type>>();
            this._typeToNames = new Dictionary<Type, AsmTypePair>();
        }

        public void Add(string boundAssemblyName, string boundTypeName, Type type)
        {
            if (this._asmNameToType.ContainsKey(boundAssemblyName))
            {
                Dictionary<string, Type> nameToType = this._asmNameToType[boundAssemblyName];
                if (nameToType.ContainsKey(boundTypeName))
                    throw new ArgumentException("Assembly ve TypeName zaten içeriyor.");
                else
                    nameToType.Add(boundTypeName, type);
            }
            else
            {
                Dictionary<string, Type> nameToType = new Dictionary<string, Type>();
                this._asmNameToType.Add(boundAssemblyName, nameToType);
                nameToType.Add(boundTypeName, type);
            }

            if (this._typeToNames.ContainsKey(type))
                throw new ArgumentException("Tip zaten eklenmiş.");
            else
            {
                AsmTypePair pair = new AsmTypePair();
                pair.AssemblyName = boundAssemblyName;
                pair.TypeName = boundTypeName;
                this._typeToNames.Add(type, pair);
            }
        }

        public override void BindToName(Type serializedType, ref string assemblyName, ref string typeName)
        {
            if (this._typeToNames.ContainsKey(serializedType))
            {
                AsmTypePair pair = this._typeToNames[serializedType];
                assemblyName = pair.AssemblyName;
                typeName = pair.TypeName;
            }
        }
        public override Type BindToType(string assemblyName, string typeName)
        {
            if (this._asmNameToType.ContainsKey(assemblyName))
            {
                Dictionary<string, Type> nameToType = this._asmNameToType[assemblyName];
                if (nameToType.ContainsKey(typeName))
                {
                    return nameToType[typeName];
                }
                else return null;
            }
            else return null;
        }

        private class AsmTypePair
        {
            public string AssemblyName;
            public string TypeName;
        }
    }

}
