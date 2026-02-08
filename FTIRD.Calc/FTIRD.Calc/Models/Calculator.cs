
namespace FTIRD.Calc.Models;

public static class Calculator 
{
    public static double Calculate(double value1, double value2, string mathOperator) 
    {
        double result = 0; 

        switch (mathOperator) 
        {
            case "/":
                result = value1 / value2;
                break;
            case "*":
                result = value1 * value2;
                break;
            case "+": 
                result = value1 + value2;
                break;
            case "-": 
                result = value1 - value2;
                break;
        }

        return result;
    }
}


public static class DoubleExtensions 
{
    public static string ToTrimmedString(this double target, string decimalFormat) 
    {
        string strValue = target.ToString(decimalFormat, System.Globalization.CultureInfo.InvariantCulture);

        if (strValue.Contains(".")) 
        {
            strValue = strValue.TrimEnd('0');

            if (strValue.EndsWith("."))
                strValue = strValue.TrimEnd('.');
        }

        return strValue;
    }
}