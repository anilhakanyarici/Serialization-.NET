using NetworkIO;
using System;
using System.IO;
using System.Runtime.Serialization;

namespace NetworkIO.Serialization
{
    /// <summary>
    /// BinaryFormatter gibi alternatif bir serileştirici. ISerializable, IObjectReference, IDeserializationCallback arabirimlerini ve NonSerializedAttribute niteliğini destekler.
    /// IFormatter.Binder özelliği desteklenmiyor. Çünkü .Net 4.0'a kadar BindToName metodu desteklenmiyor. Her .Net sürümü üzerinde buna destek sağlamak için TypeBinder isimli yeni bir
    /// sınıf getirildi. BinaryFormatter'dan farklı olarak WriterBase ve ReaderBase IO işlemcilerini destekler ve SerializationContents ile de serileştirmelerin boyutunu daha da küçültür.
    /// Ayrıca serialize ve deserialize metotları üzerindeki "expectedType" bilgileri ile, "beklenen alan tipi" algoritmasını graph üzerine uygular. Graph tipi ile expectedType uyuşuyorsa, 
    /// tip bilgisi serileştirilmeden obje serileştirilir ve okurken aynı tip bilgisi ile deserialization yapılabilir. (Not: TypeContent, binder'dan sonra ayarlanmalı.)
    /// </summary>
    public class ObjectSerializer : IFormatter
    {
        private TypeBinder _typeBinder;

        SerializationBinder IFormatter.Binder { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

        public StreamingContext Context { get; set; }
        public ISurrogateSelector SurrogateSelector { get; set; }
        public TypeBinder TypeBinder { get { return this._typeBinder; } set { if (this.WellKnownContents != null)this.WellKnownContents.TypeBinder = value; this._typeBinder = value; } }
        public SerializationContents WellKnownContents { get; set; }

        public ObjectSerializer()
        {
            this.Context = SerializationStaticHelpers.StreamingContext;
            this.WellKnownContents = new SerializationContents();
        }

        public object Deserialize(Stream serializationStream)
        {
            return this.Deserialize(ReaderBase.StreamReader(serializationStream), null);
        }
        public object Deserialize(Stream serializationStream, Type expectedType)
        {
            return this.Deserialize(ReaderBase.StreamReader(serializationStream), expectedType);
        }
        public object Deserialize(ReaderBase reader)
        {
            try
            {
                GraphReader graphReader = new GraphReader(this.TypeBinder, reader, this.SurrogateSelector, this.Context, this.WellKnownContents);
                return graphReader.Deserialize(null);
            }
            catch (Exception inner)
            {
                throw new SerializationException(inner.Message, inner);
            }
        }
        public object Deserialize(ReaderBase reader, Type expectedType)
        {
            try
            {
                GraphReader graphReader = new GraphReader(this.TypeBinder, reader, this.SurrogateSelector, this.Context, this.WellKnownContents);
                return graphReader.Deserialize(expectedType);
            }
            catch (Exception inner)
            {
                throw new SerializationException(inner.Message, inner);
            }
        }
        public T Deserialize<T>(Stream serializationStream)
        {
            return (T)this.Deserialize(ReaderBase.StreamReader(serializationStream), typeof(T));
        }
        public T Deserialize<T>(ReaderBase reader)
        {
            try
            {
                GraphReader graphReader = new GraphReader(this.TypeBinder, reader, this.SurrogateSelector, this.Context, this.WellKnownContents);
                return (T)graphReader.Deserialize(typeof(T));
            }
            catch (Exception inner)
            {
                throw new SerializationException(inner.Message, inner);
            }
        }
        
        public void Serialize(Stream serializationStream, object graph)
        {
            this.Serialize(WriterBase.StreamWriter(serializationStream), graph, null);
        }
        public void Serialize(Stream serializationStream, object graph, Type expectedType)
        {
            this.Serialize(WriterBase.StreamWriter(serializationStream), graph, expectedType);
        }
        public void Serialize(WriterBase writer, object graph)
        {
            try
            {
                GraphWriter graphWriter = new GraphWriter(this.TypeBinder, writer, this.SurrogateSelector, this.Context, this.WellKnownContents);
                graphWriter.Serialize(graph, null);
            }
            catch (Exception inner)
            {
                throw new SerializationException(inner.Message, inner);
            }
        }
        public void Serialize(WriterBase writer, object graph, Type expectedType)
        {
            try
            {
                GraphWriter graphWriter = new GraphWriter(this.TypeBinder, writer, this.SurrogateSelector, this.Context, this.WellKnownContents);
                graphWriter.Serialize(graph, expectedType);
            }
            catch (Exception inner)
            {
                throw new SerializationException(inner.Message, inner);
            }
        }
    }
}
