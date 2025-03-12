using System;
using System.Runtime.CompilerServices;
using static System.Net.Mime.MediaTypeNames;

///
/// C# implementation of https://github.com/MayaPosch/Sarge
/// Slightly tweaked to better match common dotnet coding patterns
/// 
namespace SargeSharp
{
    public class Sarge(bool permissive = false)
    {
        List<Argument> allArgs = new List<Argument>();
        List<string> textArguments = new List<string>();

        int flagCounter = 0;
        bool parsed = false;

        public string Description { get; set; } = "";
        public string Usage { get; set; } = "";
        public int ParsedFlags { get { return flagCounter; } }

        public void SetArgument(string arg_short, string arg_long, string? desc, bool hasVal)
        {
            var arg = new Argument()
            {
                arg_short = arg_short,
                arg_long = arg_long,
                description = desc,
                hasValue = hasVal
            };

            allArgs.Add(arg);
        }

        public void SetArguments(List<Argument> args)
        {
            foreach (Argument a in args)
            {
                SetArgument(a.arg_short, a.arg_long, a.description, a.hasValue);
            }
        }

        public bool ParseArguments(string[] args)
        {
            // We loop through the arguments, linking flags and values.
            bool expectValue = false;
            Argument last_arg = new Argument();

            foreach (var arg in args)
            {
                // Each flag will start with a '-' character. Multiple flags can be joined together in the
                // same string if they're the short form flag type (one character per flag).
                string entry = arg;

                if (expectValue)
                {
                    // Previous token was a flag with value. Copy value to that Argument.
                    last_arg.value = entry;
                    expectValue = false;
                }
                else if (entry.Substring(0, 1) == "-") 
                {
                    if (permissive == false && textArguments.Count() > 0)
                    {
                        //std::cerr << "Flags not allowed after text arguments." << std::endl;
                        throw new ApplicationException($"Flags not allowed after text arguments: {entry}");
                    }

                    // Parse flag.
                    // First check for the long form.
                    if (entry.Substring(0, 2) == "--")
                    {
                        // Long form of flag.
                        entry = entry.Substring(2); // Erase the double dash since we no longer need it.
                        var found = allArgs.Where(a => a.arg_long == entry).SingleOrDefault();
                        if (found == null) 
                        {
                            if (permissive) { continue; }

                            // Flag wasn't found. Abort.
                            //std::cerr << "Long flag " << entry << " wasn't found." << std::endl;
                            throw new ApplicationException($"Long flag {entry} isn't defined.");
                        }

                        // Mark as found.
                        found.parsed = true;
                        ++flagCounter;

                        if (found.hasValue)
                        {
                            last_arg = found;
                            expectValue = true; // Next argument has to be a value string.
                        }
                    }
                    else
                    {
                        // Parse short form flag. Parse all of them sequentially. Only the last one
                        // is allowed to have an additional value following it.	
                        entry = entry.Substring(1); // Erase the dash.	
                        for (int i = 0; i < entry.Length; ++i)
                        {
                            string k = entry.Substring(i, 1);
                            var found = allArgs.Where(a => a.arg_short == k).SingleOrDefault(); 
                            if (found == null)
                            {
                                if (permissive) { continue; }

                                // Flag wasn't found. Abort.
                                //std::cerr << "Short flag " << k << " wasn't found." << std::endl;
                                throw new ApplicationException($"Short flag {k} isn't defined.");
                            }

                            // Mark as found.
                            found.parsed = true;
                            ++flagCounter;

                            if (found.hasValue)
                            {
                                if (i != (entry.Length - 1))
                                {
                                    // Flag isn't at end, thus cannot have value.
                                    //std::cerr << "Flag " << k << " needs to be followed by a value string." << std::endl;
                                    throw new ApplicationException($"Flag -{k} needs to be followed by a value string.");
                                }
                                else
                                {
                                    last_arg = found;
                                    expectValue = true; // Next argument has to be a value string.
                                }
                            }
                        }
                    }
                }

                else
                {
                    // Add to list of text arguments.
                    textArguments.Add(entry);
                }
	        }

	        parsed = true;

            return true;
        }

        // --- GET FLAG ---
        // Returns whether the flag was found, along with the value if relevant.
        public bool GetFlag(string arg_flag, out string? arg_value)
        {
            arg_value = null;
            if (!parsed) { return false; }

            var found = allArgs.Where(a => a.arg_long == arg_flag || a.arg_short == arg_flag).SingleOrDefault(); // argNames.TryGetValue(arg_flag, out Argument it);
            if (found == null) { return false; }
            if (!found.parsed) { return false; }

            if (found.hasValue) { arg_value = found.value; }

            return true;
        }

        // --- EXISTS ---
        // Returns whether the flag was found.
        public bool Exists(string arg_flag)
        {
            if (!parsed) { return false; }

            var found = allArgs.Where(a => a.arg_long == arg_flag || a.arg_short == arg_flag).SingleOrDefault(); // argNames.TryGetValue(arg_flag, out Argument it);
            if (found == null) { return false; }
            if (!found.parsed) { return false; }

            return true;
        }

        // --- GET TEXT ARGUMENT ---
        // Updates the value parameter with the text argument (unbound value) if found.
        // Index starts at 0.
        // Returns true if found, else false.
        public bool GetTextArgument(int index, out string? value)
        {
            value = null;
            if (index < textArguments.Count()) 
            { 
                value = textArguments[index]; 
                return true; 
            }

            return false;
        }

        // --- PRINT HELP ---
        // Prints out the application description, usage and available options.
        public void PrintHelp()
        {
            Console.WriteLine(Description);
            Console.WriteLine("Usage:");
            Console.WriteLine(Usage);
            Console.WriteLine("");
            Console.WriteLine("Options: ");

            // Determine whitespace needed between arg_long and description.
            int maxlen = 1;
            const string VAL_STR = " <val>";

            foreach (var arg in allArgs)
            {
                int len = 0;
                if (arg.arg_long != null)
                    len = arg.arg_long.Length;
                if (arg.hasValue)
                    len += VAL_STR.Length;
                
                if (len > maxlen)
                    maxlen = len;
            }

            maxlen += 3; // Number of actual spaces between the longest arg_long and description.

            // Print out the options.            
            foreach (var arg in allArgs)
            {
                string arg_long = arg.arg_long ?? "";
                if (arg.hasValue)
                    arg_long += VAL_STR;

                Console.Write(string.IsNullOrEmpty(arg.arg_short) ? "    " : "-" + arg.arg_short + ", "); // Always 4 chars
                Console.Write("--" + arg_long.PadRight(maxlen));
                Console.WriteLine(arg.description);
            }
        }
    }

    public class Argument
    {
        public Argument()
        {
            arg_short = "";
            arg_long = "";
            hasValue = false;
            parsed = false;
        }

        public string arg_short;
        public string arg_long;
        public string? description;
        public bool hasValue;
        public string? value;
        public bool parsed;
    }
}