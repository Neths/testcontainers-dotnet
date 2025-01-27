namespace Testcontainers.Liquibase
{
  public sealed class LiquibaseBuilder : ContainerBuilder<LiquibaseBuilder, LiquibaseContainer, LiquibaseConfiguration>
  {
    public const string LiquibaseImage = "liquibase:4.31.0-alpine";

    public LiquibaseBuilder()
      : this(new LiquibaseConfiguration())
    {
      DockerResourceConfiguration = Init().DockerResourceConfiguration;
    }

    public LiquibaseBuilder(LiquibaseConfiguration resourceConfiguration)
      : base(resourceConfiguration)
    {
      DockerResourceConfiguration = resourceConfiguration;
    }

    protected override LiquibaseConfiguration DockerResourceConfiguration { get; }
    public override LiquibaseContainer Build()
    {
      Validate();
      return new LiquibaseContainer(DockerResourceConfiguration);
    }

    protected override LiquibaseBuilder Init()
    {
      return base.Init()
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Liquibase command '.*?' was executed successfully"))
        .WithImage(LiquibaseImage);
    }

    protected override void Validate()
    {
      base.Validate();

      // _ = Guard.Argument(DockerResourceConfiguration.Url, nameof(DockerResourceConfiguration.Url))
      //   .NotEmpty()
      //   .NotNull();
    }

    protected override LiquibaseBuilder Merge(LiquibaseConfiguration oldValue, LiquibaseConfiguration newValue)
    {
      return new LiquibaseBuilder(new LiquibaseConfiguration(oldValue, newValue));
    }

    protected override LiquibaseBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
    {
      return Merge(DockerResourceConfiguration, new LiquibaseConfiguration(resourceConfiguration));
    }

    protected override LiquibaseBuilder Clone(IContainerConfiguration resourceConfiguration)
    {
      return Merge(DockerResourceConfiguration, new LiquibaseConfiguration(resourceConfiguration));
    }

    public LiquibaseBuilder WithUsername(string username)
    {
      return Merge(DockerResourceConfiguration, new LiquibaseConfiguration(username: username))
        .WithEnvironment("LIQUIBASE_COMMAND_USERNAME", username);
    }

    public LiquibaseBuilder WithPassword(string password)
    {
      return Merge(DockerResourceConfiguration, new LiquibaseConfiguration(password: password))
        .WithEnvironment("LIQUIBASE_COMMAND_PASSWORD", password);
    }

    public LiquibaseBuilder WithUrl(string url)
    {
      return Merge(DockerResourceConfiguration, new LiquibaseConfiguration(url: url))
        .WithEnvironment("LIQUIBASE_COMMAND_URL", url);
    }

    public LiquibaseBuilder WithChangelogFile(string changelogFile)
    {
      return Merge(DockerResourceConfiguration, new LiquibaseConfiguration(changelogFile: changelogFile))
        .WithEnvironment("LIQUIBASE_COMMAND_CHANGELOG_FILE", changelogFile);
    }

    public LiquibaseBuilder WithAction(Action action)
    {
      return Merge(DockerResourceConfiguration, new LiquibaseConfiguration(action: action))
        .WithCommand(new [] {CommandMapping(action)});
    }

    private string CommandMapping(Action action) => action switch
    {
      Action.Update => "update",
      Action.GenerateChangelog => "generate-changelog",
      Action.None => string.Empty,
      _ => string.Empty,
    };
  }
}
