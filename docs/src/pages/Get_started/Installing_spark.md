---
name: Installing Spark
route: /install
menu: Get started
---
# Getting Started
## Hello world
Type here the most beautiful getting started that you ever saw!


You wish to deploy the Simplifier Spark FHIR Server to a machine of your own. This memo tells you how to do that.

## Install prerequisites

Install the following prerequisites.
Please make sure to install IIS first, and ASP.Net 4.6 later. IIS comes with ASP.Net 4.5, and if you do 4.6 first, the IIS installation will break 4.6.
If you have a different operation system, e.g. Windows 10, you may already have ASP.Net 4.6 available. In that case you do not need to install it.

Install IIS
1. Open Server Manager > Dashboard
2. Add roles and features
3. Select "Web Server (IIS)"
4. Make sure to include Management Tools > Management Service (needed if you want to enable Web
Deploy for non-administrators)
Install Web Deploy
1. Download Web Platform Installer, and run it (wpilauncher.exe)
2. Select "WebDeploy 3.6 for Hosting Servers"
3. Install
4. More info, especially for configuring Web Deploy for non-administrators.
Install ASP.Net 4.6
1. Spark is built on .NET Framework 4.6, but Azure Virtual Machine does not support this natively.
2. So download it from http://www.microsoft.com/en-us/download/details.aspx?id=48130
3. Install it by running the just downloaded NDP46-KB3045560-Web.exe.