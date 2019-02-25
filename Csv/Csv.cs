using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static System.FormattableString;

namespace Csv
{
    public class CsvConvert
    {
        private static readonly char[] Separator = new char[10] { '┐', '┼', '╚', '╔', '╩', '╦', '└', '┴', '┬', '├' };
        private static readonly char SeparatorForList = '■';
        private static readonly char SeparatorForDictionary = '¶';
        private static readonly char SeparatorForName = '─';
        private static readonly List<Type> Types = AppDomain.CurrentDomain.GetAssemblies().ToList().FindAll(x => !x.FullName.ToLower().Contains("system")).SelectMany(x => x.GetTypes()).ToList();
        public static string Serialize<T>(T data, int separatorIndex = 0)
        {
            if (data == null) return string.Empty;
            string separator = Separator[separatorIndex].ToString();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (PropertyInfo propertyInfo in data.GetType().GetProperties())
            {
                if (propertyInfo.GetCustomAttribute(typeof(CsvIgnore)) == null)
                {
                    object value = propertyInfo.GetValue(data);
                    if (value is IDictionary)
                    {
                        Type[] types = propertyInfo.PropertyType.GetGenericArguments();
                        StringBuilder internalStringBuilder = new StringBuilder();
                        foreach (DictionaryEntry single in propertyInfo.GetValue(data) as IDictionary)
                        {
                            string stringedKey = typeof(CsvConvert).GetMethod("ForStringBuilder", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(types[0]).Invoke(null, new object[3] { single.Key, separatorIndex, SeparatorForDictionary.ToString() }).ToString();
                            string stringedValue = typeof(CsvConvert).GetMethod("ForStringBuilder", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(types[1]).Invoke(null, new object[3] { single.Value, separatorIndex, SeparatorForList.ToString() }).ToString();
                            internalStringBuilder.Append($"{stringedKey}{stringedValue}");
                        }
                        string internalString = internalStringBuilder.ToString();
                        stringBuilder.Append($"{(internalString.Length == 0 ? internalString : internalString.Substring(0, internalStringBuilder.Length - 1))}{separator}");
                    }
                    else if (value is IList)
                    {
                        Type[] types = propertyInfo.PropertyType.GetGenericArguments();
                        StringBuilder internalStringBuilder = new StringBuilder();
                        foreach (var single in value as IList)
                        {
                            string stringedValue = typeof(CsvConvert).GetMethod("ForStringBuilder", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(types[0]).Invoke(null, new object[3] { single, separatorIndex, SeparatorForList.ToString() }).ToString();
                            internalStringBuilder.Append($"{stringedValue}");
                        }
                        string internalString = internalStringBuilder.ToString();
                        stringBuilder.Append($"{(internalString.Length == 0 ? internalString : internalString.Substring(0, internalStringBuilder.Length - 1))}{separator}");
                    }
                    else
                    {
                        string stringedValue = typeof(CsvConvert).GetMethod("ForStringBuilder", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(propertyInfo.PropertyType).Invoke(null, new object[3] { value, separatorIndex, separator }).ToString();
                        stringBuilder.Append(stringedValue);
                    }
                }
            }
            string result = stringBuilder.ToString();
            return result.Substring(0, result.Length - 1);
        }
        private static string ForStringBuilder<T>(T data, int separatorIndex, string separatorString)
        {
            Type type = typeof(T);
            if(!IsPrimitive(ref type))
            {
                string nameOfInstance = "";
                if (type.IsAbstract || type.IsInterface) nameOfInstance = $"{SeparatorForName}{data.GetType().Name}{SeparatorForName}";
                return $"{nameOfInstance}{Serialize(data, separatorIndex + 1)}{separatorString}";
            }
            else
            {
                return Invariant($"{data}{separatorString}");
            }
        }
        private static bool IsPrimitive(ref Type type)
        {
            if (type.GenericTypeArguments.Length > 0) type = type.GenericTypeArguments.FirstOrDefault();
            return type.IsPrimitive || type == typeof(string) || type.BaseType == typeof(Enum) || type == typeof(decimal);
        }
        public static T Deserialize<T>(string data, int separatorIndex = 0)
        {
            if (string.IsNullOrWhiteSpace(data)) return default(T);
            Type type = typeof(T);
            if (IsPrimitive(ref type)) return (T)Convert.ChangeType(data, type);
            char separator = Separator[separatorIndex];
            if (type.IsInterface || type.IsAbstract)
            {
                Regex regexName = new Regex($"{SeparatorForName}[^{SeparatorForName}]*{SeparatorForName}");
                string nameOfInstance = regexName.Match(data)?.Value;
                data = regexName.Replace(data, string.Empty, 1);
                nameOfInstance = nameOfInstance.Trim(SeparatorForName);
                type = Types.FirstOrDefault(x => type.IsAssignableFrom(x) && x.Name == nameOfInstance);
            }
            T entity = (T)Activator.CreateInstance(type);
            PropertyInfo[] propertyInfo = type.GetProperties();
            string[] splittedData = data.Split(separator);
            int counter = 0;
            for (int i = 0; i < propertyInfo.Length; i++)
            {
                if (propertyInfo[i].GetCustomAttribute(typeof(CsvIgnore)) == null)
                {
                    try
                    {
                        if (!propertyInfo[i].PropertyType.IsPrimitive && propertyInfo[i].PropertyType != typeof(string))
                        {
                            if (splittedData[counter].Contains(SeparatorForDictionary))
                            {
                                IDictionary dictionary = (IDictionary)Activator.CreateInstance(propertyInfo[i].PropertyType);
                                Type[] types = propertyInfo[i].PropertyType.GetGenericArguments();
                                foreach (string s in splittedData[counter].Split(SeparatorForList))
                                {
                                    string[] dictionaryEntry = s.Split(SeparatorForDictionary);
                                    object key = typeof(CsvConvert).GetMethod("Deserialize").MakeGenericMethod(types[0]).Invoke(null, new object[2] { dictionaryEntry[0], separatorIndex + 1 });
                                    object value = typeof(CsvConvert).GetMethod("Deserialize").MakeGenericMethod(types[1]).Invoke(null, new object[2] { dictionaryEntry[1], separatorIndex + 1 });
                                    dictionary.Add(key, value);
                                }
                                propertyInfo[i].SetValue(entity, dictionary);
                            }
                            else if (splittedData[counter].Contains(SeparatorForList))
                            {
                                IList list = (IList)Activator.CreateInstance(propertyInfo[i].PropertyType);
                                Type[] types = propertyInfo[i].PropertyType.GetGenericArguments();
                                foreach (string s in splittedData[counter].Split(SeparatorForList))
                                {
                                    object value = typeof(CsvConvert).GetMethod("Deserialize").MakeGenericMethod(types[0]).Invoke(null, new object[2] { s, separatorIndex + 1 });
                                    list.Add(value);
                                }
                                propertyInfo[i].SetValue(entity, list);
                            }
                            else
                            {
                                object returnedObject = typeof(CsvConvert).GetMethod("Deserialize").MakeGenericMethod(propertyInfo[i].PropertyType).Invoke(null, new object[2] { splittedData[counter], separatorIndex + 1 });
                                propertyInfo[i].SetValue(entity, returnedObject);
                            }
                        }
                        else if (propertyInfo[i].PropertyType.BaseType != typeof(Enum))
                        {
                            propertyInfo[i].SetValue(entity,
                            !string.IsNullOrWhiteSpace(splittedData[counter]) ?
                                (propertyInfo[i].PropertyType.GenericTypeArguments.Length == 0 ?
                                    Convert.ChangeType(splittedData[counter], propertyInfo[i].PropertyType, CultureInfo.InvariantCulture) :
                                    Convert.ChangeType(splittedData[counter], propertyInfo[i].PropertyType.GenericTypeArguments[0], CultureInfo.InvariantCulture)
                                )
                                : null);
                        }
                        else
                        {
                            propertyInfo[i].SetValue(entity, Enum.Parse(propertyInfo[i].PropertyType, splittedData[counter]));
                        }
                    }
                    catch (Exception er)
                    {
                        string oororo = er.ToString();
                    }
                    counter++;
                }
            }
            return entity;
        }
    }
    public class CsvIgnore : Attribute
    {
    }
}
