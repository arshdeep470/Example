using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shield.Common;
using Shield.Common.Models.Common;
using Shield.Common.Models.Loto.Shared;
using Shield.Ui.App.Common;
using Shield.Ui.App.Models.CommonModels;
using Shield.Ui.App.Models.HecpModels;
using Shield.Ui.App.Services;
using Shield.Ui.App.Tests.Helpers;
using Shield.Ui.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shield.Ui.App.Tests.Views.HECP
{
    [TestClass]
    public class HecpDashBoardTests
    {
        private HtmlParser parser;
        private HttpClient httpClient;
        private Mock<HttpClientService> _mockHttpClientService;
        private Mock<UserService> _mockUserService;
        private Mock<SessionService> _mockSessionService;
        private Mock<HecpService> _mockHecpService;
        private Mock<LotoService> _mockLotoService;
        private Mock<AirplaneDataService> _mockAirplaneDataService;
        private User _user;

        [TestInitialize]
        public void Setup()
        {
            parser = new HtmlParser();

            _user = new User()
            {
                Permissions = new List<string>()
                {
                    Shield.Common.Constants.Permission.SIGN_DISCRETE
                },
                HecpApproverDetails = new List<HecpApproverDetails>()
                {
                    new HecpApproverDetails()
                    {
                        BemsId=123,
                        Site="BSC",
                        Program="787",
                        IsHecpPublisher=true
                    }
                },
                Programs = "787",
                BemsId = 123456
            };

            _mockHttpClientService = new Mock<HttpClientService>();
            _mockUserService = new Mock<UserService>(_mockHttpClientService.Object);
            _mockLotoService = new Mock<LotoService>(_mockHttpClientService.Object);
            _mockAirplaneDataService = new Mock<AirplaneDataService>(_mockHttpClientService.Object, _mockLotoService.Object);
            _mockSessionService = new Mock<SessionService>();
            _mockHecpService = new Mock<HecpService>(_mockHttpClientService.Object);

            string rootDirectory = TestHelper.GetUiBaseDirectory() + "Shield.Ui.App";

            var builder = new WebApplicationFactory<Program>()
                   .WithWebHostBuilder(builder =>
                   {
                       builder.ConfigureServices(services =>
                       {
                           services.AddTransient<UserService>(ctx => { return (UserService)_mockUserService.Object; });
                           services.AddTransient<HecpService>(ctx => { return (HecpService)_mockHecpService.Object; });
                           services.AddTransient<SessionService>(ctx => { return (SessionService)_mockSessionService.Object; });
                           services.AddTransient<AirplaneDataService>(ctx => { return (AirplaneDataService)_mockAirplaneDataService.Object; });
                       });
                   });

            httpClient = builder.CreateClient();

            httpClient.DefaultRequestHeaders.Add("boeingbemsid", "2728906");
            Constants.MockHeaderFlag = false;
        }

        [TestMethod]
        public async Task Should_Not_Show_Edit_Button_If_User_Is_Not_ITAR_For_ITAR_Hecp()
        {
            _user.Role = new Shield.Common.Models.Users.Role()
            {
                Name = Constants.GROUP_COORDINATOR,
                Id = Constants.GROUP_COORDINATOR_ID
            };
            _user.IsITAR = false;

            List<Hecp> hecps = new List<Hecp>()
            {
                new Hecp
                {
                    Id = 5,
                    Name = "Test Hecp ITAR",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Shield.Common.Models.Loto.Shared.Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = true,
                    UpdatedDate = new DateTime(2023, 08, 20, 1, 0, 0),
                    IsMigrationDataCheckNeeded = false,
                    IsSuccessfullyMigrated = true
                },
                new Hecp
                {
                    Id = 6,
                    Name = "Test Hecp non-ITAR",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Shield.Common.Models.Loto.Shared.Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = false,
                    UpdatedDate = new DateTime(2023, 09, 20, 1, 0, 0),
                    IsMigrationDataCheckNeeded = false,
                    IsSuccessfullyMigrated = true
                }
            };

            List<string> sites = new List<string>
            {
                "BSC", "Everett"
            };
            PagingWrapper<Hecp> hecpList = new PagingWrapper<Hecp>()
            {
                Data = hecps,
                PageCount = 1,
                PageNumber = 1,
                PageSize = 1
            };
            List<Status> hecpStatus = new List<Status>()
            {
                new Status
                {
                    Id = 1,
                    Description = "inreview",
                    DisplayName = "inreview"
                },
                new Status
                {
                    Id = 2,
                    Description = "draft",
                    DisplayName = "draft"
                }
            };

            _mockSessionService.Setup(sS => sS.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_user);
            _mockHecpService.Setup(s => s.GetFilteredHecps(It.IsAny<HecpFilterRequest>())).ReturnsAsync(hecpList);
            _mockAirplaneDataService.Setup(s => s.GetSitesByProgramAsync(It.IsAny<string>())).ReturnsAsync(sites);
            _mockHecpService.Setup(s => s.GetHecpStatusList()).ReturnsAsync(hecpStatus);
            _mockAirplaneDataService.Setup(s => s.GetMinorModelData(It.IsAny<string>())).ReturnsAsync(new List<MinorModel>());

            HttpResponseMessage response = await httpClient.GetAsync("Hecp/ViewFilteredHecps?program=787&hecpName=&siteName=&ataChapterNumber=&ataChapterTitle=&affectedSystem=&hecpStatusName=&isHecpPublished=false&PageNumber=1");

            string output = await response.Content.ReadAsStringAsync();
            IHtmlDocument htmlDocument = parser.ParseDocument(output);
            string firstHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[2].TextContent.Trim();
            string secondHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[2].TextContent.Trim();
            IElement firstHecpEditButton = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[7].GetElementsByClassName("dropdown-item")[0];
            IElement secondHecpEditButton = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[7].GetElementsByClassName("dropdown-item")[0];
            string firstHecpId = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[0].TextContent.Trim();
            string secondHecpId = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[0].TextContent.Trim();

            Assert.AreEqual(hecps[0].Name, firstHecpName);
            Assert.AreEqual(hecps[1].Name, secondHecpName);
            Assert.AreEqual("5", firstHecpId);
            Assert.AreEqual("6", secondHecpId);
            Assert.AreEqual(1, firstHecpEditButton.GetElementsByClassName("hecp-edit-disabled").Length);
            Assert.AreEqual(0, secondHecpEditButton.GetElementsByClassName("hecp-edit-disabled").Length);
        }

        [TestMethod]
        public async Task Should_Show_Edit_Button_If_User_Is_ITAR_For_ITAR_Hecp()
        {
            _user.Role = new Shield.Common.Models.Users.Role()
            {
                Name = Constants.GROUP_COORDINATOR,
                Id = Constants.GROUP_COORDINATOR_ID
            };
            _user.IsITAR = true;
            List<Hecp> hecps = new List<Hecp>()
                {
                    new Hecp
                    {
                        Id = 5,
                        Name = "Test Hecp ITAR",
                        Revision = "A",
                        CreatedByBemsId = 2817760,
                        Site = "BSC",
                        Program = "787",
                        LineNumber = "9999",
                        LastStepCompleted = 7,
                        Status = new Shield.Common.Models.Loto.Shared.Status()
                        {
                            Id = 1,
                            Description = "inreview",
                            DisplayName = "inreview"
                        },
                        IsItar = true,
                        UpdatedDate = new DateTime(2023, 08, 20, 1, 0, 0),
                        IsSuccessfullyMigrated = true
                    },
                    new Hecp
                    {
                        Id = 6,
                        Name = "Test Hecp non-ITAR",
                        Revision = "A",
                        CreatedByBemsId = 2817760,
                        Site = "BSC",
                        Program = "787",
                        LineNumber = "9999",
                        LastStepCompleted = 7,
                        Status = new Shield.Common.Models.Loto.Shared.Status()
                        {
                            Id = 1,
                            Description = "inreview",
                            DisplayName = "inreview"
                        },
                        IsItar = false,
                        UpdatedDate = new DateTime(2023, 09, 20, 1, 0, 0),
                        IsSuccessfullyMigrated = true
                    }
                };
            List<string> sites = new List<string>
                {
                    "BSC", "Everett"
                };
            PagingWrapper<Hecp> hecpList = new PagingWrapper<Hecp>()
            {
                Data = hecps,
                PageCount = 1,
                PageNumber = 1,
                PageSize = 1
            };
            List<Status> hecpStatus = new List<Status>()
                {
                    new Status
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    new Status
                    {
                        Id = 2,
                        Description = "draft",
                        DisplayName = "draft"
                    }
                };

            _mockSessionService.Setup(sS => sS.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_user);
            _mockHecpService.Setup(s => s.GetFilteredHecps(It.IsAny<HecpFilterRequest>())).ReturnsAsync(hecpList);
            _mockAirplaneDataService.Setup(s => s.GetSitesByProgramAsync(It.IsAny<string>())).ReturnsAsync(sites);
            _mockHecpService.Setup(s => s.GetHecpStatusList()).ReturnsAsync(hecpStatus);
            _mockAirplaneDataService.Setup(s => s.GetMinorModelData(It.IsAny<string>())).ReturnsAsync(new List<MinorModel>());

            HttpResponseMessage response = await httpClient.GetAsync("Hecp/ViewFilteredHecps?program=787&hecpName=&siteName=&ataChapterNumber=&ataChapterTitle=&affectedSystem=&hecpStatusName=&PageNumber=1");

            string output = await response.Content.ReadAsStringAsync();
            IHtmlDocument htmlDocument = parser.ParseDocument(output);
            string firstHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[2].TextContent.Trim();
            string secondHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[2].TextContent.Trim();
            IElement firstHecpEditButton = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[7].GetElementsByClassName("dropdown-item")[0];
            IElement secondHecpEditButton = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[7].GetElementsByClassName("dropdown-item")[0]; ;

            Assert.AreEqual(hecps[0].Name, firstHecpName);
            Assert.AreEqual(hecps[1].Name, secondHecpName);
            Assert.AreEqual(0, firstHecpEditButton.GetElementsByClassName("hecp-edit-disabled").Length);
            Assert.AreEqual(0, secondHecpEditButton.GetElementsByClassName("hecp-edit-disabled").Length);
        }

        [TestMethod]
        public async Task Should_Show_FilterParameters_ForHecpFiltering()
        {
            _user.Role = new Shield.Common.Models.Users.Role()
            {
                Name = Constants.GROUP_COORDINATOR,
                Id = Constants.GROUP_COORDINATOR_ID
            };
            _user.IsITAR = true;
            List<Hecp> hecps = new List<Hecp>()
            {
                new Hecp
                {
                    Id = 4,
                    Name = "Test Hecp ITAR",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "Everett",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 6,
                    Status = new Shield.Common.Models.Loto.Shared.Status()
                    {
                        Id = 2,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = true,
                    UpdatedDate = new DateTime(2023, 08, 20, 1, 0, 0),
                    IsSuccessfullyMigrated = true
                }
            };
            List<string> sitesForSelectedProgram = new List<string>
            {
                "BSC", "Everett"
            };
            PagingWrapper<Hecp> hecpList = new PagingWrapper<Hecp>()
            {
                Data = hecps,
                PageCount = 1,
                PageNumber = 1,
                PageSize = 1
            };
            List<Status> hecpStatus = new List<Status>()
                {
                    new Status
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    new Status
                    {
                        Id = 2,
                        Description = "draft",
                        DisplayName = "draft"
                    }
                };

            _mockSessionService.Setup(sS => sS.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_user);
            _mockHecpService.Setup(s => s.GetFilteredHecps(It.IsAny<HecpFilterRequest>())).ReturnsAsync(hecpList);
            _mockAirplaneDataService.Setup(s => s.GetSitesByProgramAsync(It.IsAny<string>())).ReturnsAsync(sitesForSelectedProgram);
            _mockHecpService.Setup(s => s.GetHecpStatusList()).ReturnsAsync(hecpStatus);
            _mockAirplaneDataService.Setup(s => s.GetMinorModelData(It.IsAny<string>())).ReturnsAsync(new List<MinorModel>());

            HttpResponseMessage response = await httpClient.GetAsync("Hecp/ViewFilteredHecps?program=787&hecpName=&site=&ataChapterNumber=&ataChapterTitle=&affectedSystem=&hecpStatusName=&PageNumber=1");

            string output = await response.Content.ReadAsStringAsync();
            IHtmlDocument htmlDocument = parser.ParseDocument(output);
            IElement draftFilterForm = htmlDocument.GetElementById("draft-partial").GetElementsByClassName("discrete-card-body")[0].Children[0];

            Assert.IsTrue(draftFilterForm.InnerHtml.Contains("HecpNameId"));
            Assert.IsTrue(draftFilterForm.InnerHtml.Contains("site-select-filter"));
            Assert.IsTrue(draftFilterForm.InnerHtml.Contains("AtaChapterNumberId"));
            Assert.IsTrue(draftFilterForm.InnerHtml.Contains("AtaChapterTitleId"));
            Assert.IsTrue(draftFilterForm.InnerHtml.Contains("AffectedSystemId"));
            Assert.IsTrue(draftFilterForm.InnerHtml.Contains("HecpStatusName"));
            Assert.IsTrue(draftFilterForm.TextContent.Contains("BSC"));
            Assert.IsTrue(draftFilterForm.TextContent.Contains("Everett"));
            Assert.IsTrue(draftFilterForm.TextContent.Contains("ENGINES"));
            Assert.IsTrue(draftFilterForm.TextContent.Contains("WATER"));
            Assert.IsTrue(draftFilterForm.TextContent.Contains("HECP Title"));
            Assert.IsTrue(draftFilterForm.TextContent.Contains("Site"));
            Assert.IsTrue(draftFilterForm.TextContent.Contains("ATA No."));
            Assert.IsTrue(draftFilterForm.TextContent.Contains("ATA Title"));
            Assert.IsTrue(draftFilterForm.TextContent.Contains("Affected System"));
            Assert.IsTrue(draftFilterForm.TextContent.Contains("HECP Status"));
            Assert.IsTrue(draftFilterForm.TextContent.Contains("inreview"));
            Assert.IsTrue(draftFilterForm.TextContent.Contains("draft"));
        }

        [TestMethod]
        public async Task Should_Show_FilterParameters_ForHecpFiltering_AndPagination()
        {
            _user.Role = new Shield.Common.Models.Users.Role()
            {
                Name = Constants.GROUP_COORDINATOR,
                Id = Constants.GROUP_COORDINATOR_ID
            };
            _user.IsITAR = true;
            List<Hecp> hecps = new List<Hecp>()
            {
                new Hecp
                {
                    Id = 4,
                    Name = "Test Hecp ITAR",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "Everett",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 6,
                    Status = new Shield.Common.Models.Loto.Shared.Status()
                    {
                        Id = 2,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = true,
                    UpdatedDate = new DateTime(2023, 08, 20, 1, 0, 0),
                    IsSuccessfullyMigrated = true
                }
            };
            List<string> sitesForSelectedProgram = new List<string>
            {
                "BSC", "Everett"
            };
            PagingWrapper<Hecp> hecpList = new PagingWrapper<Hecp>()
            {
                Data = hecps,
                PageNumber = 1,
                PageCount = 3,
                PageSize = 3,
            };
            List<Status> hecpStatus = new List<Status>()
            {
                new Status
                {
                    Id = 1,
                    Description = "inreview",
                    DisplayName = "inreview"
                },
                new Status
                {
                    Id = 2,
                    Description = "draft",
                    DisplayName = "draft"
                }
            };

            _mockSessionService.Setup(sS => sS.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_user);
            _mockHecpService.Setup(s => s.GetFilteredHecps(It.IsAny<HecpFilterRequest>())).ReturnsAsync(hecpList);
            _mockAirplaneDataService.Setup(s => s.GetSitesByProgramAsync(It.IsAny<string>())).ReturnsAsync(sitesForSelectedProgram);
            _mockHecpService.Setup(s => s.GetHecpStatusList()).ReturnsAsync(hecpStatus);
            _mockAirplaneDataService.Setup(s => s.GetMinorModelData(It.IsAny<string>())).ReturnsAsync(new List<MinorModel>());

            HttpResponseMessage response = await httpClient.GetAsync("Hecp/ViewFilteredHecps?program=787&hecpName=&site=&ataChapterNumber=&ataChapterTitle=&affectedSystem=&hecpStatusName=&PageNumber=1");

            string output = await response.Content.ReadAsStringAsync();
            IHtmlDocument htmlDocument = parser.ParseDocument(output);
            IElement draftFilterForm = htmlDocument.GetElementById("draft-partial").GetElementsByClassName("discrete-card-body")[0].Children[0];
            IElement hecpPagination = htmlDocument.GetElementById("page-list");
            IHtmlCollection<IElement> publishedHecpFilterElement = htmlDocument.GetElementById("published-partial").GetElementsByClassName("discrete-card-body")[0].GetElementsByClassName("discrete-form-div");
            IHtmlCollection<IElement> advanceFilterBtn = htmlDocument.GetElementById("published-partial").GetElementsByClassName("discrete-card-body")[0].GetElementsByClassName("discrete-form-isolation")[0].GetElementsByClassName("advancefilter-button");

            Assert.IsTrue(draftFilterForm.InnerHtml.Contains("HecpNameId"));
            Assert.IsTrue(draftFilterForm.InnerHtml.Contains("site-select-filter"));
            Assert.IsTrue(draftFilterForm.InnerHtml.Contains("AtaChapterNumberId"));
            Assert.IsTrue(draftFilterForm.InnerHtml.Contains("AtaChapterTitleId"));
            Assert.IsTrue(draftFilterForm.InnerHtml.Contains("AffectedSystemId"));
            Assert.IsTrue(draftFilterForm.InnerHtml.Contains("HecpStatusName"));
            Assert.IsTrue(hecpPagination.InnerHtml.Contains("tabButton-page-1"));
            Assert.IsTrue(hecpPagination.InnerHtml.Contains("tabButton-page-2"));
            Assert.IsTrue(hecpPagination.InnerHtml.Contains("tabButton-page-3"));
            Assert.IsTrue(hecpPagination.InnerHtml.Contains("next-page-button"));

            Assert.AreEqual(8, publishedHecpFilterElement.Length);
            Assert.AreEqual(1, advanceFilterBtn.Length);
            Assert.AreEqual("Advance Filters", advanceFilterBtn[0].TextContent.Trim());
            Assert.AreEqual("HECP Title", publishedHecpFilterElement[0].TextContent.Trim());
            Assert.AreEqual("Site", publishedHecpFilterElement[1].Children[0].TextContent.Trim());
            Assert.AreEqual("ATA No.", publishedHecpFilterElement[2].TextContent.Trim());
            Assert.AreEqual("ATA Title", publishedHecpFilterElement[3].TextContent.Trim());
            Assert.AreEqual("Affected System", publishedHecpFilterElement[4].Children[0].TextContent.Trim());
            Assert.AreEqual("HECP Type", publishedHecpFilterElement[5].Children[0].TextContent.Trim());
            Assert.AreEqual("Circuit Id *", publishedHecpFilterElement[6].Children[0].TextContent.Trim());
            Assert.AreEqual("Circuit Name *", publishedHecpFilterElement[7].Children[0].TextContent.Trim());
        }

        [TestMethod]
        public async Task Should_Show_HecpDashboardPartial_For_Inwork_HECPs()
        {
            _user.Role = new Shield.Common.Models.Users.Role()
            {
                Name = Constants.GROUP_COORDINATOR,
                Id = Constants.GROUP_COORDINATOR_ID
            };
            _user.IsITAR = true;
            List<Hecp> inWorkHecps = new List<Hecp>()
            {
                new Hecp
                {
                    Id = 4,
                    Name = "Test Hecp ITAR",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "Everett",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 6,
                    Status = new Shield.Common.Models.Loto.Shared.Status()
                    {
                        Id = 2,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = true,
                    UpdatedDate = new DateTime(2023, 08, 20, 1, 0, 0),
                    IsMigrationDataCheckNeeded = true,
                    IsSuccessfullyMigrated = true
                },
                new Hecp
                {
                    Id = 5,
                    Name = "Test Hecp ITAR",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Shield.Common.Models.Loto.Shared.Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = true,
                    UpdatedDate = new DateTime(2023, 08, 20, 1, 0, 0),
                    IsMigrationDataCheckNeeded = false,
                    IsSuccessfullyMigrated = true
                }
            };
            List<string> sitesForSelectedProgram = new List<string>
            {
                "BSC", "Everett"
            };
            PagingWrapper<Hecp> hecpList = new PagingWrapper<Hecp>()
            {
                Data = inWorkHecps,
                PageNumber = 1,
                PageCount = 3,
                PageSize = 3,
            };
            List<Status> hecpStatus = new List<Status>()
            {
                new Status
                {
                    Id = 1,
                    Description = "inreview",
                    DisplayName = "inreview"
                },
                new Status
                {
                    Id = 2,
                    Description = "draft",
                    DisplayName = "draft"
                }
            };

            _mockSessionService.Setup(sS => sS.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_user);
            _mockHecpService.Setup(s => s.GetFilteredHecps(It.IsAny<HecpFilterRequest>())).ReturnsAsync(hecpList);
            _mockAirplaneDataService.Setup(s => s.GetSitesByProgramAsync(It.IsAny<string>())).ReturnsAsync(sitesForSelectedProgram);
            _mockHecpService.Setup(s => s.GetHecpStatusList()).ReturnsAsync(hecpStatus);
            _mockAirplaneDataService.Setup(s => s.GetMinorModelData(It.IsAny<string>())).ReturnsAsync(new List<MinorModel>());

            HttpResponseMessage response = await httpClient.GetAsync("Hecp/ViewHecpList?program=787&hecpName=&site=&ataChapterNumber=&ataChapterTitle=&affectedSystem=&hecpStatusName=&PageNumber=1");

            string output = await response.Content.ReadAsStringAsync();
            IHtmlDocument htmlDocument = parser.ParseDocument(output);
            IElement hecpTable = htmlDocument.GetElementById("hecpDetails");
            IElement hecpPagination = htmlDocument.GetElementById("page-list");
            IHtmlCollection<IElement> actionBtnsForFirstHecp = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByClassName("dropdown-item");
            IHtmlCollection<IElement> actionBtnsForSecondHecp = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByClassName("dropdown-item");

            Assert.IsTrue(hecpTable.InnerHtml.Contains("Title"));
            Assert.IsTrue(hecpTable.InnerHtml.Contains("Site"));
            Assert.IsTrue(hecpTable.InnerHtml.Contains("Revision"));
            Assert.IsTrue(hecpTable.InnerHtml.Contains("Status"));
            Assert.IsTrue(hecpTable.InnerHtml.Contains("Actions"));
            Assert.IsTrue(hecpPagination.InnerHtml.Contains("tabButton-page-1"));
            Assert.IsTrue(hecpPagination.InnerHtml.Contains("tabButton-page-2"));
            Assert.IsTrue(hecpPagination.InnerHtml.Contains("tabButton-page-3"));
            Assert.IsTrue(hecpPagination.InnerHtml.Contains("next-page-button"));
            Assert.AreEqual("View & Take Action", actionBtnsForFirstHecp[0].TextContent.Trim());
            Assert.AreEqual("Edit", actionBtnsForFirstHecp[1].TextContent.Trim());
            Assert.AreEqual("Delete", actionBtnsForFirstHecp[2].TextContent.Trim());
            Assert.AreEqual(1, actionBtnsForFirstHecp[2].GetElementsByClassName("disabled").Length);
            Assert.AreEqual(0, actionBtnsForFirstHecp[0].GetElementsByClassName("disabled").Length);
            Assert.AreEqual(0, actionBtnsForFirstHecp[1].GetElementsByClassName("disabled").Length);
            Assert.AreEqual("No permission to Delete this HECP. Please contact System/Site Admin.", actionBtnsForFirstHecp[2].GetAttribute("Title"));
            Assert.AreEqual(1, actionBtnsForSecondHecp[2].GetElementsByClassName("disabled").Length);
            Assert.AreEqual(0, actionBtnsForSecondHecp[0].GetElementsByClassName("disabled").Length);
            Assert.AreEqual(0, actionBtnsForSecondHecp[1].GetElementsByClassName("disabled").Length);
            Assert.AreEqual("No permission to Delete this HECP. Please contact System/Site Admin.", actionBtnsForSecondHecp[2].GetAttribute("Title"));
        }

        [TestMethod]
        public async Task Should_Show_HecpDashboardPartial_For_Published_HECPs()
        {
            _user.Role = new Shield.Common.Models.Users.Role()
            {
                Name = Constants.GROUP_COORDINATOR,
                Id = Constants.GROUP_COORDINATOR_ID
            };
            _user.IsITAR = true;
            List<Hecp> inWorkHecps = new List<Hecp>()
            {
                new Hecp
                {
                    Id = 4,
                    Name = "Test Hecp ITAR",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "Everett",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 6,
                    Status = new Shield.Common.Models.Loto.Shared.Status()
                    {
                        Id = 4,
                        Description = "Published",
                        DisplayName = "Published"
                    },
                    IsItar = true,
                    UpdatedDate = new DateTime(2023, 08, 20, 1, 0, 0),
                    IsMigrationDataCheckNeeded = true,
                    IsSuccessfullyMigrated = true
                },
                new Hecp
                {
                    Id = 5,
                    Name = "Test Hecp ITAR",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Shield.Common.Models.Loto.Shared.Status()
                    {
                        Id = 1,
                        Description = "Published",
                        DisplayName = "Published"
                    },
                    IsItar = true,
                    UpdatedDate = new DateTime(2023, 08, 20, 1, 0, 0),
                    IsMigrationDataCheckNeeded = false,
                    IsSuccessfullyMigrated = true,
                    HasInWorkRevision = true
                }
            };
            List<string> sitesForSelectedProgram = new List<string>
            {
                "BSC", "Everett"
            };
            PagingWrapper<Hecp> hecpList = new PagingWrapper<Hecp>()
            {
                Data = inWorkHecps,
                PageNumber = 1,
                PageCount = 3,
                PageSize = 3,
            };
            List<Status> hecpStatus = new List<Status>()
            {
                new Status
                {
                    Id = 1,
                    Description = "inreview",
                    DisplayName = "inreview"
                },
                new Status
                {
                    Id = 2,
                    Description = "draft",
                    DisplayName = "draft"
                },
                new Status
                {
                    Id = 3,
                    Description = "Published",
                    DisplayName = "Published"
                }
            };

            _mockSessionService.Setup(sS => sS.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_user);
            _mockHecpService.Setup(s => s.GetFilteredHecps(It.IsAny<HecpFilterRequest>())).ReturnsAsync(hecpList);
            _mockAirplaneDataService.Setup(s => s.GetSitesByProgramAsync(It.IsAny<string>())).ReturnsAsync(sitesForSelectedProgram);
            _mockHecpService.Setup(s => s.GetHecpStatusList()).ReturnsAsync(hecpStatus);
            _mockAirplaneDataService.Setup(s => s.GetMinorModelData(It.IsAny<string>())).ReturnsAsync(new List<MinorModel>());

            HttpResponseMessage response = await httpClient.GetAsync("Hecp/ViewHecpList?program=787&hecpName=&site=&ataChapterNumber=&ataChapterTitle=&affectedSystem=&hecpStatusName=&isPublishedList=true&PageNumber=1");

            string output = await response.Content.ReadAsStringAsync();
            IHtmlDocument htmlDocument = parser.ParseDocument(output);
            IElement hecpTable = htmlDocument.GetElementById("hecpDetails");
            IHtmlCollection<IElement> tableColumnTitles = htmlDocument.GetElementById("hecpDetails").Children[0].GetElementsByTagName("tr")[0].GetElementsByTagName("th");
            IElement hecpPagination = htmlDocument.GetElementById("page-list");
            IHtmlCollection<IElement> actionBtnsForFirstHecp = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByClassName("dropdown-item");
            IHtmlCollection<IElement> actionBtnsForSecondHecp = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByClassName("dropdown-item");
            
            Assert.AreEqual("HECP No", tableColumnTitles[0].TextContent.Trim());
            Assert.AreEqual("Type", tableColumnTitles[1].TextContent.Trim());
            Assert.AreEqual("Title", tableColumnTitles[2].TextContent.Trim());
            Assert.AreEqual("View", tableColumnTitles[3].TextContent.Trim());
            Assert.AreEqual("Site", tableColumnTitles[4].TextContent.Trim());
            Assert.AreEqual("Revision", tableColumnTitles[5].TextContent.Trim());
            Assert.AreEqual("Last Published", tableColumnTitles[6].TextContent.Trim());
            Assert.AreEqual("Actions", tableColumnTitles[7].TextContent.Trim());
            
            Assert.IsTrue(hecpPagination.InnerHtml.Contains("tabButton-page-1"));
            Assert.IsTrue(hecpPagination.InnerHtml.Contains("tabButton-page-2"));
            Assert.IsTrue(hecpPagination.InnerHtml.Contains("tabButton-page-3"));
            Assert.IsTrue(hecpPagination.InnerHtml.Contains("next-page-button"));

            Assert.AreEqual("Edit", actionBtnsForFirstHecp[0].TextContent.Trim());
            Assert.AreEqual("Delete", actionBtnsForFirstHecp[1].TextContent.Trim());
            Assert.AreEqual(0, actionBtnsForFirstHecp[0].GetElementsByClassName("disabled").Length);
            Assert.AreEqual(1, actionBtnsForFirstHecp[1].GetElementsByClassName("disabled").Length);
            Assert.AreEqual("No permission to Delete this HECP. Please contact System/Site Admin.", actionBtnsForFirstHecp[1].GetAttribute("Title"));
            Assert.AreEqual(1, actionBtnsForSecondHecp[0].GetElementsByClassName("hecp-edit-disabled").Length);
            Assert.AreEqual(1, actionBtnsForSecondHecp[1].GetElementsByClassName("disabled").Length);
            Assert.AreEqual("This Hecp has an in-work revision. Please use In-Work tab to edit", actionBtnsForSecondHecp[0].GetAttribute("Title"));
            Assert.AreEqual("No permission to Delete this HECP. Please contact System/Site Admin.", actionBtnsForSecondHecp[1].GetAttribute("Title"));
            Assert.AreEqual("View Hecp", htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByClassName("fa-eye")[0].GetAttribute("Title"));
        }

        [TestMethod]
        public async Task Should_Show_Published_Date_With_Sorting_HECP_Dashboard()
        {
            _user.Role = new Shield.Common.Models.Users.Role()
            {
                Name = Constants.GROUP_COORDINATOR,
                Id = Constants.GROUP_COORDINATOR_ID
            };
            _user.IsITAR = true;
            List<Hecp> hecps = new List<Hecp>()
            {
                new Hecp
                {
                    Id = 5,
                    Name = "Test Hecp ITAR",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Shield.Common.Models.Loto.Shared.Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = true,
                    UpdatedDate = new DateTime(2023, 08, 20, 1, 0, 0),
                    PublishedDate = DateTime.MinValue.AddDays(1)
                },
                new Hecp
                {
                    Id = 6,
                    Name = "Test Hecp non-ITAR",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Shield.Common.Models.Loto.Shared.Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = false,
                    UpdatedDate = new DateTime(2023, 09, 20, 1, 0, 0),
                    PublishedDate = DateTime.MinValue
                }
            };
            List<string> sites = new List<string>
            {
                "BSC", "Everett"
            };
            PagingWrapper<Hecp> hecpList = new PagingWrapper<Hecp>()
            {
                Data = hecps,
                PageCount = 1,
                PageNumber = 1,
                PageSize = 1
            };
            List<Status> hecpStatus = new List<Status>()
            {
                new Status
                {
                    Id = 1,
                    Description = "inreview",
                    DisplayName = "inreview"
                },
                new Status
                {
                    Id = 2,
                    Description = "draft",
                    DisplayName = "draft"
                }
            };

            _mockSessionService.Setup(sS => sS.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_user);
            _mockHecpService.Setup(s => s.GetFilteredHecps(It.IsAny<HecpFilterRequest>())).ReturnsAsync(hecpList);
            _mockAirplaneDataService.Setup(s => s.GetSitesByProgramAsync(It.IsAny<string>())).ReturnsAsync(sites);
            _mockHecpService.Setup(s => s.GetHecpStatusList()).ReturnsAsync(hecpStatus);
            _mockAirplaneDataService.Setup(s => s.GetMinorModelData(It.IsAny<string>())).ReturnsAsync(new List<MinorModel>());

            HttpResponseMessage response = await httpClient.GetAsync("Hecp/ViewFilteredHecps?program=787&hecpName=&siteName=&ataChapterNumber=&ataChapterTitle=&affectedSystem=&hecpStatusName=&PageNumber=1");

            string output = await response.Content.ReadAsStringAsync();
            IHtmlDocument htmlDocument = parser.ParseDocument(output);

            string firstHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[2].TextContent.Trim();
            string secondHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[2].TextContent.Trim();
            IElement firstHecpPublishedDate = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[6];
            IElement secondHecpPublishedDate = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[6];
            IElement publishedDateSortButton = htmlDocument.GetElementById("hecpDetails").Children[0].GetElementsByTagName("th")[6].GetElementsByClassName("copy-btn")[0];

            Assert.AreEqual(hecps[0].Name, firstHecpName);
            Assert.AreEqual(hecps[1].Name, secondHecpName);
            Assert.IsTrue(firstHecpPublishedDate.TextContent.Contains(DateTime.MinValue.AddDays(1).ToString()));
            Assert.AreEqual("convert-to-local-time-short", secondHecpPublishedDate.ClassName);
            Assert.IsNotNull(publishedDateSortButton);
            Assert.IsTrue(publishedDateSortButton.GetAttribute("onClick").Contains("GetFilteredAndSortedHecpDataByDate"));
        }

        [TestMethod]
        public async Task Should_Show_MinorModelColumn_For_Programs_Having_MinorModels_In_HECP_Dashboard()
        {
            _user.Role = new Shield.Common.Models.Users.Role()
            {
                Name = Constants.GROUP_COORDINATOR,
                Id = Constants.GROUP_COORDINATOR_ID
            };
            _user.IsITAR = true;
            List<Hecp> hecps = new List<Hecp>()
            {
                new Hecp
                {
                    Id = 5,
                    Name = "Test Hecp ITAR",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Shield.Common.Models.Loto.Shared.Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = true,
                    UpdatedDate = new DateTime(2023, 08, 20, 1, 0, 0),
                    PublishedDate = DateTime.MinValue.AddDays(1),
                    HecpAssociatedModelData = new List<HecpAssociatedModelData>
                    {
                        new HecpAssociatedModelData
                        {
                            HecpId = 5,
                            MinorModelId = 25
                        },
                        new HecpAssociatedModelData
                        {
                            HecpId = 5,
                            MinorModelId = 26
                        }
                    }
                },
                new Hecp
                {
                    Id = 6,
                    Name = "Test Hecp non-ITAR",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Shield.Common.Models.Loto.Shared.Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = false,
                    UpdatedDate = new DateTime(2023, 09, 20, 1, 0, 0),
                    PublishedDate = DateTime.MinValue
                }
            };
            List<string> sites = new List<string>
                {
                    "BSC", "Everett"
                };
            PagingWrapper<Hecp> hecpList = new PagingWrapper<Hecp>()
            {
                Data = hecps,
                PageCount = 1,
                PageNumber = 1,
                PageSize = 1
            };
            List<Status> hecpStatus = new List<Status>()
            {
                new Status
                {
                    Id = 1,
                    Description = "inreview",
                    DisplayName = "inreview"
                },
                new Status
                {
                    Id = 2,
                    Description = "draft",
                    DisplayName = "draft"
                }
            };
            List<MinorModel> minorModelList = new()
            {
                new MinorModel
                {
                    Id = 18,
                    Name = "A"
                },
                new MinorModel
                {
                    Id = 25,
                    Name = "B"
                },
                new MinorModel
                {
                    Id = 26,
                    Name = "C"
                }
            };

            _mockSessionService.Setup(sS => sS.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_user);
            _mockHecpService.Setup(s => s.GetFilteredHecps(It.IsAny<HecpFilterRequest>())).ReturnsAsync(hecpList);
            _mockAirplaneDataService.Setup(s => s.GetSitesByProgramAsync(It.IsAny<string>())).ReturnsAsync(sites);
            _mockHecpService.Setup(s => s.GetHecpStatusList()).ReturnsAsync(hecpStatus);
            _mockAirplaneDataService.Setup(s => s.GetMinorModelData(It.IsAny<string>())).ReturnsAsync(minorModelList);

            HttpResponseMessage response = await httpClient.GetAsync("Hecp/ViewFilteredHecps?program=787&hecpName=&siteName=&ataChapterNumber=&ataChapterTitle=&affectedSystem=&hecpStatusName=&PageNumber=1");

            string output = await response.Content.ReadAsStringAsync();
            IHtmlDocument htmlDocument = parser.ParseDocument(output);

            string minorModelFirstHecp = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[4].TextContent.Trim();
            string minorModelSecondHecp = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[4].TextContent.Trim();
            IElement minorModelElement = htmlDocument.GetElementById("hecpDetails").Children[0].GetElementsByTagName("tr")[0].GetElementsByTagName("th")[4];
            IElement spanElement = htmlDocument.GetElementById("hecpDetails").Children[0].GetElementsByTagName("tr")[0].GetElementsByTagName("th")[4].GetElementsByClassName("minor-model-tooltip")[0];

            Assert.AreEqual("", minorModelSecondHecp); 
            Assert.AreEqual("B, C", minorModelFirstHecp);
            Assert.IsTrue(minorModelElement.TextContent.Trim().Contains("Minor Models"));
            Assert.AreEqual("tooltip", spanElement.GetAttribute("data-toggle"));
            Assert.AreEqual("Minor Models linked to the current revision of HECP", spanElement.GetAttribute("title"));
        }

        [TestMethod]
        public async Task Should_Not_Show_MinorModelColumn_For_Programs_Not_Having_MinorModels_In_HECP_Dashboard()
        {
            _user.Role = new Shield.Common.Models.Users.Role()
            {
                Name = Constants.GROUP_COORDINATOR,
                Id = Constants.GROUP_COORDINATOR_ID
            };
            _user.IsITAR = true;
            List<Hecp> hecps = new List<Hecp>()
            {
                new Hecp
                {
                    Id = 5,
                    Name = "Test Hecp ITAR",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Shield.Common.Models.Loto.Shared.Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = true,
                    UpdatedDate = new DateTime(2023, 08, 20, 1, 0, 0),
                    PublishedDate = DateTime.MinValue.AddDays(1)
                },
                new Hecp
                {
                    Id = 6,
                    Name = "Test Hecp non-ITAR",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Shield.Common.Models.Loto.Shared.Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = false,
                    UpdatedDate = new DateTime(2023, 09, 20, 1, 0, 0),
                    PublishedDate = DateTime.MinValue
                }
            };
            List<string> sites = new List<string>
            {
                "BSC", "Everett"
            };
            PagingWrapper<Hecp> hecpList = new PagingWrapper<Hecp>()
            {
                Data = hecps,
                PageCount = 1,
                PageNumber = 1,
                PageSize = 1
            };
            List<Status> hecpStatus = new List<Status>()
            {
                new Status
                {
                    Id = 1,
                    Description = "inreview",
                    DisplayName = "inreview"
                },
                new Status
                {
                    Id = 2,
                    Description = "draft",
                    DisplayName = "draft"
                }
            };

            _mockSessionService.Setup(sS => sS.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_user);
            _mockHecpService.Setup(s => s.GetFilteredHecps(It.IsAny<HecpFilterRequest>())).ReturnsAsync(hecpList);
            _mockAirplaneDataService.Setup(s => s.GetSitesByProgramAsync(It.IsAny<string>())).ReturnsAsync(sites);
            _mockHecpService.Setup(s => s.GetHecpStatusList()).ReturnsAsync(hecpStatus);
            _mockAirplaneDataService.Setup(s => s.GetMinorModelData(It.IsAny<string>())).ReturnsAsync(new List<MinorModel>());

            HttpResponseMessage response = await httpClient.GetAsync("Hecp/ViewFilteredHecps?program=787&hecpName=&siteName=&ataChapterNumber=&ataChapterTitle=&affectedSystem=&hecpStatusName=&PageNumber=1");

            string output = await response.Content.ReadAsStringAsync();
            IHtmlDocument htmlDocument = parser.ParseDocument(output);

            string firstHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[2].TextContent.Trim();
            string secondHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[2].TextContent.Trim();
            IElement tableHeadingElement = htmlDocument.GetElementById("hecpDetails").Children[0].GetElementsByTagName("tr")[0];

            Assert.AreEqual(hecps[0].Name, firstHecpName);
            Assert.AreEqual(hecps[1].Name, secondHecpName);
            Assert.IsFalse(tableHeadingElement.InnerHtml.Trim().Contains("Minor Models"));
        }

        [TestMethod]
        public async Task Should_Not_Show_Edit_Button_If_User_Is_NonEngineer_For_Engineered_Hecp()
        {
            _user.Role = new Shield.Common.Models.Users.Role()
            {
                Name = Constants.GROUP_COORDINATOR,
                Id = Constants.GROUP_COORDINATOR_ID
            };
            _user.IsITAR = false;
            List<Hecp> hecps = new List<Hecp>()
            {
                new Hecp
                {
                    Id = 5,
                    Name = "Test Hecp Engineered",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsEngineered = true,
                    IsItar = false,
                    UpdatedDate = new DateTime(2023, 08, 20, 1, 0, 0),
                    IsMigrationDataCheckNeeded = false,
                    IsSuccessfullyMigrated = true
                },
                new Hecp
                {
                    Id = 6,
                    Name = "Test Hecp non-Engineered",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = false,
                    IsEngineered =false,
                    UpdatedDate = new DateTime(2023, 09, 20, 1, 0, 0),
                    IsMigrationDataCheckNeeded = false,
                    IsSuccessfullyMigrated = true
                }
            };
            List<string> sites = new List<string>
            {
                "BSC", "Everett"
            };
            PagingWrapper<Hecp> hecpList = new PagingWrapper<Hecp>()
            {
                Data = hecps,
                PageCount = 1,
                PageNumber = 1,
                PageSize = 1
            };
            List<Status> hecpStatus = new List<Status>()
            {
                new Status
                {
                    Id = 1,
                    Description = "inreview",
                    DisplayName = "inreview"
                },
                new Status
                {
                    Id = 2,
                    Description = "draft",
                    DisplayName = "draft"
                }
            };

            _mockSessionService.Setup(sS => sS.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_user);
            _mockHecpService.Setup(s => s.GetFilteredHecps(It.IsAny<HecpFilterRequest>())).ReturnsAsync(hecpList);
            _mockAirplaneDataService.Setup(s => s.GetSitesByProgramAsync(It.IsAny<string>())).ReturnsAsync(sites);
            _mockHecpService.Setup(s => s.GetHecpStatusList()).ReturnsAsync(hecpStatus);
            _mockAirplaneDataService.Setup(s => s.GetMinorModelData(It.IsAny<string>())).ReturnsAsync(new List<MinorModel>());

            HttpResponseMessage response = await httpClient.GetAsync("Hecp/ViewFilteredHecps?program=787&hecpName=&siteName=&ataChapterNumber=&ataChapterTitle=&affectedSystem=&hecpStatusName=&PageNumber=1");

            string output = await response.Content.ReadAsStringAsync();
            IHtmlDocument htmlDocument = parser.ParseDocument(output);
            string firstHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[2].TextContent.Trim();
            string secondHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[2].TextContent.Trim();
            IElement firstHecpEditButton = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[7].GetElementsByClassName("dropdown-item")[0];
            IElement secondHecpEditButton = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[7].GetElementsByClassName("dropdown-item")[0];
            string firstHecpId = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[0].TextContent.Trim();
            string secondHecpId = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[0].TextContent.Trim();

            Assert.AreEqual(hecps[0].Name, firstHecpName);
            Assert.AreEqual(hecps[1].Name, secondHecpName);
            Assert.AreEqual("5", firstHecpId);
            Assert.AreEqual("6", secondHecpId);
            Assert.AreEqual(1, firstHecpEditButton.GetElementsByClassName("hecp-edit-disabled").Length);
            Assert.AreEqual(0, secondHecpEditButton.GetElementsByClassName("hecp-edit-disabled").Length);
        }

        [TestMethod]
        public async Task Should_Show_Edit_Button_If_User_Is_Engineer_For_Engineered_Hecp()
        {
            _user.Role = new Shield.Common.Models.Users.Role()
            {
                Name = Constants.ENGINEER,
                Id = Constants.ENGINEER_ID
            };
            _user.IsITAR = false;
            List<Hecp> hecps = new List<Hecp>()
            {
                new Hecp
                {
                    Id = 5,
                    Name = "Test Hecp Engineered",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsEngineered = true,
                    IsItar = false,
                    UpdatedDate = new DateTime(2023, 08, 20, 1, 0, 0),
                    IsSuccessfullyMigrated = true,
                    IsMigrationDataCheckNeeded = false
                },
                new Hecp
                {
                    Id = 6,
                    Name = "Test Hecp non-Engineered",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = false,
                    IsEngineered =false,
                    UpdatedDate = new DateTime(2023, 09, 20, 1, 0, 0),
                    IsSuccessfullyMigrated = true,
                    IsMigrationDataCheckNeeded = false
                }
            };
            List<string> sites = new List<string>
            {
                "BSC", "Everett"
            };
            PagingWrapper<Hecp> hecpList = new PagingWrapper<Hecp>()
            {
                Data = hecps,
                PageCount = 1,
                PageNumber = 1,
                PageSize = 1
            };
            List<Status> hecpStatus = new List<Status>()
            {
                new Status
                {
                    Id = 1,
                    Description = "inreview",
                    DisplayName = "inreview"
                },
                new Status
                {
                    Id = 2,
                    Description = "draft",
                    DisplayName = "draft"
                }
            };

            _mockSessionService.Setup(sS => sS.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_user);
            _mockHecpService.Setup(s => s.GetFilteredHecps(It.IsAny<HecpFilterRequest>())).ReturnsAsync(hecpList);
            _mockAirplaneDataService.Setup(s => s.GetSitesByProgramAsync(It.IsAny<string>())).ReturnsAsync(sites);
            _mockHecpService.Setup(s => s.GetHecpStatusList()).ReturnsAsync(hecpStatus);
            _mockAirplaneDataService.Setup(s => s.GetMinorModelData(It.IsAny<string>())).ReturnsAsync(new List<MinorModel>());

            HttpResponseMessage response = await httpClient.GetAsync("Hecp/ViewFilteredHecps?program=787&hecpName=&siteName=&ataChapterNumber=&ataChapterTitle=&affectedSystem=&hecpStatusName=&PageNumber=1");

            string output = await response.Content.ReadAsStringAsync();
            IHtmlDocument htmlDocument = parser.ParseDocument(output);
            string firstHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[2].TextContent.Trim();
            string secondHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[2].TextContent.Trim();
            IElement firstHecpEditButton = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[7].GetElementsByClassName("dropdown-item")[0];
            IElement secondHecpEditButton = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[7].GetElementsByClassName("dropdown-item")[0];
            string firstHecpId = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[0].TextContent.Trim();
            string secondHecpId = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[0].TextContent.Trim();

            Assert.AreEqual(hecps[0].Name, firstHecpName);
            Assert.AreEqual(hecps[1].Name, secondHecpName);
            Assert.AreEqual("5", firstHecpId);
            Assert.AreEqual("6", secondHecpId);
            Assert.AreEqual(0, firstHecpEditButton.GetElementsByClassName("hecp-edit-disabled").Length);
            Assert.AreEqual(0, secondHecpEditButton.GetElementsByClassName("hecp-edit-disabled").Length);
        }

        [TestMethod]
        public async Task Should_Not_Show_Edit_Button_If_User_Is_NonEngineer_But_Author_For_Engineered_Hecp()
        {
            _user.Role = new Shield.Common.Models.Users.Role()
            {
                Name = Constants.GROUP_COORDINATOR,
                Id = Constants.GROUP_COORDINATOR_ID
            };
            _user.IsITAR = false;
            List<Hecp> hecps = new List<Hecp>()
            {
                new Hecp
                {
                    Id = 5,
                    Name = "Test Hecp Engineered",
                    Revision = "A",
                    CreatedByBemsId = 123456,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsEngineered = true,
                    IsItar = false,
                    UpdatedDate = new DateTime(2023, 08, 20, 1, 0, 0),
                    IsSuccessfullyMigrated = true,
                    IsMigrationDataCheckNeeded = false
                },
                new Hecp
                {
                    Id = 6,
                    Name = "Test Hecp non-Engineered",
                    Revision = "A",
                    CreatedByBemsId = 123456,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = false,
                    IsEngineered =false,
                    UpdatedDate = new DateTime(2023, 09, 20, 1, 0, 0),
                    IsSuccessfullyMigrated = true,
                    IsMigrationDataCheckNeeded = false
                }
            };
            List<string> sites = new List<string>
            {
                "BSC", "Everett"
            };
            PagingWrapper<Hecp> hecpList = new PagingWrapper<Hecp>()
            {
                Data = hecps,
                PageCount = 1,
                PageNumber = 1,
                PageSize = 1
            };
            List<Status> hecpStatus = new List<Status>()
            {
                new Status
                {
                    Id = 1,
                    Description = "inreview",
                    DisplayName = "inreview"
                },
                new Status
                {
                    Id = 2,
                    Description = "draft",
                    DisplayName = "draft"
                }
            };

            _mockSessionService.Setup(sS => sS.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_user);
            _mockHecpService.Setup(s => s.GetFilteredHecps(It.IsAny<HecpFilterRequest>())).ReturnsAsync(hecpList);
            _mockAirplaneDataService.Setup(s => s.GetSitesByProgramAsync(It.IsAny<string>())).ReturnsAsync(sites);
            _mockHecpService.Setup(s => s.GetHecpStatusList()).ReturnsAsync(hecpStatus);
            _mockAirplaneDataService.Setup(s => s.GetMinorModelData(It.IsAny<string>())).ReturnsAsync(new List<MinorModel>());

            HttpResponseMessage response = await httpClient.GetAsync("Hecp/ViewFilteredHecps?program=787&hecpName=&siteName=&ataChapterNumber=&ataChapterTitle=&affectedSystem=&hecpStatusName=&PageNumber=1");

            string output = await response.Content.ReadAsStringAsync();
            IHtmlDocument htmlDocument = parser.ParseDocument(output);
            string firstHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[2].TextContent.Trim();
            string secondHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[2].TextContent.Trim();
            IElement firstHecpEditButton = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[7].GetElementsByClassName("dropdown-item")[0];
            IElement secondHecpEditButton = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[7].GetElementsByClassName("dropdown-item")[0];
            string firstHecpId = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[0].TextContent.Trim();
            string secondHecpId = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[0].TextContent.Trim();

            Assert.AreEqual(hecps[0].Name, firstHecpName);
            Assert.AreEqual(hecps[1].Name, secondHecpName);
            Assert.AreEqual("5", firstHecpId);
            Assert.AreEqual("6", secondHecpId);
            Assert.AreEqual(1, firstHecpEditButton.GetElementsByClassName("hecp-edit-disabled").Length);
            Assert.AreEqual(0, secondHecpEditButton.GetElementsByClassName("hecp-edit-disabled").Length);
        }

        [TestMethod]
        public async Task Should_Show_Edit_Button_If_ITARUser_Is_Engineer_For_ITAR_Engineered_Hecp()
        {
            _user.Role = new Shield.Common.Models.Users.Role()
            {
                Name = Constants.ENGINEER,
                Id = Constants.ENGINEER_ID
            };
            _user.IsITAR = true;
            List<Hecp> hecps = new List<Hecp>()
            {
                new Hecp
                {
                    Id = 5,
                    Name = "Test Hecp Engineered",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsEngineered = true,
                    IsItar = true,
                    UpdatedDate = new DateTime(2023, 08, 20, 1, 0, 0),
                    IsSuccessfullyMigrated = true,
                    IsMigrationDataCheckNeeded = false
                },
                new Hecp
                {
                    Id = 6,
                    Name = "Test Hecp non-Engineered",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = false,
                    IsEngineered =false,
                    UpdatedDate = new DateTime(2023, 09, 20, 1, 0, 0),
                    IsSuccessfullyMigrated = true,
                    IsMigrationDataCheckNeeded = false
                }
            };
            List<string> sites = new List<string>
            {
                "BSC", "Everett"
            };
            PagingWrapper<Hecp> hecpList = new PagingWrapper<Hecp>()
            {
                Data = hecps,
                PageCount = 1,
                PageNumber = 1,
                PageSize = 1
            };
            List<Status> hecpStatus = new List<Status>()
                {
                    new Status
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    new Status
                    {
                        Id = 2,
                        Description = "draft",
                        DisplayName = "draft"
                    }
                };

            _mockSessionService.Setup(sS => sS.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_user);
            _mockHecpService.Setup(s => s.GetFilteredHecps(It.IsAny<HecpFilterRequest>())).ReturnsAsync(hecpList);
            _mockAirplaneDataService.Setup(s => s.GetSitesByProgramAsync(It.IsAny<string>())).ReturnsAsync(sites);
            _mockHecpService.Setup(s => s.GetHecpStatusList()).ReturnsAsync(hecpStatus);
            _mockAirplaneDataService.Setup(s => s.GetMinorModelData(It.IsAny<string>())).ReturnsAsync(new List<MinorModel>());

            HttpResponseMessage response = await httpClient.GetAsync("Hecp/ViewFilteredHecps?program=787&hecpName=&siteName=&ataChapterNumber=&ataChapterTitle=&affectedSystem=&hecpStatusName=&PageNumber=1");

            string output = await response.Content.ReadAsStringAsync();
            IHtmlDocument htmlDocument = parser.ParseDocument(output);
            string firstHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[2].TextContent.Trim();
            string secondHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[2].TextContent.Trim();
            IElement firstHecpEditButton = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[7].GetElementsByClassName("dropdown-item")[0];
            IElement secondHecpEditButton = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[7].GetElementsByClassName("dropdown-item")[0];
            string firstHecpId = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[0].TextContent.Trim();
            string secondHecpId = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[0].TextContent.Trim();

            Assert.AreEqual(hecps[0].Name, firstHecpName);
            Assert.AreEqual(hecps[1].Name, secondHecpName);
            Assert.AreEqual("5", firstHecpId);
            Assert.AreEqual("6", secondHecpId);
            Assert.AreEqual(0, firstHecpEditButton.GetElementsByClassName("hecp-edit-disabled").Length);
            Assert.AreEqual(0, secondHecpEditButton.GetElementsByClassName("hecp-edit-disabled").Length);
        }

        [TestMethod]
        public async Task Should_Not_Show_Edit_Button_If_ITARUser_Is_NonEngineer_For_ITAR_Engineered_Hecp()
        {
            _user.Role = new Shield.Common.Models.Users.Role()
            {
                Name = Constants.GROUP_COORDINATOR,
                Id = Constants.GROUP_COORDINATOR_ID
            };
            _user.IsITAR = true;
            List<Hecp> hecps = new List<Hecp>()
            {
                new Hecp
                {
                    Id = 5,
                    Name = "Test Hecp Engineered",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsEngineered = true,
                    IsItar = true,
                    UpdatedDate = new DateTime(2023, 08, 20, 1, 0, 0),
                    IsSuccessfullyMigrated = true,
                    IsMigrationDataCheckNeeded = false
                },
                new Hecp
                {
                    Id = 6,
                    Name = "Test Hecp non-Engineered",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = true,
                    IsEngineered =false,
                    UpdatedDate = new DateTime(2023, 09, 20, 1, 0, 0),
                    IsSuccessfullyMigrated = true,
                    IsMigrationDataCheckNeeded = false
                }
            };
            List<string> sites = new List<string>
            {
                "BSC", "Everett"
            };
            PagingWrapper<Hecp> hecpList = new PagingWrapper<Hecp>()
            {
                Data = hecps,
                PageCount = 1,
                PageNumber = 1,
                PageSize = 1
            };
            List<Status> hecpStatus = new List<Status>()
            {
                new Status
                {
                    Id = 1,
                    Description = "inreview",
                    DisplayName = "inreview"
                },
                new Status
                {
                    Id = 2,
                    Description = "draft",
                    DisplayName = "draft"
                }
            };

            _mockSessionService.Setup(sS => sS.GetUserFromSession(It.IsAny<HttpContext>())).Returns(_user);
            _mockHecpService.Setup(s => s.GetFilteredHecps(It.IsAny<HecpFilterRequest>())).ReturnsAsync(hecpList);
            _mockAirplaneDataService.Setup(s => s.GetSitesByProgramAsync(It.IsAny<string>())).ReturnsAsync(sites);
            _mockHecpService.Setup(s => s.GetHecpStatusList()).ReturnsAsync(hecpStatus);
            _mockAirplaneDataService.Setup(s => s.GetMinorModelData(It.IsAny<string>())).ReturnsAsync(new List<MinorModel>());

            HttpResponseMessage response = await httpClient.GetAsync("Hecp/ViewFilteredHecps?program=787&hecpName=&siteName=&ataChapterNumber=&ataChapterTitle=&affectedSystem=&hecpStatusName=&PageNumber=1");

            string output = await response.Content.ReadAsStringAsync();
            IHtmlDocument htmlDocument = parser.ParseDocument(output);
            string firstHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[2].TextContent.Trim();
            string secondHecpName = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[2].TextContent.Trim();
            IElement firstHecpEditButton = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[7].GetElementsByClassName("dropdown-item")[0];
            IElement secondHecpEditButton = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[7].GetElementsByClassName("dropdown-item")[0];
            string firstHecpId = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[0].TextContent.Trim();
            string secondHecpId = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[0].TextContent.Trim();

            Assert.AreEqual(hecps[0].Name, firstHecpName);
            Assert.AreEqual(hecps[1].Name, secondHecpName);
            Assert.AreEqual("5", firstHecpId);
            Assert.AreEqual("6", secondHecpId);
            Assert.AreEqual(1, firstHecpEditButton.GetElementsByClassName("hecp-edit-disabled").Length);
            Assert.AreEqual(0, secondHecpEditButton.GetElementsByClassName("hecp-edit-disabled").Length);
        }

        [TestMethod]
        public async Task Should_Show_Or_Not_Show_Icon_Based_On_HecpType()
        {
            // Arrange
            User user = new User()
            {
                Permissions = new List<string>()
                {
                    Shield.Common.Constants.Permission.SIGN_DISCRETE
                },
                Role = new Shield.Common.Models.Users.Role()
                {
                    Name = Constants.GROUP_COORDINATOR,
                    Id = Constants.GROUP_COORDINATOR_ID
                },
                HecpApproverDetails = new List<HecpApproverDetails>()
                {
                    new HecpApproverDetails()
                    {
                        BemsId=123,
                        Site="BSC",
                        Program="787",
                        IsHecpPublisher=true
                    }
                },
                IsITAR = false,
                Programs = "787",
                BemsId = 123456
            };
            List<Hecp> hecps = new List<Hecp>()
            {
                new Hecp
                {
                    Id = 5,
                    Name = "Test Engineered Hecp",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Shield.Common.Models.Loto.Shared.Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = true,
                    IsEngineered = true,
                    UpdatedDate = new DateTime(2023, 08, 20, 1, 0, 0),
                    IsSuccessfullyMigrated = true,
                    IsMigrationDataCheckNeeded = false
                },
                new Hecp
                {
                    Id = 6,
                    Name = "Test Non-Engineered Hecp",
                    Revision = "A",
                    CreatedByBemsId = 2817760,
                    Site = "BSC",
                    Program = "787",
                    LineNumber = "9999",
                    LastStepCompleted = 7,
                    Status = new Shield.Common.Models.Loto.Shared.Status()
                    {
                        Id = 1,
                        Description = "inreview",
                        DisplayName = "inreview"
                    },
                    IsItar = false,
                    IsEngineered = false,
                    UpdatedDate = new DateTime(2023, 09, 20, 1, 0, 0),
                    IsSuccessfullyMigrated = true,
                    IsMigrationDataCheckNeeded = false
                }
            };
            List<string> sites = new List<string>
            {
                "BSC", "Everett"
            };
            PagingWrapper<Hecp> hecpList = new PagingWrapper<Hecp>()
            {
                Data = hecps,
                PageCount = 1,
                PageNumber = 1,
                PageSize = 1
            };
            List<Status> hecpStatus = new List<Status>()
            {
                new Status
                {
                    Id = 1,
                    Description = "inreview",
                    DisplayName = "inreview"
                },
                new Status
                {
                    Id = 2,
                    Description = "draft",
                    DisplayName = "draft"
                }
            };

            _mockSessionService.Setup(sS => sS.GetUserFromSession(It.IsAny<HttpContext>())).Returns(user);
            _mockHecpService.Setup(s => s.GetFilteredHecps(It.IsAny<HecpFilterRequest>())).ReturnsAsync(hecpList);
            _mockAirplaneDataService.Setup(s => s.GetSitesByProgramAsync(It.IsAny<string>())).ReturnsAsync(sites);
            _mockHecpService.Setup(s => s.GetHecpStatusList()).ReturnsAsync(hecpStatus);
            _mockAirplaneDataService.Setup(s => s.GetMinorModelData(It.IsAny<string>())).ReturnsAsync(new List<MinorModel>());

            HttpResponseMessage response = await httpClient.GetAsync("Hecp/ViewFilteredHecps?program=787&hecpName=&siteName=&ataChapterNumber=&ataChapterTitle=&affectedSystem=&hecpStatusName=&PageNumber=1&IsEngineered=true");
            string output = await response.Content.ReadAsStringAsync();
            IHtmlDocument htmlDocument = parser.ParseDocument(output);

            var firstNonEngineeredHecp = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[1].GetElementsByTagName("td")[1].GetElementsByTagName("i");
            Assert.AreEqual(0, firstNonEngineeredHecp.Length);
            var firstEngineeredHecp = htmlDocument.GetElementById("hecpDetails").Children[1].GetElementsByTagName("tr")[0].GetElementsByTagName("td")[1].GetElementsByTagName("img");
            Assert.AreEqual(1, firstEngineeredHecp.Length);
            Assert.AreEqual("/images/engineeredHecp.png", firstEngineeredHecp[0].GetAttribute("src"));
            Assert.AreEqual("This is an Engineered Hecp", firstEngineeredHecp[0].GetAttribute("title"));

            string isEngineeredSelected = htmlDocument.GetElementById("HecpType").GetElementsByTagName("option")[1].GetAttribute("value");
            string isDiscreteSelected = htmlDocument.GetElementById("HecpType").GetElementsByTagName("option")[2].GetAttribute("value");
            string isEngineered = htmlDocument.GetElementById("HecpType").GetElementsByTagName("option")[1].TextContent.Trim();
            string isDiscrete = htmlDocument.GetElementById("HecpType").GetElementsByTagName("option")[2].TextContent.Trim();
            Assert.AreEqual("true",isEngineeredSelected);
            Assert.AreEqual("Engineered", isEngineered);
            Assert.AreEqual("false", isDiscreteSelected);
            Assert.AreEqual("Discrete", isDiscrete);
        }
    }
}
