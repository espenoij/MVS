using System.Linq;
using System.Text.RegularExpressions;
using Telerik.Windows.Controls;

// Flere av disse valideringsmetodene er litt spesiell på den måten at
// de ikke bare leverer true/false om valideringen er ok, men også en ferdig string.
// Denne stringen er lik input dersom valideringen er ok, og stringen er
// lik en satt default verdi dersom valideringen ikke er ok.

public static class DataValidation
{
    // Tar string som input + min/max verdier og returerer om input er innenfor grenseverdiene.
    // Returerer true/false om valideringen er ok.
    // Returerer output som string -> er lik input dersom validering ok, eller lik default dersom validering feilet.
    public static bool String(string input, double min, double max, double defaultValue, out string validatedInput)
    {
        bool validInput = false;
        double inputValue = 0;

        // Sjekke at ikke input er blank eller null
        if (!string.IsNullOrWhiteSpace(input))
        {

            // Sjekke om input er numerisk
            if (double.TryParse(input, out inputValue))
            {
                // Range sjekk
                if (inputValue >= min && inputValue <= max)
                {
                    validInput = true;
                }
                // Ikke gyldig
                else
                {
                    RadWindow.Alert(string.Format("Input Error\n\nValid input range: {0} - {1}", min, max));
                }
            }
            // Numerisk input
            else
            {
                RadWindow.Alert("Input Error\n\nA numeric input value is required.");
            }
        }

        // Gyldig input -> Sett output like parset verdi (kan være ulik input, f.eks. input: 1000w -> parset: 1000)
        if (validInput)
            validatedInput = inputValue.ToString();
        // Ikke gyldig output -> Sett default verdi
        else
            validatedInput = defaultValue.ToString();

        return validInput;
    }

    // Som over, men returnerer blank some default dersom verdi ikke aksepteres
    public static bool String(string input, double min, double max, out string validatedInput)
    {
        bool validInput = false;
        double inputValue = 0;

        // Sjekke at ikke input er blank eller null
        if (!string.IsNullOrWhiteSpace(input))
        {

            // Sjekke om input er numerisk
            if (double.TryParse(input, out inputValue))
            {
                // Range sjekk
                if (inputValue >= min && inputValue <= max)
                {
                    validInput = true;
                }
                // Ikke gyldig
                else
                {
                    RadWindow.Alert(string.Format("Input Error\n\nValid input range: {0} - {1}", min, max));
                }
            }
            // Numerisk input
            else
            {
                RadWindow.Alert("Input Error\n\nA numeric input value is required.");
            }
        }

        // Gyldig input -> Sett output like parset verdi (kan være ulik input, f.eks. input: 1000w -> parset: 1000)
        if (validInput)
            validatedInput = inputValue.ToString();
        // Ikke gyldig output -> Sett default verdi
        else
            validatedInput = string.Empty;

        return validInput;
    }

    public static bool Double(string input, double min, double max, double defaultValue, out double validatedInput)
    {
        // Videresender parametrene til String metoden ovenfor, parser til double og returnerer
        if (String(input, min, max, defaultValue, out string tempInput))
        {
            if (double.TryParse(tempInput, out validatedInput))
            {
                return true;
            }
            else
            {
                validatedInput = defaultValue;
                return false;
            }
        }
        else
        {
            validatedInput = defaultValue;
            return false;
        }
    }

    // Validere IP adresse
    public static bool IPAddress(string input, string defaultValue, out string validatedInput)
    {
        bool validAddress = false;

        // Sjekke at ikke input er blank eller null
        if (!string.IsNullOrWhiteSpace(input))
        {
            // Splitter IP adresse feltet opp i 4 deler
            string[] splitValues = input.Split('.');
            if (splitValues.Length == 4)
            {
                // Sjekker at hver del av adressen er innenfor byte verdi (dvs 256).
                validAddress = splitValues.All(r => byte.TryParse(r, out byte tempForParsing));
            }
        }

        // Gyldig input -> Sett output lik input
        if (validAddress)
        {
            validatedInput = input;
        }
        // Ikke gyldig output -> Sett default verdi
        else
        {
            RadWindow.Alert("Input Error\n\nInvalid IP address.");
            validatedInput = defaultValue;
        }

        return validAddress;
    }

    // Validere e-post adresse
    public static bool IsValidEmailAddress(this string input)
    {
        Regex regex = new Regex(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");
        return regex.IsMatch(input);
    }

    // Validerer input etter lengde på string
    // Sette default verdi dersom ugyldig
    public static bool StringLength(string input, int length, string defaultValue, out string validatedInput)
    {
        bool valid;
        if (input.Length == length)
        {
            validatedInput = input;
            valid = true;
        }
        else
        {
            validatedInput = defaultValue;
            valid = false;
        }

        return valid;
    }
}
