using Microsoft.Maui.Controls.Shapes;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace CalculatorApp {
    public partial class MainPage : ContentPage {
        // Track state for the calculator
        private bool _isNewCalculation = true;
        private bool _hasDecimalPoint = false;
        private int _openParenthesesCount = 0;
        private bool _awaitingSquareRootInput = false;
        private double _storedValue = 0;
        private double _lastResult = 0;
        private Border? _optionsDropdown;
        private bool _isDropdownOpen = false;

        private const double a180 = 180.0; // Represents 180 degrees for conversion to radians
        public MainPage() {
            InitializeComponent();
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "DataTable.Compute is safe here, with required types preserved via DynamicDependency attributes")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DataTable))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DataColumn))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DataSet))]
        private void OnEqualsClicked(object sender, EventArgs e) {
            // Handle square root operations
            if (_awaitingSquareRootInput) {
                try {
                    double value = double.Parse(ResultLabel.Text);
                    if (value < 0) {
                        ResultLabel.Text = "Error";
                        _awaitingSquareRootInput = false;
                        return;
                    }

                    double sqrtResult = Math.Sqrt(value);

                    // If we have a stored value (like 9√3), multiply by sqrt result
                    if (_storedValue > 0) {
                        ExpressionLabel.Text = $"{_storedValue}√{value} =";
                        ResultLabel.Text = FormatNumberString(_storedValue * sqrtResult);
                    } else {
                        ExpressionLabel.Text = $"√{value} =";
                        ResultLabel.Text = FormatNumberString(sqrtResult);
                    }

                    _awaitingSquareRootInput = false;
                    _storedValue = 0;
                    _isNewCalculation = true;
                    return;
                } catch (Exception ex) {
                    ResultLabel.Text = $"{ex.Message}";
                    _awaitingSquareRootInput = false;
                    return;
                }
            }


            try {
                // Get the complete expression to calculate
                string fullExpression = ExpressionLabel.Text + ResultLabel.Text;

                // Close any open parentheses before calculation
                if (_openParenthesesCount > 0) {
                    // Add the current number and close any open parentheses
                    fullExpression = ExpressionLabel.Text + ResultLabel.Text + new string(')', _openParenthesesCount);
                    _openParenthesesCount = 0;
                }

                // Skip calculation if expression is empty
                if (string.IsNullOrWhiteSpace(fullExpression))
                    return;

                // Replace display symbols with calculation symbols
                string calculationExpression = fullExpression
                    .Replace("×", "*")
                    .Replace("÷", "/")
                    .Replace("π", Math.PI.ToString())
                    .Replace("^", "**");

                // Calculate the result using DataTable
                DataTable table = new();
                var res = table.Compute(calculationExpression, "");


                // Update the display
                ExpressionLabel.Text = fullExpression + " =";

                // Improved number formatting
                double resultValue = Convert.ToDouble(res);

                // Format the result based on its value
                if (double.IsInfinity(resultValue) || double.IsNaN(resultValue)) {
                    ResultLabel.Text = "Error";
                } else if (Math.Abs(resultValue) > 1e12) {
                    // Scientific notation for very large numbers
                    ResultLabel.Text = resultValue.ToString("0.###e+0");
                } else if (Math.Abs(resultValue) < 1e-10 && resultValue != 0) {
                    // Scientific notation for very small numbers
                    ResultLabel.Text = resultValue.ToString("0.###e-0");
                } else {
                    // Regular formatting with appropriate decimal places
                    // Remove trailing zeros for cleaner display
                    string formatted = resultValue.ToString("G12");
                    // If it's a whole number, display without decimal point
                    if (resultValue == Math.Floor(resultValue)) {
                        formatted = resultValue.ToString("0");
                    }
                    ResultLabel.Text = formatted;
                }

                // Reset state for next calculation
                _isNewCalculation = true;
                _hasDecimalPoint = ResultLabel.Text.Contains('.');
            } catch (DivideByZeroException) {
                // Handle division by zero
                ResultLabel.Text = "Divide by Zero";
                _isNewCalculation = true;
            } catch (OverflowException) {
                ResultLabel.Text = "Overflow Exception";
                _isNewCalculation = true;
            } catch (Exception ex) {
                // Handle calculation errors
                ResultLabel.Text = $"{ex.Message}";
                _isNewCalculation = true;
            }
        }


        private void OnClearClicked(object sender, EventArgs e) {
            // Reset the calculator state
            ExpressionLabel.Text = string.Empty;
            ResultLabel.Text = "0";
            _isNewCalculation = true;
            _hasDecimalPoint = false;
        }


        private void OnDigitClicked(object sender, EventArgs e) {
            Button button = (Button)sender;
            string digit = button.Text;

            if (_isNewCalculation) {
                ResultLabel.Text = digit;
                _isNewCalculation = false;
            } else {
                if (ResultLabel.Text == "0")
                    ResultLabel.Text = digit; // Replace leading zero
                else
                    ResultLabel.Text += digit; // Append digit
            }

            // Handle real-time calculation for square root
            if (_awaitingSquareRootInput) {
                try {
                    double value = double.Parse(ResultLabel.Text);
                    if (value >= 0) {
                        double sqrtResult = Math.Sqrt(value);

                        // If we have a stored value (like 9√), multiply by sqrt result
                        if (_storedValue > 0) {
                            _lastResult = _storedValue * sqrtResult;
                        } else {
                            _lastResult = sqrtResult;
                        }
                    }
                } catch {
                    // Ignore calculation errors while typing
                }
            }
        }


        private void OnOperationClicked(object sender, EventArgs e) {
            Button button = (Button)sender;
            string operation = button.Text;

            if (_isNewCalculation && !string.IsNullOrEmpty(ResultLabel.Text) && string.IsNullOrEmpty(ExpressionLabel.Text)) {
                ExpressionLabel.Text = ResultLabel.Text + " " + operation + " ";
            }
            // If we're in the middle of building an expression
            else if (!_isNewCalculation) {
                ExpressionLabel.Text += ResultLabel.Text + " " + operation + " ";
            }
            // If continuing after a previous operation
            else if (!string.IsNullOrEmpty(ExpressionLabel.Text)) {
                // Replace the last operation if the expression ends with an operation
                if (ExpressionLabel.Text.EndsWith("+ ") ||
                    ExpressionLabel.Text.EndsWith("- ") ||
                    ExpressionLabel.Text.EndsWith("× ") ||
                    ExpressionLabel.Text.EndsWith("÷ ") ||
                    ExpressionLabel.Text.EndsWith("% ")) {
                    ExpressionLabel.Text = ExpressionLabel.Text[..^2] + operation + " ";
                } else {
                    ExpressionLabel.Text += operation + " ";
                }
            }

            _isNewCalculation = true;
            _hasDecimalPoint = false; // Reset decimal point state after operation
        }


        private void OnDecimalPointClicked(object sender, EventArgs e) {
            // Don't add another decimal point if one already exists in the current number
            if (_hasDecimalPoint)
                return;

            if (_isNewCalculation) {
                // Start a new number with "0."
                ResultLabel.Text = "0.";
                _isNewCalculation = false;
            } else {
                // Append decimal point to current number
                ResultLabel.Text += ".";
            }

            // Mark that this number now has a decimal point
            _hasDecimalPoint = true;
        }

        private void OnBackspaceClicked(object sender, EventArgs e) {
            // Don't perform backspace on a completed calculation
            if (_isNewCalculation)
                return;

            // If there's still text to delete
            if (ResultLabel.Text.Length > 1) {
                // Check it we're removing a decimal point
                if (ResultLabel.Text[^1] == '.') {
                    _hasDecimalPoint = false;
                }

                // Remove the last character
                ResultLabel.Text = ResultLabel.Text[..^1];
            } else {
                // If only one digit left, replace with zero
                ResultLabel.Text = "0";
                _isNewCalculation = true;
            }
        }


        private void OnParenthesisClicked(object sender, EventArgs e) {
            // If we're starting a new calculation, add opening parenthesis
            if (_isNewCalculation || ResultLabel.Text == "0") {
                // Add opening parenthesis to expression and increment counter
                ExpressionLabel.Text += "(";
                _openParenthesesCount++;
                return;
            }
            // If we have a number in the display, decide whether to open or close
            if (_openParenthesesCount > 0) {
                // We have unclosed parentheses, so close one and add the current number
                ExpressionLabel.Text += ResultLabel.Text + ")";
                _openParenthesesCount--;
            } else {
                // No open parentheses, so add the current number and open a new one
                ExpressionLabel.Text += ResultLabel.Text + " × (";
                _openParenthesesCount++;
            }

            // Start a new number entry after handling parentheses
            _isNewCalculation = true;
            _hasDecimalPoint = false;
        }


        private void OnSpecialOperationClicked(object sender, EventArgs e) {
            Button button = (Button)sender;
            string operation = button.Text;

            try {
                switch (operation) {
                    case "π":
                        // Handle PI value
                        if (!_isNewCalculation && ResultLabel.Text != "0") {
                            // If we have a number in the display, interpret as multiplication (e.g., 5π)
                            double currentValue = double.Parse(ResultLabel.Text);
                            double piResult = currentValue * Math.PI;

                            // Update expression and result labels
                            ExpressionLabel.Text += ResultLabel.Text + "π";
                            ResultLabel.Text = FormatNumberString(piResult);
                        } else {
                            // Just insert PI value
                            ResultLabel.Text = Math.PI.ToString("G12");

                            if (string.IsNullOrEmpty(ExpressionLabel.Text) || ExpressionLabel.Text.EndsWith(" = "))
                                ExpressionLabel.Text = "π";
                            else if (ExpressionLabel.Text.EndsWith("+ ") || ExpressionLabel.Text.EndsWith("- ") ||
                                     ExpressionLabel.Text.EndsWith("× ") || ExpressionLabel.Text.EndsWith("÷ ") ||
                                     ExpressionLabel.Text.EndsWith("^ "))
                                ExpressionLabel.Text += "π";
                            else
                                ExpressionLabel.Text += " × π";
                        }

                        _isNewCalculation = true;
                        _hasDecimalPoint = true;
                        break;

                    case "√":
                        if (ResultLabel.Text == "0" || _isNewCalculation) {
                            // Case: User pressed √ first, preparing for input like √9
                            _awaitingSquareRootInput = true;
                            ExpressionLabel.Text = "√";
                            // Keep 0 in the result label but mark it as a new calculation
                            _isNewCalculation = true;
                        } else {
                            // Case: User entered a number then pressed √ like 9√
                            double firstNumber = double.Parse(ResultLabel.Text);

                            if (firstNumber < 0) {
                                ResultLabel.Text = "Error";
                                return;
                            }

                            // Store the first number for multiplication with the next input
                            _storedValue = firstNumber;

                            // Update expression to show we're expecting a number after √
                            ExpressionLabel.Text += ResultLabel.Text + "√";

                            // Reset for next number input
                            _isNewCalculation = true;
                            _awaitingSquareRootInput = true;
                        }
                        break;

                    case "^":
                        // Exponentiation
                        if (!_isNewCalculation || (!string.IsNullOrEmpty(ResultLabel.Text) && ResultLabel.Text != "0")) {
                            // Add current value and ^ operator to expression
                            if (string.IsNullOrEmpty(ExpressionLabel.Text) || ExpressionLabel.Text.EndsWith(" = "))
                                ExpressionLabel.Text = ResultLabel.Text + " ^ ";
                            else if (ExpressionLabel.Text.EndsWith("+ ") || ExpressionLabel.Text.EndsWith("- ") ||
                                     ExpressionLabel.Text.EndsWith("× ") || ExpressionLabel.Text.EndsWith("÷ ") ||
                                     ExpressionLabel.Text.EndsWith("^ "))
                                ExpressionLabel.Text += ResultLabel.Text + " ^ ";
                            else
                                ExpressionLabel.Text += " ^ ";

                            // Store the base in the result label with ^ symbol to indicate waiting for exponent
                            ResultLabel.Text += "^";
                            _isNewCalculation = true;
                            _hasDecimalPoint = false;
                        }
                        break;

                    case "!":
                        // Factorial calculation
                        if (!_isNewCalculation || (!string.IsNullOrEmpty(ResultLabel.Text) && ResultLabel.Text != "0")) {
                            double value = double.Parse(ResultLabel.Text);

                            // Check if value is valid for factorial
                            if (value < 0 || value != Math.Floor(value)) {
                                ResultLabel.Text = "Error";
                                return;
                            }

                            // Check if value is too large
                            if (value > 170) {
                                ResultLabel.Text = "Overflow";
                                return;
                            }

                            // Calculate factorial
                            double result = 1;
                            for (int i = 2; i <= value; i++) {
                                result *= i;
                            }

                            // Update expression
                            if (string.IsNullOrEmpty(ExpressionLabel.Text) || ExpressionLabel.Text.EndsWith(" = "))
                                ExpressionLabel.Text = $"{value}!";
                            else if (ExpressionLabel.Text.EndsWith("+ ") || ExpressionLabel.Text.EndsWith("- ") ||
                                     ExpressionLabel.Text.EndsWith("× ") || ExpressionLabel.Text.EndsWith("÷ ") ||
                                     ExpressionLabel.Text.EndsWith("^ "))
                                ExpressionLabel.Text += $"{value}!";
                            else
                                ExpressionLabel.Text += $" × {value}!";

                            // Show the result
                            ResultLabel.Text = FormatNumberString(result);
                            _isNewCalculation = true;
                        }
                        break;
                }
            } catch (Exception ex) {
                ResultLabel.Text = $"{ex.Message}";
                _isNewCalculation = true;
                _awaitingSquareRootInput = false;
            }
        }



        // Helper method to format a number as a string
        private static string FormatNumberString(double value) {
            if (double.IsInfinity(value) || double.IsNaN(value)) {
                return "Error";
            } else if (Math.Abs(value) > 1e12) {
                // Scientific notation for very large numbers
                return value.ToString("0.###e+0");
            } else if (Math.Abs(value) < 1e-10 && value != 0) {
                // Scientific notation for very small numbers
                return value.ToString("0.###e-0");
            } else {
                // Regular formatting with appropriate decimal places
                string formatted = value.ToString("G12");
                // If it's a whole number, display without decimal point
                if (value == Math.Floor(value)) {
                    formatted = value.ToString("0");
                }
                return formatted;
            }
        }

        // Modified FormatResult to use the helper method
        private void FormatResult(double value) {
            ResultLabel.Text = FormatNumberString(value);
            _hasDecimalPoint = ResultLabel.Text.Contains('.');
        }



        private void OnMoreOptionsClicked(object sender, EventArgs e) {
            if (_isDropdownOpen) {
                // If dropdown is open, close it
                CloseDropdown();
                return;
            }

            // Create grid for dropdown options - 2 rows, 4 columns
            Grid optionsGrid = new() {
                RowDefinitions =
                [
                    new RowDefinition { Height = GridLength.Auto },
            new RowDefinition { Height = GridLength.Auto }
                ],
                ColumnDefinitions =
                [
                    new ColumnDefinition { Width = GridLength.Star },
            new ColumnDefinition { Width = GridLength.Star },
            new ColumnDefinition { Width = GridLength.Star },
            new ColumnDefinition { Width = GridLength.Star }
                ],
                RowSpacing = 4,
                ColumnSpacing = 4,
                Padding = new Thickness(5)
            };

            // Create the buttons
            // First row: x², sin, cos, tan
            var squaredButton = CreateButton("x²", "#e0e0e0");
            var sinButton = CreateButton("sin", "#e0e0e0");
            var cosButton = CreateButton("cos", "#e0e0e0");
            var tanButton = CreateButton("tan", "#e0e0e0");

            // Second row: x³, ln, log, e
            var cubedButton = CreateButton("x³", "#e0e0e0");
            var lnButton = CreateButton("ln", "#e0e0e0");
            var logButton = CreateButton("log", "#e0e0e0");
            var eButton = CreateButton("e", "#e0e0e0");

            // Add buttons to grid
            optionsGrid.Add(squaredButton, 0, 0);
            optionsGrid.Add(sinButton, 1, 0);
            optionsGrid.Add(cosButton, 2, 0);
            optionsGrid.Add(tanButton, 3, 0);

            optionsGrid.Add(cubedButton, 0, 1);
            optionsGrid.Add(lnButton, 1, 1);
            optionsGrid.Add(logButton, 2, 1);
            optionsGrid.Add(eButton, 3, 1);

            // Create dropdown container
            Border dropdownContainer = new() {
                Content = optionsGrid,
                Stroke = Color.FromArgb("#cccccc"),
                BackgroundColor = Colors.White,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle {
                    CornerRadius = new CornerRadius(10)
                },
                Shadow = new Shadow {
                    Opacity = 0.3f,
                    Offset = new Point(0, 3),
                    Radius = 5
                },
                WidthRequest = 240, // Fixed width instead of MaximumWidthRequest
                HorizontalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 5, 10, 0)
            };

            // Store the dropdown reference
            _optionsDropdown = dropdownContainer;
            _isDropdownOpen = true;

            // Wire up button click events - moved inside a local function for clarity
            void HandleButtonClick(Action calculation) {
                calculation();
                CloseDropdown();
            }

            // Using the local function to handle all button clicks
            squaredButton.Clicked += (s, args) => HandleButtonClick(CalculateSquared);
            sinButton.Clicked += (s, args) => HandleButtonClick(() => CalculateTrig("sin"));
            cosButton.Clicked += (s, args) => HandleButtonClick(() => CalculateTrig("cos"));
            tanButton.Clicked += (s, args) => HandleButtonClick(() => CalculateTrig("tan"));
            cubedButton.Clicked += (s, args) => HandleButtonClick(CalculateCubed);
            lnButton.Clicked += (s, args) => HandleButtonClick(() => CalculateLog("ln"));
            logButton.Clicked += (s, args) => HandleButtonClick(() => CalculateLog("log"));
            eButton.Clicked += (s, args) => HandleButtonClick(CalculateEuler);

            if (Content is Grid mainGrid) {
                // The dropdown and overlay need to be added to the main grid properly
                // First, create the overlay that covers the entire grid
                var overlay = new Grid {
                    BackgroundColor = Colors.Transparent
                };

                // Set the overlay to span the entire grid
                Grid.SetRowSpan(overlay, mainGrid.RowDefinitions.Count);
                Grid.SetColumnSpan(overlay, mainGrid.ColumnDefinitions.Count);

                // Add tap gesture to close when clicking outside
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, args) => CloseDropdown();
                overlay.GestureRecognizers.Add(tapGesture);

                // Position the dropdown - IMPORTANT: this is where it was failing
                // We need to ensure it's positioned correctly in the grid
                Grid.SetRow(dropdownContainer, 1); // Position at row 1 (below the display)
                Grid.SetColumnSpan(dropdownContainer, 4); // Span 4 columns

                // Set the ZIndex to ensure it appears above other content
                overlay.ZIndex = 1;
                dropdownContainer.ZIndex = 2;


                // Add to the main grid
                mainGrid.Children.Add(overlay);
                mainGrid.Children.Add(dropdownContainer);
            }
        }

        // Updated CloseDropdown method to properly clean up
        private void CloseDropdown() {
            if (!_isDropdownOpen || _optionsDropdown == null)
                return;

            if (Content is Grid mainGrid) {
                // Find and remove the dropdown and overlay
                var itemsToRemove = new List<IView>();

                foreach (var child in mainGrid.Children) {
                    // Remove the dropdown
                    if (child == _optionsDropdown) {
                        itemsToRemove.Add(child);
                    }

                    // Remove any overlay (transparent grid that spans the entire main grid)
                    if (child is Grid grid &&
                        grid.BackgroundColor == Colors.Transparent &&
                        Grid.GetRowSpan(grid) == mainGrid.RowDefinitions.Count) {
                        itemsToRemove.Add(grid);
                    }
                }

                // Remove the items
                foreach (var item in itemsToRemove) {
                    mainGrid.Children.Remove(item);
                }
            }

            _optionsDropdown = null;
            _isDropdownOpen = false;
        }










        // Helper method to create buttons with consistent styling
        private static Button CreateButton(string text, string backgroundColor) {
            return new Button {
                Text = text,
                FontSize = 16,
                BackgroundColor = Color.FromArgb(backgroundColor),
                TextColor = Colors.Black,
                Margin = new Thickness(0.2)
            };
        }

        // Helper methods for calculations - Fixed to properly use and display current values
        private void CalculateSquared() {
            if (double.TryParse(ResultLabel.Text, out double value)) {
                // Calculate square
                double result = value * value;

                // Update expression to show what operation was performed
                ExpressionLabel.Text = $"{value}² =";

                // Format and display the result
                ResultLabel.Text = FormatNumberString(result);

                // Set state for next calculation
                _isNewCalculation = true;
                _hasDecimalPoint = ResultLabel.Text.Contains('.');
            }
        }

        private void CalculateCubed() {
            if (double.TryParse(ResultLabel.Text, out double value)) {
                // Calculate cube
                double result = value * value * value;

                // Update expression to show what operation was performed
                ExpressionLabel.Text = $"{value}³ =";

                // Format and display the result
                ResultLabel.Text = FormatNumberString(result);

                // Set state for next calculation
                _isNewCalculation = true;
                _hasDecimalPoint = ResultLabel.Text.Contains('.');
            }
        }


        private void CalculateTrig(string function) {
            // First, make sure we can parse the current value
            if (!double.TryParse(ResultLabel.Text, out double value)) {
                ResultLabel.Text = "Error";
                ExpressionLabel.Text = $"{function}(?) =";
                _isNewCalculation = true;
                return;
            }

            try {
                // Convert degrees to radians for trigonometric calculations
                double radians = value * Math.PI / 180.0;
                double result;

                // Calculate the appropriate trigonometric function
                switch (function) {
                    case "sin":
                        // Handle special cases for exact values
                        if (Math.Abs(value % 180) == 0)
                            result = 0; // sin of 0, 180, 360, etc. is exactly 0
                        else if (Math.Abs(value % 360) == 90)
                            result = 1; // sin of 90, 450, etc. is exactly 1
                        else if (Math.Abs(value % 360) == 270)
                            result = -1; // sin of 270, 630, etc. is exactly -1
                        else
                            result = Math.Sin(radians);
                        break;

                    case "cos":
                        // Handle special cases for exact values
                        if (Math.Abs(value % 360) == 90 || Math.Abs(value % 360) == 270)
                            result = 0; // cos of 90, 270, etc. is exactly 0
                        else if (Math.Abs(value % 360) == 0)
                            result = 1; // cos of 0, 360, etc. is exactly 1
                        else if (Math.Abs(value % 360) == 180)
                            result = -1; // cos of 180, 540, etc. is exactly -1
                        else
                            result = Math.Cos(radians);
                        break;

                    case "tan":
                        // Handle special cases for tan (undefined at 90, 270, etc.)
                        if (Math.Abs(value % 180) == 90) {
                            ExpressionLabel.Text = $"{function}({value}) =";
                            ResultLabel.Text = "Undefined";
                            _isNewCalculation = true;
                            return;
                        }
                        result = Math.Tan(radians);
                        break;

                    default:
                        ResultLabel.Text = "Error";
                        _isNewCalculation = true;
                        return;
                }

                // Clean up result - avoid floating point precision issues 
                // (e.g., sin(30) should be 0.5 exactly, not 0.49999999999999994)
                if (Math.Abs(result) < 1e-14)
                    result = 0;

                // Update expression to show what operation was performed
                ExpressionLabel.Text = $"{function}({value}) =";

                // Format and display the result
                ResultLabel.Text = FormatNumberString(result);

                // Set state for next calculation
                _isNewCalculation = true;
                _hasDecimalPoint = ResultLabel.Text.Contains('.');
            } catch (Exception) {
                ResultLabel.Text = "Error";
                ExpressionLabel.Text = $"{function}({value}) =";
                _isNewCalculation = true;
            }
        }

        private void CalculateLog(string function) {
            if (double.TryParse(ResultLabel.Text, out double value)) {
                // Check for valid input (logarithm domain)
                if (value <= 0) {
                    ExpressionLabel.Text = $"{function}({value}) =";
                    ResultLabel.Text = "Error";
                    _isNewCalculation = true;
                    return;
                }

                // Calculate natural log or base-10 log based on function name
                double result = function == "ln" ? Math.Log(value) : Math.Log10(value);

                // Update expression to show what operation was performed
                ExpressionLabel.Text = $"{function}({value}) =";

                // Format and display the result
                ResultLabel.Text = FormatNumberString(result);

                // Set state for next calculation
                _isNewCalculation = true;
                _hasDecimalPoint = ResultLabel.Text.Contains('.');
            }
        }

        private void CalculateEuler() {
            // For e constant, we just update the display value
            ResultLabel.Text = FormatNumberString(Math.E);
            ExpressionLabel.Text = "e =";
            _isNewCalculation = true;
            _hasDecimalPoint = true;
        }

        private bool IsPointInsideDropdown(Point point) {
            // Check if the point is inside the dropdown area
            if (_optionsDropdown != null) {
                var bounds = _optionsDropdown.Bounds;
                return bounds.Contains(point);
            }
            return false;
        }
    }
}