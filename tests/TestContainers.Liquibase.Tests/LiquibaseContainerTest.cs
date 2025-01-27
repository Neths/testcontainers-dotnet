using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using DotNet.Testcontainers.Volumes;
using Microsoft.Data.SqlClient;
using Testcontainers.Liquibase;
using Action = Testcontainers.Liquibase.Action;

namespace Testcontainers.MsSql;

public abstract class LiquibaseContainerTest : IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer;
    private readonly string _changelogOutputPath;
    private IContainer _liquibaseContainer;
    private const string DatabaseName = "LiquibaseTest";
    private const string Password = "quantacore_axaws_2024";
    private const string Username = "sa";
    private const string ChangelogFile = "db.changelog.xml";
    private readonly INetwork _network;
    private readonly IVolume _volume;

    private const string ScriptTestTable1 = @"
create database LiquibaseTest;
go

use LiquibaseTest;
go

CREATE TABLE [dbo].[TestTable1] (
    [ID]                    INT         NOT NULL,
    [stringColumn]		    VARCHAR(50) NOT NULL,
    [intColumnNullable]     INT         NULL,
    [floatColumnNullable]   FLOAT (53)  NULL,
    [dateColumnNullable]	DATE	    NULL,
    CONSTRAINT [PK_TestTable1] PRIMARY KEY CLUSTERED ([ID] ASC)
);
go
";

    public LiquibaseContainerTest()
    {
      using IOutputConsumer outputConsumer = Consume.RedirectStdoutAndStderrToConsole();
      _network = new NetworkBuilder()
        .WithName($"liquibase-test-network-{Guid.NewGuid()}")
        .Build();

      _volume = new VolumeBuilder()
        .WithName($"changelog-volume-{Guid.NewGuid()}")
        .Build();

      _changelogOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "changelog");
      Directory.CreateDirectory(_changelogOutputPath);
      var chmodProcess = new System.Diagnostics.Process
      {
        StartInfo = new System.Diagnostics.ProcessStartInfo
        {
          FileName = "chmod",
          Arguments = $"777 {_changelogOutputPath}",
          RedirectStandardOutput = true,
          UseShellExecute = false,
          CreateNoWindow = true,
        }
      };
      chmodProcess.Start();
      chmodProcess.WaitForExit();
      File.WriteAllText(Path.Combine(_changelogOutputPath, "db.changelog.xml"), "");
      File.Delete(Path.Combine(_changelogOutputPath, "db.changelog.xml"));

      _msSqlContainer = new MsSqlBuilder()
        .WithPassword(Password)
        .WithNetwork(_network)
        .WithNetworkAliases("sqlserver")
        .Build();

      _liquibaseContainer = new LiquibaseBuilder()
        .WithName($"test-liquibase-generate-{Guid.NewGuid()}")
        // .WithNetwork(_network)
        .WithCreateParameterModifier(cm =>
        {
          cm.User = "root";
        })
        .WithUrl("jdbc:sqlserver://172.17.0.2:1433;databaseName=dev_quantacore_aws;trustServerCertificate=true")
        .WithUsername(Username)
        .WithPassword(Password)
        .WithChangelogFile(ChangelogFile)
        .WithAction(Action.GenerateChangelog)
        .WithBindMount(_changelogOutputPath, "/liquibase/changelog", AccessMode.ReadWrite)
        .WithOutputConsumer(outputConsumer)
        .WithPrivileged(true)
        .WithAutoRemove(false)
        .Build();

      // _liquibaseContainer = new ContainerBuilder()
      //   .WithImage("liquibase/liquibase:latest")
      //   .WithName($"test-liquibase-generate-{Guid.NewGuid()}")
      //   .WithNetwork(_network)
      //   .WithCreateParameterModifier(cm =>
      //   {
      //     cm.User = "root";
      //   })
      //   .WithCommand(new[]
      //   {
      //     "--url=jdbc:sqlserver://sqlserver:1433" +
      //     ";databaseName=master;trustServerCertificate=true",
      //     "--username=sa",
      //     "--password=" + Password,
      //     "--changelog-file=/liquibase/changelog/db.changelog.xml",
      //     "generate-changelog"
      //   })
      //   .WithBindMount(_changelogOutputPath, "/liquibase/changelog", AccessMode.ReadWrite)
      //   // .WithVolumeMount(_volume, "/changelog", AccessMode.ReadWrite)
      //   .WithOutputConsumer(outputConsumer)
      //   .WithPrivileged(true)
      //   .WithAutoRemove(false)
      //   .Build();
    }

    private async Task WaitForFileGeneration(string filePath, int timeoutSeconds = 30)
    {
      var timeout = DateTime.Now.AddSeconds(timeoutSeconds);
      while (DateTime.Now < timeout)
      {
        if (File.Exists(filePath))
        {
          // Attendre un peu pour s'assurer que le fichier est complètement écrit
          await Task.Delay(1000);
          return;
        }
        await Task.Delay(500);
      }
      throw new TimeoutException($"Le fichier {filePath} n'a pas été généré dans le délai imparti");
    }

    public async Task InitializeAsync()
    {
      await _msSqlContainer.StartAsync();

      // Création d'une structure de base de données de test
      using (var connection = new SqlConnection(_msSqlContainer.GetConnectionString()))
      {
        await connection.OpenAsync();
        using (var command = connection.CreateCommand())
        {
          command.CommandText = @"
                    CREATE TABLE TestTable (
                        Id INT PRIMARY KEY IDENTITY(1,1),
                        Name NVARCHAR(100) NOT NULL,
                        CreatedDate DATETIME DEFAULT GETDATE()
                    );

                    CREATE TABLE AnotherTable (
                        Id INT PRIMARY KEY IDENTITY(1,1),
                        TestTableId INT FOREIGN KEY REFERENCES TestTable(Id),
                        Description NVARCHAR(MAX)
                    );";
          await command.ExecuteNonQueryAsync();
        }
      }
    }

    public async Task DisposeAsync()
    {
      if (_liquibaseContainer != null)
      {
        await _liquibaseContainer.DisposeAsync();
      }

      if (_msSqlContainer != null)
      {
        await _msSqlContainer.DisposeAsync();
      }

      // Nettoyage des fichiers générés
      if (Directory.Exists(_changelogOutputPath))
      {
        Directory.Delete(_changelogOutputPath, true);
      }
    }

    [Fact]
    public async Task GenerateChangelog_ShouldCreateChangelogFile()
    {
      // Act
      await _liquibaseContainer.StartAsync();

      // Attendre que le fichier soit généré
      await WaitForFileGeneration(Path.Combine(_changelogOutputPath, "db.changelog.xml"));

      var readBytes = await _liquibaseContainer.ReadFileAsync("/liquibase/changelog/db.changelog.xml")
        .ConfigureAwait(false);

      await File.WriteAllBytesAsync("db.changelog.xml", readBytes)
        .ConfigureAwait(false);

      // Assert
      string generatedChangelogPath = Path.Combine(_changelogOutputPath, "db.changelog.xml");
      Assert.True(File.Exists(generatedChangelogPath), "Le fichier changelog n'a pas été généré");

      string changelogContent = await File.ReadAllTextAsync(generatedChangelogPath);
      Assert.Contains("TestTable", changelogContent);
      Assert.Contains("AnotherTable", changelogContent);
      Assert.Contains("addForeignKeyConstraint", changelogContent);
    }

    // [Fact]
    // [Trait(nameof(DockerCli.DockerPlatform), nameof(DockerCli.DockerPlatform.Linux))]
    // public async Task ConnectionStateReturnsOpen()
    // {
    //   using IOutputConsumer outputConsumer = Consume.RedirectStdoutAndStderrToConsole();
    //
    //     // Given
    //     var connectionStringParts = _msSqlContainer.GetConnectionString().Split(';')
    //       .Select(x => x.Split('='))
    //       .ToDictionary(x => x[0], x => x[1]);
    //
    //     var jdbcConnectionString = string.Format($"jdbc:sqlserver://{connectionStringParts["Server"].Replace(',',':')};database={DatabaseName};");
    //
    //     if (connectionStringParts["TrustServerCertificate"].Equals(bool.TrueString))
    //       jdbcConnectionString += "encrypt=true;trustServerCertificate=true;";
    //
    //     var _liquibaseContainer =
    //       new LiquibaseBuilder()
    //         .WithUrl(jdbcConnectionString)
    //         .WithUsername(Username)
    //         .WithPassword(Password)
    //         .WithChangelogFile(ChangelogFile)
    //         .WithAction(Action.GenerateChangelog)
    //         .WithOutputConsumer(outputConsumer)
    //         .Build();
    //
    //     var execResult = await _msSqlContainer.ExecScriptAsync(ScriptTestTable1)
    //         .ConfigureAwait(true);
    //
    //     //when
    //     await _liquibaseContainer.StartAsync().ConfigureAwait(true);
    //
    //
    //     var readBytes = await _liquibaseContainer.ReadFileAsync($"/liquibase/changelog/{ChangelogFile}")
    //       .ConfigureAwait(true);
    //
    //     await File.WriteAllBytesAsync($"{ChangelogFile}", readBytes)
    //       .ConfigureAwait(true);
    //
    //
    //     // Then
    //     //Assert.Equal(ConnectionState.Open, connection.State);
    // }

    // [Fact]
    // [Trait(nameof(DockerCli.DockerPlatform), nameof(DockerCli.DockerPlatform.Linux))]
    // public async Task ExecScriptReturnsSuccessful()
    // {
    //     // Given
    //     const string scriptContent = "SELECT 1;";
    //
    //     // When
    //     var execResult = await _msSqlContainer.ExecScriptAsync(scriptContent)
    //         .ConfigureAwait(true);
    //
    //     // Then
    //     Assert.True(0L.Equals(execResult.ExitCode), execResult.Stderr);
    //     Assert.Empty(execResult.Stderr);
    // }
}

[UsedImplicitly]
public sealed class LiquibaseDefaultConfiguration : LiquibaseContainerTest
{
  public LiquibaseDefaultConfiguration()
    : base()
  {
  }
}
