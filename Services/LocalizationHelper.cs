using System.Globalization;
using System.Windows;

public class LocalizationHelper
{
    public static readonly Dictionary<string, Language> languages = new Dictionary<string, Language>();
    public static event EventHandler? LanguageChanged;

    static LocalizationHelper()
    {
        languages.Add("ru-RU", new Language("Русский", "flags/flag_russia.png", "ru-RU"));
        languages.Add("en-US", new Language("English", "flags/flag_usa.png", "en-US"));
    }

    public static string getString(string key)
    {
        ResourceDictionary? Dict = (from d in Application.Current.Resources.MergedDictionaries
                                    where d.Source != null && d.Source.OriginalString.StartsWith("Languages/lang.")
                                    select d).First();

        if (Dict != null && Dict[key] != null)
        {
            return Dict[key].ToString() ?? key;
        }
        return key;
    }

    public static CultureInfo Language
    {
        get
        {
            return System.Threading.Thread.CurrentThread.CurrentUICulture;
        }
        set
        {
            if (value == null) throw new ArgumentNullException("value");
            if (value == System.Threading.Thread.CurrentThread.CurrentUICulture) return;

            //1. Меняем язык приложения:
            System.Threading.Thread.CurrentThread.CurrentUICulture = value;

            //2. Создаём ResourceDictionary для новой культуры
            ResourceDictionary dict = new ResourceDictionary();
            dict.Source = LocalizationHelper.languages[value.Name].DictionarySource;

            //3. Находим старую ResourceDictionary и удаляем его и добавляем новую ResourceDictionary
            ResourceDictionary oldDict = (from d in Application.Current.Resources.MergedDictionaries
                                            where d.Source != null && d.Source.OriginalString.StartsWith("Languages/lang.")
                                            select d).First();
            if (oldDict != null)
            {
                int ind = Application.Current.Resources.MergedDictionaries.IndexOf(oldDict);
                Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                Application.Current.Resources.MergedDictionaries.Insert(ind, dict);
            }
            else
            {
                Application.Current.Resources.MergedDictionaries.Add(dict);
            }

            //4. Вызываем евент для оповещения всех окон.
            if (LanguageChanged != null)
            {
                LanguageChanged(Application.Current, new EventArgs());
            }
        }
    }
}


public class Language(string Name, string FlagResource, string CultureName)
{
    public readonly string Name = Name;
    public readonly string FlagResource = FlagResource;
    public readonly string CultureName = CultureName;

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
            return new Uri(String.Format("Languages/lang.{0}.xaml", CultureName), UriKind.Relative);
        }
    }
}
