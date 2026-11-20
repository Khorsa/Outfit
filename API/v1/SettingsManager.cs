/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OutfitTool.API1;

/// <summary>
/// Generic YAML settings manager. Stores all settings groups in
/// %USERPROFILE%\OutfitTools\settings.yaml.
///
/// Запись защищена межпроцессным mutex и выполняется через временный файл
/// с резервной копией, чтобы обрыв процесса не оставлял обрезанный YAML.
/// </summary>
public class SettingsManager<T> where T : new()
{
    private readonly string _settingsFilePath;
    private readonly string _backupFilePath;
    private readonly string _groupName;
    private T? _instance;

    public SettingsManager()
        : this(null)
    {
    }

    public SettingsManager(string? settingsDirectory)
    {
        _groupName = typeof(T).Name;

        var directory = settingsDirectory;
        if (string.IsNullOrWhiteSpace(directory))
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            directory = Path.Combine(userProfile, "OutfitTools");
        }

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _settingsFilePath = Path.Combine(directory, "settings.yaml");
        _backupFilePath = _settingsFilePath + ".bak";
    }

    public T LoadSettings(bool force = false)
    {
        if (_instance != null && !force)
        {
            return _instance;
        }

        using var fileLock = SettingsFileLock.Acquire();
        var settings = LoadFromDisk();
        _instance = settings;
        return settings;
    }

    public void SaveSettings(T settings)
    {
        if (settings == null) return;

        using var fileLock = SettingsFileLock.Acquire();

        var allSettings = LoadContainerFromDisk() ?? new SettingsContainer();
        allSettings.Groups ??= new Dictionary<string, object>();
        allSettings.Groups[_groupName] = settings;

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var yamlOutput = serializer.Serialize(allSettings);
        WriteAtomically(yamlOutput);
        _instance = settings;
    }

    private T LoadFromDisk()
    {
        if (!File.Exists(_settingsFilePath) && !File.Exists(_backupFilePath))
        {
            return new T();
        }

        var allSettings = LoadContainerFromDisk();
        if (allSettings?.Groups == null
            || !allSettings.Groups.TryGetValue(_groupName, out var groupSettings))
        {
            return new T();
        }

        var settings = new T();
        if (groupSettings is not Dictionary<object, object> settingsDict)
        {
            return settings;
        }

        foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var fieldName = CamelCaseNamingConvention.Instance.Apply(field.Name);
            if (!settingsDict.TryGetValue(fieldName, out var value)) continue;

            try
            {
                field.SetValue(settings, ConvertTo(value, field.FieldType));
            }
            catch
            {
                // Оставляем значение по умолчанию: повреждено только одно поле.
            }
        }

        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propertyName = CamelCaseNamingConvention.Instance.Apply(property.Name);
            if (!settingsDict.TryGetValue(propertyName, out var value)) continue;

            try
            {
                property.SetValue(settings, ConvertTo(value, property.PropertyType));
            }
            catch
            {
                // Оставляем значение по умолчанию.
            }
        }

        return settings;
    }

    private SettingsContainer? LoadContainerFromDisk()
    {
        foreach (var path in new[] { _settingsFilePath, _backupFilePath })
        {
            if (!File.Exists(path)) continue;

            try
            {
                var yaml = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(yaml)) continue;

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var container = deserializer.Deserialize<SettingsContainer>(yaml);
                if (container?.Groups != null) return container;
            }
            catch
            {
                // Пробуем резервную копию; совсем битый файл не должен блокировать запуск.
            }
        }

        return null;
    }

    private void WriteAtomically(string yamlOutput)
    {
        var directory = Path.GetDirectoryName(_settingsFilePath)!;
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, ".settings." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(tempPath, yamlOutput, new UTF8Encoding(false));

            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    File.Replace(tempPath, _settingsFilePath, _backupFilePath, true);
                }
                catch (PlatformNotSupportedException)
                {
                    ReplaceFallback(tempPath);
                }
                catch (IOException)
                {
                    ReplaceFallback(tempPath);
                }
            }
            else
            {
                File.Move(tempPath, _settingsFilePath);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* ignored */ }
            }
        }
    }

    private void ReplaceFallback(string tempPath)
    {
        if (File.Exists(_settingsFilePath))
        {
            File.Copy(_settingsFilePath, _backupFilePath, true);
            File.Delete(_settingsFilePath);
        }

        File.Move(tempPath, _settingsFilePath);
    }

    private static object? ConvertTo(object? value, Type type)
    {
        if (value == null) return null;

        if (type == typeof(string)) return value.ToString();

        if (type.IsArray)
        {
            var elementType = type.GetElementType() ?? typeof(object);
            if (value is not IEnumerable enumerable) return null;

            var items = new List<object?>();
            foreach (var item in enumerable)
            {
                items.Add(ConvertTo(item, elementType));
            }

            var array = Array.CreateInstance(elementType, items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i] != null) array.SetValue(items[i], i);
            }

            return array;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var itemType = type.GenericTypeArguments[0];
            var listType = typeof(List<>).MakeGenericType(itemType);
            var convertedValues = (IList)Activator.CreateInstance(listType)!;

            if (value is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    convertedValues.Add(ConvertTo(item, itemType));
                }

                return convertedValues;
            }
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var keyType = type.GenericTypeArguments[0];
            var valueType = type.GenericTypeArguments[1];
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            var convertedValues = (IDictionary)Activator.CreateInstance(dictionaryType)!;

            if (value is IEnumerable<KeyValuePair<object, object>> enumerable)
            {
                foreach (var item in enumerable)
                {
                    var key = Convert.ChangeType(item.Key, keyType, CultureInfo.InvariantCulture);
                    var converted = ConvertTo(item.Value, valueType);
                    convertedValues.Add(key, converted);
                }

                return convertedValues;
            }
        }

        var valueStr = value.ToString();
        if (valueStr == null) return null;

        if (typeof(IStringSerializable).IsAssignableFrom(type))
        {
            var instance = Activator.CreateInstance(type) as IStringSerializable;
            return instance?.FromString(valueStr);
        }

        try
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => valueStr == "1" || bool.TryParse(valueStr, out var boolean) && boolean,
                TypeCode.String => valueStr,
                TypeCode.Int32 => int.Parse(valueStr, CultureInfo.InvariantCulture),
                TypeCode.Single => float.Parse(valueStr, CultureInfo.InvariantCulture),
                TypeCode.Double => double.Parse(valueStr, CultureInfo.InvariantCulture),
                TypeCode.Decimal => decimal.Parse(valueStr, CultureInfo.InvariantCulture),
                TypeCode.DateTime => DateTime.Parse(valueStr, CultureInfo.InvariantCulture),
                _ => value
            };
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Unable to convert value '{value}' to type '{type.Name}'.", ex);
        }
    }

    private class SettingsContainer
    {
        public Dictionary<string, object>? Groups { get; set; }
    }
}

/// <summary>
/// Межпроцессная блокировка settings.yaml. Используется и оболочкой, и модулями.
/// </summary>
internal sealed class SettingsFileLock : IDisposable
{
    private const string MutexName = @"Local\OutfitTool.Settings";
    private static readonly object ThreadLock = new();

    private readonly Mutex _mutex;
    private bool _hasMutex;
    private bool _hasThreadLock;

    private SettingsFileLock(Mutex mutex)
    {
        _mutex = mutex;
    }

    public static SettingsFileLock Acquire(TimeSpan? timeout = null)
    {
        var mutex = new Mutex(false, MutexName);
        Monitor.Enter(ThreadLock);

        try
        {
            bool acquired;
            try
            {
                acquired = mutex.WaitOne(timeout ?? TimeSpan.FromSeconds(10));
            }
            catch (AbandonedMutexException)
            {
                // Предыдущий владелец упал, не освободив mutex. Владение перешло к нам.
                acquired = true;
            }

            if (!acquired)
            {
                throw new TimeoutException("Не удалось получить блокировку settings.yaml.");
            }

            return new SettingsFileLock(mutex)
            {
                _hasMutex = true,
                _hasThreadLock = true
            };
        }
        catch
        {
            Monitor.Exit(ThreadLock);
            mutex.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_hasMutex)
        {
            _mutex.ReleaseMutex();
            _hasMutex = false;
        }

        if (_hasThreadLock)
        {
            Monitor.Exit(ThreadLock);
            _hasThreadLock = false;
        }

        _mutex.Dispose();
    }
}
