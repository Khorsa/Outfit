/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using System.Globalization;
using System.Windows;

namespace OutfitTool.Services;

public static class LocalizationHelper
{
    public static readonly Dictionary<string, Language> Languages = new Dictionary<string, Language>();
    public static event EventHandler? LanguageChanged;

    static LocalizationHelper()
    {
        Languages.Add("ru-RU", new Language("Русский", "flags/flag_russia.png", "ru-RU"));
        Languages.Add("en-US", new Language("English", "flags/flag_usa.png", "en-US"));
    }

    public static string GetString(string key)
    {
        ResourceDictionary? dict = (from d in Application.Current.Resources.MergedDictionaries
            where d.Source != null && d.Source.OriginalString.StartsWith("Languages/lang.")
            select d).First();

        if (dict != null && dict[key] != null)
        {
            return dict[key]?.ToString() ?? key;
        }
        return key;
    }

    public static CultureInfo Language
    {
        get => Thread.CurrentThread.CurrentUICulture;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            // Меняем язык приложения:
            Thread.CurrentThread.CurrentUICulture = value;

            // Создаём ResourceDictionary для новой культуры
            var dict = new ResourceDictionary
            {
                Source = Languages[value.Name].DictionarySource
            };

            // Находим старую ResourceDictionary и удаляем его и добавляем новую ResourceDictionary
            var oldDict = (from d in Application.Current.Resources.MergedDictionaries
                where d.Source != null && d.Source.OriginalString.StartsWith("Languages/lang.")
                select d).First();
            if (oldDict != null)
            {
                var ind = Application.Current.Resources.MergedDictionaries.IndexOf(oldDict);
                Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                Application.Current.Resources.MergedDictionaries.Insert(ind, dict);
            }
            else
            {
                Application.Current.Resources.MergedDictionaries.Add(dict);
            }

            // Вызываем евент для оповещения всех окон.
            LanguageChanged?.Invoke(Application.Current, EventArgs.Empty);
        }
    }
}


public class Language(string name, string flagResource, string cultureName)
{
    public readonly string Name = name;
    public readonly string FlagResource = flagResource;
    public readonly string CultureName = cultureName;

    public CultureInfo Culture
    {
        get
        {
            return new CultureInfo(CultureName);
        }
    }
    public Uri DictionarySource
    {
        get
        {
            var uri = new Uri(String.Format("Languages/lang.{0}.xaml", CultureName), UriKind.Relative);
            // Проверяем, существует ли ресурс
            try
            {
                var dict = new ResourceDictionary();
                dict.Source = uri;
            } catch (Exception)
            {
                uri = new Uri("Languages/lang.xaml", UriKind.Relative);
            }

            return uri;
        }
    }
}