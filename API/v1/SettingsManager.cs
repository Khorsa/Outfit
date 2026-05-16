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
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OutfitTool.API1;

/// <summary>
/// Provides a generic settings management system for loading and saving application settings to YAML files.
/// </summary>
/// <typeparam name="T">The type of settings class to manage. Must have a parameterless constructor.</typeparam>
/// <remarks>
/// <para>
/// This class handles the serialization and deserialization of settings objects to and from YAML format,
/// storing all settings groups in a single YAML file located in the user's profile directory.
/// </para>
/// <para>
/// The settings file is stored at: <c>%USERPROFILE%\OutfitTools\settings.yaml</c>
/// </para>
/// <para>
/// Each settings class is stored as a separate group within the YAML file, using the class name as the group key.
/// </para>
/// </remarks>
/// <example>
/// The following example demonstrates how to use SettingsManager with a custom settings class:
/// <code>
/// public class AppSettings
/// {
///     public string Theme { get; set; } = "Dark";
///     public int RefreshInterval { get; set; } = 30;
///     public List&lt;string&gt; RecentFiles { get; set; } = new();
/// }
/// 
/// // Usage:
/// var settingsManager = new SettingsManager&lt;AppSettings&gt;();
/// var settings = settingsManager.LoadSettings();
/// settings.Theme = "Light";
/// settingsManager.SaveSettings(settings);
/// </code>
/// </example>
public class SettingsManager<T> where T : new()
{
    private readonly string _settingsFilePath;
    private readonly string _groupName;
    private readonly T? _instance = default;

    /// <summary>
    /// Initializes a new instance of the SettingsManager class for the specified settings type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The constructor automatically determines the settings file path and group name based on the type T.
    /// The settings file is created in the user's profile directory under "OutfitTools\settings.yaml".
    /// </para>
    /// <para>
    /// If the settings directory does not exist, it will be created automatically.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Loads settings from the YAML configuration file.
    /// </summary>
    /// <param name="force">
    /// If <see langword="true"/>, forces reloading from file even if settings are already cached.
    /// If <see langword="false"/> (default), returns cached instance if available.
    /// </param>
    /// <returns>
    /// An instance of type <typeparamref name="T"/> containing the loaded settings.
    /// If the settings file doesn't exist or the specific group is not found, returns a new instance with default values.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs the following steps:
    /// <list type="number">
    /// <item><description>Returns cached instance if available and not forced to reload</description></item>
    /// <item><description>Creates a new instance with default values if settings file doesn't exist</description></item>
    /// <item><description>Reads and parses the YAML file using CamelCase naming convention</description></item>
    /// <item><description>Extracts the settings group corresponding to the type name</description></item>
    /// <item><description>Maps YAML values to fields and properties using reflection</description></item>
    /// <item><description>Handles type conversion for primitive types, arrays, lists, and dictionaries</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Conversion errors for individual fields or properties are silently ignored, allowing partial loading.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Load settings (uses cache if available)
    /// var settings = settingsManager.LoadSettings();
    /// 
    /// // Force reload from file
    /// var freshSettings = settingsManager.LoadSettings(force: true);
    /// </code>
    /// </example>
    public T LoadSettings(bool force = false)
    {
        if (_instance != null && !force)
        {
            return _instance;
        }

        if (!File.Exists(_settingsFilePath))
        {
            return new T(); // Return new instance with default values
        }

        var yaml = File.ReadAllText(_settingsFilePath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var allSettings = deserializer.Deserialize<SettingsContainer>(yaml);

        // If group not found, return new instance with default values
        if (allSettings.Groups == null || !allSettings.Groups.TryGetValue(_groupName, out var groupSettings)) return new T();

        var settings = new T();
        if (groupSettings is not Dictionary<object, object> settingsDict) return settings;
            
        foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!settingsDict.TryGetValue(field.Name, out var value)) continue;
            try
            {
                field.SetValue(settings, ConvertTo(value, field.FieldType));
            }
            catch
            {
                // ignored
            }
        }

        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!settingsDict.TryGetValue(property.Name, out var value)) continue;
            try
            {
                property.SetValue(settings, ConvertTo(value, property.PropertyType));
            }
            catch
            {
                // ignored
            }
        }

        return settings;
    }

    /// <summary>
    /// Saves the specified settings to the YAML configuration file.
    /// </summary>
    /// <param name="settings">The settings object to save.</param>
    /// <remarks>
    /// <para>
    /// This method performs the following steps:
    /// <list type="number">
    /// <item><description>Loads existing settings from file (if file exists)</description></item>
    /// <item><description>Updates or adds the settings group for the current type</description></item>
    /// <item><description>Serializes the complete settings structure to YAML using CamelCase naming</description></item>
    /// <item><description>Writes the YAML content to the settings file</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The method preserves all existing settings groups in the file while updating only the current type's group.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var settings = new AppSettings { Theme = "Dark", RefreshInterval = 30 };
    /// settingsManager.SaveSettings(settings);
    /// </code>
    /// </example>
    public void SaveSettings(T settings)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var allSettings = new SettingsContainer();

        // Load existing settings if file exists
        if (File.Exists(_settingsFilePath))
        {
            var yaml = File.ReadAllText(_settingsFilePath);
            allSettings = deserializer.Deserialize<SettingsContainer>(yaml);
        }

        // Create groups dictionary if it doesn't exist
        allSettings.Groups ??= new Dictionary<string, object>();

        // Save settings
        if (settings != null && !allSettings.Groups.TryAdd(_groupName, settings))
        {
            // Update existing group
            allSettings.Groups[_groupName] = settings;
        }

        // Serialize and save settings to file
        var yamlOutput = serializer.Serialize(allSettings);
        File.WriteAllText(_settingsFilePath, yamlOutput);
    }

    /// <summary>
    /// Internal container class for storing all settings groups in the YAML file.
    /// </summary>
    /// <remarks>
    /// This class is used internally for YAML serialization/deserialization and should not be used directly.
    /// </remarks>
    private class SettingsContainer
    {
        /// <summary>
        /// Gets or sets the dictionary of settings groups, keyed by group name.
        /// </summary>
        public Dictionary<string, object>? Groups { get; set; }
    }

    /// <summary>
    /// Converts a value from YAML deserialization to the specified target type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="type">The target type to convert to.</param>
    /// <returns>The converted value, or <see langword="null"/> if conversion fails.</returns>
    /// <remarks>
    /// <para>
    /// This method handles conversion for the following types:
    /// <list type="bullet">
    /// <item><description>Arrays and generic lists</description></item>
    /// <item><description>Generic dictionaries</description></item>
    /// <item><description>Types implementing <see cref="IStringSerializable"/></description></item>
    /// <item><description>Primitive types (bool, int, float, double, decimal, DateTime, string)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// For boolean values, accepts "1" or "true" (case-insensitive) as <see langword="true"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidCastException">
    /// Thrown when the value cannot be converted to the target type.
    /// </exception>
    private static object? ConvertTo(object? value, Type type)
    {
        if (value == null) return null;

        if (type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var listItemType = type.GenericTypeArguments[0];
            var listType = typeof(List<>).MakeGenericType(listItemType);
            var convertedValues = (IList)Activator.CreateInstance(listType)!;

            if (value is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    convertedValues.Add(ConvertTo(item, listItemType));
                }
                return convertedValues;
            }
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var dictionaryKeyType = type.GenericTypeArguments[0];
            var dictionaryValueType = type.GenericTypeArguments[1];
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(dictionaryKeyType, dictionaryValueType);
            var convertedValues = (IDictionary)Activator.CreateInstance(dictionaryType)!;
            if (value is IEnumerable<KeyValuePair<object, object>> enumerable)
            {
                foreach (var item in enumerable)
                {
                    var key = Convert.ChangeType(item.Key, dictionaryKeyType, System.Globalization.CultureInfo.InvariantCulture);
                    var val = Convert.ChangeType(item.Value, dictionaryValueType, System.Globalization.CultureInfo.InvariantCulture);
                    convertedValues.Add(key, val);
                }
                return convertedValues;
            }
        }

        var valueStr = value.ToString();
        try
        {
            if (typeof(IStringSerializable).IsAssignableFrom(type))
            {
                var instance = Activator.CreateInstance(type) as IStringSerializable;
                return instance?.FromString(valueStr);
            }

            if (valueStr == null) return null;
                
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => valueStr == "1" || valueStr.Equals("true", StringComparison.OrdinalIgnoreCase),
                TypeCode.String => valueStr,
                TypeCode.Int32 => int.Parse(valueStr),
                TypeCode.Single => float.Parse(valueStr),
                TypeCode.Double => double.Parse(valueStr),
                TypeCode.Decimal => decimal.Parse(valueStr),
                TypeCode.DateTime => DateTime.Parse(valueStr),
                _ => value
            };
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Unable to convert value '{value}' to type '{type.Name}'.", ex);
        }
    }
}