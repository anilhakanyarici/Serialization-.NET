using NetworkIO;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace NetworkIO.Serialization
{
    internal static class SerializationStaticHelpers
    {
        internal static readonly IFormatterConverter FormatterConverter = new FormatterConverter();
        internal static readonly StreamingContext StreamingContext = new StreamingContext();
        internal static readonly Type RuntimeType = typeof(Type).GetType();
        internal static readonly BindingFlags SerializationMemberBindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        internal static readonly Type TypeOfNonSerializableAttribute = typeof(NonSerializedAttribute);
        internal static readonly Type TypeOfObject = typeof(object);
        internal static readonly Type TypeOfISerializable = typeof(ISerializable);
        internal static readonly Type TypeOfIObjectReference = typeof(IObjectReference);
        internal static readonly Type TypeOfSerializationInfo = typeof(SerializationInfo);
        internal static readonly Type TypeOfStreamingContext = typeof(StreamingContext);
        internal static readonly Type TypeOfSerializationBinderAttribute = typeof(SerializationBinderAttribute);
        internal static readonly Type TypeOfExpectedFieldTypeAttribute = typeof(ExpectedFieldTypeAttribute);


        internal static void Write256String(string str, WriterBase writer)
        {
            byte[] strBytes = Encoding.UTF8.GetBytes(str);
            writer.Write((byte)strBytes.Length);
            writer.Write(strBytes, 0, strBytes.Length);
        }
        internal static string Read256String(ReaderBase reader)
        {
            byte length = reader.ReadByte();
            byte[] strBytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(strBytes);
        }

        internal static WriterBase WritePrimitive(WriterBase writer, object primitiveObj, TypeCode code)
        {
            switch (code)
            {
                case TypeCode.Boolean:
                    return writer.Write((Boolean)primitiveObj);
                case TypeCode.Byte:
                    return writer.Write((Byte)primitiveObj);
                case TypeCode.Char:
                    return writer.Write((Char)primitiveObj);
                case TypeCode.DateTime:
                    return writer.Write((DateTime)primitiveObj);
                case TypeCode.Decimal:
                    return writer.Write((Decimal)primitiveObj);
                case TypeCode.Double:
                    return writer.Write((Double)primitiveObj);
                case TypeCode.Int16:
                    return writer.Write((Int16)primitiveObj);
                case TypeCode.Int32:
                    return writer.Write((Int32)primitiveObj);
                case TypeCode.Int64:
                    return writer.Write((Int64)primitiveObj);
                case TypeCode.SByte:
                    return writer.Write((SByte)primitiveObj);
                case TypeCode.Single:
                    return writer.Write((Single)primitiveObj);
                case TypeCode.String:
                    return writer.Write((String)primitiveObj);
                case TypeCode.UInt16:
                    return writer.Write((UInt16)primitiveObj);
                case TypeCode.UInt32:
                    return writer.Write((UInt32)primitiveObj);
                case TypeCode.UInt64:
                    return writer.Write((UInt64)primitiveObj);
            }
            return writer;
        }
        internal static object ReadPrimitive(ReaderBase reader, TypeCode code)
        {
            switch (code)
            {
                case TypeCode.Boolean:
                    return reader.ReadBoolean();
                case TypeCode.Byte:
                    return reader.ReadByte();
                case TypeCode.Char:
                    return reader.ReadChar();
                case TypeCode.DateTime:
                    return reader.ReadDateTime();
                case TypeCode.Decimal:
                    return reader.ReadDecimal();
                case TypeCode.Double:
                    return reader.ReadDouble();
                case TypeCode.Int16:
                    return reader.ReadInt16();
                case TypeCode.Int32:
                    return reader.ReadInt32();
                case TypeCode.Int64:
                    return reader.ReadInt64();
                case TypeCode.SByte:
                    return reader.ReadSByte();
                case TypeCode.Single:
                    return reader.ReadSingle();
                case TypeCode.String:
                    return reader.ReadString();
                case TypeCode.UInt16:
                    return reader.ReadUInt16();
                case TypeCode.UInt32:
                    return reader.ReadUInt32();
                case TypeCode.UInt64:
                    return reader.ReadUInt64();
                case TypeCode.DBNull:
                    return DBNull.Value;
            }
            return null;
        }

        internal static bool IsSerializable(FieldInfo field)
        {
            return field.GetCustomAttributes(SerializationStaticHelpers.TypeOfNonSerializableAttribute, false).Length == 0;
        }
        internal static bool IsSerializable(Type type)
        {
            return SerializationStaticHelpers.TypeOfISerializable.IsAssignableFrom(type);
        }

        internal static int[] GetMultiDimAsLongIndex(long index, Array array)
        {
            int[] dims = new int[array.Rank];
            long temp = index;
            for (int i = dims.Length - 1; i >= 0; i--)
            {
                int dimUpperBound = array.GetLength(i);
                dims[i] = (int)(temp % dimUpperBound);
                temp /= dimUpperBound;
            }
            return dims;
        }
        internal static ConstructorInfo GetSerializationCtor(Type type)
        {
            foreach (ConstructorInfo ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.ExactBinding))
            {
                ParameterInfo[] parameters = ctor.GetParameters();
                if (parameters.Length == 2 && parameters[0].ParameterType == SerializationStaticHelpers.TypeOfSerializationInfo && parameters[1].ParameterType == SerializationStaticHelpers.TypeOfStreamingContext)
                    return ctor;
            }
            return null;
        }
        internal static Type CodeToType(TypeCode code)
        {
            switch (code)
            {
                case TypeCode.Boolean:
                    return PrimitiveTypes.Boolean;
                case TypeCode.Byte:
                    return PrimitiveTypes.Byte;
                case TypeCode.Char:
                    return PrimitiveTypes.Char;
                case TypeCode.DBNull:
                    return PrimitiveTypes.DBNull;
                case TypeCode.DateTime:
                    return PrimitiveTypes.DateTime;
                case TypeCode.Decimal:
                    return PrimitiveTypes.Decimal;
                case TypeCode.Double:
                    return PrimitiveTypes.Double;
                case TypeCode.Int16:
                    return PrimitiveTypes.Int16;
                case TypeCode.Int32:
                    return PrimitiveTypes.Int32;
                case TypeCode.Int64:
                    return PrimitiveTypes.Int64;
                case TypeCode.Object:
                    return SerializationStaticHelpers.TypeOfObject;
                case TypeCode.SByte:
                    return PrimitiveTypes.SByte;
                case TypeCode.Single:
                    return PrimitiveTypes.Single;
                case TypeCode.String:
                    return PrimitiveTypes.String;
                case TypeCode.UInt16:
                    return PrimitiveTypes.UInt16;
                case TypeCode.UInt32:
                    return PrimitiveTypes.UInt32;
                case TypeCode.UInt64:
                    return PrimitiveTypes.UInt64;
                default:
                    break;
            }
            return null;
        }
        internal static SerializationBinderAttribute GetSerializationBinderAttribute(Type type)
        {
            object[] attribObjs = type.GetCustomAttributes(SerializationStaticHelpers.TypeOfSerializationBinderAttribute, false);
            if (attribObjs.Length > 0)
                return attribObjs[0] as SerializationBinderAttribute;
            else return null;
        }
        internal static ExpectedFieldTypeAttribute GetExpectedFieldTypeAttribute(FieldInfo field)
        {
            object[] attribObjs = field.GetCustomAttributes(SerializationStaticHelpers.TypeOfExpectedFieldTypeAttribute, false);
            if (attribObjs.Length > 0)
                return attribObjs[0] as ExpectedFieldTypeAttribute;
            else return null;
        }
        internal static bool IsAllFieldExpectedType(Type type)
        {
            return type.GetCustomAttributes(SerializationStaticHelpers.TypeOfExpectedFieldTypeAttribute, false).Length > 0;
        }

        internal static class PrimitiveTypes
        {
            internal static readonly Type Boolean = typeof(Boolean);
            internal static readonly Type Byte = typeof(Byte);
            internal static readonly Type Char = typeof(Char);
            internal static readonly Type DBNull = typeof(DBNull);
            internal static readonly Type DateTime = typeof(DateTime);
            internal static readonly Type Decimal = typeof(Decimal);
            internal static readonly Type Double = typeof(Double);
            internal static readonly Type Int16 = typeof(Int16);
            internal static readonly Type Int32 = typeof(Int32);
            internal static readonly Type Int64 = typeof(Int64);
            internal static readonly Type SByte = typeof(SByte);
            internal static readonly Type Single = typeof(Single);
            internal static readonly Type String = typeof(String);
            internal static readonly Type UInt16 = typeof(UInt16);
            internal static readonly Type UInt32 = typeof(UInt32);
            internal static readonly Type UInt64 = typeof(UInt64);

            internal static IEnumerator<Type> GetTypeEnumerator()
            {
                yield return PrimitiveTypes.Boolean;
                yield return PrimitiveTypes.Byte;
                yield return PrimitiveTypes.Char;
                yield return PrimitiveTypes.DateTime;
                yield return PrimitiveTypes.DBNull;
                yield return PrimitiveTypes.Decimal;
                yield return PrimitiveTypes.Double;
                yield return PrimitiveTypes.Int16;
                yield return PrimitiveTypes.Int32;
                yield return PrimitiveTypes.Int64;
                yield return PrimitiveTypes.SByte;
                yield return PrimitiveTypes.Single;
                yield return PrimitiveTypes.String;
                yield return PrimitiveTypes.UInt16;
                yield return PrimitiveTypes.UInt32;
                yield return PrimitiveTypes.UInt64;
            }
        }
    }
}
