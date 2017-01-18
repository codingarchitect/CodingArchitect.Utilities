using Microsoft.Web.Administration;
using System;
using System.IO;
using System.Security.AccessControl;

namespace CodingArchitect.Utilities.Linqpad
{
    public static class Util
    {
        public static void SetupApplication(string companyName, string applicationName, string queryDirectory, string queryPath)
        {
            var assemblyName = string.Format("{0}.Spikes.{1}.exe", companyName, applicationName);
            var namespaceName = string.Format("{0}.Spikes.{1}", companyName, applicationName);
            var appPhysicalPath = Path.Combine(queryDirectory, applicationName);
            var entryPointTypeName = string.Format("{0}.{1}", namespaceName, "Program");

            if (!Directory.Exists(appPhysicalPath)) Directory.CreateDirectory(appPhysicalPath);
            System.Environment.CurrentDirectory = appPhysicalPath;
            System.AppDomain.CurrentDomain.SetData("APPBASE", appPhysicalPath);
            var outputFile = Path.Combine(appPhysicalPath, assemblyName);

            CodingArchitect.Utilities.Linqpad.Compiler.CompileFiles(
                CodingArchitect.Utilities.Linqpad.Options.CreateOnDiskExe(
                    @namespace: namespaceName,
                    outputFile: outputFile,
                    startupObject: entryPointTypeName)
                        .AddCodeFile(CodingArchitect.Utilities.Linqpad.CodeType.LinqProgramTypes, queryPath));

            Console.WriteLine("Successfully created assembly at " + DateTime.Now.ToLocalTime());

            CodingArchitect.Utilities.Linqpad.Compiler.CopyReferencesToAppDirectory(queryDirectory, queryPath, appPhysicalPath);
            try
            {
                CodingArchitect.Utilities.AppDomain.Launcher.ExecuteProgram(appPhysicalPath, assemblyName, entryPointTypeName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void AddDirectorySecurity(string fileName, string account, FileSystemRights rights, AccessControlType controlType)
        {
            // Create a new DirectoryInfo object.
            DirectoryInfo dInfo = new DirectoryInfo(fileName);

            // Get a DirectorySecurity object that represents the 
            // current security settings.
            DirectorySecurity dSecurity = dInfo.GetAccessControl();

            // Add the FileSystemAccessRule to the security settings. 
            dSecurity.AddAccessRule(new FileSystemAccessRule(account,
                                                            rights,
                                                            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                                                            PropagationFlags.None,
                                                            controlType));

            // Set the new access settings.
            dInfo.SetAccessControl(dSecurity);

        }

        public static void SetupWebService(string companyName, string serviceWebsitetName, string port, string webServiceTypeName, string queryDirectory, string queryPath)
        {
            var websiteDirectory = serviceWebsitetName;
            var websiteBinDirectory = "bin";
            var assemblyName = string.Format("{0}.Spikes.{1}.dll", companyName, serviceWebsitetName);
            var namespaceName = string.Format("{0}.Spikes.{1}", companyName, serviceWebsitetName);

            var namespaceQualifiedWebServiceTypeName = string.Format("{0}.{1}", namespaceName, webServiceTypeName);

            var applicationPoolName = string.Format("{0}AppPool", serviceWebsitetName);
            var websiteName = string.Format("{0}WebSite", serviceWebsitetName); ;
            var applicationName = string.Format("{0}App", serviceWebsitetName); ;
            var sitePhysicalPath = queryDirectory;
            var appPhysicalPath = Path.Combine(queryDirectory, websiteDirectory);
            var servicePhysicalPath = Path.Combine(appPhysicalPath, string.Format("{0}.svc", webServiceTypeName));
            
            if (!Directory.Exists(websiteDirectory)) Directory.CreateDirectory(websiteDirectory);
            if (!Directory.Exists(websiteDirectory + "\\" + websiteBinDirectory)) Directory.CreateDirectory(websiteDirectory + "\\" + websiteBinDirectory);
            var outputFile = Path.Combine(queryDirectory, websiteDirectory, websiteBinDirectory, assemblyName);
            if (!File.Exists(servicePhysicalPath))
                File.WriteAllText(servicePhysicalPath, "<%@ ServiceHost  Service=\"" + namespaceQualifiedWebServiceTypeName + "\" %>");
            var appPool = CodingArchitect.Utilities.IISAdmin.Util.CreateAppPool(applicationPoolName, true, ManagedPipelineMode.Integrated);
            CodingArchitect.Utilities.IISAdmin.Util.CreateIISWebsite(applicationPoolName, websiteName, sitePhysicalPath, port);
            CodingArchitect.Utilities.IISAdmin.Util.CreateIISApplication(applicationPoolName, websiteName, applicationName, appPhysicalPath);

            var account = string.Format("IIS AppPool\\{0}", applicationPoolName);
            AddDirectorySecurity(queryDirectory, account, FileSystemRights.FullControl, AccessControlType.Allow);
            CodingArchitect.Utilities.Linqpad.Compiler.CompileFiles(
                CodingArchitect.Utilities.Linqpad.Options.CreateOnDiskDll(
                    @namespace: namespaceName,
                    outputFile: outputFile)
                        .AddCodeFile(CodingArchitect.Utilities.Linqpad.CodeType.LinqProgramTypes, queryPath));

            Console.WriteLine("Successfully created assembly at " + DateTime.Now.ToLocalTime());
            Console.WriteLine(string.Format("Point to http://localhost:{0}/{1}/{2}", port, applicationName, string.Format("{0}.svc", webServiceTypeName)));
        }
    }
    
}
