namespace SargeTest
{
    using SargeSharp;

    internal class Program
    {
        static void Main(string[] args)
        {
            Sarge sarge = new Sarge();

            sarge.SetArgument("h", "help", "Get help.", false);
            sarge.SetArgument("k", "kittens", "K is for kittens. Everyone needs kittens in their life.", true);
            sarge.SetArgument("n", "number", "Gimme a number. Any number.", true);
            sarge.SetArgument("a", "apple", "Just an apple.", false);
            sarge.SetArgument("b", "bear", "Look, it's a bear.", false);
            sarge.SetArgument("", "snake", "Snakes only come in long form, there are no short snakes.", false);
            sarge.Description = "Sarge command line argument parsing testing app. For demonstration purposes and testing.";
            sarge.Usage = "sarge_test <options>";

            if (!sarge.ParseArguments(args))
            {
                Console.WriteLine("Couldn't parse arguments...");
                return;
            }

            Console.WriteLine($"Number of flags found: {sarge.ParsedFlags}");

            if (sarge.Exists("help"))
            {
                sarge.PrintHelp();
            }
            else
            {
                Console.WriteLine("No help requested...");
            }

            if (sarge.GetFlag("kittens", out string? kittens))
            {
                Console.WriteLine($"Got kittens: {kittens}");
            }

            if (sarge.GetFlag("number", out string? number))
            {
                Console.WriteLine($"Got number: {number}");
            }

            if (sarge.GetTextArgument(0, out string? textarg))
            {
                Console.WriteLine($"Got text argument: {textarg}");
            }
        }
    }
}
