using Malware.MDKUtilities;
using System.IO;
using System.Reflection;

namespace IngameScript.MDK
{
    public class TestBootstrapper
    {
        // All the files in this folder, as well as all files containing the file ".debug.", will be excluded
        // from the build process. You can use this to create utilites for testing your scripts directly in 
        // Visual Studio.

        static TestBootstrapper()
        {
            // Initialize the MDK utility framework
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var mdkPathsProps = Path.Combine(assemblyDirectory, @"MDK\MDK.paths.props");
            MDKUtilityFramework.Load(mdkPathsProps);
        }

        public static void Main()
        {
            Run(default(MDKFactory.ProgramConfig));
        }

        public static void Run(MDKFactory.ProgramConfig config, string argument = "")
        {
            var program = MDKFactory.CreateProgram<Program>(config);
            MDKFactory.Run(program, argument);
        }
    }
}