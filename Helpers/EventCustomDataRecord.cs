using RuleTemplateEngine.Interfaces;
using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq; // Added just in case, though the code uses System.Text.Json mostly.

namespace RuleTemplateEngine.Helpers
{
    public class EventCustomDataRecord<TEntity> : IDataRecord, IRecord
    {
        private readonly string _namePrefix;
        private readonly Dictionary<string, Type> _typeProperties;
        private readonly Dictionary<string, JsonElement> _jsonProperties = new();
        private readonly JsonDocument _entityDocument;

        public virtual object this[string columnName]
        {
            get
            {
                var key = columnName;
                if (!_jsonProperties.ContainsKey(key) && !string.IsNullOrEmpty(_namePrefix) && !columnName.Contains('.'))
                    key = _namePrefix + "." + columnName;

                if (!_jsonProperties.ContainsKey(key))
                {
                    return null;
                }

                string text = Regex.Replace(key, Regex.Escape("[") + "\\d+" + Regex.Escape("]"), "[]");
                if (!_typeProperties.ContainsKey(text))
                {
                    // Property exists in JSON but not in the type map (e.g. declared as object).
                    // Fall back to returning the raw value from the JsonElement.
                    return _jsonProperties[key].ValueKind switch
                    {
                        JsonValueKind.String => _jsonProperties[key].GetString(),
                        JsonValueKind.Number => _jsonProperties[key].TryGetInt64(out long l) ? l : _jsonProperties[key].GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null,
                        _ => _jsonProperties[key].GetRawText()
                    };
                }

                var targetType = _typeProperties[text];
                var effectiveTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;

                // Handle enums explicitly to support both numeric and string representations
                if (effectiveTarget.IsEnum)
                {
                    if (_jsonProperties[key].ValueKind == JsonValueKind.Null)
                        return null;

                    try
                    {
                        if (_jsonProperties[key].ValueKind == JsonValueKind.String)
                        {
                            var s = _jsonProperties[key].GetString();
                            if (s is null)
                                return null;
                            var parsed = Enum.Parse(effectiveTarget, s, true);
                            return parsed;
                        }

                        if (_jsonProperties[key].ValueKind == JsonValueKind.Number && _jsonProperties[key].TryGetInt64(out var l))
                        {
                            var obj = Enum.ToObject(effectiveTarget, l);
                            return obj;
                        }
                    }
                    catch
                    {
                        // fall through to generic deserialization below on failure
                    }
                }

                return _jsonProperties[key].Deserialize(targetType);
            }
        }

        public virtual string Id => Guid.NewGuid().ToString();

        public virtual string[] Columns => _jsonProperties.Keys.ToArray();
        public EventCustomDataRecord(TEntity entity, string namePrefix)
        {
            _namePrefix = namePrefix ?? string.Empty;
            _typeProperties = new Dictionary<string, Type>();
            AddTypeProperties(typeof(TEntity), _namePrefix, _typeProperties);

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(entity);
            _entityDocument = JsonDocument.Parse(json);
            AddJsonProperties(_entityDocument.RootElement, _namePrefix, _jsonProperties);
        }

        private static void AddTypeProperties(Type type, string namePrefix, Dictionary<string, Type> typesMap)
        {
            // Register current prefix for value types / System types (e.g. Guid, DateTime) so collection element paths like "InfoRequest.EntityIds[]" resolve
            if (type.IsPrimitive || (type.Namespace == "System" && (type.IsValueType || type == typeof(string))))
            {
                typesMap[namePrefix] = type;
                return;
            }

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo propertyInfo in properties)
            {
                string propertyName = string.IsNullOrEmpty(namePrefix) ? propertyInfo.Name : $"{namePrefix}.{propertyInfo.Name}";

                if (propertyInfo.PropertyType.IsPrimitive || propertyInfo.PropertyType.Namespace == "System")
                {
                    typesMap[propertyName] = propertyInfo.PropertyType;
                    continue;
                }

                if (propertyInfo.PropertyType.IsArray)
                {
                    typesMap[propertyName] = propertyInfo.PropertyType;
                    Type elementType = propertyInfo.PropertyType.GetElementType();
                    if (elementType != null)
                    {
                        AddTypeProperties(elementType, $"{propertyName}[]", typesMap);
                    }
                }
                else if (propertyInfo.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType))
                {
                    typesMap[propertyName] = propertyInfo.PropertyType;
                    Type genericArgument = propertyInfo.PropertyType.GetGenericArguments().FirstOrDefault();
                    if (genericArgument != null)
                    {
                        AddTypeProperties(genericArgument, $"{propertyName}[]", typesMap);
                    }
                }
                else if (!propertyInfo.PropertyType.IsInterface && !propertyInfo.PropertyType.IsAbstract)
                {
                    typesMap[propertyName] = propertyInfo.PropertyType;
                    AddTypeProperties(propertyInfo.PropertyType, propertyName, typesMap);
                }
            }
        }

        protected void AddJsonProperties(JsonElement element, string namePrefix, Dictionary<string, JsonElement> propertiesMap)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    // Don't add the object key itself; only add flattened child paths (e.g. TaxPeriods[0].Id not TaxPeriods[0] as whole object)
                    foreach (JsonProperty item in element.EnumerateObject().ToList())
                    {
                        string namePrefix2 = string.IsNullOrEmpty(namePrefix) ? item.Name : namePrefix + "." + item.Name;
                        AddJsonProperties(item.Value, namePrefix2, propertiesMap);
                    }
                    break;
                case JsonValueKind.Array:
                    // Don't add the array root; only add flattened indexed paths (e.g. TaxPeriods[0].Id, TaxPeriods[0].Name)
                    int num = 0;
                    JsonElement[] array = element.EnumerateArray().ToArray();
                    foreach (JsonElement element2 in array)
                    {
                        AddJsonProperties(element2, $"{namePrefix}[{num++}]", propertiesMap);
                    }
                    break;
                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    propertiesMap.Add(namePrefix ?? "", element);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported JSON value kind: {element.ValueKind}");
            }
        }
    }
}
