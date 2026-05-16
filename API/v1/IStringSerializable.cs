/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
namespace OutfitTool.API1;

/// <summary>
/// Defines a contract for objects that can be serialized to and deserialized from string representation.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a standardized approach for converting objects to string format and reconstructing
/// them from string data. It is particularly useful for scenarios such as configuration persistence,
/// data exchange between components, and storage in text-based formats.
/// </para>
/// <para>
/// Implement this interface when you need to provide custom string serialization logic for your types,
/// especially when the default serialization approaches are insufficient or when you require
/// human-readable string representations.
/// </para>
/// </remarks>
/// <example>
/// The following example demonstrates a simple implementation for a custom type:
/// <code>
/// public class Color : IStringSerializable
/// {
///     public int Red { get; set; }
///     public int Green { get; set; }
///     public int Blue { get; set; }
///     
///     public string ToString()
///     {
///         return $"{Red},{Green},{Blue}";
///     }
///     
///     public Color? FromString(string? input)
///     {
///         if (string.IsNullOrEmpty(input)) return null;
///         
///         var parts = input.Split(',');
///         if (parts.Length == 3 && 
///             int.TryParse(parts[0], out int r) &&
///             int.TryParse(parts[1], out int g) && 
///             int.TryParse(parts[2], out int b))
///         {
///             return new Color { Red = r, Green = g, Blue = b };
///         }
///         
///         return null;
///     }
/// }
/// </code>
/// </example>
public interface IStringSerializable
{
    /// <summary>
    /// Converts the current object to its string representation.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    /// <remarks>
    /// <para>
    /// The string representation should contain all necessary information to reconstruct
    /// the object using the <see cref="FromString"/> method. The format should be consistent
    /// and deterministic for the same object state.
    /// </para>
    /// <para>
    /// Consider making the string representation human-readable when possible, especially
    /// if it will be displayed to users or stored in configuration files.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Various string representation examples:
    /// "192,168,1,1"                          // IP address
    /// "2023-12-25T10:30:00"                  // DateTime
    /// "Enabled;Timeout=30;Retries=3"         // Configuration object
    /// "RGB(255,0,0)"                         // Color with format identifier
    /// </code>
    /// </example>
    string ToString();

    /// <summary>
    /// Creates a new instance of the implementing type from its string representation.
    /// </summary>
    /// <param name="input">
    /// The string representation of the object, typically produced by the <see cref="ToString"/> method.
    /// Can be <see langword="null"/> or empty, in which case the method should return <see langword="null"/>.
    /// </param>
    /// <returns>
    /// A new instance of the implementing type reconstructed from the string representation,
    /// or <see langword="null"/> if the input string cannot be parsed or is invalid.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method should be able to handle strings produced by the <see cref="ToString"/> method
    /// and reconstruct equivalent objects. The implementation should be robust and handle
    /// various error conditions gracefully.
    /// </para>
    /// <para>
    /// If the input format is invalid or cannot be parsed, the method should return <see langword="null"/>
    /// rather than throwing an exception, to allow for graceful error handling by callers.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Usage examples:
    /// var color = new Color().FromString("255,0,0");        // Returns Color object
    /// var invalid = new Color().FromString("invalid");      // Returns null
    /// var empty = new Color().FromString(null);             // Returns null
    /// 
    /// // Typical usage pattern:
    /// var serialized = obj.ToString();
    /// var deserialized = obj.FromString(serialized);
    /// </code>
    /// </example>
    IStringSerializable? FromString(string? input);
}