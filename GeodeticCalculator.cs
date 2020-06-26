// Transformation between two example Polish geodesic coordinates systems.
using System;
using static System.Math;

public class Vector2D 
{
	public double x;
	public double y;

	public Vector2D(double x, double y)
	{
		this.x = x;
		this.y = y;
	}
}

class Program
{
	static Vector2D From1992To2000 (Vector2D p)
	{
		double pi = 3.141592653589793238462643383279502884197169;
		double d01 = 6367449.14577;
		double d02 = 0.0008377318247344;
		double d03 = 0.0000007608527788826;
		double d04 = 0.000000001197638019173;
		double d05 = 0.0000000000024433762425;
		double d06 = 0.0818191910428;
		double d07 = 0.003356551485597;
		double d08 = 0.000006571873148459;
		double d09 = 0.00000001764656426454;
		double d10 = 0.0000000000540048218776;
		double d11 = -0.0008377321681641;
		double d12 = -0.00000005905869626083;
		double d13 = -0.0000000001673488904988;
		double d14 = -0.0000000000002167737805597;
		double d15 = ((p.x + 5300000) / 0.9993) / 6367449.14577;
		double d16 = ((p.y - 500000) / 0.9993) / 6367449.14577;
		double d17 = d16 + (d11 * Cos(2 * d15) * Sinh(2 * d16) + d12 * Cos(4 * d15) * Sinh(4 * d16) + d13 * Cos(6 * d15) * Sinh(6 * d16) + (d14 * Cos(8 * d15) * Sinh(8 * d16)));
		double d18 = 2 * Atan(Exp(d17)) - pi / 2;
		double d19 = d15 + (d11 * Sin(2 * d15) * Cosh(2 * d16) + d12 * Sin(4 * d15) * Cosh(4 * d16) + d13 * Sin(6 * d15) * Cosh(6 * d16) + d14 * Sin(8 * d15) * Cosh(8 * d16));
		double d20 = Asin(Cos(d18) * Sin(d19));
		double d21 = 180 * (d20 + d07 * Sin(2 * d20) + d08 * Sin(4 * d20) + d09 * Sin(6 * d20) + d10 * Sin(8 * d20))/pi;
		double d22 = Atan(Tan(d18) / Cos(d19));
		double d23 = 19 + (180 * d22 / pi);
		double d24 = pi * d21 / 180;
		double d25 = pi * 18 / 180;
		double d26 = pi * d23 / 180;
		double d27 = 2 * (Atan(Pow((1 - (d06 * Sin(d24))) / (1 + (d06 * Sin(d24))), (d06 / 2)) * Tan(d24 / 2 + pi / 4)) - pi / 4);
		double d28 = 0.5 * Log((1 + Cos(d27) * Sin(d26-d25)) / (1 - Cos(d27) * Sin(d26 - d25)));
		double d29 = Atan(Sin(d27) / (Cos(d27) * Cos(d26 - d25)));
		double d30 = (d02 * Sin(2 * d29) * Cosh(2 * d28)) + (d03 * Sin(4 * d29) * Cosh(4 * d28)) + (d04 * Sin(6 * d29) * Cosh(6 * d28)) + (d05 * Sin(8 * d29) * Cosh(8 * d28));
		double d31 = (d02 * Cos(2 * d29) * Sinh(2 * d28)) + (d03 * Cos(4 * d29) * Sinh(4 * d28)) + (d04 * Cos(6 * d29) * Sinh(6 * d28)) + (d05 * Cos(8 * d29) * Sinh(8 * d28));
		return new Vector2D((0.999923 * d01 * (d29 + d30)), (0.999923 * d01 * (d28 + d31) + 6500000));
	}

	static void Main(string[] args)
	{
		Vector2D point = new Vector2D(434590.5711, 531360.5312);
		Console.WriteLine("Poland CS92 coordinates: ");
		Console.WriteLine(String.Format("{0:N3}", point.x) + "   " + String.Format("{0:N3}", point.y));
		Console.WriteLine("Poland CS2000 zone 6 (18th meridian east) coordinates: ");
		Vector2D result = From1992To2000(point);
		Console.WriteLine(String.Format("{0:N3}", result.x) + "   " + String.Format("{0:N3}", result.y));
	}
}