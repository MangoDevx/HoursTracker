﻿using System.Runtime.InteropServices;
using Spectre.Console;
using tracker.Database;
using tracker.Database.DbModels;

namespace tracker.Services;

public class ConsoleService
{
    private readonly string[] _options =
    {
        "Add Process%Adds a new process to track",
        "Edit Process%Edits a process to track, change the name, filepath, etc",
        "Delete Process%Removes a process to track, may be readded automatically",
        "List Processes%Lists the processes that will be tracked when they are running",
        "Blacklist Menu%Displays the blacklist menu to add, remove, or list blacklist applications",
        "Hide App%Hides the application in the background. To close it end tracker.exe in Task Manager"
    };

    private readonly string[] _blOptions =
    {
        "Add Process%Adds a new process to the blacklist, this stops it from being automatically readded",
        "Delete Process%Removes a process from the blacklist, allows the processes to be automatically readded",
        "List Processes%Lists the processes that are currently being blacklisted from automatic detection",
        "Back%Goes back to the main menu"
    };


    private readonly DataContext _context;

    public ConsoleService(DataContext context)
    {
        _context = context;
    }

    public async Task RunConsole()
    {
        Console.WriteLine("Press any key to continue to the next screen...");
        Console.ReadKey();
        await MainScreen();
    }

    private async Task MainScreen()
    {
        var hidden = false;
        while (!hidden)
        {
            Console.Clear();
            Console.WriteLine();
            var table = new Table { Border = TableBorder.Rounded };
            table.Title("[white]Welcome to[/] [springgreen3]GameTracker[/][white]![/]");
            table.BorderColor(Color.DeepSkyBlue3);
            table.AddColumn("Options");
            table.AddColumn("Descriptions");
            for (var i = 0; i < _options.Length; i++)
            {
                var option = _options[i];
                var split = option.Split('%');
                var title = i + 1 + ". " + split[0];
                var desc = split[1];
                table.AddRow(title, desc);
            }

            table.Caption("[white]To select one of the [/][deepSkyBlue3]options[/][white], enter it below[/]");
            AnsiConsole.Write(table);
            Console.WriteLine();

            var optionHint = "(";
            for (var i = 0; i < _options.Length - 1; i++)
                optionHint += i + 1 + ", ";
            optionHint += $"{_options.Length})";

            int selectedOption;
            while (true)
            {
                AnsiConsole.Cursor.Show();
                selectedOption = AnsiConsole.Ask<int>($"Which [deepSkyBlue3]option[/] do you want to select {optionHint}:");
                if (selectedOption < 1 || selectedOption > _options.Length)
                    AnsiConsole.Markup("[red]Invalid input[/]\n");
                else
                    break;
            }

            switch (selectedOption)
            {
                case 1:
                    await AddProcessAsync();
                    break;
                case 2:
                    await EditProcessAsync();
                    break;
                case 3:
                    await DeleteProcessAsync();
                    break;
                case 4:
                    ListProcesses();
                    break;
                case 5:
                    await BlacklistMenu();
                    break;
                case 6:
                    hidden = HideApp();
                    break;
                default:
                    break;
            }
        }
    }

    private async Task AddProcessAsync()
    {
        Console.Clear();
        Console.WriteLine();
        AnsiConsole.Write(new Rule("[deepSkyBlue3]Add Process[/]"));
        Console.WriteLine();

        while (true)
        {
            var inputPath = AnsiConsole.Ask<string>("Please input the [deepSkyBlue3]path[/] to the game/app or [red]back[/] to go back: ");
            if (inputPath.ToLowerInvariant() == "back")
                break;

            if (!File.Exists(inputPath))
            {
                AnsiConsole.Markup("[red]Invalid input.[/] Please make sure you input the full path, including the application.\n\n");
                continue;
            }

            var gameName = inputPath.Split('\\').Last();
            if (string.IsNullOrEmpty(gameName))
                gameName = inputPath.Split('/').Last();
            if (string.IsNullOrEmpty(gameName))
                gameName = inputPath;

            if (_context.Processes.Any(x => x.Path == inputPath))
            {
                AnsiConsole.Markup("[red]This application is already in the list[/]\n\n");
            }
            else
            {
                _context.Processes.Add(new TrackedProcess { Name = gameName, Path = inputPath, HoursRan = 0, LastAccessed = DateTime.UtcNow.ToString("o"), Tracking = true });
                await _context.SaveChangesAsync();
                AnsiConsole.Write(new Markup($"Successfully added [springgreen3]{gameName}[/] to the list.\n"));
            }

            var inputAgain = AnsiConsole.Ask<string>("Do you want to add another app? ([springgreen3]y[/]/[red]n[/]): ");
            if (inputAgain.ToLowerInvariant() == "n")
                break;
            Console.WriteLine();
        }
    }

    private async Task EditProcessAsync()
    {
        Console.Clear();
        Console.WriteLine();
        AnsiConsole.Write(new Rule("[deepSkyBlue3]Edit Process[/]"));
        Console.WriteLine();

        while (true)
        {
            var inputPath = AnsiConsole.Ask<string>("Please input the [deepSkyBlue3]name or path[/] to the game/app or [red]back[/] to go back: ");
            if (inputPath.ToLowerInvariant() == "back")
                return;

            if (_context.Processes.Any(x => x.Name == inputPath) && _context.Processes.Any(x => x.Path == inputPath))
            {
                AnsiConsole.Markup("[red]No process was found with that name or path[/]\n\n");
            }
            else
            {
                while (true)
                {
                    var process = _context.Processes.FirstOrDefault(x => x.Name == inputPath) ?? _context.Processes.FirstOrDefault(x => x.Path == inputPath);
                    if (process is null)
                    {
                        AnsiConsole.Markup("[red]Failed to get the process[/]\n\n");
                        break;
                    }
                    AnsiConsole.Markup($"Process Name: [deepSkyBlue3]{process.Name}[/]\nProcess Path: [deepSkyBlue3]{process.Path}[/]\nTracking: [deepSkyBlue3]{process.Tracking}[/]\n");

                    Console.WriteLine();
                    AnsiConsole.Markup("Select what you'd like to edit about the process by highlighting it with the arrow keys, then pressing enter.\n");
                    var responseChoices = new string[] { "Process Path", "Process Name", "Track Process", "Back" };
                    var response = AnsiConsole.Prompt(new SelectionPrompt<string>()
                        .AddChoices(responseChoices)
                        .HighlightStyle(new Style(foreground: Color.DeepSkyBlue3)));

                    if (response == responseChoices.Last())
                    {
                        Console.WriteLine();
                        break;
                    }

                    if (response == responseChoices[0])
                    {
                        var newValue = AnsiConsole.Ask<string>("Please input the new [deepSkyBlue3]path[/] to the game/app or [red]back[/] to go back: ");
                        if (newValue.ToLowerInvariant() == "back")
                            continue;

                        if (!File.Exists(newValue))
                        {
                            AnsiConsole.Markup("[red]The given path was not valid. No file found there.[/]\n");
                            continue;
                        }

                        process.Path = newValue;
                        await _context.SaveChangesAsync();
                        AnsiConsole.Write(new Markup($"Successfully updated [springgreen3]{process.Name}[/]'s path.\n\n"));
                    }

                    if (response == responseChoices[1])
                    {
                        var newValue = AnsiConsole.Ask<string>("Please input the new [deepSkyBlue3]name[/] to the game/app or [red]back[/] to go back: ");
                        if (newValue.ToLowerInvariant() == "back")
                            continue;

                        process.Name = newValue;
                        await _context.SaveChangesAsync();

                        if (!inputPath.Contains('\\') && !inputPath.Contains('/'))
                            inputPath = newValue;
                        AnsiConsole.Write(new Markup($"Successfully updated [springgreen3]{process.Name}[/]'s name.\n\n"));
                    }

                    if (response == responseChoices[2])
                    {
                        var newValue = AnsiConsole.Ask<string>("Do you wish to keep tracking this process? ([springgreen3]y[/]/[red]n[/]): ");
                        newValue = newValue.ToLowerInvariant();

                        process.Tracking = newValue != "n";
                        await _context.SaveChangesAsync();

                        if (process.Tracking)
                            AnsiConsole.Write(new Markup($"Successfully tracking [springgreen3]{process.Name}[/]'s\n"));
                        else
                            AnsiConsole.Write(new Markup($"No longer tracking [springgreen3]{process.Name}[/]'s\n"));
                    }

                    var inputAgain = AnsiConsole.Ask<string>("Do you want to edit this process again? ([springgreen3]y[/]/[red]n[/]): ");
                    if (inputAgain.ToLowerInvariant() != "n") continue;
                    Console.WriteLine();
                    break;
                }
            }
        }
    }

    private async Task DeleteProcessAsync()
    {
        Console.Clear();
        Console.WriteLine();
        AnsiConsole.Write(new Rule("[deepSkyBlue3]Delete Process[/]"));
        Console.WriteLine();

        while (true)
        {
            var inputPath = AnsiConsole.Ask<string>("Please input the [deepSkyBlue3]path or name[/] to the game/app or [red]back[/] to go back: ");
            if (inputPath.ToLowerInvariant() == "back")
                break;

            var process = _context.Processes.FirstOrDefault(x => x.Path == inputPath) ?? _context.Processes.FirstOrDefault(x => x.Name == inputPath);
            if (process is null)
            {
                AnsiConsole.Markup("[red]No application found with that name or path[/]\n\n");
            }
            else
            {
                _context.Processes.Remove(process);
                await _context.SaveChangesAsync();
                AnsiConsole.Write(new Markup($"Successfully deleted [springgreen3]{process.Name}[/] from the list.\n"));
            }

            var inputAgain = AnsiConsole.Ask<string>("Do you want to delete another app? ([springgreen3]y[/]/[red]n[/]): ");
            if (inputAgain.ToLowerInvariant() == "n")
                break;
            Console.WriteLine();
        }
    }

    private void ListProcesses()
    {
        Console.Clear();
        Console.WriteLine();
        AnsiConsole.Write(new Rule("[deepSkyBlue3]List Processes[/]"));
        Console.WriteLine();

        var processes = _context.Processes.ToList();
        if (!processes.Any())
        {
            AnsiConsole.Markup("[red]No processes in list[/]\n");
            Console.WriteLine("Press any key to go back...");
            Console.ReadKey();
            return;
        }

        var table = new Table { Border = TableBorder.Rounded };
        table.BorderColor(Color.DeepSkyBlue3);
        table.AddColumns("Name", "Path", "Hours Ran", "Tracking");
        foreach (var process in processes)
            table.AddRow(process.Name ?? "NA", process.Path ?? "NA", process.HoursRan.ToString(), process.Tracking.ToString());
        AnsiConsole.Write(table);
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private bool HideApp()
    {
        Console.Clear();
        Console.WriteLine();
        AnsiConsole.Write(new Rule("[deepSkyBlue3]Hide Application[/]"));
        Console.WriteLine();
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        if (!isWindows)
        {
            AnsiConsole.Markup("[red]This feature only works on Windows.[/]\n");
            return false;
        }

        AnsiConsole.Markup("[red]WARNING:[/] This will hide the console window from the system bar. " +
                           "To close the app you must kill tracker.exe in the task manager\n");
        var positive = AnsiConsole.Ask<string>("Are you sure you want to hide the application? ([springgreen3]y[/]/[red]n[/]):");
        if (positive.ToLowerInvariant() != "y")
            return false;

        var window = Pinvoke.Pinvoke.GetConsoleWindow();
        Pinvoke.Pinvoke.ShowWindow(window, 0);
        return true;
    }

    private async Task BlacklistMenu()
    {
        var loop = true;
        while (loop)
        {
            Console.Clear();
            Console.WriteLine();
            var table = new Table { Border = TableBorder.Rounded };
            table.Title("[white]Welcome to[/] [springgreen3]GameTracker[/][white]![/]");
            table.BorderColor(Color.DeepSkyBlue3);
            table.AddColumn("Options");
            table.AddColumn("Descriptions");
            for (var i = 0; i < _blOptions.Length; i++)
            {
                var option = _blOptions[i];
                var split = option.Split('%');
                var title = i + 1 + ". " + split[0];
                var desc = split[1];
                table.AddRow(title, desc);
            }

            table.Caption("[white]To select one of the [/][deepSkyBlue3]options[/][white], enter it below[/]");
            AnsiConsole.Write(table);
            Console.WriteLine();

            var optionHint = "(";
            for (var i = 0; i < _blOptions.Length - 1; i++)
                optionHint += i + 1 + ", ";
            optionHint += $"{_blOptions.Length})";

            int selectedOption;
            while (true)
            {
                AnsiConsole.Cursor.Show();
                selectedOption = AnsiConsole.Ask<int>($"Which [deepSkyBlue3]option[/] do you want to select {optionHint}:");
                if (selectedOption < 1 || selectedOption > _blOptions.Length)
                    AnsiConsole.Markup("[red]Invalid input[/]\n");
                else
                    break;
            }

            switch (selectedOption)
            {
                case 1:
                    await AddBlacklistProcessAsync();
                    break;
                case 2:
                    await DeleteBlacklistProcessAsync();
                    break;
                case 3:
                    ListBlacklistProcesses();
                    break;
                case 4:
                    loop = false;
                    break;
            }
        }
    }

    private async Task AddBlacklistProcessAsync()
    {
        Console.Clear();
        Console.WriteLine();
        AnsiConsole.Write(new Rule("[deepSkyBlue3]Add Blacklist Process[/]"));
        Console.WriteLine();

        while (true)
        {
            var inputPath = AnsiConsole.Ask<string>("Please input the [deepSkyBlue3]path[/] to the game/app or [red]back[/] to go back: ");
            if (inputPath.ToLowerInvariant() == "back")
                break;

            if (!File.Exists(inputPath))
            {
                AnsiConsole.Markup("[red]Invalid input.[/] Please make sure you input the full path, including the application.\n\n");
                continue;
            }

            if (_context.Blacklists.Any(x => x.Path == inputPath))
            {
                AnsiConsole.Markup("[red]This application is already in the blacklist.[/]\n\n");
                continue;
            }

            var gameName = inputPath.Split('\\').Last();
            if (string.IsNullOrEmpty(gameName))
                gameName = inputPath.Split('/').Last();
            if (string.IsNullOrEmpty(gameName))
                gameName = inputPath;

            if (_context.Processes.Any(x => x.Path == inputPath))
            {
                AnsiConsole.Markup("[red]This application is in the tracking list.[/] Deleting application...\n");
                var process = _context.Processes.FirstOrDefault(x => x.Path == inputPath);
                if (process is null)
                {
                    AnsiConsole.Markup("[red]Failed to delete application.[/]\n\n");
                    continue;
                }

                _context.Processes.Remove(process);
                await _context.SaveChangesAsync();
                AnsiConsole.Write(new Markup($"Successfully deleted [springgreen3]{process.Name}[/] from the list.\n"));
            }

            _context.Blacklists.Add(new Blacklist { Path = inputPath, Name = gameName });
            await _context.SaveChangesAsync();
            AnsiConsole.Write(new Markup($"Successfully added [springgreen3]{gameName}[/] to the blacklist.\n"));

            var inputAgain = AnsiConsole.Ask<string>("Do you want to add another app? ([springgreen3]y[/]/[red]n[/]): ");
            if (inputAgain.ToLowerInvariant() == "n")
                break;
            Console.WriteLine();
        }
    }

    private async Task DeleteBlacklistProcessAsync()
    {
        Console.Clear();
        Console.WriteLine();
        AnsiConsole.Write(new Rule("[deepSkyBlue3]Delete Blacklist Process[/]"));
        Console.WriteLine();

        while (true)
        {
            var inputPath = AnsiConsole.Ask<string>("Please input the [deepSkyBlue3]path or name[/] to the game/app or [red]back[/] to go back: ");
            if (inputPath.ToLowerInvariant() == "back")
                break;

            var process = _context.Blacklists.FirstOrDefault(x => x.Path == inputPath) ?? _context.Blacklists.FirstOrDefault(x => x.Name == inputPath);
            if (process is null)
            {
                AnsiConsole.Markup("[red]No application found with that name or path[/]\n\n");
            }
            else
            {
                _context.Blacklists.Remove(process);
                await _context.SaveChangesAsync();
                AnsiConsole.Write(new Markup($"Successfully deleted [springgreen3]{process.Name}[/] from the blacklist.\n"));
            }

            var inputAgain = AnsiConsole.Ask<string>("Do you want to delete another app from the blacklist? ([springgreen3]y[/]/[red]n[/]): ");
            if (inputAgain.ToLowerInvariant() == "n")
                break;
            Console.WriteLine();
        }
    }

    private void ListBlacklistProcesses()
    {
        Console.Clear();
        Console.WriteLine();
        AnsiConsole.Write(new Rule("[deepSkyBlue3]List Blacklist Processes[/]"));
        Console.WriteLine();

        var processes = _context.Blacklists.ToList();
        if (!processes.Any())
        {
            AnsiConsole.Markup("[red]No processes in blacklist[/]\n");
            Console.WriteLine("Press any key to go back...");
            Console.ReadKey();
            return;
        }

        var table = new Table { Border = TableBorder.Rounded };
        table.BorderColor(Color.DeepSkyBlue3);
        table.AddColumns("Name", "Path");
        foreach (var process in processes)
            table.AddRow(process.Name ?? "NA", process.Path ?? "NA");
        AnsiConsole.Write(table);
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }
}
