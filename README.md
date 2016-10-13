<h1>Azure Backup Manager</h1>

<h3>Manager is for automating backups and restores</h3>
<p>
    Azure Backup Manager is mainly targeted for handling and automating backups and restoring data of web site projects (like Episerver CMS).
    It's backing up data to Azure Blob storage from virtual machines or local server and restoring it vice versa.
    Backup data includes SQL Server Database and AppData folders and it's extendable for other storages.
</p>

<h3>Manager is designed for following use cases and purposes</h3>
<ul>
    <li><b>Managing backup archives</b>
        <ul><li>List, Create, Delete, Download and Upload backups</li></ul>
    </li>
    <li><b>Monitoring backups</b></li>
    <li>Scheduling <b>daily, weekly and monthly backups</b></li>
    <li>Scheduling <b>cleaning of old backups</b></li>
    <li><b>Transferring data</b> between environments (Production, Staging, Testing, Dev, Local)</li>
    <li><b>Helping integration testing</b> (or any other kind of processes which needs the environment to be restored several times)
        <ul>
            <li>Quickly create/restore snapshots of data so it would be easy to rerun the process.</li>
            <li>You may even have different snapshots for different test scenarios.</li>
        </ul>
    </li>
    <li><b>Backups before production deployments</b> or even automate the backups in deployments</li>
    <li><b>Works on my machine -problems</b> and easing the problem solving of production environment</li>
    <li>Improving the data integrity by handling full backup packages. Packages are meant to create and restored with the same tools.</li>
</ul>

<h3>Installation</h3>
<p>This instruction is to install the manager as parallel web site which exists in same server or at least in same network.</p>
<ol>
    <li>Create IIS site based on the <a href="https://github.com/huilaaja/AzureBackupManager" target="_blank">GitHub repository</a> sources</li>
    <li>Enable IIS Basic Authentication or implement your own authentication.</li>
    <li>Create Azure Blob Storage (type: private)</li>
    <li>
        Set up 2 connection strings:
        <ol>
            <li>
                <b>AzureBackupBlobStorage</b><br/>
                Connections string to Azure Blob Storage. <a href="https://azure.microsoft.com/en-us/documentation/articles/storage-configure-connection-string/" target="_blank">Read more</a><br/>
                Example: &lt;add name="AzureBackupBlobStorage" connectionString="DefaultEndpointsProtocol=[http|https];AccountName=accountName;AccountKey=accountKey" /&gt;
            </li>
            <li>
                <b>BackupManager</b><br/>
                Connection string to SQL Server (with high user privileges)<br/>
                Example: &lt;add name="BackupManager" connectionString="Data Source=(local);Initial Catalog=DatabaseName;User ID=sa;Password=sa_password;Connection Timeout=1800;Integrated Security=False;MultipleActiveResultSets=True" providerName="System.Data.SqlClient" /&gt;
            </li>
        </ol>
    </li>
    <li>Make sure that following settings are correct
        <ul>
            <li>Make sure your IIS application user has access to required resources (folders) and SQL Server user (in connection string) has necessary privileges.</li>
            <li>Backup manager needs to have long timeouts for requests and db connections. By default this site has 60min (3600secs) timeouts and SQL Connection timeout is set to 30 minutes (1800secs).</li>
            <li>
                Scheduled services required that the IIS site needs to be alive all the time.
                So you need to set:
                <ol>
                    <li>application pool <b>Start Mode = "AlwaysRunning"</b></li>
                    <li>from IIS site Advance Settings <b>Preload Enabled = True</b></li>
                </ol>
                <a href="https://weblog.west-wind.com/posts/2013/oct/02/use-iis-application-initialization-for-keeping-aspnet-apps-alive" target="_blank">Here you can find further information on those settings.</a>
            </li>
        </ul>
    </li>
    <li>Start using it!</li>
</ol>

<h3>Configuration options</h3>
<p>From web.config &lt;appSettings&gt; you can change the default configuration of following settings:</p>
<ul>
    <li>Local repository path:<br/>
        &lt;add key="BackupManager.LocalRepositoryPath" value="C:\Sites\ExampleSite\BackupManagerRepository\"/&gt;</li>
    <li>AppData folder path:<br/>
        &lt;add key="BackupManager.AppDataFolder" value="C:\Sites\ExampleSite\AppData" /&gt;</li>
    <li>IIS site name:<br/>
        &lt;add key="BackupManager.IisSiteName" value="ExampleSite" /&gt;</li>
    <li>Blob storage container name:<br/>
        &lt;add key="BackupManager.ContainerName" value="CustomBlobStorageContainerName"/&gt;</li>
    <li>Database shared folder path (for transferring database backup):<br/>
        &lt;add key="BackupManager.DbSharedBackupFolder" value="\\ServerName\Shared\Folder\"/&gt;</li>
    <li>Disabling basic authentication:<br/>
        &lt;add key="BackupManager.UseBasicAuth" value="False" /&gt;</li>
    <li>Disabling user group checks:<br/>
        &lt;add key="BackupManager.CheckUserGroups" value="False"/&gt;</li>
</ul>

<h3>Requirements and inside information</h3>
<p>Build with .NET Framework 4.6.1.</p>
<h4>Depends on 4 NuGet Packages</h4>
<ol>
    <li>WindowsAzure.Storage version="7.2.1"</li>
    <li>DotNetZip version="1.9.1.8"</li>
    <li>FluentScheduler version="5.0.0"</li>
    <li>Microsoft.Web.Administration version="7.0.0.0"</li>
</ol>
<h4>Require SQL Server SMO DLLs <small>(these are included in project)</small></h4>
<ol>
    <li>Microsoft.SqlServer.ConnectionInfo.dll version=13.0.0.0</li>
    <li>Microsoft.SqlServer.Management.Sdk.Sfc.dll version=13.0.0.0</li>
    <li>Microsoft.SqlServer.Smo.dll version=13.0.0.0</li>
    <li>Microsoft.SqlServer.SqlClrProvider.dll version=13.0.0.0</li>
    <li>Microsoft.SqlServer.SqlEnum.dll version=13.0.0.0</li>
</ol>
<h4>Two different ways to run it</h4>
<ol>
    <li>Parallel web site <i>(recommended)</i> which will access the project's resources. <i>(More secure and optimal way)</i></li>
    <li>Inside of target web site which you are targeting the backups. It can be Episerver web site or any other type. <i>(Easier to maintain)</i></li>
</ol>
<h4>Transferring the backups</h4>
<p>To be able to transferring the backups easily from environment to another, it's required that this tool is installed in all the environments. Only the Azure Blob storage remain the same in all environments.</p>
<h4>Backup manager requires lot of privileges</h4>
<ul>
    <li>IIS application pool user needs to have write and delete access to Local Repository -folder and AppData-folder.</li>
    <li>SQL Server user needs to have sysadmin (create, restore and query databases) privileges in SQL Server</li>
</ul>
<h3>Security and limitations</h3>
<p>This tool is not meant for basic editors or users nor it's meant to backup hole server. It's meant that only developers, devops and hosting "Super Administrators" have access to this tool.</p>
<p>By default this tool uses basic authentication and check's that user belongs to one of this groups: WebAdmins, CmsAdmins, Administrators. But you can easily change authentication and use your own.</p>
<p>With basic authentication it's recommended to use secured connection with HTTPS-protocol.</p>
