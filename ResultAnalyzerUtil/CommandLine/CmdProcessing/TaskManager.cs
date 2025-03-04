namespace ResultAnalyzerUtil.CommandLine;
public class TaskManager
{
    private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
    protected static double CurrentWeight;
    protected static double MaxWeight;

    static TaskManager()
    {
        CurrentWeight = 0;
        MaxWeight = 1;

        try
        {
            var dir = Path.GetFullPath(System.Reflection.Assembly.GetEntryAssembly()?.Location);
            if (dir.Contains("Nic"))
            {
                MaxWeight = 1.5;
                Console.WriteLine($"Detected Nic's Computer: Max Weight = {MaxWeight}");
            }
            else if (dir.Contains("Artemis"))
            {
                MaxWeight = 1;
                Console.WriteLine($"Detected Artemis: Max Weight = {MaxWeight}");
            }
            else if (dir.Contains("Smith Lab")) // Beefy Boi
            {
                MaxWeight = 2.5;
                Console.WriteLine($"Detected Beefy Boi: Max Weight = {MaxWeight}");
            }
            else if (dir.Contains("Michael Shortreed"))
            {
                MaxWeight = 2;
                Console.WriteLine($"Detected Shortreed Old: Max Weight = {MaxWeight}");
            }
            else if (dir.Contains("trish")) // Beefy Boi
            {
                MaxWeight = 2.5;
                Console.WriteLine($"Detected Shortreed New: Max Weight = {MaxWeight}");
            }
            else
                Console.WriteLine($"Unknown Computer: Default Max Weight = {MaxWeight}");
        }
        catch
        {
            // ignore
        }
    }

    public async Task RunProcesses(List<CmdProcess> processes)
    {
        List<Task> runningTasks = new List<Task>();

        foreach (var process in processes)
        {
            var task = RunProcess(process);
            runningTasks.Add(task);

            // Wait for any task to complete if current weight exceeds the limit
            while (CurrentWeight >= MaxWeight)
            {
                var completedTask = await Task.WhenAny(runningTasks);
                runningTasks.Remove(completedTask);
            }
        }

        // Wait for all remaining tasks to complete
        await Task.WhenAll(runningTasks);
    }

    private async Task RunProcess(CmdProcess process)
    {
        string dependencyResult = null;

        if (process.Dependency != null)
        {
            dependencyResult = await process.Dependency.Task;
        }

        // Manages the ability to run on multiple computers at once
        // if already finished, return
        // if still running, wait until finish and return
        if (process.HasStarted())
        {
            if (process.IsCompleted())
            {
                Console.WriteLine($"Previous Completion Detected: {process.SummaryText}");

                // TODO: add functionality to determine if a task has others depending on it
                if (process is MetaMorpheusGptmdSearchCmdProcess { IsLibraryCreator: true } imm)
                {
                    var filePath = Directory.GetFiles(imm.OutputDirectory, "*.msp", SearchOption.AllDirectories).FirstOrDefault();
                    if (filePath != null)
                        process.CompletionSource.SetResult(filePath);
                }
            }
            else
                Console.WriteLine($"Has Started Elsewhere: {process.SummaryText}");

            while (!process.IsCompleted())
            {
                await Task.Delay(100000); // Adjust delay as needed
            }

            return;
        }

        while (true)
        {
            await semaphore.WaitAsync();

            if (CurrentWeight + process.Weight <= MaxWeight)
            {
                CurrentWeight += process.Weight;
                semaphore.Release();
                break;
            }

            semaphore.Release();
            await Task.Delay(100000); // Adjust delay as needed
        }

        try
        {
            // Simulate running the command and generate a file path
            Console.WriteLine($"Starting process: {process.SummaryText}");

            // Use the dependency result if it exists
            if (!string.IsNullOrEmpty(dependencyResult))
            {
                //var result = string.Join("\\", dependencyResult.Split('\\').TakeLast(3));
                var result = Path.GetFileName(dependencyResult);
                Console.WriteLine($"\tUsing dependency result: {result} in {process.QuickName}");
            }

            if (!process.IsCmdTask)
            {
                await process.RunTask();
            }
            else
            {
                var proc = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = process.ProgramExe,
                        Arguments = process.Prompt,
                        UseShellExecute = true,
                        CreateNoWindow = false,
                        WorkingDirectory = process.WorkingDirectory,
                        ErrorDialog = true,
                    }
                };
                proc.Start();
                await proc.WaitForExitAsync();
            }

            Console.WriteLine($"Completed process: {process.SummaryText}");
        }
        finally
        {
            await semaphore.WaitAsync();
            CurrentWeight -= process.Weight;
            semaphore.Release();
        }
    }

}