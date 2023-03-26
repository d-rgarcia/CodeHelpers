using System.Xml;

namespace CodeHelpers
{
    public static class XMLHelper
    {
        // See xml structure for <runtime> inside web.config: https://learn.microsoft.com/en-us/dotnet/framework/configure-apps/file-schema/runtime/
        public static void CombineWebConfigDependentAssemblies(string sourceConfigFile, string finalConfigFile, string destinationWebConfig)
        {
            XmlDocument xml1 = new XmlDocument();
            xml1.Load(sourceConfigFile);

            XmlDocument xml2 = new XmlDocument();
            xml2.Load(finalConfigFile);

            XmlNodeList nodes1 = xml1.SelectNodes("//runtime/assemblyBinding/dependentAssembly");
            XmlNodeList nodes2 = xml2.SelectNodes("//runtime/assemblyBinding/dependentAssembly");

            foreach (XmlNode node2 in nodes2)
            {
                string identity2 = node2.SelectSingleNode("assemblyIdentity").Attributes["name"].Value;
                bool exists = false;

                foreach (XmlNode node1 in nodes1)
                {
                    string identity1 = node1.SelectSingleNode("assemblyIdentity").Attributes["name"].Value;

                    if (identity1 == identity2)
                    {
                        // If the node already exists in the first xml, get the biding redirect of the new one.
                        XmlNode bindingRedirectNode2 = node2.SelectSingleNode("bindingRedirect");
                        if (bindingRedirectNode2 != null)
                        {
                            XmlNode bindingRedirectNode1 = node1.SelectSingleNode("bindingRedirect");
                            if (bindingRedirectNode1 != null)
                            {
                                bindingRedirectNode1.Attributes["oldVersion"].Value = bindingRedirectNode2.Attributes["oldVersion"].Value;
                                bindingRedirectNode1.Attributes["newVersion"].Value = bindingRedirectNode2.Attributes["newVersion"].Value;
                            }
                            else
                            {
                                XmlNode importedNode = xml1.ImportNode(bindingRedirectNode2, true);
                                node1.AppendChild(importedNode);
                            }
                        }
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    // If in the first xml the node does not exist, add it directly:
                    XmlNode importedNode = xml1.ImportNode(node2, true);
                    xml1.DocumentElement.AppendChild(importedNode);
                }
            }

            // Save xml1 because we want to have the original xml but modified. For nodes in the first xml and not in the second,
            // saving the xml1 we still have them.
            xml1.Save(destinationWebConfig);
        }
    }
}
