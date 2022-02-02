using UnityEngine;

namespace Infrastructure.Data
{
    public static class DataExtensions
    {
        public static T Deserialize<T>(this string json)
        {
            return JsonUtility.FromJson<T>(json);
        }
    }
}
