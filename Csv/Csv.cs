using System;
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
        private static string[] Separator = new string[5] { "||", "§§", "##", "££", "^^" };
        private static List<Type> Types = AppDomain.CurrentDomain.GetAssemblies().ToList().FindAll(x => !x.FullName.ToLower().Contains("system")).SelectMany(x => x.GetTypes()).ToList();
        public static string Serialize<T>(T data, int separatorIndex = 0)
        {
            string separator = Separator[separatorIndex];
            StringBuilder stringBuilder = new StringBuilder();
            string separatorString = separator.ToString();
            foreach (PropertyInfo propertyInfo in data.GetType().GetProperties())
            {
                if (propertyInfo.GetCustomAttribute(typeof(CsvIgnore)) == null)
                {
                    if (!propertyInfo.PropertyType.IsPrimitive && propertyInfo.PropertyType != typeof(string))
                    {
                        string nameOfInstance = "";
                        if (propertyInfo.PropertyType.IsAbstract || propertyInfo.PropertyType.IsInterface) nameOfInstance = "[[" + propertyInfo.Name + "]]";
                        stringBuilder.Append($"{nameOfInstance}{Serialize(propertyInfo.GetValue(data), separatorIndex + 1)}{separatorString}");
                    }
                    else
                    {
                        stringBuilder.Append(Invariant($"{propertyInfo.GetValue(data)}{separatorString}"));
                    }
                }
            }
            string result = stringBuilder.ToString();
            return result.Substring(0, result.Length - 2);
        }
        public static T Deserialize<T>(string data, int separatorIndex = 0)
        {
            string separator = Separator[separatorIndex];
            Type type = typeof(T);
            if(type.IsInterface || type.IsAbstract)
            {
                Regex regexName = new Regex(@"\[\[[^]]*\]\]");
                string nameOfInstance = regexName.Match(data)?.Value;
                data = data.Replace(nameOfInstance, "");
                nameOfInstance = nameOfInstance.Trim(']').Trim('[');
                type = Types.FirstOrDefault(x => type.IsAssignableFrom(x) && x.Name == nameOfInstance);
            } 
            T entity = (T)Activator.CreateInstance(type);
            PropertyInfo[] propertyInfo = type.GetProperties();
            List<string> splittedData = Split(data, separator);
            int counter = 0;
            for (int i = 0; i < propertyInfo.Length; i++)
            {
                if (propertyInfo[i].GetCustomAttribute(typeof(CsvIgnore)) == null)
                {
                    try
                    {
                        if (!propertyInfo[i].PropertyType.IsPrimitive && propertyInfo[i].PropertyType != typeof(string))
                        {
                            object returnedObject = typeof(CsvConvert).GetMethod("Deserialize").MakeGenericMethod(propertyInfo[i].PropertyType).Invoke(null, new object[2] { splittedData[counter], separatorIndex + 1 });
                            propertyInfo[i].SetValue(entity, returnedObject);
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
        private static List<string> Split(string start, string value)
        {
            List<string> splitted = new List<string>();
            string x = "";
            for (int i = 0; i < start.Length; i++)
            {
                if(i == start.Length - 1 || (start[i] == value[0] && start?[i+1] == value[1]))
                {
                    if (i == start.Length - 1) x += start[i];
                    i++;
                    splitted.Add(x);
                    x = string.Empty;
                }
                else
                {
                    x += start[i];
                }
            }
            return splitted;
        }
    }
    public class CsvIgnore : Attribute
    {
    }
}
