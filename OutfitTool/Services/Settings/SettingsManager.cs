/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using OutfitTool.ModulesAdapter;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OutfitTool.Services.Settings;

public class SettingsManager<T> where T : new()
{
    private class SettingsContainer
    {
        public Dictionary<string, object>? Groups { get; set; }
    }
    
    private readonly string _settingsFilePath;
    private readonly string _groupName;
    private T? _instance;

    public SettingsManager()
    {
        _groupName = typeof(T).Name;
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var settingsDirPath = Path.Combine(userProfile, "OutfitTools");
        if (!Directory.Exists(settingsDirPath))
        {
            Directory.CreateDirectory(settingsDirPath);
        }

        _settingsFilePath = Path.Combine(settingsDirPath, "settings.yaml");
    }

    public T LoadSettings(bool force = false)
    {
        if (_instance != null && !force)
        {
            return _instance;
        }

        if (!File.Exists(_settingsFilePath))
        {
            return new T(); // Возвращаем новый экземпляр со значениями по умолчанию
        }

        var yaml = File.ReadAllText(_settingsFilePath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var allSettings = deserializer.Deserialize<SettingsContainer>(yaml);

        // Если группа не найдена, возвращаем новый экземпляр со значениями по умолчанию
        if (allSettings.Groups == null || !allSettings.Groups.TryGetValue(_groupName, out var groupSettings))
        {
            return new T();
        }

        var settings = new T();

        if (groupSettings is Dictionary<object, object> settingsDict)
        {
            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (settingsDict.TryGetValue(ToCamelCase(field.Name), out var value))
                {
                    try
                    {
                        field.SetValue(settings, ConvertTo(value, field.FieldType));
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (settingsDict.TryGetValue(ToCamelCase(property.Name), out var value))
                {
                    try
                    {
                        property.SetValue(settings, ConvertTo(value, property.PropertyType));
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        _instance = settings;
        return settings;
    }

    public void SaveSettings(T settings)
    {
        if (settings == null) return;
        
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var allSettings = new SettingsContainer();

        // Загружаем существующие настройки, если файл существует
        if (File.Exists(_settingsFilePath))
        {
            var yaml = File.ReadAllText(_settingsFilePath);
            allSettings = deserializer.Deserialize<SettingsContainer>(yaml);
        }

        // Создаем группу, если её нет
        allSettings.Groups ??= new Dictionary<string, object>();

        if (!allSettings.Groups.ContainsKey(_groupName))
        {
            allSettings.Groups[_groupName] = settings; // Сохраняем настройки
        } else {
            allSettings.Groups[_groupName] = settings; // Обновляем существующую группу
        }

        // Сериализуем и сохраняем настройки в файл
        var yamlOutput = serializer.Serialize(allSettings);
        File.WriteAllText(_settingsFilePath, yamlOutput);
    }

    private object? ConvertTo(object? value, Type type)
    {
        if (value == null) return null;

        if (type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            Type listItemType = type.GenericTypeArguments[0];
            Type listType = typeof(List<>).MakeGenericType(listItemType);
            var convertedValues = Activator.CreateInstance(listType) as IList;

            if (value is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    convertedValues?.Add(ConvertTo(item, listItemType));
                }
                return convertedValues;
            }
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var dictionaryKeyType = type.GenericTypeArguments[0];
            var dictionaryValueType = type.GenericTypeArguments[1];
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(dictionaryKeyType, dictionaryValueType);
            var convertedValues = Activator.CreateInstance(dictionaryType) as IDictionary;
            if (value is IEnumerable<KeyValuePair<object, object>> enumerable)
            {
                foreach (var item in enumerable)
                {
                    var key = Convert.ChangeType(item.Key, dictionaryKeyType, System.Globalization.CultureInfo.InvariantCulture);
                    var val = Convert.ChangeType(item.Value, dictionaryValueType, System.Globalization.CultureInfo.InvariantCulture);
                    convertedValues?.Add(key, val);
                }
                return convertedValues;
            }
        }

        var valueStr = value.ToString();
        try
        {
            if (typeof(IStringSerializable).IsAssignableFrom(type) && valueStr != null)
            {
                var instance = Activator.CreateInstance(type) as IStringSerializable;
                return instance?.FromString(valueStr);
            }

            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => valueStr is "1" or "true",
                TypeCode.String => valueStr,
                TypeCode.Int32 => int.Parse(valueStr ?? "0"),
                TypeCode.Single => float.Parse(valueStr ?? "0"),
                TypeCode.Double => double.Parse(valueStr ?? "0"),
                TypeCode.Decimal => decimal.Parse(valueStr ?? "0"),
                TypeCode.DateTime => DateTime.Parse(valueStr ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                _ => value
            };
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Не удалось преобразовать значение '{value}' к типу '{type.Name}'.", ex);
        }
    }

    private static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Разбиваем строку на слова: по разделителям ИЛИ по заглавным буквам
        string pattern = @"[^a-zA-Z0-9]+|(?<!^)(?=[A-Z])";
        string[] words = Regex.Split(input, pattern);
    
        StringBuilder result = new StringBuilder();
        bool firstWord = true;
    
        foreach (string word in words)
        {
            if (string.IsNullOrEmpty(word))
                continue;
            
            if (firstWord)
            {
                result.Append(word.ToLower());
                firstWord = false;
            }
            else
            {
                result.Append(char.ToUpper(word[0]));
                if (word.Length > 1)
                    result.Append(word.Substring(1).ToLower());
            }
        }
    
        return result.ToString();
    }

}


