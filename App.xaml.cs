namespace MauiCalculator;

public partial class App : Application
{
    public const string ThemePreferenceKey = "AppThemeSelection";

    public App()
    {
        InitializeComponent();
        LoadSavedTheme();
        MainPage = new AppShell();
    }

    private void LoadSavedTheme()
    {
        // 0: System (Varsayılan), 1: Light, 2: Dark
        int savedTheme = Preferences.Default.Get(ThemePreferenceKey, 0);

        UserAppTheme = savedTheme switch
        {
            1 => AppTheme.Light,
            2 => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }
}