using NetworkIO;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace NetworkIO.Serialization
{
    [Flags]
    internal enum TypeState : byte { Object = 1, Array = 2, Generic = 4, Primitive = 128 }

    internal class GraphWriter
    {
        private TypeBinder _binder;
        private ObjectIDGenerator _objIdGenerator;
        private ISurrogateSelector _selector;
        private List<string> _stringContents;
        private WriterBase _writer;
        private List<string> _writtenAssemblies;
        private List<string> _writtenTypes;
        private StreamingContext _context;

        internal GraphWriter(TypeBinder binder, WriterBase writer, ISurrogateSelector selector)
            : this(binder, writer, selector, SerializationStaticHelpers.StreamingContext)
        {

        }
        internal GraphWriter(TypeBinder binder, WriterBase writer, ISurrogateSelector selector, SerializationContents contents)
            : this(binder, writer, selector, SerializationStaticHelpers.StreamingContext, contents)
        {

        }
        internal GraphWriter(TypeBinder binder, WriterBase writer, ISurrogateSelector selector, StreamingContext context)
            : this(binder, writer, selector, context, null)
        {

        }
        internal GraphWriter(TypeBinder binder, WriterBase writer, ISurrogateSelector selector, StreamingContext context, SerializationContents contents)
        {
            this._binder = binder;
            this._objIdGenerator = new ObjectIDGenerator();
            if (contents == null)
            {
                this._writtenAssemblies = new List<string>();
                this._writtenTypes = new List<string>();
                this._stringContents = new List<string>();
            }
            else
            {
                this._writtenAssemblies = new List<string>(contents._AssemblyContents);
                this._writtenTypes = new List<string>(contents._TypeContents);
                this._stringContents = new List<string>(contents._StringContents);
            }
            this._selector = selector;
            this._writer = writer;
            this._context = context;
        }

        internal void Serialize(object graph, Type expectedType)
        {
            Type graphType = graph == null ? null : graph.GetType();
            TypeCode code = Type.GetTypeCode(graphType);
            code = code == TypeCode.Object && graphType == SerializationStaticHelpers.RuntimeType ? (TypeCode)19 : code;
            code = graph != null && graph is Enum ? TypeCode.Object : code;

            this._writer.Write((byte)code);
            if (code == TypeCode.Object)
            {
                ushort objId = 1;
                bool first;
                objId = (ushort)this._objIdGenerator.GetId(graph, out first);
                this._writer.Write(objId);

                if (first)
                {
                    ISurrogateSelector selector = null;
                    ISerializationSurrogate surrogate = this._selector == null ? null : this._selector.GetSurrogate(graphType, this._context, out selector);
                    this._selector = selector == null ? this._selector : selector;

                    if (graphType.IsArray)
                    {
                        this.typeWriter(graphType, expectedType);

                        Array arrayGraph = (Array)graph;
                        for (int i = 0; i < arrayGraph.Rank; i++) //Rank değeri Type'ın içinde gidiyor.
                            this._writer.Write(arrayGraph.GetLength(i));
                        this.getArrayValues(arrayGraph, graphType);
                    }
                    else if (SerializationStaticHelpers.TypeOfISerializable.IsAssignableFrom(graphType))
                    {
                        SerializationInfo info = new SerializationInfo(graphType, SerializationStaticHelpers.FormatterConverter);
                        ((ISerializable)graph).GetObjectData(info, this._context);
                        Type infoObjType = this._binder == null ? Type.GetType(info.FullTypeName + ", " + info.AssemblyName) : this._binder.BindToType(info.AssemblyName, info.FullTypeName);
                        if (infoObjType != graphType)
                            graphType = infoObjType;

                        this.typeWriter(graphType, expectedType);
                        this.getISerializableValues(info, graphType);
                    }
                    else if (surrogate == null)
                    {
                        this.typeWriter(graphType, expectedType);
                        this.getObjectValues(graph, graphType);
                    }
                    else
                    {
                        SerializationInfo info = new SerializationInfo(graphType, SerializationStaticHelpers.FormatterConverter);
                        surrogate.GetObjectData(graph, info, this._context);
                        this.typeWriter(graphType, expectedType);
                        this.getISerializableValues(info, graphType);
                    }
                }
            }
            else if (code == TypeCode.String)
            {
                string strGraph = (string)graph;
                short index = (short)this._stringContents.IndexOf(strGraph);
                this._writer.Write(index);
                if (index == -1)
                    this._writer.Write(strGraph);
            }
            else
            {
                if (code == (TypeCode)19)
                    this.typeWriter(graph as Type, expectedType);
                else
                    SerializationStaticHelpers.WritePrimitive(this._writer, graph, code);
            }
        }

        private void getObjectValues(object graph, Type graphType)
        {
            bool isAllExcepted = SerializationStaticHelpers.IsAllFieldExpectedType(graphType);

            FieldInfo[] allFields = graphType.GetFields(SerializationStaticHelpers.SerializationMemberBindings);
            List<FieldInfo> fields = new List<FieldInfo>();
            for (int i = 0; i < allFields.Length; i++)
            {
                FieldInfo field = allFields[i];
                if (SerializationStaticHelpers.IsSerializable(field))
                    fields.Add(field);
            }

            this._writer.Write((ushort)fields.Count);

            if (isAllExcepted)
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    FieldInfo field = fields[i];
                    SerializationStaticHelpers.Write256String(field.Name, this._writer);
                    object memberValue = field.GetValue(graph);
                    this.Serialize(memberValue, field.FieldType);
                }
            }
            else
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    FieldInfo field = fields[i];
                    SerializationStaticHelpers.Write256String(field.Name, this._writer);
                    object memberValue = field.GetValue(graph);
                    if (SerializationStaticHelpers.GetExpectedFieldTypeAttribute(field) == null)
                        this.Serialize(memberValue, null);
                    else
                        this.Serialize(memberValue, field.FieldType);
                }
            }
        }
        private void getArrayValues(Array graph, Type graphType)
        {
            Type elementType = graphType.GetElementType();
            foreach (object element in graph)
                this.Serialize(element, elementType);
        }
        private void getISerializableValues(SerializationInfo info, Type graphType)
        {
            this._writer.Write((ushort)info.MemberCount);
            SerializationInfoEnumerator enumerator = info.GetEnumerator();

            while (enumerator.MoveNext())
            {
                SerializationStaticHelpers.Write256String(enumerator.Name, this._writer);
                this.Serialize(enumerator.Value, null);
            }
        }
        private void typeWriter(Type graphType, Type expectedType)
        {
            if (expectedType != null && graphType == expectedType)
            {
                this._writer.Write((byte)1);
                return;
            }
            else
            {
                this._writer.Write((byte)0);
                this.fullTypeWriter(graphType);
            }
        }
        private void fullTypeWriter(Type graphType)
        {
            TypeState state;
            byte arrayRank = graphType.IsArray ? (byte)graphType.GetArrayRank() : (byte)0;
            bool isArray = graphType.IsArray;

            if (isArray)
            {
                state = TypeState.Array;
                graphType = graphType.GetElementType();
            }
            else state = TypeState.Object;

            TypeCode code = Type.GetTypeCode(graphType);
            code =  graphType.IsEnum ? TypeCode.Object : code;

            if (code == TypeCode.Object)
            {
                if (graphType.IsGenericType && !graphType.IsGenericTypeDefinition)
                {
                    state = state | TypeState.Generic;
                    this._writer.Write((byte)state);
                    if (isArray) this._writer.Write(arrayRank);
                    Type genericDef = graphType.GetGenericTypeDefinition();
                    this.fullTypeWriter(genericDef);
                    Type[] genericArguments = graphType.GetGenericArguments();
                    this._writer.Write((byte)genericArguments.Length);
                    for (int i = 0; i < genericArguments.Length; i++)
                        this.fullTypeWriter(genericArguments[i]);
                }
                else
                {
                    this._writer.Write((byte)state);
                    if (isArray) this._writer.Write(arrayRank);
                    string boundAssemblyName = graphType.Assembly.FullName, boundTypeName = graphType.FullName;
                    if (this._binder != null)
                        this._binder.BindToName(graphType, ref boundAssemblyName, ref boundTypeName);
                    
                    int asmId = this._writtenAssemblies.IndexOf(boundAssemblyName);
                    int typeId = this._writtenTypes.IndexOf(boundTypeName);

                    if (asmId == -1)
                    {
                        asmId = (byte)this._writtenAssemblies.Count;
                        this._writtenAssemblies.Add(boundAssemblyName);
                        this._writer.Write((byte)asmId);
                        this._writer.Write(boundAssemblyName);

                        typeId = this._writtenTypes.Count;
                        this._writtenTypes.Add(boundTypeName);
                        this._writer.Write(boundTypeName);
                        return;
                    }
                    else
                        this._writer.Write((byte)asmId);

                    if (typeId == -1)
                    {
                        typeId = this._writtenTypes.Count;
                        this._writtenTypes.Add(boundTypeName);
                        this._writer.Write((ushort)typeId);
                        this._writer.Write(boundTypeName);
                    }
                    else
                        this._writer.Write((ushort)typeId);
                }
            }
            else
            {
                state |= TypeState.Primitive;
                this._writer.Write((byte)state);

                if (isArray)
                    this._writer.Write(arrayRank);

                this._writer.Write((byte)code);
                
            }
            
        }
    }
}
