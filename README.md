<div>
  
  <h1>SnapRepo - Snapshot Backup Azure Repository</h1>

  <blockquote>Single click and scheduled backups/restores for SQL Server DB and AppData files.</blockquote>
  
  <h3>SnapRepo is for automating backups and restores</h3>
  <p>
      SnapRepo is mainly targeted for handling and automating backups and restoring data of web site projects (like Episerver CMS).
      It's backing up data to Azure Blob storage from virtual machines or local server and restoring it vice versa.
      Backup data includes SQL Server Database and AppData folders.
  </p>
  
  <p>
    <a href="https://raw.githubusercontent.com/huilaaja/SnapRepo/master/Screenshots/snaprepo-environments.png">
      <img src="https://raw.githubusercontent.com/huilaaja/SnapRepo/master/Screenshots/snaprepo-environments.png" width="500"/>
    </a>
  </p>
  
  <h3>SnapRepo is designed for following use cases and purposes</h3>
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
      <li>Improving the data integrity by handling full backup packages. Packages are meant to create and restored with the same application.</li>
  </ul>

  <h3>Usage</h3>
  <p>
    <a href="https://raw.githubusercontent.com/huilaaja/SnapRepo/master/Screenshots/screencapture-fullsize.png">
      <img src="https://raw.githubusercontent.com/huilaaja/SnapRepo/master/Screenshots/screencapture-fullsize.png" width="500" />
    </a>
  </p>
  <p>
    <a href="https://htmlpreview.github.io/?https://github.com/huilaaja/SnapRepo/blob/master/Screenshots/html-demo.html">HTML-demo of the application.</a> It contains only visual side of the application.
  </p>
  
  <h3>Installation</h3>
  <p>This instruction is to install the SnapRepo as parallel to target web site. 
    SnapRepo needs to exists in same server or at least in same network to access the data.</p>
  <ol>
      <li>Create IIS site based on the <a href="https://github.com/huilaaja/SnapRepo" target="_blank">GitHub repository</a> sources</li>
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
                  <b>SnapRepo</b><br/>
                  Connection string to SQL Server (with high user privileges)<br/>
                  Example: &lt;add name="SnapRepo" connectionString="Data Source=(local);Initial Catalog=DatabaseName;User ID=sa;Password=sa_password;Connection Timeout=1800;Integrated Security=False;MultipleActiveResultSets=True" providerName="System.Data.SqlClient" /&gt;
              </li>
          </ol>
      </li>
      <li>Make sure that following settings are correct
          <ul>
              <li>Make sure your IIS application user has access to required resources (folders) and SQL Server user (in connection string) has necessary privileges.</li>
              <li>SnapRepo needs to have long timeouts for requests and db connections. By default this site has 60min (3600secs) timeouts and SQL Connection timeout is set to 30 minutes (1800secs).</li>
              <li>
                  Scheduled services required that the IIS site needs to be alive all the time.
                  So you need to set:
                  <ol>
                      <li>application pool <b>Start Mode = "AlwaysRunning"</b></li>
                      <li>from IIS site Advance Settings <b>Preload Enabled = True</b></li>
                      <li>or some sort of scheduled pinger (Example <a href="http://uptimerobot.com/" target="_blank">uptimerobot.com</a>)</li>
                  </ol>
              </li>
          </ul>
      </li>
      <li>Start using it!</li>
  </ol>

  <h3>Configuration options</h3>
  <p>From web.config &lt;appSettings&gt; you can change the default configuration of following settings:</p>
  <ul>
      <li>Local repository path:<br/>
          &lt;add key="SnapRepo.LocalRepositoryPath" value="C:\Sites\ExampleSite\SnapRepository\"/&gt;</li>
      <li>AppData folder path:<br/>
          &lt;add key="SnapRepo.AppDataFolder" value="C:\Sites\ExampleSite\AppData" /&gt;</li>
      <li>IIS site name:<br/>
          &lt;add key="SnapRepo.IisSiteName" value="ExampleSite" /&gt;</li>
      <li>Blob storage container name:<br/>
          &lt;add key="SnapRepo.ContainerName" value="CustomBlobStorageContainerName"/&gt;</li>
      <li>Database shared folder path (for transferring database backup):<br/>
          &lt;add key="SnapRepo.DbSharedBackupFolder" value="\\ServerName\Shared\Folder\"/&gt;</li>
      <li>Disable basic authentication:<br/>
          &lt;add key="SnapRepo.UseBasicAuth" value="False" /&gt;</li>
      <li>Protect the site with IP-restrictions:<br/>
          &lt;add key="SnapRepo.AllowedIpAddresses" value="127.0.0.1, 8.8.8.8, 8.8.4.4" /&gt;</li>
      <li>Disable user group checks:<br/>
          &lt;add key="SnapRepo.CheckUserGroups" value="False"/&gt;</li>
      <li>Disable HTTPS-redirect:<br/>
          &lt;add key="SnapRepo.ForceHttps" value="False"/&gt;</li>
  </ul>

  <p>Same settings are in application:<br/><img src="https://raw.githubusercontent.com/huilaaja/SnapRepo/master/Screenshots/screencapture-settings.png" width="500" /></p>

  <h3>Requirements and inside information</h3>
  <p>Build with .NET Framework 4.6.1.</p>
  <h4>Depends on 4 NuGet Packages</h4>
  <ol>
      <li>WindowsAzure.Storage version="7.2.1"</li>
      <li>DotNetZip version="1.9.1.8"</li>
      <li>FluentScheduler version="5.0.0"</li>
      <li>Microsoft.Web.Administration version="7.0.0.0"</li>
  </ol>
  <!--<h4>Require SQL Server SMO DLLs <small>(these are included in project)</small></h4>
  <ol>
      <li>Microsoft.SqlServer.ConnectionInfo.dll version=13.0.0.0</li>
      <li>Microsoft.SqlServer.Management.Sdk.Sfc.dll version=13.0.0.0</li>
      <li>Microsoft.SqlServer.Smo.dll version=13.0.0.0</li>
      <li>Microsoft.SqlServer.SqlClrProvider.dll version=13.0.0.0</li>
      <li>Microsoft.SqlServer.SqlEnum.dll version=13.0.0.0</li>
  </ol>-->
  <h4>Transferring the backups</h4>
  <p>To be able to transferring the backups easily from environment to another, it's required that SnapRepo is installed in multiple the environments. Only the Azure Blob storage remain the same in all environments.</p>
  <h4>SnapRepo requires lot of privileges</h4>
  <p>You may use SnapRepo without restore function so then read priviledges are sufficient.</p>
  <ul>
      <li>IIS application pool user needs to have write and delete access to Local Repository -folder and AppData-folder.</li>
      <li>SQL Server user needs to have sysadmin (create, restore and query databases) privileges in SQL Server</li>
  </ul>
  <h3>Security and usage</h3>
  <p>By default this application uses basic authentication and check's that user belongs to one of this groups: WebAdmins, CmsAdmins, Administrators. But you can easily change authentication and use your own.</p>
  <p>With basic authentication it's recommended to use secured connection with HTTPS-protocol.</p>
  <p>It's also recommended to use IP-restrictions to restrain access from your network with IIS or implement it with URLRewrite module.</p>

</div>