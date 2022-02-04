using UnityEngine;

namespace Infrastructure.Data
{
    public static class DataExtensions
    {
        public static T Deserialize<T>(this string json) =>
            JsonUtility.FromJson<T>(json);

        public static string ToJson(this object @object) =>
            JsonUtility.ToJson(@object);
    }
}
