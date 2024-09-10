using GVLinQOptimizer.CodeGeneration;
using GVLinQOptimizer.CodeGeneration.Engine;
using MediatR;

namespace GVLinQOptimizer.Programs;

public sealed class ExtractDTOs
{
    public class Request : IRequest<string>
    {
        public string SettingsFilePath { get; set; }
        public string OutputDirectory { get; set; }
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

            var renderer = provider.GetRenderer("DTOModels");
            var generatedCode = await renderer.RenderAsync(templateEngine, definition, cancellationToken);
            var fileName = string.Format(renderer.FileNameFormat, definition.ContextName);
            var filePath = Path.Combine(request.OutputDirectory, fileName);
            await File.WriteAllTextAsync(filePath, generatedCode, cancellationToken);

            return request.OutputDirectory;
        }

        private string GenerateDTOClass(string MetaFile)
        {
            var DTOFile = "C:\\Temp\\" + Path.GetFileName(MetaFile).Replace("_MetaFile.csv", "") + "_DTOClass" +
                          ".cs"; //+ Guid.NewGuid().ToString() 
            var ResultSetFolder = "C:\\Temp\\" + Path.GetFileName(MetaFile).Replace("_MetaFile.csv", "") + "_ResultSet";

            var sw = File.CreateText(DTOFile);
            var sr = File.OpenText(MetaFile);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains(",Query,"))
                {
                    string[] Metadata = line.Split(",");
                    var spName = Metadata[0].Replace("dbo.", "");
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
                                Console.WriteLine((char) 9 + "public " + Col[0] + " " + Col[1] + " { get; set; }");
                                sw.WriteLine((char) 9 + "public " + Col[0] + " " + Col[1] + " { get; set; }");
                            }

                            Console.WriteLine("}");
                            sw.WriteLine("}");

                            sw.WriteLine("");
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Not Able to Get the value. Please convert Mannualy");
                        sw.WriteLine(
                            "Error: Not able to retrive column details From DB. Please convert Mannualy for this method: " +
                            spName);
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