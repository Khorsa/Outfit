/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
namespace OutfitTool.API1.Dto;

/// <summary>
/// Represents a semantic version for application modules with major and minor version components and optional label.
/// </summary>
/// <remarks>
/// <para>
/// This class implements semantic versioning for modules with support for version comparison,
/// parsing from strings, and formatted output. It is used throughout the application framework
/// for version management, dependency resolution, and compatibility checking.
/// </para>
/// <para>
/// The version follows the format: <c>major.minor</c> with an optional label (e.g., "1.2", "2.0 beta", "3.1 RC1").
/// </para>
/// </remarks>
/// <example>
/// The following example demonstrates common usage patterns:
/// <code>
/// // Create version instances
/// var version1 = new ModuleVersion(1, 0);
/// var version2 = new ModuleVersion(2, 1, "beta");
/// 
/// // Parse from string
/// var parsedVersion = ModuleVersion.FromString("3.2");
/// 
/// // Compare versions
/// if (version1 < version2)
/// {
///     Console.WriteLine("Version 1 is older than version 2");
/// }
/// 
/// // Display formatted version
/// Console.WriteLine(version2.FullVersion); // "2.1 beta"
/// Console.WriteLine(version2.ToString());  // "2.1"
/// </code>
/// </example>
public class ModuleVersion(
    int majorVersion,
    int minorVersion,
    string versionLabel = "")
{
    private const int VERSION_MULTIPLIER = 10000;
    
    /// <summary>
    /// Gets the major version component indicating significant, incompatible API changes.
    /// </summary>
    /// <value>
    /// An integer representing the major version number. Incremented when making incompatible API changes.
    /// </value>
    /// <remarks>
    /// According to semantic versioning principles, changes to the major version number indicate
    /// backward-incompatible changes that may require updates to dependent modules or code.
    /// </remarks>
    /// <example>
    /// <code>
    /// var version = new ModuleVersion(2, 5);
    /// Console.WriteLine(version.MajorVersion); // Output: 2
    /// </code>
    /// </example>
    public readonly int MajorVersion = majorVersion;

    /// <summary>
    /// Gets the minor version component indicating backward-compatible new functionality.
    /// </summary>
    /// <value>
    /// An integer representing the minor version number. Incremented when adding functionality in a backward-compatible manner.
    /// </value>
    /// <remarks>
    /// Changes to the minor version number indicate new features or enhancements that are
    /// backward-compatible with previous versions in the same major version series.
    /// </remarks>
    /// <example>
    /// <code>
    /// var version = new ModuleVersion(2, 5);
    /// Console.WriteLine(version.MinorVersion); // Output: 5
    /// </code>
    /// </example>
    public readonly int MinorVersion = minorVersion;

    /// <summary>
    /// Gets the complete version string including both version components and optional label.
    /// </summary>
    /// <value>
    /// A string representing the full version in the format: <c>major.minor [label]</c>.
    /// If no label is provided, returns just the version numbers.
    /// </value>
    /// <remarks>
    /// <para>
    /// The full version string is suitable for display to users in UI elements, about dialogs,
    /// and version information screens.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var version1 = new ModuleVersion(1, 2);
    /// Console.WriteLine(version1.FullVersion); // "1.2"
    /// 
    /// var version2 = new ModuleVersion(2, 0, "beta");
    /// Console.WriteLine(version2.FullVersion); // "2.0 beta"
    /// </code>
    /// </example>
    public string FullVersion 
    { 
        get
        {
            var res = MajorVersion + "." + MinorVersion;
            if (versionLabel != "") res += " " + versionLabel;
            return res;
        } 
    }

    /// <summary>
    /// Creates a new ModuleVersion instance by parsing a version string.
    /// </summary>
    /// <param name="version">The version string to parse in "major.minor" format.</param>
    /// <param name="label">Optional label to associate with the version (e.g., "beta", "RC1").</param>
    /// <returns>A new ModuleVersion instance representing the parsed version.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the <paramref name="version"/> string is null or empty.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when the <paramref name="version"/> string is not in the correct format
    /// or contains non-integer components.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The version string must be in the format "major.minor" where both major and minor
    /// are valid integers. Additional version components (like patch version) are not supported.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Parse basic version
    /// var version1 = ModuleVersion.FromString("1.2");
    /// 
    /// // Parse version with label
    /// var version2 = ModuleVersion.FromString("2.0", "stable");
    /// 
    /// // These will throw exceptions:
    /// // var invalid1 = ModuleVersion.FromString("1");          // FormatException
    /// // var invalid2 = ModuleVersion.FromString("1.a");        // FormatException  
    /// // var invalid3 = ModuleVersion.FromString(null);         // ArgumentException
    /// </code>
    /// </example>
    public static ModuleVersion FromString(string version, string label = "")
    {
        if (string.IsNullOrEmpty(version))
            throw new ArgumentException("Version string cannot be null or empty", nameof(version));
        
        var parts = version.Split('.');
        if (parts.Length < 2)
            throw new FormatException("Version string must be in format 'major.minor'");
        
        if (!int.TryParse(parts[0], out int major) || !int.TryParse(parts[1], out int minor))
            throw new FormatException("Version components must be valid integers");
        
        return new ModuleVersion(major, minor, label);
    }

    /// <summary>
    /// Returns the version string in "major.minor" format without the label.
    /// </summary>
    /// <returns>A string representation of the version in "major.minor" format.</returns>
    /// <remarks>
    /// <para>
    /// This method returns the canonical version string suitable for machine processing,
    /// configuration files, and internal version comparisons.
    /// </para>
    /// <para>
    /// Use the <see cref="FullVersion"/> property to get the version including the label.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var version = new ModuleVersion(2, 1, "RC1");
    /// Console.WriteLine(version.ToString()); // "2.1"
    /// </code>
    /// </example>
    public override string ToString()
    {
        return MajorVersion + "." + MinorVersion;
    }

    /// <summary>
    /// Determines whether one version is less than another version.
    /// </summary>
    /// <param name="a">The first version to compare.</param>
    /// <param name="b">The second version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="a"/> is less than <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Version comparison is performed by converting the major and minor versions into a single
    /// numeric value using the formula: <c>major * 10000 + minor</c>.
    /// </remarks>
    public static bool operator <(ModuleVersion a, ModuleVersion b)
    {
        return a.MajorVersion * VERSION_MULTIPLIER + a.MinorVersion < b.MajorVersion * VERSION_MULTIPLIER + b.MinorVersion;
    }

    /// <summary>
    /// Determines whether one version is greater than another version.
    /// </summary>
    /// <param name="a">The first version to compare.</param>
    /// <param name="b">The second version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="a"/> is greater than <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(ModuleVersion a, ModuleVersion b)
    {
        return a.MajorVersion * VERSION_MULTIPLIER + a.MinorVersion > b.MajorVersion * VERSION_MULTIPLIER + b.MinorVersion;
    }

    /// <summary>
    /// Determines whether one version is less than or equal to another version.
    /// </summary>
    /// <param name="a">The first version to compare.</param>
    /// <param name="b">The second version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="a"/> is less than or equal to <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(ModuleVersion a, ModuleVersion b)
    {
        return a.MajorVersion * VERSION_MULTIPLIER + a.MinorVersion <= b.MajorVersion * VERSION_MULTIPLIER + b.MinorVersion;
    }

    /// <summary>
    /// Determines whether one version is greater than or equal to another version.
    /// </summary>
    /// <param name="a">The first version to compare.</param>
    /// <param name="b">The second version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="a"/> is greater than or equal to <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(ModuleVersion a, ModuleVersion b)
    {
        return a.MajorVersion * VERSION_MULTIPLIER + a.MinorVersion >= b.MajorVersion * VERSION_MULTIPLIER + b.MinorVersion;
    }

    /// <summary>
    /// Determines whether two versions are equal.
    /// </summary>
    /// <param name="a">The first version to compare.</param>
    /// <param name="b">The second version to compare.</param>
    /// <returns><see langword="true"/> if the versions are equal; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Version labels are not considered in equality comparisons. Only major and minor version numbers are compared.
    /// </remarks>
    public static bool operator ==(ModuleVersion a, ModuleVersion b)
    {
        return a.MajorVersion * VERSION_MULTIPLIER + a.MinorVersion == b.MajorVersion * VERSION_MULTIPLIER + b.MinorVersion;
    }

    /// <summary>
    /// Determines whether two versions are not equal.
    /// </summary>
    /// <param name="a">The first version to compare.</param>
    /// <param name="b">The second version to compare.</param>
    /// <returns><see langword="true"/> if the versions are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(ModuleVersion a, ModuleVersion b)
    {
        return a.MajorVersion * VERSION_MULTIPLIER + a.MinorVersion != b.MajorVersion * VERSION_MULTIPLIER + b.MinorVersion;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current version.
    /// </summary>
    /// <param name="obj">The object to compare with the current version.</param>
    /// <returns><see langword="true"/> if the specified object is equal to the current version; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Two ModuleVersion instances are considered equal if they have the same major and minor version numbers.
    /// Version labels are not considered in equality comparisons.
    /// </remarks>
    public override bool Equals(object? obj)
    {
        return obj is ModuleVersion other && 
               MajorVersion == other.MajorVersion && 
               MinorVersion == other.MinorVersion;
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current version.</returns>
    /// <remarks>
    /// The hash code is computed based on both major and minor version numbers.
    /// </remarks>
    public override int GetHashCode()
    {
        return HashCode.Combine(MajorVersion, MinorVersion);
    }
}