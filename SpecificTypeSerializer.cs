using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

namespace NetworkIO.Serialization
{
    public class SpecificTypeSerializer<TSpecific> : IFormatter
    {
        private Type _specificType;
        private ObjectSerializer _realSerializer;

        SerializationBinder IFormatter.Binder { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        ISurrogateSelector IFormatter.SurrogateSelector { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

        public StreamingContext Context { get { return this._realSerializer.Context; } set { this._realSerializer.Context = value; } }

        public SpecificTypeSerializer()
        {
            this._realSerializer = new ObjectSerializer();
            this._realSerializer.WellKnownContents.Add(typeof(Object));
            this._specificType = typeof(TSpecific);
            this.internalAddTypeContents(this._specificType);
        }

        public void AddStringContent(string wellKnownString)
        {
            this._realSerializer.WellKnownContents.Add(wellKnownString);
        }
        public void SetSerializationReference(Type serializationReference)
        {
            if (SerializationStaticHelpers.TypeOfIObjectReference.IsAssignableFrom(serializationReference) && SerializationStaticHelpers.TypeOfISerializable.IsAssignableFrom(serializationReference))
            {
                this.internalAddTypeContents(serializationReference);
            }
            else throw new InvalidOperationException("Type is not assignable from IObjectReference or ISerializable.");
        }
        public TSpecific Deserialize(ReaderBase reader)
        {
            return (TSpecific)this._realSerializer.Deserialize(reader, this._specificType);
        }
        public TSpecific Deserialize(Stream stream)
        {
            return (TSpecific)this._realSerializer.Deserialize(stream, this._specificType);
        }
        public void Serialize(WriterBase writer, TSpecific graph)
        {
            this._realSerializer.Serialize(writer, graph, this._specificType);
        }
        public void Serialize(Stream stream, TSpecific graph)
        {
            this._realSerializer.Serialize(stream, graph, this._specificType);
        }

        object IFormatter.Deserialize(Stream serializationStream)
        {
            return this.Deserialize(serializationStream);
        }
        void IFormatter.Serialize(Stream serializationStream, object graph)
        {
            Type graphType = graph.GetType();
            if (graphType == this._specificType)
                this.Serialize(serializationStream, (TSpecific)graph);
            else
                throw new SerializationException("Type of Graph must be " + this._specificType);
        }

        private void internalAddTypeContents(Type type)
        {
            FieldInfo[] fields = type.GetFields(SerializationStaticHelpers.SerializationMemberBindings);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                TypeCode code = Type.GetTypeCode(field.FieldType);
                if (code == TypeCode.Object)
                {
                    if (!this._realSerializer.WellKnownContents.Contains(field.FieldType))
                        this._realSerializer.WellKnownContents.Add(field.FieldType);
                    this.internalAddTypeContents(field.FieldType);
                }
            }
        }
    }
}
