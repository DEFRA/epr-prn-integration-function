using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using EprPrnIntegration.Api.Functions;
using EprPrnIntegration.Api.Models;
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
    private readonly Mock<ICoreServices> _core = new();
    private readonly Mock<IOrganisationService> _organisationService = new();
    private readonly Mock<IMessagingServices> _messagingServices = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IWasteOrganisationsService> _woService = new();
    private readonly IOptions<FetchRrepwIssuedPrnsConfiguration> _config = Options.Create(
        new FetchRrepwIssuedPrnsConfiguration { DefaultStartDate = "2024-01-01" }
    );

    private readonly FetchRrepwIssuedPrnsFunction _function;
    private readonly Fixture _fixture = new();

    public FetchRrepwIssuedPrnsFunctionTests()
    {
        _core.Setup(c => c.OrganisationService).Returns(_organisationService.Object);
        _messagingServices.Setup(c => c.EmailService).Returns(_emailService.Object);
        _function = new(
            _lastUpdateServiceMock.Object,
            _loggerMock.Object,
            _rrepwServiceMock.Object,
            _prnServiceMock.Object,
            _config,
            _core.Object,
            _messagingServices.Object,
            _woService.Object
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
        var stubbedRrepwService = new StubbedRrepwService(Mock.Of<ILogger<StubbedRrepwService>>());

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
            _core.Object,
            _messagingServices.Object,
            _woService.Object
        );

        // Act
        await function.Run(new TimerInfo());

        // Verify last update was set
        lastUpdateServiceMock.Verify(
            x => x.SetLastUpdate(It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Once
        );
    }

    private void SetupGetOrganisation(Guid organisationId, string organisationTypeCode)
    {
        _woService
            .Setup(o => o.GetOrganisation(organisationId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new HttpResponseMessage
                {
                    Content = JsonContent.Create(
                        _fixture
                            .Build<WoApiOrganisation>()
                            .With(o => o.Id, organisationId)
                            .With(
                                o => o.Registration,
                                _fixture
                                    .Build<WoApiRegistration>()
                                    .With(w => w.Type, organisationTypeCode)
                                    .Create()
                            )
                            .Create()
                    ),
                }
            );
    }

    private PackagingRecyclingNote CreatePrn(
        string evidenceNo,
        string accreditationNo = "ACC-001",
        int accreditationYear = 2025,
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

        for (int i = 0; i < 3; i++)
        {
            SetupGetOrganisation(Guid.Parse(prns[i].IssuedToOrganisation!.Id!), orgTypes[i]);
            SetupGetEmails(emails[i], prns[i].IssuedToOrganisation!.Id!, orgTypes[i]);
        }

        await _function.Run(new TimerInfo());

        _emailService.Verify(e =>
            e.SendCancelledPrnsNotificationEmails(
                It.Is<List<ProducerEmail>>(pe => VerifyProducerEmail(pe, emails[0], prns[0])),
                prns[0].IssuedToOrganisation!.Id!
            )
        );
        for (int i = 1; i < 3; i++)
        {
            _emailService.Verify(e =>
                e.SendEmailsToProducers(
                    It.Is<List<ProducerEmail>>(pe => VerifyProducerEmail(pe, emails[i], prns[i])),
                    prns[i].IssuedToOrganisation!.Id!
                )
            );
        }
    }

    private static bool VerifyProducerEmail(
        List<ProducerEmail> producerEmails,
        List<PersonEmail> personEmails,
        PackagingRecyclingNote packagingRecyclingNote
    )
    {
        var valid = true;
        valid &= producerEmails.Count == personEmails.Count;
        for (int i = 0; i < producerEmails.Count; i++)
        {
            valid &= producerEmails[i].EmailAddress == personEmails[i].Email;
            valid &= producerEmails[i].FirstName == personEmails[i].FirstName;
            valid &= producerEmails[i].LastName == personEmails[i].LastName;
            valid &= producerEmails[i].IsExporter == packagingRecyclingNote.IsExport;
            valid &= producerEmails[i]
                .Material.Equals(
                    packagingRecyclingNote.Accreditation!.Material,
                    StringComparison.CurrentCultureIgnoreCase
                );
            valid &=
                producerEmails[i].NameOfExporterReprocessor
                == packagingRecyclingNote.IssuedByOrganisation!.Name;
            valid &=
                producerEmails[i].NameOfProducerComplianceScheme
                == packagingRecyclingNote.IssuedToOrganisation!.Name;
            valid &= producerEmails[i].PrnNumber == packagingRecyclingNote.PrnNumber;
            valid &= producerEmails[i].Tonnage == packagingRecyclingNote.TonnageValue;
        }
        return valid;
    }

    private void SetupGetEmails(List<PersonEmail> emails, string orgId, string orgType)
    {
        var ot =
            orgType == WoApiOrganisationType.ComplianceScheme
                ? OrganisationType.ComplianceScheme_CS
                : OrganisationType.LargeProducer_DR;
        _organisationService
            .Setup(o => o.GetPersonEmailsAsync(orgId, ot, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emails);
    }
}
