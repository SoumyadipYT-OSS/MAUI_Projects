
using FTIRD.Calc.Models;
using System.Globalization;

namespace FTIRD.Calc.Pages;

public partial class Calcu : ContentPage
{
	public Calcu()
	{
		InitializeComponent();
		OnClear(this, null);
	}

	string currentEntry = "0";
	string? pendingOperator;
	double? firstNumber;
	double? secondNumber;
	string? mathOperator;
	bool isEnteringSecond;
	bool hasDecimal;
	bool clearEntryOnNextDigit;
	const string IntegerFormat = "N0";
	const string DecimalFormat = "N2";

	string FormatNumber(double value)
	{
		var format = value % 1 == 0 ? IntegerFormat : DecimalFormat;
		return value.ToTrimmedString(format);
	}

	string ActiveOperatorDisplay => mathOperator == "*" ? "x" : (mathOperator ?? string.Empty);
	string PendingOperatorDisplay => pendingOperator == "*" ? "x" : (pendingOperator ?? string.Empty);

	string CurrentNumberText => string.IsNullOrEmpty(currentEntry) ? "0" : currentEntry;

	double ParseCurrentEntry()
	{
		if (double.TryParse(CurrentNumberText, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
			return number;
		return 0;
	}

	void SetEntry(double value)
	{
		var format = value % 1 == 0 ? IntegerFormat : DecimalFormat;
		var text = value.ToTrimmedString(format);
		currentEntry = text;
		resultText.Text = text;
		hasDecimal = text.Contains('.');
	}

	void RefreshCurrentCalculation()
	{
		if (firstNumber is null)
		{
			CurrentCalculation.Text = string.Empty;
			return;
		}

		if (string.IsNullOrEmpty(mathOperator) || (isEnteringSecond && clearEntryOnNextDigit))
		{
			CurrentCalculation.Text = firstNumber.Value.ToTrimmedString(IntegerFormat);
			return;
		}

		var firstText = FormatNumber(firstNumber.Value);
		if (!isEnteringSecond || clearEntryOnNextDigit)
		{
			CurrentCalculation.Text = $"{firstText} {ActiveOperatorDisplay}";
			return;
		}

		CurrentCalculation.Text = $"{firstText} {ActiveOperatorDisplay} {CurrentNumberText}";
	}


	void OnSelectNumber(object sender, EventArgs e) 
	{
		Button button = (Button)sender;
		string pressed = button.Text;

		if (mathOperator == "%" && !isEnteringSecond)
		{
			isEnteringSecond = true;
			secondNumber = null;
			clearEntryOnNextDigit = true;
		}

		if (pressed == ".")
		{
			if (hasDecimal)
				return;

			if (clearEntryOnNextDigit)
			{
				currentEntry = "0";
				clearEntryOnNextDigit = false;
			}

			hasDecimal = true;
			currentEntry = CurrentNumberText + ".";
			resultText.Text = currentEntry;
			RefreshCurrentCalculation();
			return;
		}

		if (clearEntryOnNextDigit)
		{
			currentEntry = "0";
			hasDecimal = false;
			clearEntryOnNextDigit = false;
			pendingOperator = null;
		}

		if (pressed == "00")
		{
			pressed = "00";
		}

		if (CurrentNumberText == "0" && pressed != "00")
		{
			currentEntry = pressed;
		}
		else
		{
			currentEntry = CurrentNumberText + pressed;
		}

		resultText.Text = currentEntry;
		if (isEnteringSecond)
			secondNumber = ParseCurrentEntry();
		else
			firstNumber = ParseCurrentEntry();

		RefreshCurrentCalculation();
	}

	void OnSelectOperator(object sender, EventArgs e) 
	{
		Button button = (Button)sender;
		string pressed = button.Text;

		if (firstNumber is null)
			firstNumber = ParseCurrentEntry();

		mathOperator = pressed;
		if (pressed == "%")
		{
			// Default to unary percent (e.g. 500% then '=' => 5) until user starts typing a second operand
			isEnteringSecond = false;
			secondNumber = null;
			clearEntryOnNextDigit = false;
		}
		else
		{
			isEnteringSecond = true;
			secondNumber = null;
			clearEntryOnNextDigit = true;
			pendingOperator = pressed;
			resultText.Text = $"{FormatNumber(firstNumber.Value)}{PendingOperatorDisplay}";
		}
		// Do not update CurrentCalculation yet
	} 

	void OnClear(object sender, EventArgs? e) 
	{
		firstNumber = null;
		secondNumber = null;
		mathOperator = null;
		isEnteringSecond = false;
		hasDecimal = false;
		clearEntryOnNextDigit = false;
		currentEntry = "0";
		resultText.Text = "0";
		CurrentCalculation.Text = string.Empty;
	}

    void OnCalculate(object sender, EventArgs? e) 
	{
		if (firstNumber is null)
			return;

		// Unary percent: 200% => 2
		if (mathOperator == "%" && !isEnteringSecond)
		{
			var result = firstNumber.Value * 0.01;
			CurrentCalculation.Text = $"{firstNumber.Value.ToTrimmedString(IntegerFormat)}%";
			firstNumber = result;
			mathOperator = null;
			isEnteringSecond = false;
			clearEntryOnNextDigit = true;
			SetEntry(result);
			return;
		}

		if (string.IsNullOrEmpty(mathOperator))
			return;

		// For binary operators, require an explicit second operand entry.
		// Scenario: user enters "500" then presses "*" then "=" => do nothing.
		if (mathOperator != "%" && isEnteringSecond && clearEntryOnNextDigit)
			return;

		var lhs = firstNumber.Value;
		var rhs = secondNumber ?? ParseCurrentEntry();

		// Binary percent-of: 500 % 20 => 100 (shows 500 x 20)
		if (mathOperator == "%")
		{
			var result = lhs * (rhs * 0.01);
			CurrentCalculation.Text = $"{FormatNumber(lhs)}%x{FormatNumber(rhs)}=";
			firstNumber = result;
			secondNumber = null;
			mathOperator = null;
			isEnteringSecond = false;
			clearEntryOnNextDigit = true;
			SetEntry(result);
			return;
		}

		var calcResult = Calculator.Calculate(lhs, rhs, mathOperator);
		CurrentCalculation.Text = $"{FormatNumber(lhs)} {ActiveOperatorDisplay} {FormatNumber(rhs)} =";
		firstNumber = calcResult;
		secondNumber = null;
		mathOperator = null;
		isEnteringSecond = false;
		clearEntryOnNextDigit = true;
		pendingOperator = null;
		SetEntry(calcResult);
	}

	void OnBackspace(object sender, EventArgs e)
	{
		// If the display is showing a pending operator like "55+" and the user hits backspace,
		// remove the operator and revert to editing the first operand.
		if (isEnteringSecond && clearEntryOnNextDigit && !string.IsNullOrEmpty(pendingOperator))
		{
			mathOperator = null;
			pendingOperator = null;
			isEnteringSecond = false;
			clearEntryOnNextDigit = false;
			currentEntry = FormatNumber(firstNumber ?? 0);
			hasDecimal = currentEntry.Contains('.');
			resultText.Text = currentEntry;
			RefreshCurrentCalculation();
			return;
		}

		if (clearEntryOnNextDigit)
			return;

		if (string.IsNullOrEmpty(currentEntry) || currentEntry == "0")
			return;

		currentEntry = currentEntry[..^1];
		if (string.IsNullOrEmpty(currentEntry) || currentEntry == "-")
			currentEntry = "0";

		// If trailing '.', strip it as well
		if (currentEntry.EndsWith('.'))
			currentEntry = currentEntry[..^1];

		hasDecimal = currentEntry.Contains('.');
		resultText.Text = currentEntry;

		var value = ParseCurrentEntry();
		if (isEnteringSecond)
			secondNumber = value;
		else
			firstNumber = value;

		RefreshCurrentCalculation();
	}

	void OnPercentage(object sender, EventArgs e) 
	{
		if (firstNumber is null)
			firstNumber = ParseCurrentEntry();

		mathOperator = "%";
		// unary by default; if user types a second number we'll switch to binary automatically
		isEnteringSecond = false;
		secondNumber = null;
		clearEntryOnNextDigit = false;
		RefreshCurrentCalculation();
	}

}