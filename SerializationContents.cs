using NetworkIO.Threading.Collection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace NetworkIO.Serialization
{
    /// <summary>
    /// Serileştirme içerisinde bulunabilme ihtimali olan tipleri önceden imzalar. Karşılıklı her iki serileştirici için aynı tiplerin eklenmesi gerekir.
    /// En çok kullanılan tiplerin eklenmesi durumunda serileştirme boyutunca ciddi bir düşüş elde edilir. Eklenen tiple ilgili her bilgiyi işaretler.
    /// Örneğin List<object> türü bir eklemede List<>, object ve bunların bulunduğu mscorlib üzerindeki namespace ve assembly bilgileri de işaretlenir.
    /// Kurucudaki AddPrimitives'in true olması ile tüm primitive tipler içeriğe eklenir.
    /// </summary>
    public class SerializationContents
    {
        private static bool _initReflectionContents;
        private static List<Type> _reflectionRefAndOwnsTypes;
        private static void initialize()
        {
            if (SerializationContents._initReflectionContents)
                return;

            SerializationContents._reflectionRefAndOwnsTypes = new List<Type>();

            
            MethodBase currentMethod = MethodBase.GetCurrentMethod();
            SerializationInfo info = new SerializationInfo(typeof(MethodInfo), SerializationStaticHelpers.FormatterConverter);
            ((ISerializable)currentMethod).GetObjectData(info, SerializationStaticHelpers.StreamingContext);

            SerializationContents._reflectionRefAndOwnsTypes.Add(Type.GetType(info.FullTypeName + ", " + info.AssemblyName));
            SerializationContents._reflectionRefAndOwnsTypes.Add(currentMethod.GetType());
            
            
            FieldInfo anyField = typeof(SerializationContents).GetFields(SerializationStaticHelpers.SerializationMemberBindings)[0];
            info = new SerializationInfo(typeof(FieldInfo), SerializationStaticHelpers.FormatterConverter);
            ((ISerializable)anyField).GetObjectData(info, SerializationStaticHelpers.StreamingContext);

            SerializationContents._reflectionRefAndOwnsTypes.Add(Type.GetType(info.FullTypeName + ", " + info.AssemblyName));
            SerializationContents._reflectionRefAndOwnsTypes.Add(anyField.GetType());


            PropertyInfo anyProperty = typeof(SerializationContents).GetProperties(SerializationStaticHelpers.SerializationMemberBindings)[0];
            info = new SerializationInfo(typeof(PropertyInfo), SerializationStaticHelpers.FormatterConverter);
            ((ISerializable)anyProperty).GetObjectData(info, SerializationStaticHelpers.StreamingContext);

            SerializationContents._reflectionRefAndOwnsTypes.Add(Type.GetType(info.FullTypeName + ", " + info.AssemblyName));
            SerializationContents._reflectionRefAndOwnsTypes.Add(anyProperty.GetType());


            Type anyType = SerializationStaticHelpers.RuntimeType;
            info = new SerializationInfo(typeof(PropertyInfo), SerializationStaticHelpers.FormatterConverter);
            ((ISerializable)anyType).GetObjectData(info, SerializationStaticHelpers.StreamingContext);

            SerializationContents._reflectionRefAndOwnsTypes.Add(Type.GetType(info.FullTypeName + ", " + info.AssemblyName));
            SerializationContents._reflectionRefAndOwnsTypes.Add(anyType);

            SerializationContents._initReflectionContents = true;
        }

        public const int MaxAssemblyStack = 256;
        public const int MaxTypeStack = 65536;
        public const int MaxStringStack = 32768;

        internal List<string> _AssemblyContents;
        internal List<string> _TypeContents;
        internal List<string> _StringContents;

        public int AssemblyCount { get { return this._AssemblyContents.Count; } }
        public TypeBinder TypeBinder { get; internal set; }
        public int TypeCount { get { return this._TypeContents.Count; } }
        public int StringCount { get { return this._StringContents.Count; } }

        public SerializationContents()
            : this(null)
        {

        }
        public SerializationContents(TypeBinder binder)
        {
            this._AssemblyContents = new List<string>();
            this._TypeContents = new List<string>();
            this._StringContents = new List<string>();
            this.TypeBinder = binder;
        }

        public void Add(Type wellKnownTypeAndAssembly)
        {
            TypeCode code = Type.GetTypeCode(wellKnownTypeAndAssembly);
            if (code == TypeCode.Object)
            {
                this.internalAdd(wellKnownTypeAndAssembly);
                this._AssemblyContents.Sort();
                this._TypeContents.Sort();
            }
            else
                throw new InvalidOperationException("Primitive types not required to add content. Trying for adding type is " + code);
        }
        public void Add(string wellKnownString)
        {
            if (!this._StringContents.Contains(wellKnownString))
            {
                if (this._StringContents.Count >= SerializationContents.MaxStringStack)
                    throw new IndexOutOfRangeException("Maximum string stack count is " + SerializationContents.MaxStringStack);

                this._StringContents.Add(wellKnownString);
                this._StringContents.Sort();
            }
        }
        public bool Contains(Type wellKnownTypeAndAssembly)
        {
            TypeCode code = Type.GetTypeCode(wellKnownTypeAndAssembly);
            if (code == TypeCode.Object)
            {
                Queue<Type> controlTypeQueue = new Queue<Type>();
                List<Type> baseTypes = new List<Type>();
                controlTypeQueue.Enqueue(wellKnownTypeAndAssembly);

                while (controlTypeQueue.Count > 0)
                {
                    Type currentType = controlTypeQueue.Dequeue();

                    if (currentType.IsArray)
                        controlTypeQueue.Enqueue(currentType.GetElementType());
                    else if (currentType.IsGenericType)
                    {
                        if (currentType.IsGenericTypeDefinition)
                            baseTypes.Add(currentType);
                        else
                        {
                            Type[] genericArgs = currentType.GetGenericArguments();
                            baseTypes.Add(currentType.GetGenericTypeDefinition());
                            for (int i = 0; i < genericArgs.Length; i++)
                                controlTypeQueue.Enqueue(genericArgs[i]);
                        }
                    }
                    else
                        baseTypes.Add(currentType);
                }

                for (int i = 0; i < baseTypes.Count; i++)
                {
                    Type current = baseTypes[i];
                    code = Type.GetTypeCode(current);
                    if (code == TypeCode.Object)
                    {
                        string boundAssemblyName = current.Assembly.FullName, boundTypeName = current.FullName;
                        if (this.TypeBinder != null)
                            this.TypeBinder.BindToName(current, ref boundAssemblyName, ref boundTypeName);
                        
                        int asmId = this._AssemblyContents.IndexOf(boundAssemblyName);
                        int typeId = this._TypeContents.IndexOf(boundTypeName);

                        if (asmId == -1 || typeId == -1)
                            return false;
                    }
                   
                }
                return true;
            }
            else return true;
        }
        public void FillReflectionContents()
        {
            SerializationContents.initialize();
            for (int i = 0; i < SerializationContents._reflectionRefAndOwnsTypes.Count; i++)
                this.Add(SerializationContents._reflectionRefAndOwnsTypes[i]);
        }

        private void internalAdd(Type content)
        {
            if (content.IsArray)
                content = content.GetElementType();

            TypeCode code = Type.GetTypeCode(content);
            if (code != TypeCode.Object)
                return;
            
            if (content.IsGenericType && !content.IsGenericTypeDefinition)
            {
                Type genericDef = content.GetGenericTypeDefinition();
                this.internalAdd(genericDef);
                Type[] genericArguments = content.GetGenericArguments();
                for (int i = 0; i < genericArguments.Length; i++)
                    this.internalAdd(genericArguments[i]);
            }
            else
            {
                string boundAssemblyName = content.Assembly.FullName, boundTypeName = content.FullName;
                if (this.TypeBinder != null)
                    this.TypeBinder.BindToName(content, ref boundAssemblyName, ref boundTypeName);
                    
                int asmId = this._AssemblyContents.IndexOf(boundAssemblyName);
                int typeId = this._TypeContents.IndexOf(boundTypeName);

                if (asmId == -1)
                {
                    if (this._AssemblyContents.Count >= SerializationContents.MaxAssemblyStack)
                        throw new IndexOutOfRangeException("Maximum assembly stack count is " + SerializationContents.MaxAssemblyStack);

                    asmId = (byte)this._AssemblyContents.Count;
                    this._AssemblyContents.Add(boundAssemblyName);

                    typeId = this._TypeContents.Count;
                    this._TypeContents.Add(boundTypeName);
                    return;
                }

                if (typeId == -1)
                {
                    if (this._TypeContents.Count >= SerializationContents.MaxTypeStack)
                        throw new IndexOutOfRangeException("Maximum type stack count is " + SerializationContents.MaxTypeStack);

                    typeId = this._TypeContents.Count;
                    this._TypeContents.Add(boundTypeName);
                }
            }
        }
    }
}
