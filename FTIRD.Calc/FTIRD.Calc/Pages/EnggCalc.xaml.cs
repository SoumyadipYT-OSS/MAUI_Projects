using System.Globalization;
using System.Text;

namespace FTIRD.Calc.Pages;

public partial class EnggCalc : ContentPage
{
	readonly StringBuilder expression = new();
	bool clearEntryOnNextDigit;
	bool isDegrees = true;

	const string DecimalFormat = "G12";

	public EnggCalc()
	{
		InitializeComponent();
		OnClear(this, null);
		UpdateAngleModeUI(); // Set initial DEG/RAD visual state
	}

	string CurrentExpressionText => expression.Length == 0 ? "0" : expression.ToString();

	string FormatNumber(double value)
	{
		if (double.IsNaN(value))
			return "Error";
		if (double.IsInfinity(value))
			return value > 0 ? "?" : "-?";

		var text = value.ToString(DecimalFormat, CultureInfo.InvariantCulture);
		if (text.Contains('E') || text.Contains('e'))
			return text;

		return text.TrimEnd('0').TrimEnd('.');
	}

	void UpdateResultDisplay()
	{
		resultText.Text = CurrentExpressionText;
	}

	// ============ INPUT HANDLERS ============

	void OnSelectNumber(object sender, EventArgs e)
	{
		var button = (Button)sender;
		var pressed = button.Text;

		if (clearEntryOnNextDigit)
		{
			expression.Clear();
			clearEntryOnNextDigit = false;
		}

		if (pressed == ".")
		{
			// Check if current number already has a decimal
			if (CurrentNumberHasDecimal())
				return;

			// Auto-prepend 0 if starting fresh or after operator/paren
			if (expression.Length == 0 || !char.IsDigit(expression[^1]))
				expression.Append('0');

			expression.Append('.');
			UpdateResultDisplay();
			return;
		}

		// Handle digits
		if (pressed == "00")
			expression.Append("00");
		else
			expression.Append(pressed);

		UpdateResultDisplay();
	}

	void OnSelectOperator(object sender, EventArgs e)
	{
		var button = (Button)sender;
		var pressed = button.Text;

		if (clearEntryOnNextDigit)
			clearEntryOnNextDigit = false;

		if (expression.Length == 0)
		{
			// Allow starting with minus for negative numbers
			if (pressed == "-")
			{
				expression.Append('-');
				UpdateResultDisplay();
			}
			return;
		}

		var last = expression[^1];

		// If last is an operator, replace it (except for unary minus handling)
		if (IsBinaryOperator(last))
		{
			// Allow e.g., "5*-3" (implicit unary minus after operator)
			if (pressed == "-" && last != '-')
			{
				expression.Append('-');
			}
			else
			{
				expression[^1] = pressed[0];
			}
		}
		else if (last == '(')
		{
			// Allow unary minus after open paren
			if (pressed == "-")
				expression.Append('-');
			// Don't allow other operators right after (
			return;
		}
		else
		{
			expression.Append(pressed);
		}

		UpdateResultDisplay();
	}

	void OnParenthesis(object sender, EventArgs e)
	{
		var button = (Button)sender;
		var pressed = button.Text;

		if (clearEntryOnNextDigit)
			clearEntryOnNextDigit = false;

		if (pressed == "(")
		{
			// Implicit multiplication: "2(" -> "2*("
			if (expression.Length > 0)
			{
				var last = expression[^1];
				if (char.IsDigit(last) || last == ')' || last == '!' || last == '²' || last == '³')
					expression.Append('*');
			}
			expression.Append('(');
		}
		else if (pressed == ")")
		{
			// Only allow closing if we have matching open parens
			if (CountOpenParens() > 0)
				expression.Append(')');
			else
				return;
		}

		UpdateResultDisplay();
	}

	void OnToggleSign(object sender, EventArgs e)
	{
		// Toggle sign of the last number in the expression
		if (expression.Length == 0)
			return;

		// Find the start of the last number
		int i = expression.Length - 1;

		// Skip trailing postfix ops
		while (i >= 0 && (expression[i] == '!' || expression[i] == '²' || expression[i] == '³'))
			i--;

		if (i < 0)
			return;

		// Now find the start of the number
		int numEnd = i;
		while (i >= 0 && (char.IsDigit(expression[i]) || expression[i] == '.'))
			i--;

		// i is now at the char before the number (or -1)
		int numStart = i + 1;

		// Check if there's a minus sign before
		if (i >= 0 && expression[i] == '-')
		{
			// Check if this minus is unary (at start, after operator, or after '(')
			if (i == 0 || IsBinaryOperator(expression[i - 1]) || expression[i - 1] == '(')
			{
				// Remove the minus
				expression.Remove(i, 1);
				UpdateResultDisplay();
				return;
			}
		}

		// Add a minus (with implicit multiplication if needed)
		if (numStart > 0)
		{
			var before = expression[numStart - 1];
			if (char.IsDigit(before) || before == ')' || before == '!' || before == '²' || before == '³')
			{
				// Can't negate here without breaking expression
				return;
			}
		}

		expression.Insert(numStart, '-');
		UpdateResultDisplay();
	}

	void OnUnary(object sender, EventArgs e)
	{
		var button = (Button)sender;
		var key = button.Text;

		if (clearEntryOnNextDigit)
			clearEntryOnNextDigit = false;

		if (expression.Length == 0)
			return;

		// Append postfix operator
		switch (key)
		{
			case "x²":
				expression.Append('²');
				break;
			case "x³":
				expression.Append('³');
				break;
			case "x!":
				expression.Append('!');
				break;
		}

		UpdateResultDisplay();
	}

	void OnFunction(object sender, EventArgs e)
	{
		var button = (Button)sender;
		var key = button.Text;

		if (clearEntryOnNextDigit)
		{
			expression.Clear();
			clearEntryOnNextDigit = false;
		}

		// Constants
		if (key == "?")
		{
			InsertWithImplicitMult("?");
			UpdateResultDisplay();
			return;
		}
		if (key == "e")
		{
			InsertWithImplicitMult("e");
			UpdateResultDisplay();
			return;
		}

		// Functions that take arguments
		switch (key)
		{
			case "Sin":
			case "Cos":
			case "Tan":
			case "Asin":
			case "Acos":
			case "Atan":
			case "Log":
			case "Ln":
			case "?":
			case "10?":
			case "e?":
			case "|x|":
			case "1/x":
			case "%":
				InsertFunction(key);
				break;
			case "x?":
			case "Mod":
				// Binary operators
				InsertBinaryFunction(key);
				break;
			case "x²":
			case "x³":
				expression.Append(key == "x²" ? '²' : '³');
				UpdateResultDisplay();
				break;
		}
	}

	void InsertWithImplicitMult(string token)
	{
		if (expression.Length > 0)
		{
			var last = expression[^1];
			if (char.IsDigit(last) || last == ')' || last == '!' || last == '²' || last == '³' || last == '?' || last == 'e')
				expression.Append('*');
		}
		expression.Append(token);
	}

	void InsertFunction(string func)
	{
		// Implicit multiplication before function call
		if (expression.Length > 0)
		{
			var last = expression[^1];
			if (char.IsDigit(last) || last == ')' || last == '!' || last == '²' || last == '³' || last == '?' || last == 'e')
				expression.Append('*');
		}

		expression.Append($"{func}(");
		UpdateResultDisplay();
	}

	void InsertBinaryFunction(string func)
	{
		if (expression.Length == 0)
			return;

		// Add operator representation
		if (func == "x?")
			expression.Append('^');
		else if (func == "Mod")
			expression.Append('%');

		UpdateResultDisplay();
	}

	void OnAngleMode(object sender, EventArgs e)
	{
		var button = (Button)sender;
		isDegrees = button.Text.Equals("Deg", StringComparison.OrdinalIgnoreCase);
		UpdateAngleModeUI();
	}

	void UpdateAngleModeUI()
	{
		// Find the Deg and Rad buttons in the page
		var degButton = this.FindByName<Button>("DegButton");
		var radButton = this.FindByName<Button>("RadButton");

		if (degButton != null && radButton != null)
		{
			if (isDegrees)
			{
				degButton.BackgroundColor = Color.FromArgb("#D2691E"); // Chocolate (active)
				degButton.TextColor = Colors.White;
				radButton.BackgroundColor = Color.FromArgb("#DEB887"); // BurlyWood (inactive)
				radButton.TextColor = Color.FromArgb("#555555");
			}
			else
			{
				radButton.BackgroundColor = Color.FromArgb("#D2691E"); // Chocolate (active)
				radButton.TextColor = Colors.White;
				degButton.BackgroundColor = Color.FromArgb("#DEB887"); // BurlyWood (inactive)
				degButton.TextColor = Color.FromArgb("#555555");
			}
		}
	}

	void OnBackspace(object sender, EventArgs e)
	{
		if (clearEntryOnNextDigit)
		{
			clearEntryOnNextDigit = false;
			return;
		}

		if (expression.Length == 0)
			return;

		// Token-based backspace: remove last token
		RemoveLastToken();
		UpdateResultDisplay();
	}

	void RemoveLastToken()
	{
		if (expression.Length == 0)
			return;

		var last = expression[^1];

		// Single-char tokens
		if (IsBinaryOperator(last) || last == '(' || last == ')' || last == '!' || last == '²' || last == '³' || last == '^' || last == '%')
		{
			expression.Remove(expression.Length - 1, 1);
			return;
		}

		// Constants
		if (last == '?' || last == 'e')
		{
			expression.Remove(expression.Length - 1, 1);
			return;
		}

		// Numbers (digit or decimal)
		if (char.IsDigit(last) || last == '.')
		{
			expression.Remove(expression.Length - 1, 1);
			return;
		}

		// Function calls like "Sin(" - remove entire token
		if (last == '(')
		{
			int i = expression.Length - 2;
			while (i >= 0 && char.IsLetter(expression[i]))
				i--;

			expression.Remove(i + 1, expression.Length - i - 1);
			return;
		}

		// Fallback: remove one char
		expression.Remove(expression.Length - 1, 1);
	}

	void OnClear(object sender, EventArgs? e)
	{
		expression.Clear();
		clearEntryOnNextDigit = false;
		resultText.Text = "0";
		CurrentCalculation.Text = string.Empty;
	}

	void OnCalculate(object sender, EventArgs? e)
	{
		if (expression.Length == 0)
			return;

		// Check balanced parentheses
		if (CountOpenParens() != 0)
		{
			resultText.Text = "Complete brackets, please";
			return;
		}

		var expr = expression.ToString();

		// Don't allow trailing operators
		if (expr.Length > 0 && IsBinaryOperator(expr[^1]))
			return;

		try
		{
			var value = Evaluate(expr);
			CurrentCalculation.Text = expr + "=";
			expression.Clear();
			expression.Append(FormatNumber(value));
			resultText.Text = FormatNumber(value);
			clearEntryOnNextDigit = true;
		}
		catch
		{
			resultText.Text = "Error";
			clearEntryOnNextDigit = true;
		}
	}

	// ============ UTILITY ============

	static bool IsBinaryOperator(char c) => c is '+' or '-' or '*' or '/' or '^' or '%';

	bool CurrentNumberHasDecimal()
	{
		for (int i = expression.Length - 1; i >= 0; i--)
		{
			var c = expression[i];
			if (c == '.')
				return true;
			if (!char.IsDigit(c))
				break;
		}
		return false;
	}

	int CountOpenParens()
	{
		int count = 0;
		foreach (var c in expression.ToString())
		{
			if (c == '(') count++;
			if (c == ')') count--;
		}
		return count;
	}

	// ============ EVALUATION ENGINE ============

	readonly record struct Tok(string Kind, string? Text = null, double Value = 0);

	static bool IsDigitOrDot(char c) => char.IsDigit(c) || c == '.';

	double Evaluate(string expr)
	{
		var tokens = Tokenize(expr);
		var rpn = ToRpn(tokens);
		return EvalRpn(rpn);
	}

	List<Tok> Tokenize(string expr)
	{
		var tokens = new List<Tok>();
		for (int i = 0; i < expr.Length; i++)
		{
			var c = expr[i];
			if (char.IsWhiteSpace(c))
				continue;

			// Numbers
			if (IsDigitOrDot(c))
			{
				int start = i;
				while (i < expr.Length && IsDigitOrDot(expr[i]))
					i++;
				var slice = expr[start..i];
				i--;
				var val = double.Parse(slice, CultureInfo.InvariantCulture);
				tokens.Add(new Tok("num", Value: val));
				continue;
			}

			// Functions
			if (char.IsLetter(c))
			{
				int start = i;
				while (i < expr.Length && char.IsLetter(expr[i]))
					i++;
				var name = expr[start..i];
				i--;
				tokens.Add(new Tok("func", name));
				continue;
			}

			// Operators & symbols
			switch (c)
			{
				case '(':
					tokens.Add(new Tok("("));
					break;
				case ')':
					tokens.Add(new Tok(")"));
					break;
				case '+':
				case '-':
				case '*':
				case '/':
					tokens.Add(new Tok("op", c.ToString()));
					break;
				case '^':
					tokens.Add(new Tok("op", "^"));
					break;
				case '%':
					tokens.Add(new Tok("op", "%"));
					break;
				case '!':
					tokens.Add(new Tok("post", "!"));
					break;
				case '²':
					tokens.Add(new Tok("post", "²"));
					break;
				case '³':
					tokens.Add(new Tok("post", "³"));
					break;
				case '?':
					tokens.Add(new Tok("num", Value: Math.PI));
					break;
				case 'e':
					tokens.Add(new Tok("num", Value: Math.E));
					break;
				default:
					throw new InvalidOperationException($"Unknown token: {c}");
			}
		}

		// Handle unary minus
		return NormalizeUnaryMinus(tokens);
	}

	List<Tok> NormalizeUnaryMinus(List<Tok> tokens)
	{
		var result = new List<Tok>();
		for (int i = 0; i < tokens.Count; i++)
		{
			var t = tokens[i];

			// Detect unary minus: at start, or after ( or operator
			if (t.Kind == "op" && t.Text == "-")
			{
				bool isUnary = i == 0 ||
				               result[^1].Kind == "(" ||
				               result[^1].Kind == "op";

				if (isUnary)
				{
					// Convert to unary function
					result.Add(new Tok("func", "neg"));
					continue;
				}
			}

			result.Add(t);
		}
		return result;
	}

	static int Prec(string op) => op switch
	{
		"+" or "-" => 1,
		"*" or "/" or "%" => 2,
		"^" => 3,
		_ => 0
	};

	static bool IsRightAssoc(string op) => op == "^";

	List<Tok> ToRpn(List<Tok> tokens)
	{
		var output = new List<Tok>();
		var ops = new Stack<Tok>();

		for (int i = 0; i < tokens.Count; i++)
		{
			var t = tokens[i];

			if (t.Kind == "num")
			{
				output.Add(t);
				continue;
			}

			if (t.Kind == "post")
			{
				output.Add(t);
				continue;
			}

			if (t.Kind == "func")
			{
				ops.Push(t);
				continue;
			}

			if (t.Kind == "op")
			{
				while (ops.Count > 0 && ops.Peek().Kind == "op")
				{
					var top = ops.Peek();
					if ((IsRightAssoc(t.Text!) && Prec(t.Text!) < Prec(top.Text!)) ||
					    (!IsRightAssoc(t.Text!) && Prec(t.Text!) <= Prec(top.Text!)))
					{
						output.Add(ops.Pop());
					}
					else
						break;
				}
				ops.Push(t);
				continue;
			}

			if (t.Kind == "(")
			{
				ops.Push(t);
				continue;
			}

			if (t.Kind == ")")
			{
				while (ops.Count > 0 && ops.Peek().Kind != "(")
					output.Add(ops.Pop());

				if (ops.Count == 0)
					throw new InvalidOperationException("Mismatched parentheses");

				ops.Pop(); // Remove '('

				// If there's a function on top, pop it
				if (ops.Count > 0 && ops.Peek().Kind == "func")
					output.Add(ops.Pop());
			}
		}

		while (ops.Count > 0)
		{
			var op = ops.Pop();
			if (op.Kind is "(" or ")")
				throw new InvalidOperationException("Mismatched parentheses");
			output.Add(op);
		}

		return output;
	}

	double EvalRpn(List<Tok> rpn)
	{
		var stack = new Stack<double>();

		foreach (var t in rpn)
		{
			if (t.Kind == "num")
			{
				stack.Push(t.Value);
				continue;
			}

			if (t.Kind == "op")
			{
				var b = stack.Pop();
				var a = stack.Pop();
				stack.Push(t.Text switch
				{
					"+" => a + b,
					"-" => a - b,
					"*" => a * b,
					"/" => b == 0 ? double.NaN : a / b,
					"^" => Math.Pow(a, b),
					"%" => b == 0 ? double.NaN : a % b,
					_ => throw new InvalidOperationException()
				});
				continue;
			}

			if (t.Kind == "post")
			{
				var a = stack.Pop();
				stack.Push(t.Text switch
				{
					"!" => Factorial(a),
					"²" => a * a,
					"³" => a * a * a,
					_ => throw new InvalidOperationException()
				});
				continue;
			}

			if (t.Kind == "func")
			{
				var a = stack.Pop();
				stack.Push(ApplyFunction(t.Text!, a));
			}
		}

		return stack.Pop();
	}

	double ApplyFunction(string func, double x)
	{
		return func switch
		{
			"neg" => -x,
			"Sin" => Math.Sin(ToRadians(x)),
			"Cos" => Math.Cos(ToRadians(x)),
			"Tan" => Math.Tan(ToRadians(x)),
			"Asin" => FromRadians(Math.Asin(x)),
			"Acos" => FromRadians(Math.Acos(x)),
			"Atan" => FromRadians(Math.Atan(x)),
			"Log" => x <= 0 ? double.NaN : Math.Log10(x),
			"Ln" => x <= 0 ? double.NaN : Math.Log(x),
			"?" => x < 0 ? double.NaN : Math.Sqrt(x),
			"|x|" => Math.Abs(x),
			"1/x" => x == 0 ? double.NaN : 1.0 / x,
			"%" => x * 0.01,
			"10?" => Math.Pow(10, x),
			"e?" => Math.Exp(x),
			_ => throw new InvalidOperationException($"Unknown function: {func}")
		};
	}

	double ToRadians(double angle) => isDegrees ? angle * (Math.PI / 180.0) : angle;
	double FromRadians(double angle) => isDegrees ? angle * (180.0 / Math.PI) : angle;

	static double Factorial(double n)
	{
		if (n < 0)
			return double.NaN;
		if (Math.Abs(n % 1) > double.Epsilon)
			return double.NaN;
		if (n > 170)
			return double.PositiveInfinity;

		double result = 1;
		for (int i = 2; i <= (int)n; i++)
			result *= i;
		return result;
	}

	private async void OnToggleSlideClicked(object sender, EventArgs e)
	{
		if (FunctionPanel.IsVisible)
		{
			await FunctionPanel.FadeToAsync(0, 250);
			FunctionPanel.IsVisible = false;
			ToggleSlideButton.Text = "f(x)";
		}
		else
		{
			FunctionPanel.IsVisible = true;
			await FunctionPanel.FadeToAsync(1, 250);
			ToggleSlideButton.Text = "?";
		}
	}
}
