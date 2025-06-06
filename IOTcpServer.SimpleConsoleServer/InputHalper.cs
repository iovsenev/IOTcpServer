﻿namespace IOTcpServer.SimpleConsoleServer;
public static class InputHalper
{
    public static string GetString(string question, string? defaultAnswer)
    {
        string? text;
        while (true)
        {
            Console.Write(question);
            if (!string.IsNullOrEmpty(defaultAnswer))
            {
                Console.Write(" [" + defaultAnswer + "]");
            }

            Console.Write(" ");
            text = Console.ReadLine();
            if (!string.IsNullOrEmpty(text))
            {
                break;
            }

            if (!string.IsNullOrEmpty(defaultAnswer))
            {
                return defaultAnswer;
            }
        }
        return text;
    }

    public static int GetInteger(string question, int defaultAnswer, bool positiveOnly, bool allowZero)
    {
        int result;
        while (true)
        {
            Console.Write(question);
            Console.Write(" [" + defaultAnswer + "] ");
            string? text = Console.ReadLine();
            if (string.IsNullOrEmpty(text))
            {
                return defaultAnswer;
            }

            result = 0;
            if (!int.TryParse(text, out result))
            {
                Console.WriteLine("Please enter a valid integer.");
                continue;
            }

            if (result == 0 && allowZero)
            {
                return 0;
            }

            if (result >= 0 || !positiveOnly)
            {
                break;
            }

            Console.WriteLine("Please enter a value greater than zero.");
        }

        return result;
    }

    public static bool GetBoolean(string question, bool trueDefault)
    {
        Console.Write(question);
        if (trueDefault)
        {
            Console.Write(" [Y/n]? ");
        }
        else
        {
            Console.Write(" [y/N]? ");
        }

        string? text = Console.ReadLine();
        if (string.IsNullOrEmpty(text))
        {
            if (trueDefault)
            {
                return true;
            }

            return false;
        }

        text = text.ToLower();
        if (trueDefault)
        {
            if (string.Compare(text, "n") == 0 || string.Compare(text, "no") == 0 || string.Compare(text, "0") == 0)
            {
                return false;
            }

            return true;
        }

        if (string.Compare(text, "y") == 0 || string.Compare(text, "yes") == 0 || string.Compare(text, "1") == 0)
        {
            return true;
        }

        return false;
    }

    public static Dictionary<string, TValue> GetDictionary<TValue>(string keyQuestion, string valQuestion)
    {
        Dictionary<string, TValue> dictionary = new();
        while (true)
        {
            string? @string = GetString(keyQuestion, null);
            if (string.IsNullOrEmpty(@string))
            {
                break;
            }

            string? string2 = GetString(valQuestion, null);

            var key = @string;

            if (key == null)
                break;

            TValue? value = default;

            if (string.IsNullOrEmpty(string2))
            {
                string2 = "";
            }
            value = (TValue)Convert.ChangeType(string2, typeof(TValue));

            dictionary.Add(key, value);
        }

        return dictionary;
    }
}
