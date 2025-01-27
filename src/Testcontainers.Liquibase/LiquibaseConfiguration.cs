namespace Testcontainers.Liquibase
{
  public sealed class LiquibaseConfiguration : ContainerConfiguration
  {
    public string Url { get; }
    public string Username { get; }
    public string Password { get; }
    public string ChangelogFile { get; }
    public string ReferenceUrl { get; }
    public string ReferenceUsername { get; }
    public string ReferencePassword { get; }
    public Action Action { get; }

    public LiquibaseConfiguration(
      string url = "",
      string username = "",
      string password = "",
      string changelogFile = "",
      string referenceUrl = "",
      string referenceUsername = "",
      string referencePassword = "",
      Action action = Action.None)
    {
      Url = url;
      Username = username;
      Password = password;
      ChangelogFile = changelogFile;
      ReferenceUrl = referenceUrl;
      ReferenceUsername = referenceUsername;
      ReferencePassword = referencePassword;
      Action = action;
    }

    public LiquibaseConfiguration(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
      : base(resourceConfiguration)
    { }

    public LiquibaseConfiguration(IContainerConfiguration resourceConfiguration)
      : base(resourceConfiguration)
    { }

    public LiquibaseConfiguration(LiquibaseConfiguration resourceConfiguration)
      : this(new LiquibaseConfiguration(), resourceConfiguration)
    { }

    public LiquibaseConfiguration(LiquibaseConfiguration oldValue, LiquibaseConfiguration newValue)
      : base(oldValue, newValue)
    {
      Url = BuildConfiguration.Combine(oldValue.Url, newValue.Url);
      Username = BuildConfiguration.Combine(oldValue.Username, newValue.Username);
      Password = BuildConfiguration.Combine(oldValue.Password, newValue.Password);
      ChangelogFile = BuildConfiguration.Combine(oldValue.ChangelogFile, newValue.ChangelogFile);
      ReferenceUrl = BuildConfiguration.Combine(oldValue.ReferenceUrl, newValue.ReferenceUrl);
      ReferenceUsername = BuildConfiguration.Combine(oldValue.ReferenceUsername, newValue.ReferenceUsername);
      ReferencePassword = BuildConfiguration.Combine(oldValue.ReferencePassword, newValue.ReferencePassword);
      Action = BuildConfiguration.Combine(oldValue.Action, newValue.Action);
    }
  }
}
