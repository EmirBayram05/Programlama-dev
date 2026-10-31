using System.Globalization;

namespace MauiCalculator;

public partial class ScientificPage : ContentPage
{
    private double _firstNumber = 0;
    private string _currentOperator = string.Empty;
    private string _pendingUnaryOp = string.Empty; // Önce sin, cos, log, √ basılırsa tutar
    private bool _isNewEntry = true;
    private bool _hasError = false;
    private bool _isDegree = true;

    public ScientificPage()
    {
        InitializeComponent();
    }

    private void ResetIfError()
    {
        if (_hasError)
        {
            _hasError = false;
            _firstNumber = 0;
            _currentOperator = string.Empty;
            _pendingUnaryOp = string.Empty;
            HistoryLabel.Text = string.Empty;
            DisplayLabel.Text = "0";
            _isNewEntry = true;
        }
    }

    private void OnAngleUnitClicked(object sender, EventArgs e)
    {
        _isDegree = !_isDegree;
        AngleUnitBtn.Text = _isDegree ? "DEG" : "RAD";
    }

    private void OnNumberClicked(object sender, EventArgs e)
    {
        ResetIfError();
        var button = (Button)sender;
        string digit = button.Text;

        if (_isNewEntry || DisplayLabel.Text == "0")
        {
            DisplayLabel.Text = digit;
            _isNewEntry = false;
        }
        else
        {
            DisplayLabel.Text += digit;
        }
    }

    private void OnConstantClicked(object sender, EventArgs e)
    {
        ResetIfError();
        var button = (Button)sender;
        double val = button.Text == "π" ? Math.PI : Math.E;
        DisplayLabel.Text = FormatNumber(val);
        _isNewEntry = false; // Sabit seçildikten sonra '=' veya fonksiyona basılabilsin
    }

    private void OnDecimalClicked(object sender, EventArgs e)
    {
        ResetIfError();
        if (_isNewEntry)
        {
            DisplayLabel.Text = "0,";
            _isNewEntry = false;
            return;
        }

        if (!DisplayLabel.Text.Contains(','))
        {
            DisplayLabel.Text += ",";
        }
    }

    private void OnPlusMinusClicked(object sender, EventArgs e)
    {
        if (_hasError) return;
        if (TryParseDisplay(out double val) && val != 0)
        {
            DisplayLabel.Text = FormatNumber(-val);
        }
    }

    private void OnClearClicked(object sender, EventArgs e)
    {
        _firstNumber = 0;
        _currentOperator = string.Empty;
        _pendingUnaryOp = string.Empty;
        _isNewEntry = true;
        _hasError = false;
        DisplayLabel.Text = "0";
        HistoryLabel.Text = string.Empty;
    }

    private void OnClearEntryClicked(object sender, EventArgs e)
    {
        if (_hasError)
        {
            OnClearClicked(sender, e);
            return;
        }
        DisplayLabel.Text = "0";
        _isNewEntry = true;
    }

    private void OnBackspaceClicked(object sender, EventArgs e)
    {
        if (_hasError || _isNewEntry) return;

        if (DisplayLabel.Text.Length > 1)
        {
            DisplayLabel.Text = DisplayLabel.Text[..^1];
            if (DisplayLabel.Text == "-")
            {
                DisplayLabel.Text = "0";
                _isNewEntry = true;
            }
        }
        else
        {
            DisplayLabel.Text = "0";
            _isNewEntry = true;
        }
    }

    private void OnPercentClicked(object sender, EventArgs e)
    {
        if (_hasError) return;
        if (TryParseDisplay(out double current))
        {
            double result = string.IsNullOrEmpty(_currentOperator)
                ? current / 100.0
                : _firstNumber * (current / 100.0);

            DisplayLabel.Text = FormatNumber(result);
            _isNewEntry = true;
        }
    }

    private void OnScientificUnaryClicked(object sender, EventArgs e)
    {
        ResetIfError();
        var button = (Button)sender;
        string op = button.Text;

        if (!TryParseDisplay(out double current)) return;

        // DURUM 1: Kullanıcı henüz sayı yazmadan önce fonksiyona bastıysa (Örn: sin -> 30 -> =)
        if (_isNewEntry && DisplayLabel.Text == "0")
        {
            _pendingUnaryOp = op;
            string prefix = string.IsNullOrEmpty(_currentOperator)
                ? ""
                : $"{FormatNumber(_firstNumber)} {(_currentOperator == "xʸ" ? "^" : _currentOperator)} ";
            HistoryLabel.Text = $"{prefix}{GetUnarySymbol(op)}(";
            return;
        }

        // DURUM 2: Kullanıcı önce fonksiyon, sonra sayı yazıp, '=' yerine tekrar fonksiyona bastıysa
        if (!string.IsNullOrEmpty(_pendingUnaryOp))
        {
            if (!ExecuteScientificUnary(_pendingUnaryOp, current, out current, out _))
                return;
            _pendingUnaryOp = string.Empty;
        }

        // DURUM 3: Kullanıcı önce sayıyı yazıp sonra fonksiyona bastıysa (Örn: 30 -> sin)
        if (ExecuteScientificUnary(op, current, out double result, out string historyText))
        {
            HistoryLabel.Text = historyText;
            DisplayLabel.Text = FormatNumber(result);
            _isNewEntry = true;
        }
    }

    private string GetUnarySymbol(string op) => op switch
    {
        "¹/x" => "1/",
        "x²" => "sqr",
        "x³" => "cube",
        "²√x" => "√",
        "|x|" => "abs",
        "10ˣ" => "10^",
        "eˣ" => "e^",
        "n!" => "fact",
        _ => op
    };

    private bool ExecuteScientificUnary(string op, double current, out double result, out string historyText)
    {
        result = 0;
        string formattedInput = FormatNumber(current);
        historyText = string.Empty;

        switch (op)
        {
            case "sin":
                result = Math.Sin(ToRadians(current));
                historyText = $"sin({formattedInput})";
                break;

            case "cos":
                result = Math.Cos(ToRadians(current));
                historyText = $"cos({formattedInput})";
                break;

            case "tan":
                if (_isDegree && Math.Abs(current % 180) == 90)
                {
                    ShowError("Geçersiz giriş");
                    return false;
                }
                result = Math.Tan(ToRadians(current));
                historyText = $"tan({formattedInput})";
                break;

            case "x²":
                result = current * current;
                historyText = $"sqr({formattedInput})";
                break;

            case "x³":
                result = current * current * current;
                historyText = $"cube({formattedInput})";
                break;

            case "²√x":
                if (current < 0)
                {
                    ShowError("Geçersiz giriş");
                    return false;
                }
                result = Math.Sqrt(current);
                historyText = $"√({formattedInput})";
                break;

            case "¹/x":
                if (current == 0)
                {
                    ShowError("Sıfıra bölünemez");
                    return false;
                }
                result = 1.0 / current;
                historyText = $"1/({formattedInput})";
                break;

            case "|x|":
                result = Math.Abs(current);
                historyText = $"abs({formattedInput})";
                break;

            case "10ˣ":
                result = Math.Pow(10, current);
                historyText = $"10^({formattedInput})";
                break;

            case "eˣ":
                result = Math.Exp(current);
                historyText = $"e^({formattedInput})";
                break;

            case "log":
                if (current <= 0)
                {
                    ShowError("Geçersiz giriş");
                    return false;
                }
                result = Math.Log10(current);
                historyText = $"log({formattedInput})";
                break;

            case "ln":
                if (current <= 0)
                {
                    ShowError("Geçersiz giriş");
                    return false;
                }
                result = Math.Log(current);
                historyText = $"ln({formattedInput})";
                break;

            case "n!":
                if (current < 0 || current != Math.Floor(current) || current > 170)
                {
                    ShowError("Geçersiz giriş");
                    return false;
                }
                result = Factorial((int)current);
                historyText = $"fact({formattedInput})";
                break;
        }

        if (double.IsNaN(result) || double.IsInfinity(result))
        {
            ShowError("Sonuç tanımsız");
            return false;
        }

        return true;
    }

    private void OnOperatorClicked(object sender, EventArgs e)
    {
        if (_hasError) return;
        var button = (Button)sender;
        string newOp = button.Text;

        if (!TryParseDisplay(out double current)) return;

        // Bekleyen bir fonksiyon varsa (örn: sin -> 30 -> +) önce onu hesapla
        if (!string.IsNullOrEmpty(_pendingUnaryOp))
        {
            if (!ExecuteScientificUnary(_pendingUnaryOp, current, out current, out _))
                return;
            _pendingUnaryOp = string.Empty;
            DisplayLabel.Text = FormatNumber(current);
        }

        if (!string.IsNullOrEmpty(_currentOperator) && !_isNewEntry)
        {
            if (!Calculate(_firstNumber, current, _currentOperator, out double intermediate))
                return;

            _firstNumber = intermediate;
            DisplayLabel.Text = FormatNumber(intermediate);
        }
        else
        {
            _firstNumber = current;
        }

        _currentOperator = newOp;
        string displayOp = newOp == "xʸ" ? "^" : newOp;
        HistoryLabel.Text = $"{FormatNumber(_firstNumber)} {displayOp}";
        _isNewEntry = true;
    }

    private void OnEqualsClicked(object sender, EventArgs e)
    {
        if (_hasError) return;
        if (!TryParseDisplay(out double currentNumber)) return;

        // Eğer kullanıcı önce fonksiyon (sin, log, √ vb.), sonra sayı, sonra '=' bastıysa:
        if (!string.IsNullOrEmpty(_pendingUnaryOp))
        {
            string opToRun = _pendingUnaryOp;
            _pendingUnaryOp = string.Empty;

            if (ExecuteScientificUnary(opToRun, currentNumber, out double unaryResult, out string historyText))
            {
                // Öncesinde bekleyen bir ikili işlem varsa (Örn: 5 + sin -> 30 -> =) onu da tamamla
                if (!string.IsNullOrEmpty(_currentOperator))
                {
                    if (Calculate(_firstNumber, unaryResult, _currentOperator, out double finalRes))
                    {
                        string displayOp = _currentOperator == "xʸ" ? "^" : _currentOperator;
                        HistoryLabel.Text = $"{FormatNumber(_firstNumber)} {displayOp} {historyText} =";
                        DisplayLabel.Text = FormatNumber(finalRes);
                        _firstNumber = finalRes;
                        _currentOperator = string.Empty;
                    }
                }
                else
                {
                    HistoryLabel.Text = $"{historyText} =";
                    DisplayLabel.Text = FormatNumber(unaryResult);
                    _firstNumber = unaryResult;
                }
                _isNewEntry = true;
            }
            return;
        }

        if (string.IsNullOrEmpty(_currentOperator)) return;

        if (Calculate(_firstNumber, currentNumber, _currentOperator, out double result))
        {
            string displayOp = _currentOperator == "xʸ" ? "^" : _currentOperator;
            HistoryLabel.Text = $"{FormatNumber(_firstNumber)} {displayOp} {FormatNumber(currentNumber)} =";
            DisplayLabel.Text = FormatNumber(result);
            _firstNumber = result;
            _currentOperator = string.Empty;
            _isNewEntry = true;
        }
    }

    private bool Calculate(double a, double b, string op, out double result)
    {
        result = 0;
        switch (op)
        {
            case "+":
                result = a + b;
                break;
            case "-":
                result = a - b;
                break;
            case "×":
                result = a * b;
                break;
            case "÷":
                if (b == 0)
                {
                    ShowError("Sıfıra bölünemez");
                    return false;
                }
                result = a / b;
                break;
            case "mod":
                if (b == 0)
                {
                    ShowError("Sıfıra bölünemez");
                    return false;
                }
                result = a % b;
                break;
            case "xʸ":
                result = Math.Pow(a, b);
                break;
        }

        if (double.IsNaN(result) || double.IsInfinity(result))
        {
            ShowError("Sonuç tanımsız");
            return false;
        }

        return true;
    }

    private double ToRadians(double angle)
    {
        return _isDegree ? angle * Math.PI / 180.0 : angle;
    }

    private static double Factorial(int n)
    {
        double res = 1;
        for (int i = 2; i <= n; i++)
            res *= i;
        return res;
    }

    private void ShowError(string message)
    {
        DisplayLabel.Text = message;
        _hasError = true;
        _isNewEntry = true;
        _pendingUnaryOp = string.Empty;
        _currentOperator = string.Empty;
    }

    private bool TryParseDisplay(out double value)
    {
        string normalized = DisplayLabel.Text.Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    private static string FormatNumber(double value)
    {
        // Radyan dönüşümündeki ufak kaymaları (örn. sin(30) = 0.49999999999999994 -> 0.5) yuvarlar
        double rounded = Math.Round(value, 10);
        if (Math.Abs(rounded) < 1e-12) rounded = 0;
        return rounded.ToString("G12", CultureInfo.InvariantCulture).Replace('.', ',');
    }
}