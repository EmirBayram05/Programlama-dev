using System.Globalization;

namespace MauiCalculator;

public partial class StandardPage : ContentPage
{
    private double _firstNumber = 0;
    private string _currentOperator = string.Empty;
    private string _pendingUnaryOp = string.Empty; // Önce karekök/kare basılırsa tutar
    private bool _isNewEntry = true;
    private bool _hasError = false;

    public StandardPage()
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

    private void OnUnaryOperatorClicked(object sender, EventArgs e)
    {
        ResetIfError();
        var button = (Button)sender;
        string op = button.Text;

        if (!TryParseDisplay(out double current)) return;

        // Kullanıcı henüz sayı girmeden (ekran 0 iken) doğrudan √ veya x² tuşuna bastıysa:
        // Önce fonksiyon sonra sayı (Örn: √ -> 9 -> =) moduna geç
        if (_isNewEntry && DisplayLabel.Text == "0" && string.IsNullOrEmpty(_currentOperator))
        {
            _pendingUnaryOp = op;
            HistoryLabel.Text = $"{GetUnarySymbol(op)}(";
            return;
        }

        // Kullanıcı önce sayıyı yazıp sonra √ tuşuna bastıysa (Örn: 9 -> √): Anında hesapla
        if (ExecuteUnary(op, current, out double result, out string historyText))
        {
            HistoryLabel.Text = historyText;
            DisplayLabel.Text = FormatNumber(result);
            _pendingUnaryOp = string.Empty;
            _isNewEntry = true;
        }
    }

    private string GetUnarySymbol(string op) => op switch
    {
        "¹/x" => "1/",
        "x²" => "sqr",
        "²√x" => "√",
        _ => op
    };

    private bool ExecuteUnary(string op, double input, out double result, out string historyText)
    {
        result = 0;
        string formattedInput = FormatNumber(input);
        historyText = string.Empty;

        switch (op)
        {
            case "¹/x":
                if (input == 0)
                {
                    ShowError("Sıfıra bölünemez");
                    return false;
                }
                result = 1.0 / input;
                historyText = $"1/({formattedInput})";
                break;

            case "x²":
                result = input * input;
                historyText = $"sqr({formattedInput})";
                break;

            case "²√x":
                if (input < 0)
                {
                    ShowError("Geçersiz giriş");
                    return false;
                }
                result = Math.Sqrt(input);
                historyText = $"√({formattedInput})";
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

        // Bekleyen bir tekli işlem (örn. √ -> 9 -> +) varsa önce onu hesapla
        if (!string.IsNullOrEmpty(_pendingUnaryOp))
        {
            if (!ExecuteUnary(_pendingUnaryOp, current, out current, out _))
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
        HistoryLabel.Text = $"{FormatNumber(_firstNumber)} {_currentOperator}";
        _isNewEntry = true;
    }

    private void OnEqualsClicked(object sender, EventArgs e)
    {
        if (_hasError) return;
        if (!TryParseDisplay(out double currentNumber)) return;

        // Eğer kullanıcı önce fonksiyon, sonra sayı, sonra '=' bastıysa (Örn: √ -> 9 -> =)
        if (!string.IsNullOrEmpty(_pendingUnaryOp))
        {
            string opToRun = _pendingUnaryOp;
            _pendingUnaryOp = string.Empty;

            if (ExecuteUnary(opToRun, currentNumber, out double unaryResult, out string historyText))
            {
                if (!string.IsNullOrEmpty(_currentOperator))
                {
                    if (Calculate(_firstNumber, unaryResult, _currentOperator, out double finalRes))
                    {
                        HistoryLabel.Text = $"{FormatNumber(_firstNumber)} {_currentOperator} {historyText} =";
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
            HistoryLabel.Text = $"{FormatNumber(_firstNumber)} {_currentOperator} {FormatNumber(currentNumber)} =";
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
        }

        if (double.IsNaN(result) || double.IsInfinity(result))
        {
            ShowError("Sonuç tanımsız");
            return false;
        }

        return true;
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
        if (Math.Abs(value) < 1e-12) value = 0;
        return value.ToString("G12", CultureInfo.InvariantCulture).Replace('.', ',');
    }
}