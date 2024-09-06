using MediatR;
using Mustache;

namespace GVLinQOptimizer.Programs;

public sealed class CreateRepository 
{
    public class Request : IRequest<string>
    {
        public string SettingsFilePath { get; set; }
        public string OutputDirectory { get; set; }
    }

    public class Handler : IRequestHandler<Request, string>
    {
        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            var definition = await Utils.LoadSettingsFileAsync(request.SettingsFilePath, cancellationToken);

            var template = await Utils.GetResourceAsync("IRepositorySettings.hbs", cancellationToken);
            var result = ProcessTemplate(template, definition);
            var filePath = Path.Combine(request.OutputDirectory, $"I{definition.ContextName}RepositorySettings.cs");
            await File.WriteAllTextAsync(filePath, result, cancellationToken);

            template = await Utils.GetResourceAsync("ContextRepositorySettings.hbs", cancellationToken);
            result = ProcessTemplate(template, definition);
            filePath = Path.Combine(request.OutputDirectory, $"{definition.ContextName}RepositorySettings.cs");
            await File.WriteAllTextAsync(filePath, result, cancellationToken);

            template = await Utils.GetResourceAsync("IRepository.hbs", cancellationToken);
            result = ProcessTemplate(template, definition);
            filePath = Path.Combine(request.OutputDirectory, $"I{definition.ContextName}Repository.cs");
            await File.WriteAllTextAsync(filePath, result, cancellationToken);

            template = await Utils.GetResourceAsync("ContextRepository.hbs", cancellationToken);
            result = ProcessTemplate(template, definition);
            filePath = Path.Combine(request.OutputDirectory, $"{definition.ContextName}Repository.cs");
            await File.WriteAllTextAsync(filePath, result, cancellationToken);

            return request.OutputDirectory;
        }

        private static string ProcessTemplate(string template, ContextDefinition definition)
        {
            var engine = new FormatCompiler();
            var generator = engine.Compile(template);
            var result = generator.Render(definition);
            return result;
        }

        private void GenerateRepoCode(string MetaFile)
        {
            var sr = File.OpenText(MetaFile);

            var RepoFile = "C:\\Temp\\" + Path.GetFileName(MetaFile).Replace("_MetaFile.csv", "") + "_Repo" +
                           ".cs"; //+ Guid.NewGuid().ToString() 
            var ResultSetFolder = "C:\\Temp\\" + Path.GetFileName(MetaFile).Replace("_MetaFile.csv", "") + "_ResultSet";

            var sw = File.CreateText(RepoFile);


            string line;
            List<string> ParamList = new List<string>();
            while ((line = sr.ReadLine()) != null)
            {
                ParamList.Clear();
                try
                {
                    string[] Metadata = line.Split(",");
                    var spName = Metadata[0];
                    var MethodName = Metadata[0].Replace("dbo.", "");
                    var Parameters = Metadata[2].Replace("[", "").Replace("]", "");

                    var FirstLine = "";
                    foreach (var Parameter in Parameters.Split("~"))
                    {
                        string[] Param = Parameter.Split(":");
                        FirstLine += "," + (Param[1].ToLower().Contains("varchar") ? "string" : Param[1].ToLower()) +
                                     " " + Param[0];
                        ParamList.Add("query.AddParameter(" + (Char) 34 + (char) 64 + Param[0] + (Char) 34 +
                                      ",SqlDbType." + (Param[1].ToLower().Contains("varchar") ? "nvarchar" : Param[1]) +
                                      "," + Param[0] + ")");

                        //Console.WriteLine(Parameter);
                    }

                    List<string> ResultSet = new List<string>();
                    var MethodType = "";
                    if (line.Contains("NonQuery"))
                    {
                        MethodType = "void";
                    }
                    else if (line.Contains(",Query,"))
                    {
                        ResultSet = GetResultSetFromFile(ResultSetFolder, spName);
                        if (ResultSet.Count > 1)
                        {
                            MethodType = "IEnumerable<" + spName.Replace("dbo.", "") + "Result>";

                        }
                        else if (ResultSet.Count == 1)
                        {
                            MethodType = ResultSet.First().Split(":")[0].Replace("?", "");
                        }

                    }

                    Console.WriteLine("public " + MethodType + " " + MethodName + "(" + FirstLine.Substring(1) + ")");
                    sw.WriteLine("public " + MethodType + " " + MethodName + "(" + FirstLine.Substring(1) + ")");

                    Console.WriteLine("{");
                    sw.WriteLine("{");



                    if (line.Contains(",Query,"))
                    {
                        var OutVariable = "";
                        if (MethodType == "int")
                        {
                            Console.WriteLine((char) 9 + "var Result = 0;");
                            sw.WriteLine((char) 9 + "var Result = 0;");
                        }
                        else if (MethodType == "datetime")
                        {
                            Console.WriteLine((char) 9 + "var Result = new DateTime();");
                            sw.WriteLine((char) 9 + "var Result = new DateTime();");
                        }
                        else if (MethodType == "string")
                        {
                            Console.WriteLine((char) 9 + "var Result = " + (char) 34 + (char) 34 + ";");
                            sw.WriteLine((char) 9 + "var Result = " + (char) 34 + (char) 34 + ";");
                        }
                        else if (MethodType == "long")
                        {
                            Console.WriteLine((char) 9 + "var Result = 0;");
                            sw.WriteLine((char) 9 + "var Result = 0;");
                        }
                        else
                        {
                            Console.WriteLine((char) 9 + "var Result = new List<" + spName.Replace("dbo.", "") +
                                              "Result>();");
                            sw.WriteLine(
                                (char) 9 + "var Result = new List<" + spName.Replace("dbo.", "") + "Result>();");
                        }
                    }

                    Console.WriteLine((char) 9 + "using (var query = CreateQuery())");
                    sw.WriteLine((char) 9 + "using (var query = CreateQuery())");

                    Console.WriteLine((char) 9 + "{");
                    sw.WriteLine((char) 9 + "{");

                    Console.WriteLine((char) 9 + "" + (char) 9 + "query.CommandType = CommandType.StoredProcedure;");
                    sw.WriteLine((char) 9 + "" + (char) 9 + "query.CommandType = CommandType.StoredProcedure;");

                    Console.WriteLine((char) 9 + "" + (char) 9 + "query.SQL = " + (char) 34 + spName + (char) 34 + ";");
                    sw.WriteLine((char) 9 + "" + (char) 9 + "query.SQL = " + (char) 34 + spName + (char) 34 + ";");


                    foreach (var item in ParamList)
                    {
                        Console.WriteLine((char) 9 + "" + (char) 9 + item + ";");
                        sw.WriteLine((char) 9 + "" + (char) 9 + item + ";");
                    }

                    if (line.Contains("NonQuery"))
                    {
                        Console.WriteLine((char) 9 + "" + (char) 9 + "return query.ExecuteNonQuery();");
                        sw.WriteLine((char) 9 + "" + (char) 9 + "return query.ExecuteNonQuery();");

                        Console.WriteLine((char) 9 + "}");
                        sw.WriteLine((char) 9 + "}");

                    }
                    else if (line.Contains(",Query,"))
                    {
                        if (ResultSet.Count > 1)
                        {
                            Console.WriteLine((char) 9 + "" + (char) 9 + "while (query.Read())" + Environment.NewLine +
                                              (char) 9 + "" + (char) 9 + "{");
                            sw.WriteLine((char) 9 + "" + (char) 9 + "while (query.Read())" + Environment.NewLine +
                                         (char) 9 + "" + (char) 9 + "{");

                            Console.WriteLine((char) 9 + "" + (char) 9 + "" + (char) 9 + "result.Add(new " +
                                              spName.Replace("dbo.", "") + Environment.NewLine + (char) 9 + "" +
                                              (char) 9 + "" + (char) 9 + "{");
                            sw.WriteLine((char) 9 + "" + (char) 9 + "" + (char) 9 + "result.Add(new " +
                                         spName.Replace("dbo.", "") + Environment.NewLine + (char) 9 + "" + (char) 9 +
                                         "" + (char) 9 + "{");

                            foreach (var Column in ResultSet)
                            {
                                Console.WriteLine((char) 9 + "" + (char) 9 + "" + (char) 9 + "" + (char) 9 +
                                                  Column.Split(":")[1] + " = query.Value(" + (char) 34 +
                                                  Column.Split(":")[1] + (char) 34 + ");");
                                sw.WriteLine((char) 9 + "" + (char) 9 + "" + (char) 9 + "" + (char) 9 +
                                             Column.Split(":")[1] + " = query.Value(" + (char) 34 +
                                             Column.Split(":")[1] + (char) 34 + ");");

                            }

                            Console.WriteLine((char) 9 + "" + (char) 9 + "" + (char) 9 + "});");
                            sw.WriteLine((char) 9 + "" + (char) 9 + "" + (char) 9 + "});");

                            Console.WriteLine((char) 9 + "" + (char) 9 + "}");
                            sw.WriteLine((char) 9 + "" + (char) 9 + "}");

                            Console.WriteLine((char) 9 + "}" + Environment.NewLine + (char) 9 +
                                              "return result.AsReadOnly();");
                            sw.WriteLine(
                                (char) 9 + "}" + Environment.NewLine + (char) 9 + "return result.AsReadOnly();");
                        }
                        else if (ResultSet.Count == 1)
                        {
                            Console.WriteLine((char) 9 + "" + (char) 9 + "if (query.Read())" + Environment.NewLine +
                                              (char) 9 + "" + (char) 9 + "{");
                            sw.WriteLine((char) 9 + "" + (char) 9 + "if (query.Read())" + Environment.NewLine + "" +
                                         (char) 9 + (char) 9 + "{");

                            Console.WriteLine((char) 9 + "" + (char) 9 + "" + (char) 9 + "result = query.Value(" +
                                              (char) 34 + ResultSet.First().Split(":")[1] + (char) 34 + ");");
                            sw.WriteLine((char) 9 + "" + (char) 9 + "" + (char) 9 + "result = query.Value(" +
                                         (char) 34 + ResultSet.First().Split(":")[1] + (char) 34 + ");");

                            Console.WriteLine((char) 9 + "" + (char) 9 + "}");
                            sw.WriteLine((char) 9 + "" + (char) 9 + "}");

                            Console.WriteLine((char) 9 + "}" + Environment.NewLine + (char) 9 + "return result;");
                            sw.WriteLine((char) 9 + "}" + Environment.NewLine + (char) 9 + "return result;");

                        }

                    }

                    Console.WriteLine("}");
                    sw.WriteLine("}");

                    Console.WriteLine("");
                    sw.WriteLine("");
                }
                catch (Exception)
                {
                    Console.WriteLine("Not Able to Get the value. Please convert Mannualy");
                    sw.WriteLine(
                        "Error: Not able to retrive column details From DB. Please convert Mannualy for this method: " +
                        line);
                    //throw;
                }
            }

            sw.Flush();
            sw.Close();
            sw.Dispose();

            sr.Close();
            sr.Dispose();
        }

        private List<string> GetResultSetFromFile(string FolderPath, string SPName)
        {
            var ResultSetFile =
                FolderPath + "\\" + SPName.Replace("dbo.", "") + "Result.csv"; //+ Guid.NewGuid().ToString() 

            var sr = File.OpenText(ResultSetFile);
            string line;
            List<string> ResultSet = new List<string>();

            while ((line = sr.ReadLine()) != null)
            {

                string[] ColumnDetails = line.Split(":");
                var ColName = ColumnDetails[0];
                var ColType = ColumnDetails[1].ToString().ToLower().Contains("system.datetime")
                    ? "DateTime"
                    : (ColumnDetails[1].ToString().Replace("System.Nullable<", "")
                        .Replace(">", "")); //.Contains("numeric") ? "long" : ColumnDetails[1].ToString());
                //bool IsNullable = (bool)rdr["is_nullable"];
                if (line.Contains("System.Nullable"))
                {
                    ColType = ColType + "?";
                }

                ResultSet.Add(ColType + ":" + ColName);
            }

            return ResultSet;

        }
    }
}