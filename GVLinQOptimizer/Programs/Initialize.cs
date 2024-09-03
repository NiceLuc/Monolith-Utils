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
        public Task Handle(Request request, CancellationToken cancellationToken)
        {
            // c:\Temp\*.designer.cs
            if (!File.Exists(request.DesignerFilePath))
                throw new FileNotFoundException($"File not found: {request.DesignerFilePath}");

            // c:\Temp\*_MetaFile.csv
            if (File.Exists(request.SettingsFilePath) && !request.ForceOverwrite)
                throw new InvalidOperationException(
                    $"File already exists: {request.SettingsFilePath} (use -f to overwrite)");

            var fileName = CreateMetaFile(request.DesignerFilePath);

            return Task.CompletedTask;
        }

        private string CreateMetaFile(string SrcFile)
        {
            var OPFile = "C:\\Temp\\" + Path.GetFileName(SrcFile).Replace(".designer.cs", "") + "_MetaFile" +
                         ".csv"; //+ Guid.NewGuid().ToString() 
            var sw = File.CreateText(OPFile);

            var sr = File.OpenText(SrcFile);
            string line;
            string line1;
            string OutPut;

            var Type = "";
            var FuncName = "";
            var Parameters = "";
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains("FunctionAttribute"))
                {
                    FuncName = line.Substring(line.IndexOf("Name=") + 6,
                        (line.IndexOf(")]") - line.IndexOf("Name=")) - 7);

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

                    try
                    {
                        if (line1.Contains("IC_GetProfilesByRoleID"))
                        {
                            Console.WriteLine("For Debugging Purpose");
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

        private string GetParams(string line)
        {
            try
            {
                var Params = "";
                if (!line.Contains("Name="))
                {
                    var InputParams = line.Split("System.Nullable<");
                    var Counter = 0;
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
                            var Name = Param.Substring(Param.IndexOf("Name=") + 6,
                                Param.IndexOf("DbType") - (Param.IndexOf("Name=") + 9));
                            var Type = Param.Substring(Param.IndexOf("DbType=") + 8,
                                Param.IndexOf(")]") - (Param.IndexOf("DbType=") + 9));
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
    }
}