using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using EprPrnIntegration.Api.Functions;
using EprPrnIntegration.Api.Models;
using EprPrnIntegration.Api.Services;
using EprPrnIntegration.Api.UnitTests.Helpers;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Exceptions;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Rpd;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.RESTServices.PrnBackendService.Interfaces;
using EprPrnIntegration.Common.RESTServices.RrepwService;
using EprPrnIntegration.Common.RESTServices.RrepwService.Interfaces;
using EprPrnIntegration.Common.RESTServices.WasteOrganisationsService.Interfaces;
using EprPrnIntegration.Common.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EprPrnIntegration.Api.UnitTests;

public class FetchRrepwIssuedPrnsFunctionTests
{
    private readonly Guid _organisationId = Guid.NewGuid();
    private readonly string _organisationTypeCode = WoApiOrganisationType.ComplianceScheme;
    private readonly Mock<ILogger<FetchRrepwIssuedPrnsFunction>> _loggerMock = new();
    private readonly Mock<ILastUpdateService> _lastUpdateServiceMock = new();
    private readonly Mock<IRrepwService> _rrepwServiceMock = new();
    private readonly Mock<IPrnService> _prnServiceMock = new();
    private readonly Mock<IWasteOrganisationsService> _woService = new();
    private readonly Mock<IProducerEmailService> _producerEmailServiceMock = new();
    private readonly IOptions<FetchRrepwIssuedPrnsConfiguration> _config = Options.Create(
        new FetchRrepwIssuedPrnsConfiguration { DefaultStartDate = "2024-01-01" }
    );

    private readonly FetchRrepwIssuedPrnsFunction _function;
    private readonly Fixture _fixture = new();
    private const int _year = 2025;

    public FetchRrepwIssuedPrnsFunctionTests()
    {
        _function = new(
            _lastUpdateServiceMock.Object,
            _loggerMock.Object,
            _rrepwServiceMock.Object,
            _prnServiceMock.Object,
            _config,
            _woService.Object,
            _producerEmailServiceMock.Object
        );
        SetupGetOrganisation(_organisationId, _organisationTypeCode);
    }

    [Fact]
    public async Task ProcessesMultiplePrns()
    {
        var prns = new List<PackagingRecyclingNote>
        {
            CreatePrn("PRN-001"),
            CreatePrn("PRN-002"),
            CreatePrn("PRN-003"),
        };

        SetupSavePrns(prns);

        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync(DateTime.MinValue);

        _rrepwServiceMock
            .Setup(x =>
                x.ListPackagingRecyclingNotes(
                    ItEx.IsCloseTo(DateTime.MinValue),
                    ItEx.IsCloseTo(DateTime.UtcNow)
                )
            )
            .ReturnsAsync(prns);

        await _function.Run(new TimerInfo());

        _prnServiceMock.Verify(
            x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-001"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _prnServiceMock.Verify(
            x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-002"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _prnServiceMock.Verify(
            x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-003"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _lastUpdateServiceMock.Verify(
            x =>
                x.SetLastUpdate(FunctionName.FetchRrepwIssuedPrns, ItEx.IsCloseTo(DateTime.UtcNow)),
            Times.Once
        );
    }

    [Fact]
    public async Task WhenRrepwPrnServiceThrows_DoesNotProcessPrns()
    {
        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync(DateTime.MinValue);
        _rrepwServiceMock
            .Setup(x => x.ListPackagingRecyclingNotes(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        await Assert.ThrowsAsync<HttpRequestException>(() => _function.Run(new TimerInfo()));

        // Verify no PRNs were saved
        _prnServiceMock.Verify(
            x => x.SavePrn(It.IsAny<SavePrnDetailsRequest>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        // Verify last update was NOT set
        _lastUpdateServiceMock.Verify(
            x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never
        );
    }

    [Fact]
    public async Task WhenRrepwPrnServiceReturnsZeroItems_DoesNotSetLastUpdateTime()
    {
        var emptyPrnsList = new List<PackagingRecyclingNote>();

        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync(DateTime.MinValue);
        _rrepwServiceMock
            .Setup(x => x.ListPackagingRecyclingNotes(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(emptyPrnsList);

        await _function.Run(new TimerInfo());

        // Verify no PRNs were saved
        _prnServiceMock.Verify(
            x => x.SavePrn(It.IsAny<SavePrnDetailsRequest>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        // Verify last update was NOT set
        _lastUpdateServiceMock.Verify(
            x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never
        );
    }

    [Fact]
    public async Task WhenOnePrnSaveFails_ContinuesProcessingAndUpdatesLastUpdatedTime()
    {
        var prns = new List<PackagingRecyclingNote>
        {
            CreatePrn("PRN-001"),
            CreatePrn("PRN-003"),
            CreatePrn("PRN-002-fails"),
        };
        SetupSavePrns(prns.Take(2));
        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync(DateTime.MinValue);

        _rrepwServiceMock
            .Setup(x =>
                x.ListPackagingRecyclingNotes(
                    ItEx.IsCloseTo(DateTime.MinValue),
                    ItEx.IsCloseTo(DateTime.UtcNow)
                )
            )
            .ReturnsAsync(prns);
        _prnServiceMock
            .Setup(x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-002-fails"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        await _function.Run(new TimerInfo());

        // Verify all three PRNs were attempted
        _prnServiceMock.Verify(
            x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-001"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _prnServiceMock.Verify(
            x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-002-fails"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _prnServiceMock.Verify(
            x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-003"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        // Verify last update WAS still set despite one failure
        _lastUpdateServiceMock.Verify(
            x =>
                x.SetLastUpdate(FunctionName.FetchRrepwIssuedPrns, ItEx.IsCloseTo(DateTime.UtcNow)),
            Times.Once
        );
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task WhenTransientErrorOccurs_ForPrnService_RethrowsExceptionAndDoesNotUpdateLastUpdatedTime(
        HttpStatusCode statusCode
    )
    {
        var prns = new List<PackagingRecyclingNote>
        {
            CreatePrn("PRN-001"),
            CreatePrn("PRN-002-transient"),
        };

        SetupSavePrns(prns.Take(1));

        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync(DateTime.MinValue);
        _rrepwServiceMock
            .Setup(x => x.ListPackagingRecyclingNotes(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(prns);

        _prnServiceMock
            .Setup(x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-002-transient"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new HttpResponseMessage(statusCode));

        // Act & Assert - expect the exception to be rethrown
        await Assert.ThrowsAsync<ServiceException>(() => _function.Run(new TimerInfo()));

        // Verify last update was NOT set when transient error occurs
        _lastUpdateServiceMock.Verify(
            x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never
        );
    }

    private void SetupSavePrns(IEnumerable<PackagingRecyclingNote> prns)
    {
        foreach (var prn in prns)
        {
            _prnServiceMock
                .Setup(x =>
                    x.SavePrn(
                        It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == prn.PrnNumber),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted));
        }
    }

    [Fact]
    public async Task WhenNoBlobStorageValueExists_UsesDefaultStartDateFromConfiguration()
    {
        var prns = new List<PackagingRecyclingNote> { CreatePrn("PRN-001"), CreatePrn("PRN-002") };
        SetupSavePrns(prns);
        var expectedStartDate = DateTime.SpecifyKind(
            DateTime.ParseExact(
                _config.Value.DefaultStartDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture
            ),
            DateTimeKind.Utc
        );

        // Setup: GetLastUpdate returns null (no blob storage value)
        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync((DateTime?)null);

        _rrepwServiceMock
            .Setup(x => x.ListPackagingRecyclingNotes(expectedStartDate, It.IsAny<DateTime>()))
            .ReturnsAsync(prns);

        await _function.Run(new TimerInfo());

        // Verify GetLastUpdate was called
        _lastUpdateServiceMock.Verify(
            x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns),
            Times.Once
        );

        // Verify PRNs were processed
        _prnServiceMock.Verify(
            x => x.SavePrn(It.IsAny<SavePrnDetailsRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );

        // Verify last update was set
        _lastUpdateServiceMock.Verify(
            x =>
                x.SetLastUpdate(FunctionName.FetchRrepwIssuedPrns, ItEx.IsCloseTo(DateTime.UtcNow)),
            Times.Once
        );
    }

    [Fact]
    public async Task StubbedRrepwService_WorksWithMappersAndFunction()
    {
        // Arrange - use concrete StubbedRrepwService instead of mock
        var rrepwApiConfig = Options.Create(
            new RrepwApiConfiguration { StubOrgId = "0b51240c-c013-4973-9d06-d4f90ee4ad8b" }
        );
        var stubbedRrepwService = new StubbedRrepwService(
            Mock.Of<ILogger<StubbedRrepwService>>(),
            rrepwApiConfig
        );

        //get the stubbed data
        var prns = await stubbedRrepwService.ListPackagingRecyclingNotes(
            new DateTime(),
            new DateTime()
        );

        SetupGetOrganisation(_organisationId, _organisationTypeCode);
        var lastUpdateServiceMock = new Mock<ILastUpdateService>();
        var prnServiceMock = new Mock<IPrnService>();

        lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(It.IsAny<string>()))
            .ReturnsAsync(DateTime.MinValue);

        prnServiceMock
            .Setup(x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req =>
                        req.PrnNumber == "STUB-12345"
                        && req.AccreditationYear == "2026"
                        && req.MaterialName != null
                        && req.ProcessToBeUsed != null
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted));
        var function = new FetchRrepwIssuedPrnsFunction(
            lastUpdateServiceMock.Object,
            Mock.Of<ILogger<FetchRrepwIssuedPrnsFunction>>(),
            stubbedRrepwService,
            prnServiceMock.Object,
            _config,
            _woService.Object,
            _producerEmailServiceMock.Object
        );

        // Act
        await function.Run(new TimerInfo());

        // Verify last update was set
        lastUpdateServiceMock.Verify(
            x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Once
        );
    }

    private WoApiOrganisation SetupGetOrganisation(
        Guid organisationId,
        string organisationTypeCode,
        string? businessCountry = null
    )
    {
        var registration = _fixture
            .Build<WoApiRegistration>()
            .With(w => w.Type, organisationTypeCode)
            .With(w => w.RegistrationYear, _year)
            .With(w => w.Status, WoApiOrganisationStatus.Registered)
            .Create();

        var organisation = _fixture
            .Build<WoApiOrganisation>()
            .With(o => o.Id, organisationId)
            .With(o => o.BusinessCountry, businessCountry)
            .Create();

        // Set Registrations after Create() to avoid AutoFixture overwriting it
        organisation.Registrations = [registration];

        // Use Newtonsoft.Json to serialize since that's what the actual code uses to deserialize
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(organisation);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        _woService
            .Setup(o => o.GetOrganisation(organisationId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage { Content = content });
        return organisation;
    }

    private PackagingRecyclingNote CreatePrn(
        string evidenceNo,
        string accreditationNo = "ACC-001",
        int accreditationYear = _year,
        string material = RrepwMaterialName.Plastic,
        int tonnes = 100
    )
    {
        return new PackagingRecyclingNote
        {
            Id = Guid.NewGuid().ToString(),
            PrnNumber = evidenceNo,
            Status = new Status
            {
                CurrentStatus = "ACTIVE",
                AuthorisedAt = DateTime.UtcNow.AddDays(-30),
            },
            IssuedByOrganisation = new Organisation
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Issuer Organization",
            },
            IssuedToOrganisation = new Organisation
            {
                Id = _organisationId.ToString(),
                Name = "Test Recipient Organization",
            },
            Accreditation = new Accreditation
            {
                Id = Guid.NewGuid().ToString(),
                AccreditationNumber = accreditationNo,
                AccreditationYear = accreditationYear,
                Material = material,
                SubmittedToRegulator = RrepwSubmittedToRegulator.EnvironmentAgency_EA,
            },
            IsDecemberWaste = false,
            IsExport = false,
            TonnageValue = tonnes,
            IssuerNotes = "Test PRN",
        };
    }

    [Fact]
    public async Task ProcessesPrns_ShouldSendEmails()
    {
        var prns = new List<PackagingRecyclingNote>
        {
            CreatePrn("PRN-001"),
            CreatePrn("PRN-002"),
            CreatePrn("PRN-003"),
        };
        prns[0].IssuedToOrganisation!.Id = Guid.NewGuid().ToString();
        prns[1].IssuedToOrganisation!.Id = Guid.NewGuid().ToString();
        prns[2].IssuedToOrganisation!.Id = Guid.NewGuid().ToString();
        prns[0].Status!.CurrentStatus = RrepwStatus.Cancelled;
        prns[1].Status!.CurrentStatus = RrepwStatus.AwaitingAcceptance;
        prns[2].Status!.CurrentStatus = RrepwStatus.AwaitingAcceptance;
        var emails = _fixture.CreateMany<List<PersonEmail>>().ToList();
        var orgTypes = new List<string>
        {
            WoApiOrganisationType.LargeProducer,
            WoApiOrganisationType.LargeProducer,
            WoApiOrganisationType.ComplianceScheme,
        };
        SetupSavePrns(prns);
        _rrepwServiceMock
            .Setup(x => x.ListPackagingRecyclingNotes(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(prns);

        List<WoApiOrganisation> orgs = new List<WoApiOrganisation>();
        for (int i = 0; i < 3; i++)
        {
            orgs.Add(
                SetupGetOrganisation(Guid.Parse(prns[i].IssuedToOrganisation!.Id!), orgTypes[i])
            );
        }

        await _function.Run(new TimerInfo());

        for (int i = 0; i < 3; i++)
        {
            _producerEmailServiceMock.Verify(e =>
                e.SendEmailToProducersAsync(
                    It.Is<SavePrnDetailsRequest>(p => p.PrnNumber == prns[i].PrnNumber),
                    It.Is<WoApiOrganisation>(o => o.Id == orgs[i].Id)
                )
            );
        }
    }

    [Theory]
    [InlineData(WoApiBusinessCountry.England, RpdReprocessorExporterAgency.EnvironmentAgency)]
    [InlineData(
        WoApiBusinessCountry.NorthernIreland,
        RpdReprocessorExporterAgency.NorthernIrelandEnvironmentAgency
    )]
    [InlineData(
        WoApiBusinessCountry.Scotland,
        RpdReprocessorExporterAgency.ScottishEnvironmentProtectionAgency
    )]
    [InlineData(WoApiBusinessCountry.Wales, RpdReprocessorExporterAgency.NaturalResourcesWales)]
    public async Task Run_ShouldMapProducerFields_ForValidBusinessCountries(
        string businessCountry,
        string expectedAgency
    )
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var prn = CreatePrn("PRN-001");
        prn.IssuedToOrganisation!.Id = orgId.ToString();

        SetupGetOrganisation(orgId, WoApiOrganisationType.ComplianceScheme, businessCountry);

        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync(DateTime.MinValue);

        _rrepwServiceMock
            .Setup(x => x.ListPackagingRecyclingNotes(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PackagingRecyclingNote> { prn });

        _prnServiceMock
            .Setup(x => x.SavePrn(It.IsAny<SavePrnDetailsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted));

        // Act
        await _function.Run(new TimerInfo());

        // Assert
        _prnServiceMock.Verify(
            x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req =>
                        req.PrnNumber == "PRN-001"
                        && req.PackagingProducer == expectedAgency
                        && req.ProducerAgency == expectedAgency
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("GB-XYZ")]
    [InlineData("")]
    public async Task Run_ShouldMapProducerFieldsToNull_ForInvalidBusinessCountries(
        string businessCountry
    )
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var prn = CreatePrn("PRN-002");
        prn.IssuedToOrganisation!.Id = orgId.ToString();

        SetupGetOrganisation(orgId, WoApiOrganisationType.ComplianceScheme, businessCountry);

        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync(DateTime.MinValue);

        _rrepwServiceMock
            .Setup(x => x.ListPackagingRecyclingNotes(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PackagingRecyclingNote> { prn });

        _prnServiceMock
            .Setup(x => x.SavePrn(It.IsAny<SavePrnDetailsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted));

        // Act
        await _function.Run(new TimerInfo());

        // Assert
        _prnServiceMock.Verify(
            x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req =>
                        req.PrnNumber == "PRN-002"
                        && req.PackagingProducer == null
                        && req.ProducerAgency == null
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Run_ShouldMapProducerFieldsToNull_WhenBusinessCountryIsNull()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var prn = CreatePrn("PRN-003");
        prn.IssuedToOrganisation!.Id = orgId.ToString();

        SetupGetOrganisation(orgId, WoApiOrganisationType.ComplianceScheme, null);

        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync(DateTime.MinValue);

        _rrepwServiceMock
            .Setup(x => x.ListPackagingRecyclingNotes(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PackagingRecyclingNote> { prn });

        _prnServiceMock
            .Setup(x => x.SavePrn(It.IsAny<SavePrnDetailsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted));

        // Act
        await _function.Run(new TimerInfo());

        // Assert
        _prnServiceMock.Verify(
            x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req =>
                        req.PrnNumber == "PRN-003"
                        && req.PackagingProducer == null
                        && req.ProducerAgency == null
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    #region GetWoApiOrganisation Transient Error Tests

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task WhenTransientErrorOccurs_ForWoService_RethrowsExceptionAndDoesNotUpdateLastUpdatedTime(
        HttpStatusCode statusCode
    )
    {
        // Arrange
        var prns = new List<PackagingRecyclingNote> { CreatePrn("PRN-001"), CreatePrn("PRN-002") };

        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync(DateTime.MinValue);

        _rrepwServiceMock
            .Setup(x => x.ListPackagingRecyclingNotes(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(prns);

        // Setup WO service to return transient error for the first PRN's organisation
        _woService
            .Setup(o =>
                o.GetOrganisation(prns[0].IssuedToOrganisation!.Id!, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new HttpResponseMessage(statusCode));

        // Act & Assert - expect the exception to be rethrown
        await Assert.ThrowsAsync<ServiceException>(() => _function.Run(new TimerInfo()));

        // Verify no PRNs were saved
        _prnServiceMock.Verify(
            x => x.SavePrn(It.IsAny<SavePrnDetailsRequest>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        // Verify last update was NOT set when transient error occurs
        _lastUpdateServiceMock.Verify(
            x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never
        );

        // Verify appropriate error was logged
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (o, t) =>
                            o.ToString()!.Contains("transient error")
                            && o.ToString()!.Contains("terminating")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task WhenNonTransientErrorOccurs_ForWoService_ContinuesProcessingAndUpdatesLastUpdatedTime(
        HttpStatusCode statusCode
    )
    {
        // Arrange
        var orgId1 = Guid.NewGuid();
        var orgId2 = Guid.NewGuid();

        var prn1 = CreatePrn("PRN-001");
        prn1.IssuedToOrganisation!.Id = orgId1.ToString();

        var prn2 = CreatePrn("PRN-002");
        prn2.IssuedToOrganisation!.Id = orgId2.ToString();

        var prns = new List<PackagingRecyclingNote> { prn1, prn2 };

        SetupSavePrns(prns);

        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync(DateTime.MinValue);

        _rrepwServiceMock
            .Setup(x => x.ListPackagingRecyclingNotes(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(prns);

        // Setup WO service to return non-transient error for the first PRN's organisation
        _woService
            .Setup(o => o.GetOrganisation(orgId1.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(statusCode));

        // Setup WO service to succeed for the second PRN
        SetupGetOrganisation(orgId2, WoApiOrganisationType.ComplianceScheme);

        // Act
        await _function.Run(new TimerInfo());

        // Verify PRN-001 (with error) was NOT saved, PRN-002 was saved
        _prnServiceMock.Verify(
            x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-001"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never,
            "PRN-001 should not be saved when GetWoApiOrganisation returns non-transient error"
        );

        _prnServiceMock.Verify(
            x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-002"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once,
            "PRN-002 should be saved normally"
        );

        // Verify last update WAS set despite the non-transient error
        _lastUpdateServiceMock.Verify(
            x =>
                x.SetLastUpdate(FunctionName.FetchRrepwIssuedPrns, ItEx.IsCloseTo(DateTime.UtcNow)),
            Times.Once
        );

        // Verify appropriate error was logged for non-transient error
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (o, t) =>
                            o.ToString()!.Contains("non transient error")
                            && o.ToString()!.Contains("continuing with next")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task WhenExceptionThrown_ForWoService_ContinuesProcessingAndUpdatesLastUpdatedTime()
    {
        // Arrange
        var orgId1 = Guid.NewGuid();
        var orgId2 = Guid.NewGuid();

        var prn1 = CreatePrn("PRN-001");
        prn1.IssuedToOrganisation!.Id = orgId1.ToString();

        var prn2 = CreatePrn("PRN-002");
        prn2.IssuedToOrganisation!.Id = orgId2.ToString();

        var prns = new List<PackagingRecyclingNote> { prn1, prn2 };

        SetupSavePrns(prns);

        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync(DateTime.MinValue);

        _rrepwServiceMock
            .Setup(x => x.ListPackagingRecyclingNotes(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(prns);

        // Setup WO service to throw an exception for the first PRN's organisation
        _woService
            .Setup(o => o.GetOrganisation(orgId1.ToString(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Setup WO service to succeed for the second PRN
        SetupGetOrganisation(orgId2, WoApiOrganisationType.ComplianceScheme);

        // Act
        await _function.Run(new TimerInfo());

        // Verify PRN-001 (with exception) was NOT saved, PRN-002 was saved
        _prnServiceMock.Verify(
            x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-001"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never,
            "PRN-001 should not be saved when GetWoApiOrganisation throws exception"
        );

        _prnServiceMock.Verify(
            x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-002"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once,
            "PRN-002 should be saved normally"
        );

        // Verify last update WAS set despite the exception
        _lastUpdateServiceMock.Verify(
            x =>
                x.SetLastUpdate(FunctionName.FetchRrepwIssuedPrns, ItEx.IsCloseTo(DateTime.UtcNow)),
            Times.Once
        );

        // Verify appropriate error was logged for the exception
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (o, t) => o.ToString()!.Contains("exception, continuing with next")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task WhenWoServiceReturnsNull_ContinuesProcessingWithNullOrganisation()
    {
        // Arrange
        var orgId1 = Guid.NewGuid();
        var orgId2 = Guid.NewGuid();

        var prn1 = CreatePrn("PRN-001");
        prn1.IssuedToOrganisation!.Id = orgId1.ToString();

        var prn2 = CreatePrn("PRN-002");
        prn2.IssuedToOrganisation!.Id = orgId2.ToString();

        var prns = new List<PackagingRecyclingNote> { prn1, prn2 };

        SetupSavePrns(prns);

        _lastUpdateServiceMock
            .Setup(x => x.GetLastUpdate(FunctionName.FetchRrepwIssuedPrns))
            .ReturnsAsync(DateTime.MinValue);

        _rrepwServiceMock
            .Setup(x => x.ListPackagingRecyclingNotes(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(prns);

        // Setup WO service to return empty content (results in null after deserialization)
        _woService
            .Setup(o => o.GetOrganisation(orgId1.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") }
            );

        // Setup WO service to succeed for the second PRN
        SetupGetOrganisation(orgId2, WoApiOrganisationType.ComplianceScheme);

        // Act
        await _function.Run(new TimerInfo());

        // Verify PRN-001 (with null org) was NOT saved, PRN-002 was saved
        _prnServiceMock.Verify(
            x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-001"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never,
            "PRN-001 should not be saved when GetWoApiOrganisation returns null"
        );

        _prnServiceMock.Verify(
            x =>
                x.SavePrn(
                    It.Is<SavePrnDetailsRequest>(req => req.PrnNumber == "PRN-002"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once,
            "PRN-002 should be saved normally"
        );

        // Verify last update WAS set
        _lastUpdateServiceMock.Verify(
            x =>
                x.SetLastUpdate(FunctionName.FetchRrepwIssuedPrns, ItEx.IsCloseTo(DateTime.UtcNow)),
            Times.Once
        );
    }

    #endregion
}
