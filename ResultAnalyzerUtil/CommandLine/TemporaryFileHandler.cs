using Readers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResultAnalyzerUtil.CommandLine
{
    public abstract class TemporaryFileHandler
    {
        protected static readonly object FilePathLock = new();
        protected static readonly ConcurrentBag<string> ClaimedFilePaths = new();
        protected static readonly object FileCombiningLock = new();

        public static string GenerateUniqueFilePathThreadSafe(string filePath)
        {
            var extension = System.IO.Path.GetExtension(filePath);
            int index = 1;

            lock (FilePathLock)
            {
                var toInsert = $"({index})";
                while (File.Exists(filePath) || ClaimedFilePaths.Contains(filePath))
                {
                    var previous = toInsert;
                    toInsert = $"({index})";

                    // if first time needing to add an integer to filename
                    if (index != 1)
                    {
                        var lastInsertIndex = filePath.LastIndexOf(previous, StringComparison.Ordinal);
                        filePath = filePath.Remove(lastInsertIndex, previous.Length);
                    }

                    int indexToInsert = filePath.IndexOf(extension, StringComparison.InvariantCulture);
                    filePath = filePath.Insert(indexToInsert, toInsert);
                    index++;
                }
                ClaimedFilePaths.Add(filePath);
            }
            return filePath;
        }

        public static string GenerateUniqueFilePath(string filePath)
        {
            var extension = System.IO.Path.GetExtension(filePath);
            int index = 1;

            var toInsert = $"({index})";
            while (File.Exists(filePath))
            {
                var previous = toInsert;
                toInsert = $"({index})";

                // if first time needing to add an integer to filename
                if (index != 1)
                {
                    var lastInsertIndex = filePath.LastIndexOf(previous, StringComparison.Ordinal);
                    filePath = filePath.Remove(lastInsertIndex, previous.Length);
                }

                int indexToInsert = filePath.IndexOf(extension, StringComparison.InvariantCulture);
                filePath = filePath.Insert(indexToInsert, toInsert);
                index++;
            }

            return filePath;
        }
    }

    /// <summary>
    /// Class used for caching intermediate results. 
    /// Results are written to a temporary file and then combined into a final file when disposed.
    /// </summary>
    /// <typeparam name="TResultFile"></typeparam>
    /// <typeparam name="TResultType"></typeparam>
    internal class TemporaryFileHandler<TResultFile, TResultType> : TemporaryFileHandler, IDisposable
        where TResultFile : ResultFile<TResultType>, new()
    {
        private readonly ConcurrentBag<string> _temporaryFiles = new();
        private readonly string _finalOutPath;
        private readonly string _tempFilePathBase;

        public TemporaryFileHandler(string finalOutPath)
        {
            _finalOutPath = finalOutPath;
            _tempFilePathBase = GetTempPath(finalOutPath);
        }

        public void Add(IEnumerable<TResultType> results)
        {
            var tempFilePath = GenerateUniqueFilePathThreadSafe(_tempFilePathBase);
            var tempFile = new TResultFile
            {
                FilePath = tempFilePath,
                Results = results.ToList()
            };
            tempFile.WriteResults(tempFilePath);
            _temporaryFiles.Add(tempFilePath);
        }

        private void CombineToFinal()
        {
            lock (FileCombiningLock)
            {
                // check inside in case a thread was waiting at the lock
                if (_temporaryFiles.IsEmpty)
                    return;

                TResultFile finalFile = new TResultFile
                {
                    FilePath = _finalOutPath
                };

                // if final file exists, load it in and start there
                if (System.IO.File.Exists(_finalOutPath))
                {
                    finalFile.LoadResults();
                }

                // load in temp files and add to final file
                foreach (var file in _temporaryFiles)
                {
                    var tempFile = new TResultFile
                    {
                        FilePath = file
                    };
                    tempFile.LoadResults();
                    finalFile.Results.AddRange(tempFile.Results);
                }

                // write final file and clear temp files
                finalFile.WriteResults(_finalOutPath);
                _temporaryFiles.Clear();
            }
        }

        private string GetTempPath(string finalPath)
        {
            var extension = Path.GetExtension(finalPath);
            var tempPath = finalPath.Insert(finalPath.Length - extension.Length, "_temp");
            return tempPath;
        }


        public void Dispose()
        {
            CombineToFinal();

            // TODO: GC/finalizer suppression or something
        }
    }
}
