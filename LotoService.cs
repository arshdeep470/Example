using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Shield.Common;
using Shield.Common.Models.Common;
using Shield.Common.Models.Loto;
using Shield.Common.Models.Loto.Shared;
using Shield.Ui.App.Common;
using Shield.Ui.App.Models.CommonModels;
using Shield.Ui.App.Models.HecpModels;
using Shield.Ui.App.Models.LotoModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Shield.Ui.App.Services
{
    public class LotoService
    {
        private HttpClientService _clientService;

        public LotoService(HttpClientService clientService)
        {
            _clientService = clientService;
        }

        public virtual async Task<HTTPResponseWrapper<Models.LotoModels.Loto>> Create(string workPackage, string reason, string site, string model, string lineNumber, User user, List<int> associatedMinorModelIdList)
        {
            try
            {
                string username = user.FirstName + " " + user.LastName;
                string jsonItem = JsonConvert.SerializeObject(new Models.CommonModels.CreateLotoRequest
                {
                    WorkPackage = workPackage,
                    Reason = reason,
                    Site = site,
                    Model = model,
                    LineNumber = lineNumber,
                    CreatedByBemsId = user.BemsId,
                    CreatedByName = username,
                    AssociatedMinorModelIdList = associatedMinorModelIdList
                });

                var uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Loto/");
                StringContent content = new StringContent(jsonItem, Encoding.UTF8, "application/json");
                if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                {
                    _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                }
                var msg = await _clientService.GetClient().PostAsync(uri, content, Constants.LotoServiceAPIKey, APISystems.LotoAPI);

                string value = await msg.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<HTTPResponseWrapper<Models.LotoModels.Loto>>(value);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return null;
            }
        }

        public virtual async Task<HTTPResponseWrapper<Models.LotoModels.Loto>> CreateLotoFromDiscrete(Models.CommonModels.CreateLotoRequest request)
        {
            try
            {
                string jsonItem = JsonConvert.SerializeObject(request);

                var uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Loto/FromDiscrete/");
                if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                {
                    _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                }
                StringContent content = new StringContent(jsonItem, Encoding.UTF8, "application/json");
                var msg = await _clientService.GetClient().PostAsync(uri, content, Constants.LotoServiceAPIKey, APISystems.LotoAPI);

                string value = await msg.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<HTTPResponseWrapper<Models.LotoModels.Loto>>(value);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return null;
            }
        }

        public virtual async Task<HTTPResponseWrapper<Models.LotoModels.Loto>> Update(Models.LotoModels.Loto loto)
        {
            try
            {
                string jsonItem = JsonConvert.SerializeObject(loto);

                var uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Loto");

                StringContent content = new StringContent(jsonItem, Encoding.UTF8, "application/json");
                if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                {
                    _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                }
                var msg = await _clientService.GetClient().PutAsync(uri, content, Constants.LotoServiceAPIKey, APISystems.LotoAPI);

                HTTPResponseWrapper<Shield.Ui.App.Models.LotoModels.Loto> response = JsonConvert.DeserializeObject<HTTPResponseWrapper<Shield.Ui.App.Models.LotoModels.Loto>>(await msg.Content.ReadAsStringAsync());

                return response;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return null;
            }
        }

        public virtual async Task<ObjectResult> DeleteLotoById(int lotoId)
        {
            try
            {
                var uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Loto/DeleteLoto/" + lotoId);
                string jsonItem = JsonConvert.SerializeObject(new { id = lotoId });
                StringContent content = new StringContent(jsonItem, Encoding.UTF8, "application/json");

                if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                {
                    _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                }
                var response = await _clientService.GetClient().PostAsync(uri, content, Constants.LotoServiceAPIKey, APISystems.LotoAPI);

                string responseMessage = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return new ObjectResult(responseMessage) { StatusCode = 200 };
                }
                else
                {
                    return new ObjectResult("Failed to delete isolation.") { StatusCode = 500 };
                }

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return new ObjectResult("Failed to delete isolation.") { StatusCode = 500 }; ;
            }


        }

        public virtual async Task<ObjectResult> DeleteIsolation(int isoId, int lotoId)
        {
            try
            {
                var uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Isolation/IsolationRemoval/IsolationId/" + isoId + "/lotoId/" + lotoId);
                string jsonItem = JsonConvert.SerializeObject(new { id = isoId });

                StringContent content = new StringContent(jsonItem, Encoding.UTF8, "application/json");
                if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                {
                    _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                }
                var response = await _clientService.GetClient().PostAsync(uri, content, Constants.LotoServiceAPIKey, APISystems.LotoAPI);

                string responseMessage = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return new ObjectResult(responseMessage) { StatusCode = 200 };
                }
                else
                {
                    return new ObjectResult("Failed to delete isolation.") { StatusCode = 500 };
                }

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return new ObjectResult("Failed to delete isolation.") { StatusCode = 500 };
            }

        }

        public virtual async Task<List<Models.LotoModels.Loto>> GetLotosByLineNumberAndModel(string lineNumber, string model)
        {
            try
            {
                var response = await _clientService.GetClient().GetAsync(new Uri(EnvironmentHelper.LotoServiceAddress + "Loto/" + $"Program/{model}/LineNumber/{lineNumber}"));
                return JsonConvert.DeserializeObject<HTTPResponseWrapper<List<Models.LotoModels.Loto>>>(await response.Content.ReadAsStringAsync()).Data;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("GetLotoByLineNumberAndModel ---  " + e.Message);
                return new List<Models.LotoModels.Loto>();
            }
        }

        public virtual async Task<Models.LotoModels.Loto> GetLotoDetail(int lotoId)
        {
            try
            {
                var response = await _clientService.GetClient().GetAsync(new Uri(EnvironmentHelper.LotoServiceAddress + "Loto/LotoDetail/LotoId/" + lotoId));
                var resContent = await response.Content.ReadAsStringAsync();
                var deserializedRes = JsonConvert.DeserializeObject<HTTPResponseWrapper<Models.LotoModels.Loto>>(resContent).Data;
                return deserializedRes;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("GetLotoDetail exception: ");
                Console.Error.WriteLine(ex);
                return null;
            }
        }

        public virtual async Task<HTTPResponseWrapper<Models.LotoModels.Loto>> AssignPAE(int paeBemsId, int lotoId, string paeDisplayName,bool overrideTraining, string reasonToOverride, int gcBemsId)
        {
            try
            {
                string jsonItem = JsonConvert.SerializeObject(new SigningPAERequest
                {
                    BemsId = paeBemsId,
                    LotoId = lotoId,
                    PAEName = paeDisplayName,
                    OverrideTraining = overrideTraining,
                    ReasonToOverrideTriaining = reasonToOverride,
                    GcBemsId = gcBemsId
                });

                var uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Loto/PAESignIn");

                StringContent content = new StringContent(jsonItem, Encoding.UTF8, "application/json");
                if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                {
                    _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                }
                var msg = await _clientService.GetClient().PostAsync(uri, content, Constants.LotoServiceAPIKey, APISystems.LotoAPI);

                string strResponse = await msg.Content.ReadAsStringAsync();

                HTTPResponseWrapper<Models.LotoModels.Loto> pae = JsonConvert.DeserializeObject<HTTPResponseWrapper<Models.LotoModels.Loto>>(strResponse);

                return pae;

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);

                return null;
            }
        }

        public virtual async Task<List<LotoTransaction>> GetLotoTransactionsByLotoId(int lotoId)
        {
            try
            {
                Uri uri = new Uri(EnvironmentHelper.LotoServiceAddress + "LotoTransaction/LotoId/" + lotoId);
                var msg = await _clientService.GetClient().GetAsync(uri);
                List<LotoTransaction> list = JsonConvert.DeserializeObject<HTTPResponseWrapper<List<LotoTransaction>>>(await msg.Content.ReadAsStringAsync()).Data;
                list = list.OrderByDescending(c => c.Date).ToList();
                return list;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return new List<LotoTransaction>();
            }

        }

        public virtual async Task<HTTPResponseWrapper<AssignedAE>> AssignAE(AssignedAE aeToAssign, bool overrideTraining, string GCDisplayName, int GCBemsId, string reasonToOverride)
        {
            try
            {
                string jsonItem = JsonConvert.SerializeObject(new SigningAERequest
                {
                    AEBemsId = aeToAssign.AEBemsId,
                    LotoId = aeToAssign.LotoId,
                    AEName = aeToAssign.AEName,
                    OverrideTraining = overrideTraining,
                    ReasonToOverrideTriaining = reasonToOverride,
                    UserName = GCDisplayName,
                    UserBemsId = GCBemsId
                });

                var uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Loto/AESignIn");

                StringContent content = new StringContent(jsonItem, Encoding.UTF8, "application/json");
                if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                {
                    _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                }
                var msg = await _clientService.GetClient().PostAsync(uri, content, Constants.LotoServiceAPIKey, APISystems.LotoAPI);
                
                string strResponse = await msg.Content.ReadAsStringAsync();

                HTTPResponseWrapper<AssignedAE> ae = JsonConvert.DeserializeObject<HTTPResponseWrapper<AssignedAE>>(strResponse);

                return ae;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);

                return null;
            }
        }

        public virtual async Task<HTTPResponseWrapper<AssignedAE>> AssignVisitor(AssignedAE aeToAssign, bool overrideTraining, string GCDisplayName, int GCBemsId)
        {
            try
            {
                string jsonItem = JsonConvert.SerializeObject(new SigningAERequest
                {
                    AEBemsId = aeToAssign.AEBemsId,
                    LotoId = aeToAssign.LotoId,
                    AEName = aeToAssign.AEName,
                    OverrideTraining = overrideTraining,
                    UserName = GCDisplayName,
                    UserBemsId = GCBemsId
                });

                var uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Loto/VisitorSignIn");

                StringContent content = new StringContent(jsonItem, Encoding.UTF8, "application/json");
                if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                {
                    _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                }
                var msg = await _clientService.GetClient().PostAsync(uri, content, Constants.LotoServiceAPIKey, APISystems.LotoAPI);

                return JsonConvert.DeserializeObject<HTTPResponseWrapper<AssignedAE>>(await msg.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);

                return null;
            }
        }

        public virtual async Task<HTTPResponseWrapper<LotoIsolationsDiscreteHecp>> InstallIsolation(InstallIsolationRequestWithId iso, User installer)
        {
            try
            {
                InstallIsolationRequestWithId request = new InstallIsolationRequestWithId
                {
                    LotoId = iso.LotoId,
                    SystemCircuitId = iso.SystemCircuitId,
                    CircuitNomenclature = iso.CircuitNomenclature,
                    Tag = iso.Tag,
                    InstalledByBemsId = iso.InstalledByBemsId,
                    InstallerDisplayName = installer.FirstName + " " + installer.LastName,
                    LotoAssociatedId = iso.LotoAssociatedId
                };

                string jsonItem = JsonConvert.SerializeObject(request);
                var uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Isolation");

                var stringContent = new StringContent(jsonItem, Encoding.UTF8, "application/json");
                if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                {
                    _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                }
                var msg = await _clientService.GetClient().PostAsync(uri, stringContent, Constants.LotoServiceAPIKey, APISystems.LotoAPI);

                return JsonConvert.DeserializeObject<HTTPResponseWrapper<LotoIsolationsDiscreteHecp>>(await msg.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);

                return null;
            }
        }

        public virtual async Task<Isolation> InstallDiscreteIsolation(InstallIsolationRequest iso, User installer)
        {
            try
            {
                iso.InstallerDisplayName = installer.FirstName + " " + installer.LastName;

                string jsonItem = JsonConvert.SerializeObject(iso);
                var uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Isolation");

                var stringContent = new StringContent(jsonItem, Encoding.UTF8, "application/json");
                if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                {
                    _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                }
                var msg = await _clientService.GetClient().PutAsync(uri, stringContent, Constants.LotoServiceAPIKey, APISystems.LotoAPI);

                return JsonConvert.DeserializeObject<Isolation>(await msg.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);

                return null;
            }
        }

        public virtual async Task<bool> SignOutAE(int lotoId, int aeBemsId, string aeName, int userBemsId, string userName)
        {
            var webToken = await _clientService.GetWebToken(Constants.LotoServiceAPIKey, APISystems.LotoAPI);
            if (webToken)
            {
                try
                {
                    SigningAERequest req = new SigningAERequest()
                    {
                        LotoId = lotoId,
                        AEBemsId = aeBemsId,
                        AEName = aeName,
                        UserBemsId = userBemsId,
                        UserName = userName
                    };

                    string json = JsonConvert.SerializeObject(req);
                    StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    var lotoUri = new Uri(EnvironmentHelper.LotoServiceAddress + "Loto/AESignOut");
                    if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                    {
                        _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                    }
                    var msg = await _clientService.GetClient().PutAsync(lotoUri, content);

                    return (int)msg.StatusCode == 200;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
            }
            return false;

        }

        public virtual async Task<bool> SignOutVisitor(int lotoId, int idToDelete, string aeName, int userBemsId, string userName)
        {
            var webToken = await _clientService.GetWebToken(Constants.LotoServiceAPIKey, APISystems.LotoAPI);
            if (webToken)
            {
                try
                {
                    SigningAERequest req = new SigningAERequest()
                    {
                        LotoId = lotoId,
                        AEBemsId = idToDelete,
                        AEName = aeName,
                        UserBemsId = userBemsId,
                        UserName = userName
                    };

                    string json = JsonConvert.SerializeObject(req);
                    var lotoUri = new Uri(EnvironmentHelper.LotoServiceAddress + "Loto/VisitorSignOut");

                    StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                    {
                        _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                    }
                    var msg = await _clientService.GetClient().PutAsync(lotoUri, content);

                    return (int)msg.StatusCode == 200;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
            }
            return false;

        }

        public virtual async Task<Models.LotoModels.Loto> UninstallIsolationsAndCompleteLoto(int lotoId, int removeByBemsId, string removeByName)
        {
            try
            {
                var uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Status/Complete");

                StatusChangeRequest requestObject = new StatusChangeRequest()
                {
                    Id = lotoId,
                    BemsId = removeByBemsId,
                    DisplayName = removeByName
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(requestObject), Encoding.UTF8, "application/json");

                if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                {
                    _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                }
                var res = await _clientService.GetClient().PostAsync(uri, content, Constants.LotoServiceAPIKey, APISystems.LotoAPI);

                if (res.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return JsonConvert.DeserializeObject<Models.LotoModels.Loto>(await res.Content.ReadAsStringAsync());
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error in Uninstall Isolations " + ex);
                return null;
            }
        }

        #region status change

        public virtual async Task<bool> Lockout(StatusChangeRequest request)
        {

            try
            {
                string jsonLotoId = JsonConvert.SerializeObject(request);
                var uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Status/Lockout");

                StringContent content = new StringContent(jsonLotoId, Encoding.UTF8, "application/json");
                if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                {
                    _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                }
                var res = await _clientService.GetClient().PostAsync(uri, content, Constants.LotoServiceAPIKey, APISystems.LotoAPI);
                if (res.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);

                return false;
            }

            return false;
        }

        public virtual async Task<bool> Transfer(StatusChangeRequest changeRequest)
        {
            try
            {
                string jsonRequest = JsonConvert.SerializeObject(changeRequest);
                var uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Status/Transfer");

                StringContent content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                {
                    _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                }
                var res = await _clientService.GetClient().PostAsync(uri, content, Constants.LotoServiceAPIKey, APISystems.LotoAPI);
                if (res.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error in Transfer service" + ex);

                return false;
            }

            return false;
        }

        #endregion status change

        public virtual async Task<List<Models.LotoModels.Loto>> GetAllInProgressLotosWithIsolations(string program, string lineNumber)
        {

            List<Models.LotoModels.Loto> lotos = new List<Models.LotoModels.Loto>();

            try
            {

                Uri uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Loto/" + $"ActiveIsolations/Program/{program}/LineNumber/{lineNumber}");

                HttpResponseMessage res = await _clientService.GetClient().GetAsync(uri);

                if (res.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string str = await res.Content.ReadAsStringAsync();
                    lotos = JsonConvert.DeserializeObject<List<Models.LotoModels.Loto>>(str);
                }

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error getting active isolations " + ex);

                return new List<Models.LotoModels.Loto>();
            }

            return lotos;
        }

        public virtual async Task<Dictionary<int, int>> AirplaneLotoCount(List<Shield.Ui.App.Models.CommonModels.Aircraft> aircraft)
        {
            Uri uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Loto/AirplaneLotoCount");

            IHttpClient client = _clientService.GetClient();
            var content = new StringContent(JsonConvert.SerializeObject(aircraft), Encoding.UTF8, "application/json");
            if (client.DefaultRequestHeaders != null && !client.DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
            {
                client.DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
            }
            var response = await client.PostAsync(uri, content, Constants.LotoServiceAPIKey, APISystems.LotoAPI);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                // Dictionary<Airplane Id, Loto Count>
                Dictionary<int, int> dictAirplaneToLotoCount = JsonConvert.DeserializeObject<Dictionary<int, int>>(responseString);
                return dictAirplaneToLotoCount;
            }

            throw new Exception(responseString);
        }

        public virtual async Task<HTTPResponseWrapper<Isolation>> UnlockIsolation(int isoId)
        {
            var uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Isolation/IsolationUnlock/IsolationId/" + isoId);
            string jsonItem = JsonConvert.SerializeObject(new { id = isoId });

            StringContent content = new StringContent(jsonItem, Encoding.UTF8, "application/json");
            if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
            {
                _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
            }
            var response = await _clientService.GetClient().PutAsync(uri, content, Constants.LotoServiceAPIKey, APISystems.LotoAPI);

            string responseString = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<HTTPResponseWrapper<Isolation>>(responseString);
        }

        public virtual async Task<PagingWrapper<Hecp>> GetPublishedHecpsForLoto(string site, string program, string title, string ata, List<int> minorModelIdList,int pageNumber, bool? isEngineered = null)
        {
            StringBuilder sb = new StringBuilder();
            if (minorModelIdList.Count > 0)
            {
                foreach (int id in minorModelIdList)
                {
                    sb.Append("&minorModelIdList=" + id.ToString());
                }
            }
            else
            {
                sb.Append("&minorModelIdList=null");
            }
            Uri uri = new Uri(EnvironmentHelper.LotoServiceAddress + $"Hecp/GetPublishedHecpForLoto?Site=" + site + "&Program=" + program + "&hecpTitle=" + title + "&Ata=" + ata + sb.ToString() +"&pageNumber=" +pageNumber + "&isEngineered=" + isEngineered);

            HttpResponseMessage res = await _clientService.GetClient().GetAsync(uri);

            if (res.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string strResponse = await res.Content.ReadAsStringAsync();

                if (!String.IsNullOrEmpty(strResponse))
                {
                    try
                    {
                        HTTPResponseWrapper<PagingWrapper<Hecp>> hecpList = JsonConvert.DeserializeObject<HTTPResponseWrapper<PagingWrapper<Hecp>>>(strResponse);
                        return hecpList.Data;
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e);
                        return null;
                    }
                }
            }

            return null;
        }

        public virtual async Task<List<HecpIsolationTag>> GetHecpIsolationTagsForLoto(GetIsolationtagRequest request)
        {
            var hecpIsolationTags = new List<HecpIsolationTag>();
            string jsonItem = JsonConvert.SerializeObject(request);
            Uri uri = new Uri(EnvironmentHelper.LotoServiceAddress + $"Hecp/GetIsolationTags");
            if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
            {
                _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
            }
            StringContent content = new StringContent(jsonItem, Encoding.UTF8, "application/json");
            var res = await _clientService.GetClient().PostAsync(uri, content, Constants.LotoServiceAPIKey, APISystems.LotoAPI);

            if (res.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string strResponse = await res.Content.ReadAsStringAsync();
                hecpIsolationTags = JsonConvert.DeserializeObject<List<HecpIsolationTag>>(strResponse);
                if (hecpIsolationTags.Count > 0)
                {
                    List<int> existingIsolationTagHecpIds = hecpIsolationTags.Select(i => i.HecpId).Distinct().ToList();
                    request.HecpIds.RemoveAll(x => existingIsolationTagHecpIds.Contains(x));
                    jsonItem = JsonConvert.SerializeObject(request);
                }

                uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Hecp/" + $"GetDeactivationStepIsolations");
                if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                {
                    _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                }
                content = new StringContent(jsonItem, Encoding.UTF8, "application/json");
                res = await _clientService.GetClient().PostAsync(uri, content, Constants.LotoServiceAPIKey, APISystems.LotoAPI);

                if (res.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    strResponse = await res.Content.ReadAsStringAsync();
                    if (!String.IsNullOrEmpty(strResponse))
                    {
                        string location = string.Empty;

                        var isolationList = JsonConvert.DeserializeObject<List<HECPIsolation>>(strResponse);

                        if (isolationList != null && isolationList.Count > 0)
                        {
                            foreach (var iso in isolationList)
                            {
                                location = (!string.IsNullOrWhiteSpace(iso.CircuitDetails.Column) && !string.IsNullOrWhiteSpace(iso.CircuitDetails.Row)) ? iso.CircuitDetails.Row + "-" + iso.CircuitDetails.Column
                                            : (string.IsNullOrWhiteSpace(iso.CircuitDetails.Row) && !string.IsNullOrWhiteSpace(iso.CircuitDetails.Column)) ? iso.CircuitDetails.Column
                                            : (string.IsNullOrWhiteSpace(iso.CircuitDetails.Column) && !string.IsNullOrWhiteSpace(iso.CircuitDetails.Row)) ? iso.CircuitDetails.Row : string.Empty;
                                hecpIsolationTags.Add(
                                new HecpIsolationTag
                                {
                                    HecpIsolationId = iso.Id,
                                    CircuitId = iso.CircuitDetails.CircuitId,
                                    CircuitName = iso.CircuitDetails.CircuitNomenclature,
                                    CircuitPanel = iso.CircuitDetails.Panel,
                                    CircuitLocation = location,
                                    State = iso.State,
                                    HecpId = iso.HecpId,
                                    HecpIsolationAssociatedModelDataList = iso.HecpIsolationAssociatedModelDataList
                                });
                            }
                        }
                    }
                }
            }
            return hecpIsolationTags;
        }

        public virtual async Task<List<ViewModels.ConflictIsolationViewModel>> GetConflictIsolation(int lotoId, string program, List<int> hecpIds, string lineNumber)
        {
            List<ViewModels.ConflictIsolationViewModel> conflictIsolations = new List<ViewModels.ConflictIsolationViewModel>();
            try
            {
                string jsonItem = JsonConvert.SerializeObject(hecpIds);
                Uri uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Loto/" + $"ConflictIsolations/LotoId/{lotoId}/program/{program}/lineNumber/{lineNumber}");
                if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                {
                    _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                }
                StringContent content = new StringContent(jsonItem, Encoding.UTF8, "application/json");
                HttpResponseMessage res = await _clientService.GetClient().PostAsync(uri, content, Constants.LotoServiceAPIKey, APISystems.LotoAPI);

                if (res.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    List<HecpIsolationTag> hecpIsolations = new List<HecpIsolationTag>();
                    string resultString = await res.Content.ReadAsStringAsync();
                    hecpIsolations = JsonConvert.DeserializeObject<List<HecpIsolationTag>>(resultString);
                    foreach (var iso in hecpIsolations)
                    {
                        conflictIsolations.Add(
                        new ViewModels.ConflictIsolationViewModel
                        {
                            CircuitId = iso.CircuitId,
                            CircuitName = iso.CircuitName,
                            CircuitPanel = iso.CircuitPanel,
                            ConflictLotoName = iso.CircuitLocation,
                            ConflictLotoId = iso.LotoId,
                            State = iso.State
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error getting conflicted isolations " + ex);
                return new List<ViewModels.ConflictIsolationViewModel>();
            }
            return conflictIsolations;
        }

        public virtual async Task<HTTPResponseWrapper<bool>> IsHecpDeletable(int hecpId)
        {
            try
            {
                Uri uri = new Uri(EnvironmentHelper.LotoServiceAddress + $"Loto/IsHecpDeletable/HecpId/{hecpId}");
                HttpResponseMessage result = await _clientService.GetClient().GetAsync(uri);
                return JsonConvert.DeserializeObject<HTTPResponseWrapper<bool>>(await result.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return new HTTPResponseWrapper<bool>
                {
                    Data = false,
                    Message = ex.Message,
                    Status = Shield.Common.Constants.ShieldHttpWrapper.Status.FAILED
                };
            }
        }

        public virtual async Task<HTTPResponseWrapper<Models.LotoModels.Loto>> UpdateLotoJobInfo(Models.CommonModels.CreateLotoRequest lotoJobInfo)
        {
            try
            {
                string jsonItem = JsonConvert.SerializeObject(lotoJobInfo);

                var uri = new Uri(EnvironmentHelper.LotoServiceAddress + "Loto/UpdateJobInfo");

                StringContent content = new StringContent(jsonItem, Encoding.UTF8, "application/json");
                if (_clientService.GetClient().DefaultRequestHeaders != null && !_clientService.GetClient().DefaultRequestHeaders.Contains(Constants.LotoApiKeyHeaderName))
                {
                    _clientService.GetClient().DefaultRequestHeaders.Add(Constants.LotoApiKeyHeaderName, Constants.LotoServiceAPIKey);
                }
                var msg = await _clientService.GetClient().PutAsync(uri, content, Constants.LotoServiceAPIKey, APISystems.LotoAPI);

                HTTPResponseWrapper<Models.LotoModels.Loto> response = JsonConvert.DeserializeObject<HTTPResponseWrapper<Models.LotoModels.Loto>>(await msg.Content.ReadAsStringAsync());

                return response;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return null;
            }
        }
    }
}
