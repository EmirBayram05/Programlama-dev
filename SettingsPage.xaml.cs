namespace MauiCalculator;

public partial class SettingsPage : ContentPage
{
    private bool _isInitializing = true;

    public SettingsPage()
    {
        InitializeComponent();
        LoadCurrentSelection();
    }

    private void LoadCurrentSelection()
    {
        _isInitializing = true;
        int savedTheme = Preferences.Default.Get(App.ThemePreferenceKey, 0);

        switch (savedTheme)
        {
            case 1:
                LightRadio.IsChecked = true;
                break;
            case 2:
                DarkRadio.IsChecked = true;
                break;
            default:
                SystemRadio.IsChecked = true;
                break;
        }

        _isInitializing = false;
    }

    private void OnThemeRadioCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_isInitializing || !e.Value) return;

        var radio = (RadioButton)sender;
        int selectedValue = int.Parse(radio.Value.ToString()!);

        // Seçimi Preferences ile kaydet
        Preferences.Default.Set(App.ThemePreferenceKey, selectedValue);

        // Uygulama temasını anında güncelle
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = selectedValue switch
            {
                1 => AppTheme.Light,
                2 => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }
    }
}