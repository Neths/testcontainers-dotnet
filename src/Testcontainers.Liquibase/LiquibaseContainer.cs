using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Testcontainers.Liquibase
{

  // docker run
  // -u root
  // -v $(pwd):/liquibase/changelog
  // liquibase
  // generate-changelog
  // --changelog-file=/liquibase/changelog/test2.xml
  // --url="jdbc:sqlserver://172.17.0.2:1433;database=QuantaCore_Standard_MVA;encrypt=true;trustServerCertificate=true"
  // --username=sa
  // --password=quantacore_axaws_2024
  public sealed class LiquibaseContainer : DockerContainer
  {
    public LiquibaseContainer(IContainerConfiguration configuration)
      : base(configuration)
    {
    }
  }
}
