using System.Text.RegularExpressions;
using MediatR;

namespace GVLinQOptimizer.Programs;

public sealed class Initialize
{
    public class Request : IRequest
    {
        public string DesignerFilePath { get; set; }
        public string SettingsFilePath { get; set; }
        public bool ForceOverwrite { get; set; }
    }

    public class Handler : IRequestHandler<Request>
    {
        private static readonly Regex _classRegex = new(
            @"public partial class (?<class_name>.+)", 
            RegexOptions.Singleline);

        private static readonly Regex _sprocRegex = new Regex(
            @"FunctionAttribute\(Name\=""(?<sproc_name>.+)""", 
            RegexOptions.Singleline);

        private static readonly Regex _methodRegex = new Regex(
            @"public (ISingleResult\<(?<return_type>.+?)\>|(?<return_type>.+?))\s(?<method_name>.+?)\(", 
            RegexOptions.Singleline);

        private static readonly Regex _parameterRegex = new Regex(
            @"ParameterAttribute\(Name\=""(?<db_name>.+?)"",\sDbType\=""(?<db_type>.+?)"".+?]\s(?<ref_token>ref\s)?(?<net_type>.+?)\s(?<net_name>.+?)[,\)]",
            RegexOptions.Singleline);

        private static readonly Regex _charLengthRegex = new Regex(
            @"nvarchar\((?<db_length>.+?)\)", 
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex _nullableRegex = new Regex(
            @"Nullable\<(?<nullable_type>.+?)\>", 
            RegexOptions.Singleline);

        public Task Handle(Request request, CancellationToken cancellationToken)
        {
            // c:\Temp\*.designer.cs
            if (!File.Exists(request.DesignerFilePath))
                throw new FileNotFoundException($"File not found: {request.DesignerFilePath}");

            // c:\Temp\*_MetaFile.csv
            if (File.Exists(request.SettingsFilePath) && !request.ForceOverwrite)
                throw new InvalidOperationException(
                    $"File already exists: {request.SettingsFilePath} (use -f to overwrite)");

            var fileName = CreateMetaFile(request);

            return Task.CompletedTask;
        }

        private string CreateMetaFile(Request request)
        {
            var definition = new ContextDefinition
            {
                ContextName = Path.GetFileNameWithoutExtension(request.DesignerFilePath)
                    .Replace(".designer", "")
            };

            var sr = File.OpenText(request.DesignerFilePath);

            while (sr.ReadLine() is { } line)
            {
                var match = _sprocRegex.Match(line);
                if (match.Success)
                {
                    var method = new MethodDefinition
                    {
                        DatabaseName = match.Groups["sproc_name"].Value
                    };

                    // extract all details about the method (including parameters)
                    FillMethodDefinition(method, sr);

                    definition.Methods.Add(method);

                    //Get ResultSet for "Query" Type
                    if (Type == "Query")
                    {
                        GenerateResultSetFile(FuncName.Replace("dbo.", "") + "Result", request.DesignerFilePath);
                    }

                    continue;
                }

                match = _classRegex.Match(line);
                if (match.Success)
                {
                    if (line.Contains("Linq.DataContext"))
                        continue; // we don't want the data context class definition

                    var type = new TypeDefinition
                    {
                        ClassName = match.Groups["class_name"].Value,
                    };

                    definition.Types.Add(type);
                }

                // new up a method definition and get the stored procedure name
            }

            var sw = File.CreateText(request.SettingsFilePath);
            sw.Close();
            sw.Dispose();

            sr.Close();
            sr.Dispose();
            Console.WriteLine();
            Console.WriteLine("MetaFile Generated..." + request.SettingsFilePath);
            return request.SettingsFilePath;
        }

        private static void FillMethodDefinition(MethodDefinition method, StreamReader sr)
        {
            // the details about the method are on the next line (including parameters)
            var methodLine = sr.ReadLine();
            if (string.IsNullOrEmpty(methodLine))
                throw new InvalidOperationException($"Unexpected end of file after {method.DatabaseName}");

            var match = _methodRegex.Match(methodLine);
            if (!match.Success)
                throw new InvalidOperationException($"Unable to parse method definition for '{method.DatabaseName}'");

            if (methodLine.Contains("ISingleResult"))
            {
                method.DatabaseType = "Query";

                // todo: determine T vs. IEnumerable<T> for IsList
                method.IsList = true;
            }
            else
            {
                method.DatabaseType = "NonQuery";
            }

            // extract all parameters from the method line
            var parameterMatches = _parameterRegex.Matches(methodLine);
            foreach (Match parameterMatch in parameterMatches)
            {
                var parameterDefinition = CreateParameterDefinition(parameterMatch);
                method.Parameters.Add(parameterDefinition);
            }
        }

        private static ParameterDefinition CreateParameterDefinition(Match parameterMatch)
        {
            var parameterDefinition = new ParameterDefinition
            {
                DatabaseName = parameterMatch.Groups["db_name"].Value,
                DatabaseType = parameterMatch.Groups["db_type"].Value,
                CodeName = parameterMatch.Groups["net_name"].Value,
                CodeType = parameterMatch.Groups["net_type"].Value,
                IsRef = parameterMatch.Groups["ref_token"].Success
            };

            var stringMatch = _charLengthRegex.Match(parameterDefinition.DatabaseType);
            if (stringMatch.Success)
            {
                parameterDefinition.DatabaseType = "nvarchar";
                parameterDefinition.DatabaseLength = stringMatch.Groups["db_length"].Value;
            }

            var nullableMatch = _nullableRegex.Match(parameterDefinition.CodeType);
            if (nullableMatch.Success)
            {
                parameterDefinition.CodeType = nullableMatch.Groups["nullable_type"].Value + "?";
            }

            return parameterDefinition;
        }


        private void GenerateResultSetFile(string ResultsetName, string SrcFile)
        {
            if (!Directory.Exists("C:\\Temp\\" + Path.GetFileName(SrcFile).Replace(".designer.cs", "") + "_ResultSet"))
            {
                Directory.CreateDirectory("C:\\Temp\\" + Path.GetFileName(SrcFile).Replace(".designer.cs", "") +
                                          "_ResultSet");

            }

            var OPFile = "C:\\Temp\\" + Path.GetFileName(SrcFile).Replace(".designer.cs", "") + "_ResultSet\\" +
                         ResultsetName + ".csv"; //+ Guid.NewGuid().ToString() 
            var sw = File.CreateText(OPFile);



            var sr = File.OpenText(SrcFile);
            string line;
            string line1;
            var StartExtract = false;
            var StopExtract = false;

            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains(ResultsetName + "()"))
                {
                    break;
                }

                if (StartExtract == true)
                {
                    if (line.Replace("\t{", "").Trim() != "")
                    {
                        var ResultSetCols = line.Split(" ");
                        Console.WriteLine(ResultSetCols[2].ToString().Substring(1, ResultSetCols[2].Length - 2) + ":" +
                                          ResultSetCols[1].ToString());
                        sw.WriteLine(ResultSetCols[2].ToString().Substring(1, ResultSetCols[2].Length - 2) + ":" +
                                     ResultSetCols[1].ToString());

                    }
                }

                if (line.Contains("class " + ResultsetName)) StartExtract = true;


            }

            sw.Close();
            sw.Dispose();

            sr.Close();
            sr.Dispose();

        }

    }
}