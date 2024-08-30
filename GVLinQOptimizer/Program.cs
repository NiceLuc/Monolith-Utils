//// See https://aka.ms/new-console-template for more information
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;


///
do
{
    Console.WriteLine("Select from the Options Below");
    Console.WriteLine("------------------------------");
    Console.WriteLine("1. To Generate MetaFile from Designer File, Enter 1");
    Console.WriteLine("2. To Generate Repo Class File from Meta File, Enter 2");
    Console.WriteLine("3. To Generate Test Class from Meta File, Enter 3");
    Console.WriteLine("4. To Generate DTO Class from Meta File, Enter 4");
    Console.WriteLine("5. To Generate Interface File from Meta File, Enter 5");
    Console.WriteLine("6. Enter X to Exit");
    string Option = Console.ReadLine();
    bool ExitLoop = false;

    switch (Option.ToUpper())
    {
        case "1":
            Console.WriteLine("Enter the Designer File To be Converted (along with full path):");

            var SrcFile = Console.ReadLine();
            //if (SrcFile == "") SrcFile = @"C:\GV\API13764v1\Code\C#\Library\HoursOfOperationDAL\HoursOfOperationDAL\HoursOfOperationManager.designer.cs";
            if (SrcFile != "") CreateMetaFile(SrcFile);
            break;
        case "2":
            Console.WriteLine("Enter the Meta File To Generate Repo Class File (along with full path):");
            var MetaFile = Console.ReadLine();
            if (MetaFile == "") MetaFile = @"C:\Temp\HoursOfOperationManager_MetaFile.csv";
            if (MetaFile != "") GenerateRepoCode(MetaFile.Replace('"', ' '));
            break;
        case "3":
            Console.WriteLine("Enter the Meta File To Generate Test Class (along with full path):");
            var MetaFile1 = Console.ReadLine();
            if (MetaFile1 == "") MetaFile1 = @"C:\Temp\HoursOfOperationManager_MetaFile.csv";
            if (MetaFile1 != "") GenerateTestClass(MetaFile1);
            break;
        case "4":
            Console.WriteLine("Enter the Meta File To Generate DTO Class (along with full path):");
            var MetaFile2 = Console.ReadLine();
            if (MetaFile2 == "") MetaFile2 = @"C:\Temp\HoursOfOperationManager_MetaFile.csv";
            if (MetaFile2 != "") GenerateDTOClass(MetaFile2);
            break;
        case "X":
            ExitLoop = true;
            break;
        default:

            break;
    }
    if (ExitLoop == true) break;

} while (true);

string CreateMetaFile(string SrcFile)
{
    string OPFile = "C:\\Temp\\" + Path.GetFileName(SrcFile).Replace(".designer.cs", "") + "_MetaFile" + ".csv"; //+ Guid.NewGuid().ToString() 
    StreamWriter sw = File.CreateText(OPFile);



    StreamReader sr = File.OpenText(SrcFile);
    string line;
    string line1;
    string OutPut;

    string Type = "";
    string FuncName = "";
    string Parameters = "";
    while ((line = sr.ReadLine()) != null)
    {
        if (line.Contains("FunctionAttribute"))
        {
            FuncName = line.Substring(line.IndexOf("Name=") + 6, (line.IndexOf(")]") - line.IndexOf("Name=")) - 7);

            line1 = sr.ReadLine();

            if (line1.Contains("ISingleResult"))
            {
                Type = "Query";
            }
            else
            {
                if (line1.Contains("int"))
                {
                    Type = "NonQuery";

                }
            }

            //Console.WriteLine(line);
            //Console.WriteLine(line1);
            try
            {
                if (line1.Contains("IC_GetProfilesByRoleID"))
                {
                    //Debugig point
                    Console.WriteLine("For Debuging Purpose");
                }
                if (line1.Contains("ParameterAttribute")) Parameters = GetParams(line1);
                Console.WriteLine(FuncName + "," + Type + ",[" + Parameters + "]");
                sw.WriteLine(FuncName + "," + Type + ",[" + Parameters + "]");
                sw.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Not able to parse the parameter" + line + "Exception : " + ex.Message);
                sw.WriteLine("Not able to parse the parameter" + line + "Exception : " + ex.Message);
                //throw;
            }

            //Get ResultSet for "Query" Type
            if (Type == "Query")
            {
                GenerateResultSetFile(FuncName.Replace("dbo.", "") + "Result", SrcFile);
            }

        }
    }
    sw.Close();
    sw.Dispose();

    sr.Close();
    sr.Dispose();
    Console.WriteLine();
    Console.WriteLine("MetaFile Generated..." + OPFile);
    return OPFile;
}
void GenerateResultSetFile(string ResultsetName, string SrcFile)
{
    if (!Directory.Exists("C:\\Temp\\" + Path.GetFileName(SrcFile).Replace(".designer.cs", "") + "_ResultSet"))
    {
        Directory.CreateDirectory("C:\\Temp\\" + Path.GetFileName(SrcFile).Replace(".designer.cs", "") + "_ResultSet");

    }
    string OPFile = "C:\\Temp\\" + Path.GetFileName(SrcFile).Replace(".designer.cs", "") + "_ResultSet\\" + ResultsetName + ".csv"; //+ Guid.NewGuid().ToString() 
    StreamWriter sw = File.CreateText(OPFile);



    StreamReader sr = File.OpenText(SrcFile);
    string line;
    string line1;
    bool StartExtract = false;
    bool StopExtract = false;

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
                Console.WriteLine(ResultSetCols[2].ToString().Substring(1, ResultSetCols[2].Length - 2) + ":" + ResultSetCols[1].ToString());
                sw.WriteLine(ResultSetCols[2].ToString().Substring(1, ResultSetCols[2].Length - 2) + ":" + ResultSetCols[1].ToString());

            }
        }

        if (line.Contains("class " + ResultsetName)) StartExtract = true;


    }
    sw.Close();
    sw.Dispose();

    sr.Close();
    sr.Dispose();

}
string GetParams(string line)
{
    try
    {
        string Params = "";
        if (!line.Contains("Name="))
        {
            var InputParams = line.Split("System.Nullable<");
            int Counter = 0;
            foreach (var Param in InputParams)
            {
                Counter++;
                if (Counter > 1)
                {
                    var Temp = Param.Split(",");
                    var Temp1 = Temp[0].Split("> ");
                    var Name = Temp1[1].Replace(")", "");
                    var Type = Temp1[0];
                    Params += Name + ":" + Type + "?" + "~";
                }
            }
            return Params.Substring(0, Params.Length - 1);
        }
        else
        {
            var InputParams = line.Split("ParameterAttribute");
            foreach (var Param in InputParams)
            {

                if (Param.Contains("Name="))
                {
                    var Name = Param.Substring(Param.IndexOf("Name=") + 6, Param.IndexOf("DbType") - (Param.IndexOf("Name=") + 9));
                    var Type = Param.Substring(Param.IndexOf("DbType=") + 8, Param.IndexOf(")]") - (Param.IndexOf("DbType=") + 9));
                    Params += Name + ":" + Type + "~";
                    //Parameters.Add(Name, Type);
                    //Console.WriteLine(Name + ":" + Type);
                }
                else if (Param.Contains("System.Nullable<"))
                {
                    var Temp = Param.Split(",");
                    var Temp1 = Temp[0].Split("> ");
                    var Name = Temp1[1].Replace(")", "");
                    var Type = Temp1[0].Split("System.Nullable<")[1];
                    Params += Name + ":" + Type + "?" + "~";
                }
            }
            return Params.Substring(0, Params.Length - 1);
        }
    }
    catch (Exception ex)
    {
        throw;
    }
}

void GenerateRepoCode(string MetaFile)
{

    StreamReader sr = File.OpenText(MetaFile);

    string RepoFile = "C:\\Temp\\" + Path.GetFileName(MetaFile).Replace("_MetaFile.csv", "") + "_Repo" + ".cs"; //+ Guid.NewGuid().ToString() 
    string ResultSetFolder = "C:\\Temp\\" + Path.GetFileName(MetaFile).Replace("_MetaFile.csv", "") + "_ResultSet";

    StreamWriter sw = File.CreateText(RepoFile);


    string line;
    List<string> ParamList = new List<string>();
    while ((line = sr.ReadLine()) != null)
    {
        ParamList.Clear();
        try
        {
            string[] Metadata = line.Split(",");
            string spName = Metadata[0];
            string MethodName = Metadata[0].Replace("dbo.", "");
            string Parameters = Metadata[2].Replace("[", "").Replace("]", "");

            string FirstLine = "";
            foreach (var Parameter in Parameters.Split("~"))
            {
                string[] Param = Parameter.Split(":");
                FirstLine += "," + (Param[1].ToLower().Contains("varchar") ? "string" : Param[1].ToLower()) + " " + Param[0];
                ParamList.Add("query.AddParameter(" + (Char)34 + (char)64 + Param[0] + (Char)34 + ",SqlDbType." + (Param[1].ToLower().Contains("varchar") ? "nvarchar" : Param[1]) + "," + Param[0] + ")");

                //Console.WriteLine(Parameter);
            }
            List<string> ResultSet = new List<string>();
            string MethodType = "";
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
                string OutVariable = "";
                if (MethodType == "int")
                {
                    Console.WriteLine((char)9 + "var Result = 0;");
                    sw.WriteLine((char)9 + "var Result = 0;");
                }
                else if (MethodType == "datetime")
                {
                    Console.WriteLine((char)9 + "var Result = new DateTime();");
                    sw.WriteLine((char)9 + "var Result = new DateTime();");
                }
                else if (MethodType == "string")
                {
                    Console.WriteLine((char)9 + "var Result = " + (char)34 + (char)34 + ";");
                    sw.WriteLine((char)9 + "var Result = " + (char)34 + (char)34 + ";");
                }
                else if (MethodType == "long")
                {
                    Console.WriteLine((char)9 + "var Result = 0;");
                    sw.WriteLine((char)9 + "var Result = 0;");
                }
                else
                {
                    Console.WriteLine((char)9 + "var Result = new List<" + spName.Replace("dbo.", "") + "Result>();");
                    sw.WriteLine((char)9 + "var Result = new List<" + spName.Replace("dbo.", "") + "Result>();");
                }
            }
            Console.WriteLine((char)9 + "using (var query = CreateQuery())");
            sw.WriteLine((char)9 + "using (var query = CreateQuery())");

            Console.WriteLine((char)9 + "{");
            sw.WriteLine((char)9 + "{");

            Console.WriteLine((char)9 + "" + (char)9 + "query.CommandType = CommandType.StoredProcedure;");
            sw.WriteLine((char)9 + "" + (char)9 + "query.CommandType = CommandType.StoredProcedure;");

            Console.WriteLine((char)9 + "" + (char)9 + "query.SQL = " + (char)34 + spName + (char)34 + ";");
            sw.WriteLine((char)9 + "" + (char)9 + "query.SQL = " + (char)34 + spName + (char)34 + ";");


            foreach (var item in ParamList)
            {
                Console.WriteLine((char)9 + "" + (char)9 + item + ";");
                sw.WriteLine((char)9 + "" + (char)9 + item + ";");
            }

            if (line.Contains("NonQuery"))
            {
                Console.WriteLine((char)9 + "" + (char)9 + "return query.ExecuteNonQuery();");
                sw.WriteLine((char)9 + "" + (char)9 + "return query.ExecuteNonQuery();");

                Console.WriteLine((char)9 + "}");
                sw.WriteLine((char)9 + "}");

            }
            else if (line.Contains(",Query,"))
            {
                if (ResultSet.Count > 1)
                {
                    Console.WriteLine((char)9 + "" + (char)9 + "while (query.Read())" + Environment.NewLine + (char)9 + "" + (char)9 + "{");
                    sw.WriteLine((char)9 + "" + (char)9 + "while (query.Read())" + Environment.NewLine + (char)9 + "" + (char)9 + "{");

                    Console.WriteLine((char)9 + "" + (char)9 + "" + (char)9 + "result.Add(new " + spName.Replace("dbo.", "") + Environment.NewLine + (char)9 + "" + (char)9 + "" + (char)9 + "{");
                    sw.WriteLine((char)9 + "" + (char)9 + "" + (char)9 + "result.Add(new " + spName.Replace("dbo.", "") + Environment.NewLine + (char)9 + "" + (char)9 + "" + (char)9 + "{");

                    foreach (var Column in ResultSet)
                    {
                        Console.WriteLine((char)9 + "" + (char)9 + "" + (char)9 + "" + (char)9 + Column.Split(":")[1] + " = query.Value(" + (char)34 + Column.Split(":")[1] + (char)34 + ");");
                        sw.WriteLine((char)9 + "" + (char)9 + "" + (char)9 + "" + (char)9 + Column.Split(":")[1] + " = query.Value(" + (char)34 + Column.Split(":")[1] + (char)34 + ");");

                    }
                    Console.WriteLine((char)9 + "" + (char)9 + "" + (char)9 + "});");
                    sw.WriteLine((char)9 + "" + (char)9 + "" + (char)9 + "});");

                    Console.WriteLine((char)9 + "" + (char)9 + "}");
                    sw.WriteLine((char)9 + "" + (char)9 + "}");

                    Console.WriteLine((char)9 + "}" + Environment.NewLine + (char)9 + "return result.AsReadOnly();");
                    sw.WriteLine((char)9 + "}" + Environment.NewLine + (char)9 + "return result.AsReadOnly();");
                }
                else if (ResultSet.Count == 1)
                {
                    Console.WriteLine((char)9 + "" + (char)9 + "if (query.Read())" + Environment.NewLine + (char)9 + "" + (char)9 + "{");
                    sw.WriteLine((char)9 + "" + (char)9 + "if (query.Read())" + Environment.NewLine + "" + (char)9 + (char)9 + "{");

                    Console.WriteLine((char)9 + "" + (char)9 + "" + (char)9 + "result = query.Value(" + (char)34 + ResultSet.First().Split(":")[1] + (char)34 + ");");
                    sw.WriteLine((char)9 + "" + (char)9 + "" + (char)9 + "result = query.Value(" + (char)34 + ResultSet.First().Split(":")[1] + (char)34 + ");");

                    Console.WriteLine((char)9 + "" + (char)9 + "}");
                    sw.WriteLine((char)9 + "" + (char)9 + "}");

                    Console.WriteLine((char)9 + "}" + Environment.NewLine + (char)9 + "return result;");
                    sw.WriteLine((char)9 + "}" + Environment.NewLine + (char)9 + "return result;");

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
            sw.WriteLine("Error: Not able to retrive column details From DB. Please convert Mannualy for this method: " + line);
            //throw;
        }
    }
    sw.Flush();
    sw.Close();
    sw.Dispose();

    sr.Close();
    sr.Dispose();
}

void GenerateTestClass(string MetaFile)
{

    StreamReader sr = File.OpenText(MetaFile);

    string RepoFile = "C:\\Temp\\" + Path.GetFileName(MetaFile).Replace("_MetaFile.csv", "") + "_TestClass" + ".cs"; //+ Guid.NewGuid().ToString() 
    StreamWriter sw = File.CreateText(RepoFile);

    string ResultSetFolder = "C:\\Temp\\" + Path.GetFileName(MetaFile).Replace("_MetaFile.csv", "") + "_ResultSet";


    string line;
    while ((line = sr.ReadLine()) != null)
    {
        string[] Metadata = line.Split(",");
        string spName = Metadata[0];
        string MethodName = Metadata[0].Replace("dob.", "");
        string Type = Metadata[1];
        string Parameters = Metadata[2].Replace("[", "").Replace("]", "");


        Console.WriteLine("[TestMethod]");
        sw.WriteLine("[TestMethod]");

        Console.WriteLine("public void " + MethodName + "_HappyPath()");
        sw.WriteLine("public void " + MethodName + "_HappyPath()");

        Console.WriteLine("{");
        sw.WriteLine("{");

        Console.WriteLine();
        sw.WriteLine();
        Console.WriteLine((char)9 + "// Arrange");
        sw.WriteLine((char)9 + "// Arrange");

        List<string> ResultSet = new List<string>();
        string FirstSet = "";
        string SecondSet = "";
        string ThirdSet = "";
        if (Type == "Query")
        {

            ResultSet = GetResultSetFromFile(ResultSetFolder, spName);
            //Need to perform this check in future to differentiate between "IEnumerable" and "Single Object"
            //if (ResultSet.Count > 1)
            //{
            //MethodType = "IEnumerable<" + spName.Replace("dbo.", "") + "Result>";
            Console.WriteLine((char)9 + "var expectedResults = Enumerable.Range(1, 10).Select(x =>");
                sw.WriteLine((char)9 + "var expectedResults = Enumerable.Range(1, 10).Select(x =>");

                Console.WriteLine((char)9 + "new " + spName.Replace("dbo.", ""));
                sw.WriteLine((char)9 + "new " + spName.Replace("dbo.", ""));

                Console.WriteLine((char)9 + "{");
                sw.WriteLine((char)9 + "{");



                
                foreach (string result in ResultSet)
                {
                    if (result.ToLower().Contains("int"))
                    {
                        FirstSet += (char)9 + " " + (Char)9 + result.Split(":")[1] + " = x,\n";
                    }
                    else if (result.ToLower().Contains("long"))
                    {
                        FirstSet += (char)9 + " " + (Char)9 + result.Split(":")[1] + " = x,\n";
                    }
                    else
                    {
                        FirstSet += (char)9 + " " + (Char)9 + result.Split(":")[1] + " = $" + (char)34 + result.Split(":")[1] + "{x}" + (Char)34 + ",\n";
                    }
                    SecondSet += (char)9 + " " + (Char)9 + "q.SetupValue(" + (Char)34 + result.Split(":")[1] + (char)34 + " , model." + result.Split(":")[1] + ");\n";
                    ThirdSet += (char)9 + " " + (Char)9 + "Assert.AreEqual(expected." + result.Split(":")[1] + ", actual." + result.Split(":")[1] + ");\n";
                }
                Console.WriteLine(FirstSet.Substring(0, FirstSet.Length - 3));
                sw.WriteLine(FirstSet.Substring(0, FirstSet.Length - 3));

                Console.WriteLine((char)9 + "}).ToArray();\n" + (char)9 + "_query.ConfigureReads(expectedResults.Length, (q, x) =>\n" + (char)9 + "{\n" + (char)9 + "" + (char)9 + "var model = expectedResults[x];");
                sw.WriteLine((char)9 + "}).ToArray();\n" + (char)9 + "_query.ConfigureReads(expectedResults.Length, (q, x) =>\n" + (char)9 + "{\n" + (char)9 + "" + (char)9 + "var model = expectedResults[x];");


                Console.WriteLine(SecondSet.Substring(0, SecondSet.Length - 2));
                sw.WriteLine(SecondSet.Substring(0, SecondSet.Length - 2));

                Console.WriteLine((char)9 + "});");
                sw.WriteLine((char)9 + "});");

            //}
            //else if (ResultSet.Count == 1)
            //{
            //    //MethodType = ResultSet.First().Split(":")[0].Replace("?", "");
            //}
        }

        string Temp = "";
        string Temp1 = "";
        foreach (var Parameter in Parameters.Split("~"))
        {
            string[] Param = Parameter.Split(":");

            string curLine = ("var " + Param[0] + "=");
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
                curLine += (Char)34 + "01/01/2024" + (Char)34;
                Temp1 += (Char)34 + "01/01/2024" + (Char)34;
            }
            else if (Param[1].ToLower() == "bit")
            {
                curLine += "true;";
                Temp1 += "true,";

            }
            else if (Param[1].ToLower().Contains("varchar"))
            {
                curLine += (Char)34 + "Dummy Text" + (Char)34 + ";";
                Temp1 += (Char)34 + "Dummy Text" + (Char)34 + ",";

            }

            Console.WriteLine((char)9 + curLine);
            sw.WriteLine((char)9 + curLine);
        }


        Console.WriteLine((char)9 + "_query.ConfigureNonQuery();" + Environment.NewLine + (char)9 + "var repository = CreateRepository();");
        sw.WriteLine((char)9 + "_query.ConfigureNonQuery();" + Environment.NewLine + (char)9 + "var repository = CreateRepository();");


        Console.WriteLine();
        sw.WriteLine();
        Console.WriteLine((char)9 + "// Act");
        sw.WriteLine((char)9 + "// Act");


        Console.WriteLine();
        sw.WriteLine();
        if (Type == "NonQuery")
        {
            Console.WriteLine((char)9 + "var results = repository." + MethodName + "(" + Temp.Substring(0, Temp.Length - 1) + ").ToList();");
            sw.WriteLine((char)9 + "var results = repository." + MethodName + "(" + Temp.Substring(0, Temp.Length - 1) + ").ToList();");

            Console.WriteLine((char)9 + "// Assert" + Environment.NewLine + (char)9 + "_query.VerifySet(q => q.CommandType = CommandType.StoredProcedure);");
            sw.WriteLine((char)9 + "// Assert" + Environment.NewLine + (char)9 + "_query.VerifySet(q => q.CommandType = CommandType.StoredProcedure);");

            Console.WriteLine((char)9 + "_query.VerifySet(q => q.SQL = " + (char)34 + "dbo." + MethodName + (char)34 + ");");
            sw.WriteLine((char)9 + "_query.VerifySet(q => q.SQL = " + (char)34 + "dbo." + MethodName + (char)34 + ");");
        }
        else if (Type == "Query")
        {
            Console.WriteLine((char)9 + "repository." + MethodName + "(" + Temp.Substring(0, Temp.Length - 1) + ");");
            sw.WriteLine((char)9 + "repository." + MethodName + "(" + Temp.Substring(0, Temp.Length - 1) + ");");

            Console.WriteLine((char)9 + "// Assert" + Environment.NewLine + (char)9 + "Assert.AreEqual(expectedResults.Length, results.Count);\n" + (char)9 + "for (var x = 0; x < expectedResults.Length; x++)\n" + (char)9 + "{\n" + (char)9 + " " + (char)9 + "var expected = expectedResults[x];\n" + (char)9 + " " + (char)9 + "var actual = results[x];\n");
            sw.WriteLine((char)9 + "// Assert" + Environment.NewLine + (char)9 + "Assert.AreEqual(expectedResults.Length, results.Count);\n" + (char)9 + "for (var x = 0; x < expectedResults.Length; x++)\n" + (char)9 + "{\n" + (char)9 + " " + (char)9 + "var expected = expectedResults[x];\n" + (char)9 + " " + (char)9 + "var actual = results[x];\n");

            Console.WriteLine(ThirdSet + (char)9 + "}");
            sw.WriteLine(ThirdSet + (char)9 + "}");
        }


        Console.WriteLine((char)9 + "ValidateMocks();" + Environment.NewLine + "}");
        sw.WriteLine((char)9 + "ValidateMocks();" + Environment.NewLine + "}");


        //Writing Exception Cases
        Console.WriteLine();
        sw.WriteLine();
        Console.WriteLine("[TestMethod]");
        sw.WriteLine("[TestMethod]");

        Console.WriteLine("public void " + MethodName + "_LogsException()");
        sw.WriteLine("public void " + MethodName + "_LogsException()");

        Console.WriteLine("{");
        sw.WriteLine("{");

        Console.WriteLine((char)9 + "// Arrange");
        sw.WriteLine((char)9 + "// Arrange");

        Console.WriteLine((char)9 + "var exception = new Exception(expectedExceptionMessage);");
        sw.WriteLine((char)9 + "var exception = new Exception(expectedExceptionMessage);");

        Console.WriteLine((char)9 + "_query.ConfigureNonQuery(() => throw exception); ");
        sw.WriteLine((char)9 + "_query.ConfigureNonQuery(() => throw exception); ");

        Console.WriteLine((char)9 + "var repository = CreateRepository(); ");
        sw.WriteLine((char)9 + "var repository = CreateRepository(); ");

        Console.WriteLine();
        sw.WriteLine();

        Console.WriteLine((char)9 + "// Act");
        sw.WriteLine((char)9 + "// Act");
        Console.WriteLine((char)9 + "var result = Assert.ThrowsException<Exception>(() => repository.WFI_UpdateRuleNextrun(" + MethodName + "(" + Temp1.Substring(0, Temp1.Length - 1) + ")));");
        sw.WriteLine((char)9 + "var result = Assert.ThrowsException<Exception>(() => repository.WFI_UpdateRuleNextrun(" + MethodName + "(" + Temp1.Substring(0, Temp1.Length - 1) + ")));");

        Console.WriteLine();
        sw.WriteLine();
        Console.WriteLine((char)9 + "// Assert" + Environment.NewLine + (char)9 + "Assert.AreSame(exception, result);" + Environment.NewLine + (char)9 + "ValidateMocks();");
        sw.WriteLine((char)9 + "// Assert" + Environment.NewLine + (char)9 + "Assert.AreSame(exception, result);" + Environment.NewLine + (char)9 + "ValidateMocks();");




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


string GenerateDTOClass(string MetaFile)
{
    string DTOFile = "C:\\Temp\\" + Path.GetFileName(MetaFile).Replace("_MetaFile.csv", "") + "_DTOClass" + ".cs"; //+ Guid.NewGuid().ToString() 
    string ResultSetFolder = "C:\\Temp\\" + Path.GetFileName(MetaFile).Replace("_MetaFile.csv", "") + "_ResultSet";

    StreamWriter sw = File.CreateText(DTOFile);
    StreamReader sr = File.OpenText(MetaFile);
    string line;
    while ((line = sr.ReadLine()) != null)
    {
        if (line.Contains(",Query,"))
        {
            string[] Metadata = line.Split(",");
            string spName = Metadata[0].Replace("dbo.", "");
            try
            {
                List<string> ResultSet = GetResultSetFromFile(ResultSetFolder, spName);
                if (ResultSet.Count > 1)
                {
                    Console.WriteLine("public class " + spName + "Result");
                    sw.WriteLine("public class " + spName + "Result");
                    Console.WriteLine("{");
                    sw.WriteLine("{");
                    foreach (var Column in ResultSet)
                    {
                        string[] Col = Column.Split(":");
                        Console.WriteLine((char)9 + "public " + Col[0] + " " + Col[1] + " { get; set; }");
                        sw.WriteLine((char)9 + "public " + Col[0] + " " + Col[1] + " { get; set; }");
                    }
                    Console.WriteLine("}");
                    sw.WriteLine("}");

                    sw.WriteLine("");
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Not Able to Get the value. Please convert Mannualy");
                sw.WriteLine("Error: Not able to retrive column details From DB. Please convert Mannualy for this method: " + spName);
            }
        }
    }
    sw.Flush();
    sw.Close();
    sw.Dispose();

    sr.Close();
    sr.Dispose();
    return "";

}

List<string> GetResultSetFromFile(string FolderPath, string SPName)
{
    string ResultSetFile = FolderPath + "\\" + SPName.Replace("dbo.", "") + "Result.csv"; //+ Guid.NewGuid().ToString() 

    StreamReader sr = File.OpenText(ResultSetFile);
    string line;
    List<string> ResultSet = new List<string>();

    while ((line = sr.ReadLine()) != null)
    {

        string[] ColumnDetails = line.Split(":");
        string ColName = ColumnDetails[0];
        string ColType = ColumnDetails[1].ToString().ToLower().Contains("system.datetime") ? "DateTime" : (ColumnDetails[1].ToString().Replace("System.Nullable<", "").Replace(">", "")); //.Contains("numeric") ? "long" : ColumnDetails[1].ToString());
        //bool IsNullable = (bool)rdr["is_nullable"];
        if (line.Contains("System.Nullable"))
        {
            ColType = ColType + "?";
        }
        ResultSet.Add(ColType + ":" + ColName);
    }

    return ResultSet;

}

List<string> GetResultSetFromDB(string SPName)
{
    try
    {
        List<string> ResultSet = new List<string>();

        //using (SqlConnection conn = new SqlConnection("Server=.\\SQLEXPRESS;DataBase=InCode;Integrated Security=SSPI"))
        using (SqlConnection conn = new SqlConnection("Server=doa-c110cor01.in.lab;DataBase=InCode;uid=krish;password=Passme@1"))
        {
            conn.Open();
            SqlCommand cmd = new SqlCommand("sp_describe_first_result_set", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@tsql", SPName));


            using (SqlDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    string ColName = rdr["name"].ToString();
                    string ColType = rdr["system_type_name"].ToString().ToLower().Contains("nvarchar") ? "string" : (rdr["system_type_name"].ToString().Contains("numeric") ? "long" : rdr["system_type_name"].ToString());
                    bool IsNullable = (bool)rdr["is_nullable"];
                    if (IsNullable) ColType = ColType + "?";
                    ResultSet.Add(ColType + ":" + ColName);
                }
            }
        }
        return ResultSet;

    }
    catch (Exception)
    {

        throw;
    }

}