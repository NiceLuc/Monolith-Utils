using GVLinQOptimizer.CodeGeneration;
using GVLinQOptimizer.CodeGeneration.Engine;
using MediatR;

namespace GVLinQOptimizer.Programs;

public sealed class CreateUnitTests
{
    public class Request : IRequest<string>
    {
        public string SettingsFilePath { get; set; }
        public string OutputDirectory { get; set; }
        public string MethodName { get; init; }
    }

    public class Handler(
        IContextDefinitionSerializer definitionSerializer,
        ITemplateEngine templateEngine,
        IRendererProvider<ContextDefinition> provider)
        : IRequestHandler<Request, string>
    {
        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(request.OutputDirectory))
                Directory.CreateDirectory(request.OutputDirectory);

            var definition = await definitionSerializer.DeserializeAsync(request.SettingsFilePath, cancellationToken);
            if (!string.IsNullOrEmpty(request.MethodName))
                FilterMethods(definition, request.MethodName);

            await ProcessTemplate("TestUtils");
            // await ProcessTemplate("UnitTests");

            return request.OutputDirectory;

            async Task ProcessTemplate(string rendererKey)
            {
                var renderer = provider.GetRenderer(rendererKey);
                var generatedCode = await renderer.RenderAsync(templateEngine, definition, cancellationToken);
                var fileName = string.Format(renderer.FileNameFormat, definition.ContextName);
                var filePath = Path.Combine(request.OutputDirectory, fileName);
                await File.WriteAllTextAsync(filePath, generatedCode, cancellationToken);
            }
        }

        private void FilterMethods(ContextDefinition definition, string methodName)
        {
            var method = definition.RepositoryMethods.FirstOrDefault(m =>
                m.MethodName.Equals(methodName, StringComparison.InvariantCultureIgnoreCase));

            if (method == null)
                throw new InvalidOperationException($"Method '{methodName}' not found.");

            definition.RepositoryMethods = [method];
        }

        private void GenerateTestClass(string MetaFile)
        {

            var sr = File.OpenText(MetaFile);

            var RepoFile = "C:\\Temp\\" + Path.GetFileName(MetaFile).Replace("_MetaFile.csv", "") + "_TestClass" +
                           ".cs"; //+ Guid.NewGuid().ToString() 
            var sw = File.CreateText(RepoFile);

            var ResultSetFolder = "C:\\Temp\\" + Path.GetFileName(MetaFile).Replace("_MetaFile.csv", "") +
                                  "_ResultSet";


            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] Metadata = line.Split(",");
                var spName = Metadata[0];
                var MethodName = Metadata[0].Replace("dob.", "");
                var Type = Metadata[1];
                var Parameters = Metadata[2].Replace("[", "").Replace("]", "");


                Console.WriteLine("[TestMethod]");
                sw.WriteLine("[TestMethod]");

                Console.WriteLine("public void " + MethodName + "_HappyPath()");
                sw.WriteLine("public void " + MethodName + "_HappyPath()");

                Console.WriteLine("{");
                sw.WriteLine("{");

                Console.WriteLine();
                sw.WriteLine();
                Console.WriteLine((char) 9 + "// Arrange");
                sw.WriteLine((char) 9 + "// Arrange");

                List<string> ResultSet = new List<string>();
                var FirstSet = "";
                var SecondSet = "";
                var ThirdSet = "";
                if (Type == "Query")
                {

                    ResultSet = GetResultSetFromFile(ResultSetFolder, spName);
                    //Need to perform this check in future to differentiate between "IEnumerable" and "Single Object"
                    //if (ResultSet.Count > 1)
                    //{
                    //MethodType = "IEnumerable<" + spName.Replace("dbo.", "") + "Result>";
                    Console.WriteLine((char) 9 + "var expectedResults = Enumerable.Range(1, 10).Select(x =>");
                    sw.WriteLine((char) 9 + "var expectedResults = Enumerable.Range(1, 10).Select(x =>");

                    Console.WriteLine((char) 9 + "new " + spName.Replace("dbo.", ""));
                    sw.WriteLine((char) 9 + "new " + spName.Replace("dbo.", ""));

                    Console.WriteLine((char) 9 + "{");
                    sw.WriteLine((char) 9 + "{");




                    foreach (var result in ResultSet)
                    {
                        if (result.ToLower().Contains("int"))
                        {
                            FirstSet += (char) 9 + " " + (Char) 9 + result.Split(":")[1] + " = x,\n";
                        }
                        else if (result.ToLower().Contains("long"))
                        {
                            FirstSet += (char) 9 + " " + (Char) 9 + result.Split(":")[1] + " = x,\n";
                        }
                        else
                        {
                            FirstSet += (char) 9 + " " + (Char) 9 + result.Split(":")[1] + " = $" + (char) 34 +
                                        result.Split(":")[1] + "{x}" + (Char) 34 + ",\n";
                        }

                        SecondSet += (char) 9 + " " + (Char) 9 + "q.SetupValue(" + (Char) 34 +
                                     result.Split(":")[1] + (char) 34 + " , model." + result.Split(":")[1] + ");\n";
                        ThirdSet += (char) 9 + " " + (Char) 9 + "Assert.AreEqual(expected." + result.Split(":")[1] +
                                    ", actual." + result.Split(":")[1] + ");\n";
                    }

                    Console.WriteLine(FirstSet.Substring(0, FirstSet.Length - 3));
                    sw.WriteLine(FirstSet.Substring(0, FirstSet.Length - 3));

                    Console.WriteLine((char) 9 + "}).ToArray();\n" + (char) 9 +
                                      "_query.ConfigureReads(expectedResults.Length, (q, x) =>\n" + (char) 9 +
                                      "{\n" + (char) 9 + "" + (char) 9 + "var model = expectedResults[x];");
                    sw.WriteLine((char) 9 + "}).ToArray();\n" + (char) 9 +
                                 "_query.ConfigureReads(expectedResults.Length, (q, x) =>\n" + (char) 9 + "{\n" +
                                 (char) 9 + "" + (char) 9 + "var model = expectedResults[x];");


                    Console.WriteLine(SecondSet.Substring(0, SecondSet.Length - 2));
                    sw.WriteLine(SecondSet.Substring(0, SecondSet.Length - 2));

                    Console.WriteLine((char) 9 + "});");
                    sw.WriteLine((char) 9 + "});");

                    //}
                    //else if (ResultSet.Count == 1)
                    //{
                    //    //MethodType = ResultSet.First().Split(":")[0].Replace("?", "");
                    //}
                }

                var Temp = "";
                var Temp1 = "";
                foreach (var Parameter in Parameters.Split("~"))
                {
                    string[] Param = Parameter.Split(":");

                    var curLine = ("var " + Param[0] + "=");
                    Temp += Param[0] + ",";
                    if (Param[1].ToLower().Contains("int"))
                    {
                        curLine += "1;";
                        Temp1 += "1,";
                    }
                    else if (Param[1].ToLower().Contains("date"))
                    {
                        curLine += "Guid.NewGuid()";
                        Temp1 += curLine += "Guid.NewGuid()";
                    }
                    else if (Param[1].ToLower().Contains("uniqueidentifier"))
                    {
                        curLine += (Char) 34 + "01/01/2024" + (Char) 34;
                        Temp1 += (Char) 34 + "01/01/2024" + (Char) 34;
                    }
                    else if (Param[1].ToLower() == "bit")
                    {
                        curLine += "true;";
                        Temp1 += "true,";

                    }
                    else if (Param[1].ToLower().Contains("varchar"))
                    {
                        curLine += (Char) 34 + "Dummy Text" + (Char) 34 + ";";
                        Temp1 += (Char) 34 + "Dummy Text" + (Char) 34 + ",";

                    }

                    Console.WriteLine((char) 9 + curLine);
                    sw.WriteLine((char) 9 + curLine);
                }


                Console.WriteLine((char) 9 + "_query.ConfigureNonQuery();" + Environment.NewLine + (char) 9 +
                                  "var repository = CreateRepository();");
                sw.WriteLine((char) 9 + "_query.ConfigureNonQuery();" + Environment.NewLine + (char) 9 +
                             "var repository = CreateRepository();");


                Console.WriteLine();
                sw.WriteLine();
                Console.WriteLine((char) 9 + "// Act");
                sw.WriteLine((char) 9 + "// Act");


                Console.WriteLine();
                sw.WriteLine();
                if (Type == "NonQuery")
                {
                    Console.WriteLine((char) 9 + "var results = repository." + MethodName + "(" +
                                      Temp.Substring(0, Temp.Length - 1) + ").ToList();");
                    sw.WriteLine((char) 9 + "var results = repository." + MethodName + "(" +
                                 Temp.Substring(0, Temp.Length - 1) + ").ToList();");

                    Console.WriteLine((char) 9 + "// Assert" + Environment.NewLine + (char) 9 +
                                      "_query.VerifySet(q => q.CommandType = CommandType.StoredProcedure);");
                    sw.WriteLine((char) 9 + "// Assert" + Environment.NewLine + (char) 9 +
                                 "_query.VerifySet(q => q.CommandType = CommandType.StoredProcedure);");

                    Console.WriteLine((char) 9 + "_query.VerifySet(q => q.SQL = " + (char) 34 + "dbo." +
                                      MethodName + (char) 34 + ");");
                    sw.WriteLine((char) 9 + "_query.VerifySet(q => q.SQL = " + (char) 34 + "dbo." + MethodName +
                                 (char) 34 + ");");
                }
                else if (Type == "Query")
                {
                    Console.WriteLine((char) 9 + "repository." + MethodName + "(" +
                                      Temp.Substring(0, Temp.Length - 1) + ");");
                    sw.WriteLine((char) 9 + "repository." + MethodName + "(" + Temp.Substring(0, Temp.Length - 1) +
                                 ");");

                    Console.WriteLine((char) 9 + "// Assert" + Environment.NewLine + (char) 9 +
                                      "Assert.AreEqual(expectedResults.Length, results.Count);\n" + (char) 9 +
                                      "for (var x = 0; x < expectedResults.Length; x++)\n" + (char) 9 + "{\n" +
                                      (char) 9 + " " + (char) 9 + "var expected = expectedResults[x];\n" +
                                      (char) 9 + " " + (char) 9 + "var actual = results[x];\n");
                    sw.WriteLine((char) 9 + "// Assert" + Environment.NewLine + (char) 9 +
                                 "Assert.AreEqual(expectedResults.Length, results.Count);\n" + (char) 9 +
                                 "for (var x = 0; x < expectedResults.Length; x++)\n" + (char) 9 + "{\n" +
                                 (char) 9 + " " + (char) 9 + "var expected = expectedResults[x];\n" + (char) 9 +
                                 " " + (char) 9 + "var actual = results[x];\n");

                    Console.WriteLine(ThirdSet + (char) 9 + "}");
                    sw.WriteLine(ThirdSet + (char) 9 + "}");
                }


                Console.WriteLine((char) 9 + "ValidateMocks();" + Environment.NewLine + "}");
                sw.WriteLine((char) 9 + "ValidateMocks();" + Environment.NewLine + "}");


                //Writing Exception Cases
                Console.WriteLine();
                sw.WriteLine();
                Console.WriteLine("[TestMethod]");
                sw.WriteLine("[TestMethod]");

                Console.WriteLine("public void " + MethodName + "_LogsException()");
                sw.WriteLine("public void " + MethodName + "_LogsException()");

                Console.WriteLine("{");
                sw.WriteLine("{");

                Console.WriteLine((char) 9 + "// Arrange");
                sw.WriteLine((char) 9 + "// Arrange");

                Console.WriteLine((char) 9 + "var exception = new Exception(expectedExceptionMessage);");
                sw.WriteLine((char) 9 + "var exception = new Exception(expectedExceptionMessage);");

                Console.WriteLine((char) 9 + "_query.ConfigureNonQuery(() => throw exception); ");
                sw.WriteLine((char) 9 + "_query.ConfigureNonQuery(() => throw exception); ");

                Console.WriteLine((char) 9 + "var repository = CreateRepository(); ");
                sw.WriteLine((char) 9 + "var repository = CreateRepository(); ");

                Console.WriteLine();
                sw.WriteLine();

                Console.WriteLine((char) 9 + "// Act");
                sw.WriteLine((char) 9 + "// Act");
                Console.WriteLine((char) 9 +
                                  "var result = Assert.ThrowsException<Exception>(() => repository.WFI_UpdateRuleNextrun(" +
                                  MethodName + "(" + Temp1.Substring(0, Temp1.Length - 1) + ")));");
                sw.WriteLine((char) 9 +
                             "var result = Assert.ThrowsException<Exception>(() => repository.WFI_UpdateRuleNextrun(" +
                             MethodName + "(" + Temp1.Substring(0, Temp1.Length - 1) + ")));");

                Console.WriteLine();
                sw.WriteLine();
                Console.WriteLine((char) 9 + "// Assert" + Environment.NewLine + (char) 9 +
                                  "Assert.AreSame(exception, result);" + Environment.NewLine + (char) 9 +
                                  "ValidateMocks();");
                sw.WriteLine((char) 9 + "// Assert" + Environment.NewLine + (char) 9 +
                             "Assert.AreSame(exception, result);" + Environment.NewLine + (char) 9 +
                             "ValidateMocks();");




                Console.WriteLine("}");
                sw.WriteLine("}");



                Console.WriteLine();
                sw.WriteLine();
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
