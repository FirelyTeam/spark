---
name: Spark FHIR server
route: /deployment
menu: Deployment
---

This page describes how to deploy Spark from the source code onto a Windows machine, either virtual or physical. The instructions are based on Windows Server 2012.

# Install prerequisites

Install the following prerequisites.

Please make sure to install IIS first, and ASP.Net 4.6 later. IIS comes with ASP.Net 4.5, and if you do 4.6 first, the IIS installation will break 4.6.

## Install Internet Information Server (IIS)

If you do not already have a recent version of IIS (7 or 8), install it.

1. Open Server Manager > Dashboard
2. Click 'Add roled and features'
3. Select 'Web Server (IIS)'
4. Make sure to include Management Tools > Management Service
You need this if you want to enable [Web Deploy](http://www.iis.net/learn/publish/using-web-deploy/introduction-to-web-deploy) for non-administrators.

## Install Web Deploy

1. Download the [Web Platform Installer](https://www.microsoft.com/web/downloads/platform.aspx) and run it (wpilauncher.exe).
2. Select 'WebDeploy 3.6 for Hosting Servers'
3. Install

Read [more info](https://www.iis.net/learn/install/installing-publishing-technologies/installing-and-configuring-web-deploy-on-iis-80-or-later) about Web Deploy, especially for configuring Web Deploy for non-administrators.

## Install ASP.NET 4.6

If you do not already have it installed, install ASP.NET 4.6 (version 4.5 is not enough).

1. Download it from [Microsoft](http://www.microsoft.com/en-us/download/details.aspx?id=48130).
2. Install it by running the just downloaded NDP46-KB3045560-Web.exe.

## Install MongoDB

Spark uses MongoDB for storage. Install it as a service. These instructions are based on the [MongoDB documentation](https://docs.mongodb.com/v3.0/tutorial/install-mongodb-on-windows/).

1. Download [MongoDB](https://www.mongodb.com/download-center?jmp=nav#community), choose the Community Edition, 'Windows Server 2008 R2 and later, with SSL support'.
2. Run the installer.
By default it will be installed in C:\Program Files\MongoDB\Server
3. Create directories for the database, log files and configuration of Mongo. For example:

    * C:\Spark\MongoDb\Data
    * C:\Spark\MongoDb\Log
    * C:\Spark\MongoDb\Config

4. Create a configuration file for MongoDb in the Config directory, name it SparkMongoDB.cfg and add the following to it (adjust to your previously created directories). Please note the indentation: it is relevant (MongoDB uses YAML for this configuration file).

    ```yaml
        systemLog:
            destination: file
            path: c:\Spark\MongoDB\Log\mongod.log
        storage:
            dbPath: c:\Spark\MongoDB\Data
    ```

5. Run the command below in a Command Window, as Administrator. It will register MongoDB as a Windows Service.

    ```bash
        c:\Program Files\MongoDB\Server\3.2\bin>mongod.exe --config "C:\Spark\MongoDB\Config\SparkMongoDB.cfg" --install
    ```

# Opening Ports

If you want to deploy Spark with Web Deploy from within Visual Studio to this machine, you will have to open the relevant ports for it for inbound traffic:

* http (80)
* https (443)
* WebDeploy (8172)

Depending on your network configuration you may need to open 8172 locally for outbound traffic.

If you want to manage the MongoDB database remotely, you also have to open:

* 27017

Please take precautions when you do this (refer to MongoDB documentation for further information).

* Configure MongoDB security
* Start MongoDB in secure mode

# Deploy Spark

## By Web Deploy

1. Open Spark.sln in Visual Studio
2. Choose Build | Publish Spark

    1. On the Profile tab, create a new profile for this Virtual Machine:
    2. Publish target 'Custom' > choose a name (something that refers to this machine)
    3. Connection

        1. Publish method: Web Deploy
        2. Server: the ip address of your Virtual Machine
        3. Site name: Default Web Site/spark
        4. User name: login name for your VM
        5. Password: matching the User name
        6. Destination URL: can be left blank
        7. Validate Connection
        8. If the connection does not validate, check with the system administrator if the firewall lets you through.

    4. Settings

        1. Configuration: Release

    5. Publish

## By Web Deploy Package

1. Open Spark.sln in Visual Studio
1. Choose Build | Publish Spark
    1. On the Profile tab, create a new profile.
    2. Publish target 'Custom' > choose a name (it may or may not refer to this specific machine, since you can deploy the package to other machines as well).
    3. Connection
        1. Publish method: Web Deploy Package
        2. Package location: choose as suitable directory on your disk.
        3. Site name: Default Web Site/spark
    4. Settings
        1. Configuration: Release
    5. Publish
1. Connect Remote Desktop Manager to your VM, and select the option to make your local disks available to the VM.
1. In the VM, open Windows Explorer and copy the deployment package to a local directory on the VM.
1. Read (from the deployment directory) the Spark.deploy-readme.txt.
1. Start a Command Window as Administrator and run (from the deployment directory) 

    Spark.deploy.cmd /Y

# Check Spark

Open a browser on the server and try http://localhost/spark. If that works, try the same from your own machine, with http://ip-address/spark

# FAQ

## Bad Module

When I first try to open Spark in the browser I get this error:

    Handler "ExtensionlessUrlHandler-Integrated-4.0" has a bad module "ManagedPipelineHandler" in its module list

Something must have gone wrong with installing ASP.Net in IIS. I (Christiaan) had this and solved it by running in a Command Window (as Administrator):

    dism.exe /Online /Enable-Feature /all /FeatureName:IIS-ASPNET45

Source of this fix: https://www.microsoft.com/en-us/download/details.aspx?id=44989
