using Microsoft.Maui.Graphics;
using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CalculatorApp {
    public partial class MainPage : ContentPage {
        private GraphicsView? _pixelGridView;
        private readonly CalculatorEngine _calculator = new();
        private bool _isNewCalculation = true;

        public MainPage() {
            InitializeComponent();

            // Set up the pixel grid effect after the page is fully loaded
            Loaded += OnPageLoaded;

            // Wire up button click events
            WireUpButtonEvents();
        }

        private void OnPageLoaded(object? sender, EventArgs e) {
            // Create pixel grid effect only once when the page is fully loaded
            CreatePixelEffect();
        }

        private void CreatePixelEffect() {
            if (PixelBackground.Parent is not Grid parent) return;

            // Create the GraphicsView only once
            _pixelGridView = new GraphicsView {
                Drawable = new PixelGridDrawable(),
                InputTransparent = true
            };

            // Add it at the beginning
            parent.Children.Insert(0, _pixelGridView);

            // Set up a handler to update its size when PixelBackground changes
            PixelBackground.SizeChanged += (sender, e) => {
                if (PixelBackground.Width > 0 && PixelBackground.Height > 0 && _pixelGridView != null) {
                    _pixelGridView.HeightRequest = PixelBackground.Height;
                    _pixelGridView.WidthRequest = PixelBackground.Width;
                }
            };

            // Initial size setting
            if (PixelBackground.Width > 0 && PixelBackground.Height > 0) {
                _pixelGridView.HeightRequest = PixelBackground.Height;
                _pixelGridView.WidthRequest = PixelBackground.Width;
            }
        }

        private void WireUpButtonEvents() {
            // Find all buttons in the Grid (Row 1)
            var buttonC = FindButtonByGridPosition(0, 0);
            var buttonPlusMinus = FindButtonByGridPosition(0, 1);
            var buttonPercent = FindButtonByGridPosition(0, 2);
            var buttonDivide = FindButtonByGridPosition(0, 3);

            // Find all buttons in the Grid (Row 2)
            var button7 = FindButtonByGridPosition(1, 0);
            var button8 = FindButtonByGridPosition(1, 1);
            var button9 = FindButtonByGridPosition(1, 2);
            var buttonMultiply = FindButtonByGridPosition(1, 3);

            // Find all buttons in the Grid (Row 3)
            var button4 = FindButtonByGridPosition(2, 0);
            var button5 = FindButtonByGridPosition(2, 1);
            var button6 = FindButtonByGridPosition(2, 2);
            var buttonSubtract = FindButtonByGridPosition(2, 3);

            // Find all buttons in the Grid (Row 4)
            var button1 = FindButtonByGridPosition(3, 0);
            var button2 = FindButtonByGridPosition(3, 1);
            var button3 = FindButtonByGridPosition(3, 2);
            var buttonAdd = FindButtonByGridPosition(3, 3);

            // Find all buttons in the Grid (Row 5)
            var button0 = FindButtonByGridPosition(4, 0); // Spans 2 columns
            var buttonDecimal = FindButtonByGridPosition(4, 2);
            var buttonEquals = FindButtonByGridPosition(4, 3);

            // Number buttons
            if (button0 != null) button0.Clicked += (s, e) => OnNumberButtonClicked("0");
            if (button1 != null) button1.Clicked += (s, e) => OnNumberButtonClicked("1");
            if (button2 != null) button2.Clicked += (s, e) => OnNumberButtonClicked("2");
            if (button3 != null) button3.Clicked += (s, e) => OnNumberButtonClicked("3");
            if (button4 != null) button4.Clicked += (s, e) => OnNumberButtonClicked("4");
            if (button5 != null) button5.Clicked += (s, e) => OnNumberButtonClicked("5");
            if (button6 != null) button6.Clicked += (s, e) => OnNumberButtonClicked("6");
            if (button7 != null) button7.Clicked += (s, e) => OnNumberButtonClicked("7");
            if (button8 != null) button8.Clicked += (s, e) => OnNumberButtonClicked("8");
            if (button9 != null) button9.Clicked += (s, e) => OnNumberButtonClicked("9");

            // Operation buttons
            if (buttonAdd != null) buttonAdd.Clicked += (s, e) => OnOperationButtonClicked("+");
            if (buttonSubtract != null) buttonSubtract.Clicked += (s, e) => OnOperationButtonClicked("-");
            if (buttonMultiply != null) buttonMultiply.Clicked += (s, e) => OnOperationButtonClicked("*");
            if (buttonDivide != null) buttonDivide.Clicked += (s, e) => OnOperationButtonClicked("/");

            // Special function buttons
            if (buttonC != null) buttonC.Clicked += OnClearButtonClicked;
            if (buttonEquals != null) buttonEquals.Clicked += OnEqualsButtonClicked;
            if (buttonDecimal != null) buttonDecimal.Clicked += (s, e) => OnDecimalButtonClicked();
            if (buttonPlusMinus != null) buttonPlusMinus.Clicked += OnPlusMinusButtonClicked;
            if (buttonPercent != null) buttonPercent.Clicked += OnPercentButtonClicked;
        }

        private Button? FindButtonByGridPosition(int row, int column) {
            // Get the main grid that contains the calculator buttons
            var buttonGrid = this.FindByName<Grid>("ButtonGrid");
            if (buttonGrid == null) return null;

            // Find the button at the specified row and column
            foreach (var child in buttonGrid.Children) {
                if (child is Button button &&
                    Grid.GetRow(button) == row &&
                    Grid.GetColumn(button) == column) {
                    return button;
                }
            }

            return null;
        }

        private void OnNumberButtonClicked(string number) {
            if (_isNewCalculation || DisplayLabel.Text == "0") {
                DisplayLabel.Text = number;
                _isNewCalculation = false;
            } else {
                DisplayLabel.Text += number;
            }

            _calculator.AddDigit(number);
        }

        private void OnOperationButtonClicked(string operation) {
            _calculator.SetOperation(operation);
            HistoryDisplay.Text = $"{_calculator.FirstOperand} {GetOperationSymbol(operation)}";
            _isNewCalculation = true;
        }

        private void OnEqualsButtonClicked(object? sender, EventArgs e) {
            try {
                var result = _calculator.Calculate();

                // Update history display
                string operationSymbol = GetOperationSymbol(_calculator.CurrentOperation);
                HistoryDisplay.Text = $"{_calculator.FirstOperand} {operationSymbol} {_calculator.SecondOperand} =";

                // Update main display
                DisplayLabel.Text = FormatResult(result);

                // Ready for a new calculation
                _isNewCalculation = true;
            } catch (DivideByZeroException) {
                DisplayLabel.Text = "Cannot divide by zero";
                HistoryDisplay.Text = string.Empty;
            } catch (Exception ex) {
                DisplayLabel.Text = $"Error + {ex.Message}";
                HistoryDisplay.Text = string.Empty;
            }
        }

        private void OnClearButtonClicked(object? sender, EventArgs e) {
            _calculator.Clear();
            DisplayLabel.Text = "0";
            HistoryDisplay.Text = string.Empty;
            _isNewCalculation = true;
        }

        private void OnDecimalButtonClicked() {
            if (_isNewCalculation) {
                DisplayLabel.Text = "0.";
                _isNewCalculation = false;
            } else if (!DisplayLabel.Text.Contains('.')) {
                DisplayLabel.Text += ".";
            }

            _calculator.AddDecimalPoint();
        }

        private void OnPlusMinusButtonClicked(object? sender, EventArgs e) {
            // Toggle the sign of the current number
            if (DisplayLabel.Text.StartsWith('-')) { // Updated to use StartsWith(char)
                DisplayLabel.Text = DisplayLabel.Text[1..];
            } else if (DisplayLabel.Text != "0") {
                DisplayLabel.Text = "-" + DisplayLabel.Text;
            }

            _calculator.ToggleSign();
        }

        private void OnPercentButtonClicked(object? sender, EventArgs e) {
            double value = double.Parse(DisplayLabel.Text, CultureInfo.InvariantCulture);
            value /= 100.0;

            DisplayLabel.Text = FormatResult(value);
            _calculator.SetPercent();
        }

        private static string GetOperationSymbol(string operation) {
            return operation switch {
                "+" => "+",
                "-" => "−",
                "*" => "×",
                "/" => "÷",
                _ => operation
            };
        }

        private static string FormatResult(double result) {
            // Format the result to handle scientific notation and trailing zeros
            if (Math.Abs(result) >= 10_000_000_000 || (Math.Abs(result) < 0.0000001 && result != 0)) {
                return result.ToString("E", CultureInfo.InvariantCulture);
            }

            string formatted = result.ToString(CultureInfo.InvariantCulture);
            if (formatted.Contains('.')) {
                // Remove trailing zeros
                formatted = formatted.TrimEnd('0');
                // Remove the decimal point if it's the last character
                formatted = formatted.TrimEnd('.');
            }

            return formatted;
        }
    }

    // Drawable class for pixel grid effect
    public class PixelGridDrawable : IDrawable {
        public void Draw(ICanvas canvas, RectF dirtyRect) {
            // Draw subtle pixel grid
            canvas.StrokeColor = Colors.DarkGray.WithAlpha(0.1f);
            canvas.StrokeSize = 0.5f;

            // Horizontal lines
            float pixelSize = 5; // Size of each "pixel"
            for (float y = 0; y <= dirtyRect.Height; y += pixelSize) {
                canvas.DrawLine(0, y, dirtyRect.Width, y);
            }

            // Vertical lines
            for (float x = 0; x <= dirtyRect.Width; x += pixelSize) {
                canvas.DrawLine(x, 0, x, dirtyRect.Height);
            }
        }
    }

    // Calculator engine class to handle the calculations
    public class CalculatorEngine {
        public double FirstOperand { get; private set; }
        public double SecondOperand { get; private set; }
        public string CurrentOperation { get; private set; } = string.Empty;

        private bool _isNewOperand = true;
        private bool _hasDecimalPoint = false;
        private readonly StringBuilder _currentInput = new();

        // Updated code to fix CA1834 by replacing StringBuilder.Append(string) with StringBuilder.Append(char) where applicable.

        public void AddDigit(string digit) {
            if (_isNewOperand) {
                _currentInput.Clear();
                _hasDecimalPoint = false;
                _isNewOperand = false;
            }

            // Use Append(char) for single-character strings
            if (digit.Length == 1) {
                _currentInput.Append(digit[0]);
            } else {
                _currentInput.Append(digit);
            }

            // Update the current operand
            if (string.IsNullOrEmpty(CurrentOperation)) {
                FirstOperand = double.Parse(_currentInput.ToString(), CultureInfo.InvariantCulture);
            } else {
                SecondOperand = double.Parse(_currentInput.ToString(), CultureInfo.InvariantCulture);
            }
        }

        public void AddDecimalPoint() {
            if (_isNewOperand) {
                _currentInput.Clear();
                _currentInput.Append('0'); // Use Append(char) for single-character strings
                _isNewOperand = false;
            }

            if (!_hasDecimalPoint) {
                _currentInput.Append('.'); // Use Append(char) for single-character strings
                _hasDecimalPoint = true;
            }

            // Update the current operand
            if (string.IsNullOrEmpty(CurrentOperation)) {
                FirstOperand = double.Parse(_currentInput.ToString(), CultureInfo.InvariantCulture);
            } else {
                SecondOperand = double.Parse(_currentInput.ToString(), CultureInfo.InvariantCulture);
            }
        }

        public void SetOperation(string operation) {
            if (!string.IsNullOrEmpty(CurrentOperation) && !_isNewOperand) {
                // Chain operations by calculating the result so far
                Calculate();
            }

            CurrentOperation = operation;
            _isNewOperand = true;
        }

        public void ToggleSign() {
            if (_currentInput.Length > 0) {
                if (_currentInput[0] == '-') {
                    _currentInput.Remove(0, 1);
                } else {
                    _currentInput.Insert(0, '-'); // Use Insert(char) for single-character strings
                }

                // Update the current operand
                if (string.IsNullOrEmpty(CurrentOperation)) {
                    FirstOperand = double.Parse(_currentInput.ToString(), CultureInfo.InvariantCulture);
                } else {
                    SecondOperand = double.Parse(_currentInput.ToString(), CultureInfo.InvariantCulture);
                }
            }
        }

        public void SetPercent() {
            if (_currentInput.Length > 0) {
                double value = double.Parse(_currentInput.ToString(), CultureInfo.InvariantCulture);
                value /= 100.0;

                _currentInput.Clear();
                _currentInput.Append(value.ToString(CultureInfo.InvariantCulture));

                // Update _hasDecimalPoint
                _hasDecimalPoint = _currentInput.ToString().Contains('.');

                // Update the current operand
                if (string.IsNullOrEmpty(CurrentOperation)) {
                    FirstOperand = value;
                } else {
                    SecondOperand = value;
                }
            }
        }

        public double Calculate() {
            double result = CurrentOperation switch {
                "+" => FirstOperand + SecondOperand,
                "-" => FirstOperand - SecondOperand,
                "*" => FirstOperand * SecondOperand,
                "/" => SecondOperand != 0 ? FirstOperand / SecondOperand : throw new DivideByZeroException(),
                _ => SecondOperand != 0 ? FirstOperand : throw new InvalidOperationException()
            };

            // Store result as first operand for chaining calculations
            FirstOperand = result;

            // Reset for the next calculation
            _currentInput.Clear();
            _currentInput.Append(result.ToString(CultureInfo.InvariantCulture));
            _isNewOperand = true;
            _hasDecimalPoint = _currentInput.ToString().Contains('.');

            return result;
        }

        public void Clear() {
            FirstOperand = 0;
            SecondOperand = 0;
            CurrentOperation = string.Empty;
            _isNewOperand = true;
            _hasDecimalPoint = false;
            _currentInput.Clear();
        }
    }
}