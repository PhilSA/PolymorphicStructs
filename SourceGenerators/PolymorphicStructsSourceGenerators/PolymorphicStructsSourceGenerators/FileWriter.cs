using System;
using System.Collections.Generic;
using System.Text;

namespace PolymorphicStructsSourceGenerators
{
    internal class FileWriter
    {
        public string FileContents = "";
        private int _indentLevel = 0;
        private bool _hasNamespace = false;

        public void WriteLine(string line)
        {
            FileContents += GetIndentString() + line + "\n";
        }

        public void WriteUsings(List<string> usings)
        {
            // Remove duplicates
            List<string> filteredUsings = new List<string>();
            foreach (var item in usings)
            {
                if (string.IsNullOrEmpty(item) || string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }

                if (filteredUsings.Contains(item))
                {
                    continue;
                }

                filteredUsings.Add(item);
            }

            foreach (var item in filteredUsings)
            {
                WriteLine("using " + item + ";");
            }
        }

        public void BeginScope()
        {
            WriteLine("{");
            _indentLevel++;
        }

        public void EndScope(string suffix = "")
        {
            _indentLevel--;
            WriteLine("}" + suffix);
        }

        public void BeginNamespace(string namespaceName)
        {
            if (!string.IsNullOrEmpty(namespaceName))
            {
                _hasNamespace = true;
                WriteLine("namespace " + namespaceName);
                BeginScope();
            }
        }

        public void EndNamespace()
        {
            if (_hasNamespace)
            {
                EndScope();
            }
        }

        private string GetIndentString()
        {
            string indentation = "";
            for (int i = 0; i < _indentLevel; i++)
            {
                indentation += "\t";
            }
            return indentation;
        }
    }
}
