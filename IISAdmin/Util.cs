using System.Linq;
using Microsoft.Web.Administration;

namespace CodingArchitect.Utilities.IISAdmin
{
    public static class Util
    {
        private static ServerManager CreateServerManager()
        {
            return new ServerManager(@"%windir%\system32\inetsrv\config\applicationhost.config");
        }

        public static ApplicationPool CreateAppPool(string poolname, bool enable32bitOn64, ManagedPipelineMode mode, string runtimeVersion = "v4.0")
        {
            using (var serverManager = CreateServerManager())
            {
                var appPool = serverManager.ApplicationPools[poolname];
                if (appPool == null)
                {
                    appPool = serverManager.ApplicationPools.Add(poolname);
                    appPool.ManagedRuntimeVersion = runtimeVersion;
                    appPool.Enable32BitAppOnWin64 = enable32bitOn64;
                    appPool.ManagedPipelineMode = mode;
                    serverManager.CommitChanges();
                }
                return appPool;
            }
        }

        /// <summary>
        /// Creates the IIS web site.
        /// </summary>
        /// <param name="applicationPoolName">Name of the application pool.</param>
        /// <param name="websiteName">Name of the website.</param>
        /// <param name="physicalPath">Path to website folder.</param>
        /// <param name="port">Port that IIS listens on.</param>
        public static void CreateIISWebsite(string applicationPoolName, string websiteName, string physicalPath, string port)
        {
            using (ServerManager server = CreateServerManager())
            {
                if (server.Sites != null && server.Sites.Count > 0)
                {
                    //we will first check to make sure that the site isn't already there
                    if (server.Sites.FirstOrDefault(s => s.Name == websiteName) == null)
                    {
                        string path = physicalPath;

                        //we must specify the Binding information
                        string ip = "*";
                        string hostName = "*";

                        string bindingInfo = string.Format(@"{0}:{1}:{2}", ip, port, hostName);

                        //add the new Site to the Sites collection
                        Site site = server.Sites.Add(websiteName, "http", bindingInfo, path);

                        //set the ApplicationPool for the new Site
                        site.ApplicationDefaults.ApplicationPoolName = applicationPoolName;

                        //save the new Site!
                        server.CommitChanges();
                    }
                }
            }
        }


        /// <summary>
        /// Creates the IIS application.
        /// </summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="physicalPath">The physical path.</param>
        public static void CreateIISApplication(string applicationPoolName, string websiteName, string applicationName, string physicalPath)
        {
            using (ServerManager serverManager = CreateServerManager())
            {
                var defaultSite = serverManager.Sites[websiteName];
                Application newApplication = defaultSite.Applications["/" + applicationName];

                // Remove if exists?!
                if (newApplication != null)
                {
                    defaultSite.Applications.Remove(newApplication);
                    serverManager.CommitChanges();
                }

                defaultSite = serverManager.Sites[websiteName];
                newApplication = defaultSite.Applications.Add("/" + applicationName, physicalPath);

                newApplication.ApplicationPoolName = applicationPoolName;

                serverManager.CommitChanges();
                serverManager.Dispose();
            }
        }

        /// <summary>
        /// Deletes the IIS application.
        /// </summary>
        /// <param name="applicationName">Name of the application.</param>
        public static void DeleteIISApplication(string websiteName, string applicationName)
        {
            using (ServerManager serverManager = new ServerManager())
            {
                var defaultSite = serverManager.Sites[websiteName];
                Application newApplication = defaultSite.Applications["/" + applicationName];

                // Remove
                if (newApplication != null)
                {
                    defaultSite.Applications.Remove(newApplication);
                    serverManager.CommitChanges();
                }

                serverManager.Dispose();
            }
        }


    }
}
