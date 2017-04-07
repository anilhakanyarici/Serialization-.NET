using System;

namespace NetworkIO.Serialization
{
    /// <summary>
    /// Bir sınıfın yeni sürümü yapıldığında ve geriye dönük uyumluluk için eski sürüme bağlandığında, eski sürüm yeni sürümün field bilgisini tamamen içermediği için 
    /// serileştirme hatası oluşur. Bu hatanın sebebi, "beklenen alan tipi" (expected field type) algoritmasının eski sürümün field'ını bulamamasından kaynaklanır.
    /// Algoritma, yalnızca bu niteliği içeren fieldlar için çalışır. Yeni versiyon paketlerde kullanılmaması iyi olur. Primitive tipler için kullanmaya gerek yok.
    /// Class veya Struct'lar için eklenirse, her fieldı otomatik olarak işaretler.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public class ExpectedFieldTypeAttribute : Attribute
    {
    }
}
