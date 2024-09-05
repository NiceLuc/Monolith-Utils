namespace GVLinQOptimizer.Templates
{
    internal static class ResourceUtils
    {
        public static async Task<string> GetResourceAsync(string fileName, CancellationToken cancellationToken)
        {
            var assembly = typeof(ResourceUtils).Assembly;
            var resourceName = assembly.GetManifestResourceNames().Single(n => n.EndsWith(fileName));
            var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
                throw new InvalidOperationException($"Unable to locate resource: {fileName}");

            using var sr = new StreamReader(resourceStream);
            return await sr.ReadToEndAsync(cancellationToken);
        }
    }
}
