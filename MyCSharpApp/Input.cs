using Github;
using Spectre.Console;

namespace MinecraftInstaller {
    public class UserInput {
        public static void DisplayWelcomeMessage() {
            Console.WriteLine("Welcome to the Minecraft Modpack Installer!");
            Console.WriteLine("This tool will help you install and manage your Minecraft modpacks easily.");
            Console.WriteLine("Please follow the instructions provided to complete the installation.");
        }

        public static async Task GetModpackName() {
            await Task.CompletedTask;
            throw new Exception("This method is not implemented yet.");
            // GitHubTree tree = await GithubHelper.GetGitHubTreeAsync(false, "main");

            // string modpackName = EditableInput(
            //     "Enter the name of the modpack",
            //     "default-modpack-name",
            //     (input) => tree.Tree.Any(item => item.Path.Equals(input, StringComparison.OrdinalIgnoreCase))
            //     );

            // Console.WriteLine($"The modpack name is: {modpackName}");
        }

        static string EditableInput(string label, string defaultText, Func<string, bool>? validator = null) {
            ConsoleKeyInfo keyInfo;
            int labelOffset = label.Length + 5;
            int cursorPosition = defaultText.Length;
            int length = defaultText.Length;
            bool isWarningDisplayed = false;

            // Convert the default text into a modifiable buffer
            char[] buffer = new char[128]; // Allocate a fixed buffer size
            defaultText.CopyTo(0, buffer, 0, defaultText.Length);

            // Display the label and default text
            AnsiConsole.Markup(" [blue]?[/] " + label + ": ");
            Console.Write(defaultText);

            while (true) {
                keyInfo = Console.ReadKey(intercept: true);

                // Handle Enter key
                if (keyInfo.Key == ConsoleKey.Enter) {
                    string input = new(buffer, 0, length);

                    if (validator == null || validator(input)) {
                        Console.WriteLine(); // Move to the next line if valid
                        break;
                    }
                    else {
                        if (isWarningDisplayed == true) continue; // Skip if the warning is already displayed

                        isWarningDisplayed = true;
                        // Save the current cursor position
                        int originalCursorLeft = Console.CursorLeft;
                        int originalCursorTop = Console.CursorTop;

                        // Display the validator's warning message in red
                        Console.SetCursorPosition(0, Console.CursorTop + 1); // Move to the next line for the warning
                        AnsiConsole.MarkupLine("[red]Invalid input! Please try again.[/]");

                        // Restore the cursor to the input line and its original position
                        Console.SetCursorPosition(originalCursorLeft, originalCursorTop);
                    }
                }
                // Handle Backspace key
                else if (keyInfo.Key == ConsoleKey.Backspace) {
                    if (cursorPosition > 0) { // Ensure we're not at the start of the line
                        cursorPosition--; // Move cursor back

                        length--; // Reduce the length of the text

                        // Shift characters left starting from the cursor position
                        for (int i = cursorPosition; i < length; i++) {
                            buffer[i] = buffer[i + 1];
                        }

                        buffer[length] = '\0'; // Clear the last character

                        // Update console
                        RedrawBuffer(label, buffer, length, cursorPosition);
                    }
                }
                // Handle Delete key
                else if (keyInfo.Key == ConsoleKey.Delete) {
                    if (cursorPosition < length) {
                        length--;
                        Console.Write("\b"); // Move the cursor back

                        // Shift characters left
                        for (int i = cursorPosition; i < length; i++) {
                            buffer[i] = buffer[i + 1];
                        }

                        buffer[length] = '\0'; // Clear the last character

                        // Update console
                        RedrawBuffer(label, buffer, length, cursorPosition);
                    }
                }
                // Handle Arrow Keys
                else if (keyInfo.Key == ConsoleKey.LeftArrow) {
                    if (cursorPosition > 0) {
                        cursorPosition--;
                        Console.SetCursorPosition(cursorPosition + labelOffset, Console.CursorTop);
                    }
                }
                else if (keyInfo.Key == ConsoleKey.RightArrow) {
                    if (cursorPosition < length) {
                        cursorPosition++;
                        Console.SetCursorPosition(cursorPosition + labelOffset, Console.CursorTop);
                    }
                }
                // Handle Regular Keys
                else if (!char.IsControl(keyInfo.KeyChar)) {
                    if (length < buffer.Length && 80 > length) {
                        ClearWarningMessageIfNeeded(ref isWarningDisplayed);
                        // Shift characters right
                        for (int i = length; i > cursorPosition; i--) {
                            buffer[i] = buffer[i - 1];
                        }

                        // Insert the new character
                        buffer[cursorPosition] = keyInfo.KeyChar;
                        cursorPosition++;
                        length++;

                        // Update console
                        RedrawBuffer(label, buffer, length, cursorPosition);
                    }
                }
            }

            // Return the final string
            return new string(buffer, 0, length);
        }

        static void RedrawBuffer(string label, char[] buffer, int length, int cursorPosition) {
            int labelOffset = label.Length + 5;

            Console.SetCursorPosition(labelOffset, Console.CursorTop);
            Console.Write(new string(' ', labelOffset + length + 1)); // Clear the line
            Console.SetCursorPosition(labelOffset, Console.CursorTop);

            Console.Write(new string(buffer, 0, length));
            Console.SetCursorPosition(cursorPosition + labelOffset, Console.CursorTop);
        }


        static void ClearWarningMessageIfNeeded(ref bool isWarningDisplayed) {
            if (isWarningDisplayed) {
                int currentLine = Console.CursorTop + 1; // Line where the warning is displayed
                Console.SetCursorPosition(0, currentLine); // Go to the warning line
                Console.Write(new string(' ', Console.WindowWidth)); // Clear the warning line
                isWarningDisplayed = false; // Reset the flag
            }
        }

    }
}