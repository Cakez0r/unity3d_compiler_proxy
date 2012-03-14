using System;
using System.Diagnostics;
using System.IO;

namespace MonoImposter
{
    /// <summary>
    /// Proof of concept to show how you can inject compiler arguments in to unity builds. 
    /// Two immediate benefits of this are:
    /// - being able to define our own preprocessor symbols on a per-project basis (unity currently only supports per-file #defines :( )
    /// - being able to build with 'treat warnings as errors' turned on
    /// 
    /// As long as the compiler supports it, you can inject it with this. (see http://linux.die.net/man/1/mcs)
    /// 
    /// This could also be extended to do pretty much any pre-build logic (E.G. if you ever wanted to use a coding style plugin like FxCop, this could
    /// be used to integrate it in to unity and enforce style rules. The sky is the limit :P
    /// </summary>
    class Program
    {
        //Path pointing to the real mono exe
        static readonly string MONO_COMPILER_PATH = Environment.GetEnvironmentVariable("MONO_PATH") + @"\..\..\..\bin\_mono.exe";

        //Path to the file listing compiler options we want to inject
        const string COMPILER_ARGS_PATH = "compiler_args.txt";


        static int Main(string[] args)
        {
            //We're expecting 2 args here...
            //args[0] = Compiler path
            //args[1] = Compiler input path
            if (args.Length != 2)
            {
                WriteError("Invalid arguments!");
                return 1;
            }

            //Check if the legit mono compiler is where it should be
            if (!File.Exists(MONO_COMPILER_PATH))
            {
                WriteError(string.Format("Failed to find the Mono compiler ({0}). You broke it, you fool. :(", MONO_COMPILER_PATH));
                return 1;
            }

            //See if there is anything to inject
            if (File.Exists(COMPILER_ARGS_PATH))
            {
                try
                {
                    //Unity puts an "@" in front of the file path (this indicates to the compiler that it should read its options from file).
                    //.NET doesn't like it, so we'll trim it off.
                    string unityCompilerArgsPath = args[1].Remove(0, 1);

                    string file = File.ReadAllText(unityCompilerArgsPath);
                    //file = file.Replace("UNITY_WEBPLAYER", "UNITY_STANDALONE");
                    File.Delete(unityCompilerArgsPath);
                    File.WriteAllText(unityCompilerArgsPath, file);

                    //Append the context of the compiler args file on to the end of 
                    File.AppendAllText(unityCompilerArgsPath, File.ReadAllText(COMPILER_ARGS_PATH));

                    File.Copy(unityCompilerArgsPath, "copy.txt", true);
                }
                catch
                {
                    WriteWarning("Failed to inject compiler arguments. Continuing anyway...");
                }
            }
            else
            {
                WriteWarning("No compiler arguments file found. Continuing anyway...");
            }

            int exitCode = 1;

            //Ok, we're all set up. Let's compile!
            using (Process mono = new Process())
            {
                try
                {
                    mono.StartInfo.FileName = MONO_COMPILER_PATH;

                    //Pass the args through to mono
                    mono.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\"", args[0], args[1]);

                    //Stop the console window popping up
                    mono.StartInfo.UseShellExecute = false;

                    //Sacrifice a mountain goat upon the altar of code
                    mono.Start();
                    mono.WaitForExit();

                    exitCode = mono.ExitCode;
                }
                catch (Exception ex)
                {
                    WriteError(string.Format("Failed to compile!: {0}", ex.Message));
                }
            }

            //Hope for the best
            return exitCode;
        }


        //Unity will log anything that matches "\s*(?<filename>.*)\((?<line>\d+),(?<column>\d+)\):\s*(?<type>warning|error)\s*(?<id>[^:]*):\s*(?<message>.*)"
        static void WriteWarning(string warning)
        {
            Console.Error.WriteLine(string.Format("[Compile Interceptor](0,0): warning: {0}", warning));
        }


        static void WriteError(string error)
        {
            Console.Error.WriteLine(string.Format("[Compile Interceptor](0,0): error: {0}", error));
        }
    }
}
