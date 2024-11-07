﻿using Readers;

namespace Analyzer.Util
{
    public class WrappedFile<T> where T : IResultFile
    {
        private T? _file { get; set; }
        private Func<T> CreateFile { get; init; }
        public string FilePath { get; init; }
        public T File => _file ??= CreateFile();

        public WrappedFile(string path, Func<T> creationFunction)
        {
            FilePath = path;
            CreateFile = creationFunction;
        }
    }
}
