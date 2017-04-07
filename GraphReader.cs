using NetworkIO;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace NetworkIO.Serialization
{
    internal class GraphReader
    {
        private TypeBinder _binder;
        private StreamingContext _context;
        private List<string> _readAssemblies;
        private ReaderBase _reader;
        private Dictionary<ushort, object> _readObjects;
        private List<string> _readTypes;
        private ISurrogateSelector _selector;
        private List<string> _stringContents;


        internal GraphReader(TypeBinder binder, ReaderBase reader, ISurrogateSelector selector)
            : this(binder, reader, selector, SerializationStaticHelpers.StreamingContext, null)
        {

        }
        internal GraphReader(TypeBinder binder, ReaderBase reader, ISurrogateSelector selector, StreamingContext context)
            : this(binder, reader, selector, context, null)
        {

        }
        internal GraphReader(TypeBinder binder, ReaderBase reader, ISurrogateSelector selector, SerializationContents contents)
            : this(binder, reader, selector, SerializationStaticHelpers.StreamingContext, contents)
        {

        }
        internal GraphReader(TypeBinder binder, ReaderBase reader, ISurrogateSelector selector, StreamingContext context, SerializationContents contents)
        {
            this._binder = binder;
            this._reader = reader;
            this._readObjects = new Dictionary<ushort, object>();
            if (contents == null)
            {
                this._readAssemblies = new List<string>();
                this._readTypes = new List<string>();
                this._stringContents = new List<string>();
            }
            else
            {
                this._readAssemblies = new List<string>(contents._AssemblyContents);
                this._readTypes = new List<string>(contents._TypeContents);
                this._stringContents = new List<string>(contents._StringContents);
            }
            this._selector = selector;
            this._context = context;
        }

        public object Deserialize(Type expectedType)
        {
            TypeCode code = (TypeCode)this._reader.ReadByte();

            if (code == TypeCode.Object)
            {
                ushort objId = this._reader.ReadUInt16();
                object returnGraph = null;

                if (this._readObjects.ContainsKey(objId))
                    returnGraph = this._readObjects[objId];
                else
                {
                    Type graphType = this.typeReader(expectedType);
                    object graph = null;

                    ISurrogateSelector selector = null;
                    ISerializationSurrogate surrogate = this._selector == null ? null : this._selector.GetSurrogate(graphType, this._context, out selector);
                    this._selector = selector == null ? this._selector : selector;

                    if (graphType.IsArray)
                    {
                        object[] dimensions = new object[graphType.GetArrayRank()];
                        for (int i = 0; i < dimensions.Length; i++)
                            dimensions[i] = this._reader.ReadInt32();
                        graph = Activator.CreateInstance(graphType, dimensions);
                        this._readObjects.Add(objId, graph);
                        this.setArrayValues((Array)graph, graphType);
                    }
                    else if (SerializationStaticHelpers.TypeOfISerializable.IsAssignableFrom(graphType))
                    {
                        graph = FormatterServices.GetUninitializedObject(graphType);

                        this._readObjects.Add(objId, graph);

                        ConstructorInfo serializableConstructor = SerializationStaticHelpers.GetSerializationCtor(graphType);
                        SerializationInfo info = new SerializationInfo(graphType, SerializationStaticHelpers.FormatterConverter);
                        this.setISerializableValues(info, graphType);

                        serializableConstructor.Invoke(graph, new object[] { info, this._context });

                        if (graph is IObjectReference)
                        {
                            object realGraph = ((IObjectReference)graph).GetRealObject(this._context);
                            this._readObjects[objId] = realGraph;
                            graph = realGraph;
                        }

                        if (graph is IDeserializationCallback)
                            ((IDeserializationCallback)graph).OnDeserialization(graph);
                    }
                    else if (surrogate == null)
                    {
                        graph = FormatterServices.GetUninitializedObject(graphType);
                        this._readObjects.Add(objId, graph);

                        this.setObjectValues(graph, graphType);

                        if (graph is IDeserializationCallback)
                            ((IDeserializationCallback)graph).OnDeserialization(graph);
                    }
                    else
                    {
                        graph = FormatterServices.GetUninitializedObject(graphType);

                        SerializationInfo info = new SerializationInfo(graphType, SerializationStaticHelpers.FormatterConverter);
                        this.setISerializableValues(info, graphType);

                        surrogate.SetObjectData(graph, info, this._context, this._selector);
                        this._readObjects.Add(objId, graph);

                    }
                    returnGraph = graph;
                }

                if (returnGraph is IObjectReference)
                {
                    object realGraph = ((IObjectReference)returnGraph).GetRealObject(this._context);

                    this._readObjects[objId] = realGraph;
                    returnGraph = realGraph;
                }
                return returnGraph;
            }
            else if (code == TypeCode.String)
            {
                short index = this._reader.ReadInt16();
                if (index == -1)
                    return this._reader.ReadString();
                else
                    return this._stringContents[index];
            }
            else
            {
                if (code == (TypeCode)19)
                    return this.typeReader(expectedType);
                else
                    return SerializationStaticHelpers.ReadPrimitive(this._reader, code);
            }
        }

        private void setObjectValues(object graph, Type graphType)
        {
            ushort memberCount = this._reader.ReadUInt16();
            for (int i = 0; i < memberCount; i++)
            {
                string memberName = SerializationStaticHelpers.Read256String(this._reader);
                FieldInfo field = graphType.GetField(memberName, SerializationStaticHelpers.SerializationMemberBindings);
                object memberValue = null;
                if (field == null)
                {
                    memberValue = this.Deserialize(null);
                    continue;
                }
                memberValue = this.Deserialize(field.FieldType);
                field.SetValue(graph, memberValue);
            }
        }
        private void setArrayValues(Array graph, Type graphType)
        {
            Type elementType = graphType.GetElementType();
            for (long i = 0; i < graph.LongLength; i++)
            {
                int[] multiDimIndex = SerializationStaticHelpers.GetMultiDimAsLongIndex(i, graph);
                object value = this.Deserialize(elementType);
                graph.SetValue(value, multiDimIndex);
            }
        }
        private void setISerializableValues(SerializationInfo info, Type graphType)
        {
            ushort memberCount = this._reader.ReadUInt16();
            for (int i = 0; i < memberCount; i++)
            {
                string memberName = SerializationStaticHelpers.Read256String(this._reader);
                object memberValue = this.Deserialize(null);
                info.AddValue(memberName, memberValue);
            }
        }
        private Type typeReader(Type expectedType)
        {
            byte typeState = this._reader.ReadByte();

            if (typeState == 1)
                return expectedType;
            else
                return fullTypeReader();
        }
        private Type fullTypeReader()
        {
            TypeState state = (TypeState)this._reader.ReadByte();
            bool isArray = (state & TypeState.Array) == TypeState.Array;
            bool isPrimitive = (state & TypeState.Primitive) == TypeState.Primitive;
            byte arrayRank = isArray ? this._reader.ReadByte() : (byte)0;

            if (isPrimitive)
            {
                TypeCode code = (TypeCode)this._reader.ReadByte();
                Type graphType = SerializationStaticHelpers.CodeToType(code);
                if (isArray)
                    graphType = graphType.MakeArrayType(arrayRank);
                return graphType;
            }

            bool isGeneric = (state & TypeState.Generic) == TypeState.Generic;
            if (isGeneric)
            {
                Type genericDef = this.fullTypeReader();
                byte argCount = this._reader.ReadByte();
                Type[] genericArguments = new Type[argCount];
                for (int i = 0; i < argCount; i++)
                    genericArguments[i] = this.fullTypeReader();
                Type generic = genericDef.MakeGenericType(genericArguments);
                return isArray ? generic.MakeArrayType(arrayRank) : generic;
            }

            string boundAssemblyName, boundTypeName;

            byte asmId = this._reader.ReadByte();
            ushort typeId = 0;

            if (this._readAssemblies.Count > asmId)
                boundAssemblyName = this._readAssemblies[asmId];
            else
            {
                boundAssemblyName = this._reader.ReadString();
                this._readAssemblies.Add(boundAssemblyName);

                boundTypeName = this._reader.ReadString();
                this._readTypes.Add(boundTypeName);
                if (this._binder == null)
                {
                    Type boundType = Type.GetType(boundTypeName + ", " + boundAssemblyName);
                    return isArray ? boundType.MakeArrayType(arrayRank) : boundType;
                }

                else
                {
                    Type boundType = this._binder.BindToType(boundAssemblyName, boundTypeName);
                    if (boundType == null)
                        boundType = Type.GetType(boundTypeName + ", " + boundAssemblyName);
                    return isArray ? boundType.MakeArrayType(arrayRank) : boundType;
                }
            }
            typeId = this._reader.ReadUInt16();
            if (this._readTypes.Count > typeId)
                boundTypeName = this._readTypes[typeId];
            else
            {
                boundTypeName = this._reader.ReadString();
                this._readTypes.Add(boundTypeName);
            }

            if (this._binder == null)
            {
                Type boundType = Type.GetType(boundTypeName + ", " + boundAssemblyName);
                return isArray ? boundType.MakeArrayType(arrayRank) : boundType;
            }
            else
            {
                Type boundType = this._binder.BindToType(boundAssemblyName, boundTypeName);
                if (boundType == null)
                    boundType = Type.GetType(boundTypeName + ", " + boundAssemblyName);
                return isArray ? boundType.MakeArrayType(arrayRank) : boundType;
            }
        }
    }
}
