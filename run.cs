
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;


namespace murmur3_dumper
{
    internal class run
    {

        // key is the actual string
        // value is the hash
        // while not ideal, this is so we dont get any hash collisions
        public Dictionary<string, string> Hashedstrings = new();

        public string Current_String; // we construct strings by reading a single byte at a time

        Regex rxStringSearch = new Regex(@"^[a-zA-Z0-9-_/\\.]*?$",RegexOptions.Compiled);
        Regex rxCapitalsFilter = new Regex(@"^[a-z0-9_]*?$", RegexOptions.Compiled);
        Regex rx3CharsFilter = new Regex(@"(.*[a-z]){3}", RegexOptions.Compiled);


        
        public void Main1()
        {
            Console.WriteLine(Hashedstrings.Count + " strings loaded in mem");
            Console.WriteLine("select an action");
            Console.WriteLine("\"load\" - this will attempt to load hashed strings from a previously dumped file");
            Console.WriteLine("\"hash\" - this will attempt to strip, hash and load strings from every file in a directory");
            Console.WriteLine("\"hashbig\" - if \"hash\" failed, then likely the file is too large, try this one");
            Console.WriteLine("");
            Console.WriteLine("\"dump\" - this will offload all the loaded string hashes into a text file");
            Console.WriteLine("\"splitdump\" - this will offload all the loaded string hashes into a text file");
            Console.WriteLine("");
            Console.WriteLine("\"filter\" - runs the loaded hashed strings against the binaries inside the directory - to filter unused hashes (REALLY slow)");
            Console.WriteLine("\"nocaps\" - removes currently loaded strings that: has upper case chars and/or contains unusual chars except underscore");
            Console.WriteLine("\"no4reps\" - filters currently loaded strings that have 4 or more of the same character in a row"); // this one really didn't turn out too well
            Console.WriteLine("\"min3chars\" - filters currently loaded strings that don't contain at least 3 lowercase alphabetical characters"); // this one really didn't turn out too well


            string selection = Console.ReadLine();

            if (selection == "load") // we're gonna load a dumped dictionary
            {
                Console.WriteLine("select a dumped hash file to load");
                string Path = Console.ReadLine();

                inload_directory(Path);
            }
            else if (selection == "hash")
            {
                Console.WriteLine("select a directory to hash");
                string Path = Console.ReadLine();

                inload_files(Path);
            }
            else if (selection == "dump")
            {
                if (Hashedstrings.Count > 0)
                {
                    Console.WriteLine("select a file to offload to (will try and create a new one if the file isnt valid)");
                    string Path = Console.ReadLine();



                    offload(Path);
                }
                else
                {
                    Console.WriteLine("you have nothing to offload");
                }
            }
            else if (selection == "hashbig") // i think we can just remove the other hash command, as cutting up the chunks would indefinitely be better *and* we don't cut any strings in between chunks
            {
                Console.WriteLine("select a directory to hash big");
                string Path = Console.ReadLine();

                inload_big_files(Path);
            }
            else if (selection == "filter")
            {
                Console.WriteLine("select a directory to filter hashes");
                string Path = Console.ReadLine();

                filter_hashes(Path);
            }
            else if (selection == "nocaps")
            {
                Console.WriteLine("filtering");

                filtercaps();
            }
            else if (selection == "no4reps")
            {
                Console.WriteLine("filtering");

                filterreps();
            }
            else if (selection == "splitdump")
            {
                if (Hashedstrings.Count > 0)
                {
                    Console.WriteLine("select a file path+name to offload to (it will add [file index].txt to the ends) ");
                    string Path = Console.ReadLine();
                    Console.WriteLine("choose a number of files to split to");
                    //try
                    //{
                        string count_num = Console.ReadLine();
                        int count = Int32.Parse(count_num);
                        splitoffload(Path, count);
                    //}
                    //catch
                    //{
                    //    Console.WriteLine("bad count; number didnt work");
                    //}
                }
                else
                {
                    Console.WriteLine("you have nothing to offload");
                }
            }
            else if (selection == "min3chars")
            {
                Console.WriteLine("filtering");

                filter3chars();

            }

            Console.WriteLine("");
            Console.WriteLine("");

            Main1(); // rinse and repeat.
        }




        public void inload_directory(string filename) // fill up our hased string dictionary from a txt file
        {
            try
            {
                var lines = File.ReadLines(filename);
                foreach (var line in lines)
                {
                    string[] parts = line.Split(":");

                    Hashedstrings[parts[1]] = parts[0];
                }
                Console.WriteLine("Successfully loaded the dumped hashes!");
            }
            catch
            {
                Console.WriteLine(" --------------------------------------------------------------------------------------------------------");
                Console.WriteLine("Error reading previous");
                Console.WriteLine(" --------------------------------------------------------------------------------------------------------");
            }
        }
        public void inload_files(string path) // fll up our hashed string dictionary from binaries, via stripping and hashing strings
        {
            try
            {
                foreach (string file in System.IO.Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    Current_String = "";
                    try
                    {
                        byte[] readText = File.ReadAllBytes(file);
                        foreach (byte s in readText)
                        {
                            byte[] b = new byte[1] { s };
                            string utfString = Encoding.UTF8.GetString(b, 0, 1);

                            //bool stringIsValid = Regex.IsMatch(utfString, @"^[a-zA-Z0-9-_/\\]*?$");
                            bool stringIsValid = rxStringSearch.IsMatch(utfString);
                            if (stringIsValid && s != 0x0A)
                            {
                                Current_String += utfString;
                            }
                            else
                            {
                                if (Current_String.Length > 4 && s == 00)
                                {
                                    if (!Hashedstrings.Keys.Contains(Current_String))
                                    {
                                        string hashd = Hash(Current_String);
                                        Hashedstrings.Add(Current_String, hashd);
                                        //Console.Write(Current_String);
                                    }

                                }
                                Current_String = "";
                            }
                        }
                    }
                    catch
                    {
                        Console.WriteLine(" --------------------------------------------------------------------------------------------------------");
                        Console.WriteLine("#####" + file + " ran into an error ####");
                        Console.WriteLine(" --------------------------------------------------------------------------------------------------------");
                    }

                    Console.WriteLine("Successfully hashed the strings from that directory!");

                }
            }
            catch
            {
                Console.WriteLine(" --------------------------------------------------------------------------------------------------------");
                Console.WriteLine("#####" + path + " was a bad directory ####");
                Console.WriteLine(" --------------------------------------------------------------------------------------------------------");
            }
        }
        public void offload(string out_path)
        {
            try
            {
                using (StreamWriter outputFile = new StreamWriter(out_path))
                {
                    foreach (KeyValuePair<string, string> kv in Hashedstrings)
                    {
                        outputFile.WriteLine(kv.Value + ":" + kv.Key);
                    }
                }
                Console.WriteLine("Successfully offloaded hashes!");
            }
            catch
            {
                Console.WriteLine(" --------------------------------------------------------------------------------------------------------");
                Console.WriteLine("#####" + out_path + " failed to write");
                Console.WriteLine(" --------------------------------------------------------------------------------------------------------");
            }
        }

        public void splitoffload(string out_path, int count)
        {
            List<Dictionary<string, string>> Dumps = new();
            for (int i = 0; i <count; i++)
            {
                Dumps.Add(new Dictionary<string, string>());
            }
            int index = 0;
            foreach (KeyValuePair<string, string> kv in Hashedstrings)
            {
                Dumps[index].Add(kv.Key, kv.Value);

                index += 1;
                if (index>=count)
                    index = 0;
            }

            for(int i = 0; i < Dumps.Count; i++)
            {
                Dictionary<string, string> d = Dumps[i];
                try
                {
                    using (StreamWriter outputFile = new StreamWriter(out_path + i + ".txt"))
                    {
                        foreach (KeyValuePair<string, string> kv in d)
                        {
                            outputFile.WriteLine(kv.Value + ":" + kv.Key);
                        }
                    }
                    Console.WriteLine("Successfully offloaded "+ d.Count +" hashes!");
                }
                catch
                {
                    Console.WriteLine(" --------------------------------------------------------------------------------------------------------");
                    Console.WriteLine("#####" + out_path + " failed to write");
                    Console.WriteLine(" --------------------------------------------------------------------------------------------------------");
                }
            }

        }

        public string Hash(string hash_in)
        {
            Encoding encoding = new UTF8Encoding();
            byte[] input = encoding.GetBytes(hash_in);
            using (MemoryStream stream = new MemoryStream(input))
            {
                int hash = MurMurHash3.Hash(stream);
                //new { Hash = hash, Bytes = BitConverter.GetBytes(hash) }.Dump("Result");
                //Console.WriteLine("Hash (" + hash+")" );
                //Console.WriteLine("Bytes (" + BitConverter.ToString(BitConverter.GetBytes(hash)).Replace("-", "") + ")" );
                return BitConverter.ToString(BitConverter.GetBytes(hash)).Replace("-", "");
            }
            return "";
        }






        public void inload_big_files(string path)
        {
            foreach (string file in System.IO.Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                Current_String = "";
                ReadAndProcessLargeFile(file, 0);
            }
        } 


        const int gigabyte = 1000000000; // amount of bytes loaded in a chunk (this is the reason for large ram usage, try lowering)

        public void ReadAndProcessLargeFile(string theFilename, long whereToStartReading = 0)
        {
            FileStream fileStram = new FileStream(theFilename, FileMode.Open, FileAccess.Read);
            using (fileStram)
            {
                byte[] buffer = new byte[gigabyte];
                fileStram.Seek(whereToStartReading, SeekOrigin.Begin);
                int bytesRead = fileStram.Read(buffer, 0, gigabyte);
                while (bytesRead > 0)
                {
                    ProcessChunk(buffer, bytesRead);
                    bytesRead = fileStram.Read(buffer, 0, gigabyte);
                }

            }
        }



        private void ProcessChunk(byte[] buffer, int bytesRead)
        {
            try
            {
                foreach (byte s in buffer)
                {
                    // Printing the binary array value of
                    // the file contents
                    byte[] b = new byte[1] { s };
                    string utfString = Encoding.UTF8.GetString(b, 0, 1);

                    //bool stringIsValid = Regex.IsMatch(utfString, @"^[a-zA-Z0-9-_/\\]*?$");
                    bool stringIsValid = rxStringSearch.IsMatch(utfString);
                    if (stringIsValid && s != 0x0A)
                    {
                        Current_String += utfString;
                    }
                    else
                    {
                        if (Current_String.Length > 4 && s == 00)
                        {
                            if (!Hashedstrings.Keys.Contains(Current_String))
                            {
                                string hashd = Hash(Current_String);
                                Hashedstrings.Add(Current_String, hashd);
                                //Console.Write(Current_String);
                            }

                        }
                        Current_String = "";
                    }
                }
            }
            catch
            {
                Console.WriteLine(" --------------------------------------------------------------------------------------------------------");
                Console.WriteLine("##### chunk ran into an error ####");
                Console.WriteLine(" --------------------------------------------------------------------------------------------------------");
            }

            Console.WriteLine("Successfully hashed chunk! (1gb)");
        }

        public void filter_hashes(string path)
        {
            Dictionary<string, string> filtered_strings = new();


            try
            {
                foreach (string file in System.IO.Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    Current_String = "";
                    try
                    {
                        byte[] readText = File.ReadAllBytes(file);

                        foreach (KeyValuePair<string, string> ks in Hashedstrings)
                        {

                            int w = IndexOf(readText, StringToByteArray(ks.Value));
                            // if (every_4byte.Contains(ks.Value))
                            if (w!=-1)
                            {
                                filtered_strings.Add(ks.Key, ks.Value);
                                Hashedstrings.Remove(ks.Key);
                            }
                        }
                    }
                    catch
                    {
                        Console.WriteLine(" --------------------------------------------------------------------------------------------------------");
                        Console.WriteLine("#####" + file + " ran into an error ####");
                        Console.WriteLine(" --------------------------------------------------------------------------------------------------------");
                    }

                    Console.WriteLine("Successfully filtered the strings from " + file);

                }
                Hashedstrings = filtered_strings;
            }
            catch
            {
                Console.WriteLine(" --------------------------------------------------------------------------------------------------------");
                Console.WriteLine("#####" + path + " was a bad directory ####");
                Console.WriteLine(" --------------------------------------------------------------------------------------------------------");
            }
        }

        public void filtercaps()
        {
            Dictionary<string, string> filtered_strings = new();

            foreach (KeyValuePair<string, string> kv in Hashedstrings)
            {

                bool stringIsValid = rxCapitalsFilter.IsMatch(kv.Key);
                if (stringIsValid)
                {
                    filtered_strings.Add(kv.Key, kv.Value);
                }
            }
            Hashedstrings = filtered_strings;
        }

        public void filterreps()
        {
            Dictionary<string, string> filtered_strings = new();

            foreach (KeyValuePair<string, string> kv in Hashedstrings)
            {
                char repeated_char_1 = new();
                char repeated_char_2 = new();
                char repeated_char_3 = new();
                string repeatedWord = kv.Key;
                int toomanymatches = 0;
                for (int i = 0; i < repeatedWord.Count()-1; i++)
                {
                    if (repeated_char_1== '\x0000' && repeated_char_2== '\x0000') // both null
                    {
                        repeated_char_1=repeatedWord[i];
                        continue;
                    }
                    if (repeated_char_1 != '\x0000' && repeated_char_2 == '\x0000') // 1 but 2 is null
                    {
                        if (repeatedWord[i] == repeated_char_1)
                        {
                            repeated_char_2 = repeatedWord[i];
                        }
                        else
                        {
                            repeated_char_1 = '\x0000';
                            repeated_char_2 = '\x0000';
                            repeated_char_3 = '\x0000';
                        }
                        continue;
                    }
                    if (repeated_char_1 != '\x0000' && repeated_char_2 != '\x0000' && repeated_char_3 == '\x0000') // both not null
                    {
                        if (repeatedWord[i] == repeated_char_1&&repeatedWord[i] == repeated_char_2)
                        {
                            repeated_char_3 = repeatedWord[i];
                            continue;
                        }
                        else
                        {
                            repeated_char_1 = '\x0000';
                            repeated_char_2 = '\x0000';
                            repeated_char_3 = '\x0000';
                        }
                        continue;
                    }
                    if (repeated_char_1 != '\x0000' && repeated_char_2 != '\x0000' && repeated_char_3 != '\x0000') // none are null
                    {
                        if (repeatedWord[i] == repeated_char_1 && repeatedWord[i] == repeated_char_2 && repeatedWord[i] == repeated_char_3)
                        {
                            toomanymatches = 1;
                            break;
                        }
                        else
                        {
                            repeated_char_1 = '\x0000';
                            repeated_char_2 = '\x0000';
                            repeated_char_3 = '\x0000';
                        }
                    }
                }

                if (toomanymatches == 1)
                {
                    filtered_strings.Add(kv.Key, kv.Value);
                }
            }
            Hashedstrings = filtered_strings;
        }

        public void filter3chars()
        {
            Dictionary<string, string> filtered_strings = new();

            foreach (KeyValuePair<string, string> kv in Hashedstrings)
            {

                bool stringIsValid = rx3CharsFilter.IsMatch(kv.Key);
                if (stringIsValid)
                {
                    filtered_strings.Add(kv.Key, kv.Value);
                }
            }
            Hashedstrings = filtered_strings;
        }









































        // random code i got off the internet // works a little better than what i had
        // or maybe not

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
        public static int IndexOf(byte[] haystack, byte[] needle)
        {
            if (needle.Length == 0)
            {
                return 0;
            }

            int[] charTable = MakeCharTable(needle);
            int[] offsetTable = MakeOffsetTable(needle);
            for (int i = needle.Length - 1; i < haystack.Length;)
            {
                int j;
                for (j = needle.Length - 1; needle[j] == haystack[i]; --i, --j)
                {
                    if (j == 0)
                    {
                        return i;
                    }
                }

                i += Math.Max(offsetTable[needle.Length - 1 - j], charTable[haystack[i]]);
            }

            return -1;
        }

        private static int[] MakeCharTable(byte[] needle)
        {
            const int ALPHABET_SIZE = 256;
            int[] table = new int[ALPHABET_SIZE];
            for (int i = 0; i < table.Length; ++i)
            {
                table[i] = needle.Length;
            }

            for (int i = 0; i < needle.Length - 1; ++i)
            {
                table[needle[i]] = needle.Length - 1 - i;
            }

            return table;
        }

        private static int[] MakeOffsetTable(byte[] needle)
        {
            int[] table = new int[needle.Length];
            int lastPrefixPosition = needle.Length;
            for (int i = needle.Length - 1; i >= 0; --i)
            {
                if (IsPrefix(needle, i + 1))
                {
                    lastPrefixPosition = i + 1;
                }

                table[needle.Length - 1 - i] = lastPrefixPosition - i + needle.Length - 1;
            }

            for (int i = 0; i < needle.Length - 1; ++i)
            {
                int slen = SuffixLength(needle, i);
                table[slen] = needle.Length - 1 - i + slen;
            }

            return table;
        }

        private static bool IsPrefix(byte[] needle, int p)
        {
            for (int i = p, j = 0; i < needle.Length; ++i, ++j)
            {
                if (needle[i] != needle[j])
                {
                    return false;
                }
            }

            return true;
        }

        private static int SuffixLength(byte[] needle, int p)
        {
            int len = 0;
            for (int i = p, j = needle.Length - 1; i >= 0 && needle[i] == needle[j]; --i, --j)
            {
                len += 1;
            }

            return len;
        }

    }
}

